using System.Text.Json;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;

public sealed class FileTelegramManualFileBindingStore : ITelegramManualFileBindingStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly EquipmentDiagnosticTelegramOptions _options;
    private readonly object _sync = new();

    public FileTelegramManualFileBindingStore(EquipmentDiagnosticTelegramOptions options)
    {
        _options = options;
    }

    public Task<TelegramManualFileBinding?> GetAsync(
        string manualId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            var document = ReadDocument();
            return Task.FromResult(document.Bindings
                .FirstOrDefault(binding => binding.ManualId.Equals(manualId, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task UpsertAsync(
        TelegramManualFileBinding binding,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            var document = ReadDocument();
            var bindings = document.Bindings
                .Where(existing => !existing.ManualId.Equals(binding.ManualId, StringComparison.OrdinalIgnoreCase))
                .Append(binding)
                .OrderBy(value => value.ManualId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var updated = new TelegramManualFileBindingDocument(1, bindings);

            var path = _options.ManualLibrary.FileBindingsPath;
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, JsonSerializer.Serialize(updated, JsonOptions));
        }

        return Task.CompletedTask;
    }

    private TelegramManualFileBindingDocument ReadDocument()
    {
        var path = _options.ManualLibrary.FileBindingsPath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new TelegramManualFileBindingDocument(1, []);
        }

        try
        {
            return JsonSerializer.Deserialize<TelegramManualFileBindingDocument>(
                    File.ReadAllText(path),
                    JsonOptions) ??
                new TelegramManualFileBindingDocument(1, []);
        }
        catch (JsonException)
        {
            return new TelegramManualFileBindingDocument(1, []);
        }
    }

    private sealed record TelegramManualFileBindingDocument(
        int SchemaVersion,
        IReadOnlyList<TelegramManualFileBinding> Bindings);
}
