using System.Globalization;
using System.Text;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

namespace AssistantEngineer.Api.Services.EquipmentDiagnostics;

public sealed class FileEquipmentDiagnosticTelegramUpdateOffsetStore : IEquipmentDiagnosticTelegramUpdateOffsetStore
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FileEquipmentDiagnosticTelegramUpdateOffsetStore(
        IHostEnvironment environment,
        EquipmentDiagnosticTelegramWebhookOptions options)
    {
        var configuredPath = options.Polling.OffsetStoreFilePath;
        _filePath = Path.IsPathRooted(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredPath));
    }

    public async Task<long?> GetLastProcessedUpdateIdAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_filePath))
            {
                return null;
            }

            var value = (await File.ReadAllTextAsync(_filePath, Encoding.UTF8, cancellationToken))
                .Trim()
                .Trim('\uFEFF');
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var updateId))
            {
                throw new InvalidOperationException("Telegram polling offset store contains an invalid value.");
            }

            return updateId;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveLastProcessedUpdateIdAsync(
        long updateId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var temporaryPath = $"{_filePath}.tmp";
            await File.WriteAllTextAsync(
                temporaryPath,
                updateId.ToString(CultureInfo.InvariantCulture),
                Utf8NoBom,
                cancellationToken);
            File.Move(temporaryPath, _filePath, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }
    }
}
