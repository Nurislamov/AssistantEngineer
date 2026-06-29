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
                .Where(binding => binding.IsActive)
                .FirstOrDefault(binding => binding.ManualId.Equals(manualId, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<TelegramManualFileBinding?> GetBySeriesAsync(
        string brand,
        string series,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            var document = ReadDocument();
            return Task.FromResult(document.Bindings
                .Where(binding => binding.IsActive)
                .FirstOrDefault(binding =>
                    string.Equals(binding.Brand, brand, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(binding.Series, series, StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task<IReadOnlyList<TelegramManualFileBinding>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            return Task.FromResult<IReadOnlyList<TelegramManualFileBinding>>(
                ReadDocument().Bindings
                    .Where(binding => binding.IsActive)
                    .OrderBy(binding => binding.ManualId, StringComparer.OrdinalIgnoreCase)
                    .ToArray());
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
            WriteDocument(updated);
        }

        return Task.CompletedTask;
    }

    public Task UpsertSeriesAsync(
        TelegramManualFileBinding binding,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            var document = ReadDocument();
            var bindings = document.Bindings
                .Where(existing =>
                    !string.Equals(existing.Brand, binding.Brand, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(existing.Series, binding.Series, StringComparison.OrdinalIgnoreCase))
                .Append(binding)
                .OrderBy(value => value.Brand, StringComparer.OrdinalIgnoreCase)
                .ThenBy(value => value.Series, StringComparer.OrdinalIgnoreCase)
                .ThenBy(value => value.ManualId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            WriteDocument(new TelegramManualFileBindingDocument(1, bindings));
        }

        return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(
        string manualId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            var document = ReadDocument();
            var bindings = document.Bindings
                .Where(existing => !existing.ManualId.Equals(manualId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(value => value.ManualId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (bindings.Length == document.Bindings.Count)
            {
                return Task.FromResult(false);
            }

            WriteDocument(new TelegramManualFileBindingDocument(1, bindings));
            return Task.FromResult(true);
        }
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

    private void WriteDocument(TelegramManualFileBindingDocument document)
    {
        var path = _options.ManualLibrary.FileBindingsPath;
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(document, JsonOptions));
    }

    private sealed record TelegramManualFileBindingDocument(
        int SchemaVersion,
        IReadOnlyList<TelegramManualFileBinding> Bindings);
}
