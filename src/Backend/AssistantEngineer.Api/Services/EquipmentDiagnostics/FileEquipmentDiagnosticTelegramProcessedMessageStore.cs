using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

namespace AssistantEngineer.Api.Services.EquipmentDiagnostics;

public sealed class FileEquipmentDiagnosticTelegramProcessedMessageStore : IEquipmentDiagnosticTelegramProcessedMessageStore
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly string _filePath;
    private readonly int _maxEntries;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FileEquipmentDiagnosticTelegramProcessedMessageStore(
        IHostEnvironment environment,
        EquipmentDiagnosticTelegramWebhookOptions options)
    {
        var configuredPath = options.Polling.ProcessedMessageStoreFilePath;
        _filePath = Path.IsPathRooted(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredPath));
        _maxEntries = Math.Clamp(options.Polling.ProcessedMessageStoreMaxEntries, 1, 50_000);
    }

    public async Task<bool> TryMarkProcessedMessageAsync(
        long chatId,
        long messageId,
        long updateId,
        CancellationToken cancellationToken = default)
    {
        var hash = CreateMessageHash(chatId, messageId);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var entries = await ReadEntriesAsync(cancellationToken);
            if (entries.Any(entry => string.Equals(entry.Hash, hash, StringComparison.Ordinal)))
            {
                return false;
            }

            entries.Add(new ProcessedMessageEntry(hash, updateId));
            var trimmed = entries.Count <= _maxEntries
                ? entries
                : entries.Skip(entries.Count - _maxEntries).ToList();
            await WriteEntriesAsync(trimmed, cancellationToken);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<ProcessedMessageEntry>> ReadEntriesAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        var lines = await File.ReadAllLinesAsync(_filePath, Encoding.UTF8, cancellationToken);
        var entries = new List<ProcessedMessageEntry>(lines.Length);
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim().Trim('\uFEFF');
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var separator = line.IndexOf('|', StringComparison.Ordinal);
            if (separator <= 0 || separator == line.Length - 1)
            {
                continue;
            }

            var hash = line[..separator];
            var updateIdText = line[(separator + 1)..];
            if (!long.TryParse(updateIdText, NumberStyles.None, CultureInfo.InvariantCulture, out var updateId))
            {
                continue;
            }

            entries.Add(new ProcessedMessageEntry(hash, updateId));
        }

        return entries;
    }

    private async Task WriteEntriesAsync(
        IReadOnlyCollection<ProcessedMessageEntry> entries,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = $"{_filePath}.tmp";
        var content = string.Join(
            Environment.NewLine,
            entries.Select(entry => $"{entry.Hash}|{entry.LastUpdateId.ToString(CultureInfo.InvariantCulture)}"));
        if (content.Length > 0)
        {
            content += Environment.NewLine;
        }

        await File.WriteAllTextAsync(temporaryPath, content, Utf8NoBom, cancellationToken);
        File.Move(temporaryPath, _filePath, overwrite: true);
    }

    private static string CreateMessageHash(long chatId, long messageId)
    {
        var identity = string.Create(
            CultureInfo.InvariantCulture,
            $"{chatId}:{messageId}");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
    }

    private sealed record ProcessedMessageEntry(
        string Hash,
        long LastUpdateId);
}
