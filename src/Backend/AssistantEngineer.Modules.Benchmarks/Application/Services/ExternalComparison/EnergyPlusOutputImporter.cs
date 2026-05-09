using System.Text.Json;
using AssistantEngineer.Modules.Benchmarks.Application.Contracts.ExternalComparison;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Benchmarks.Application.Services.ExternalComparison;

internal sealed class EnergyPlusOutputImporter
{
    public Result<ExternalComparisonExpectedOutput?> Import(string? outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            return Result<ExternalComparisonExpectedOutput?>.Success(null);

        if (!File.Exists(outputPath))
            return Result<ExternalComparisonExpectedOutput?>.Success(null);

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(outputPath));
            var root = document.RootElement;

            var metrics = new Dictionary<string, double>(StringComparer.Ordinal);
            if (root.TryGetProperty("metrics", out var metricsNode) && metricsNode.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in metricsNode.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetDouble(out var value))
                        metrics[property.Name] = value;
                }
            }

            var format = root.TryGetProperty("format", out var formatNode) && formatNode.ValueKind == JsonValueKind.String
                ? formatNode.GetString() ?? "json"
                : "json";

            return Result<ExternalComparisonExpectedOutput?>.Success(new ExternalComparisonExpectedOutput
            {
                OutputPath = outputPath,
                Format = format,
                Metrics = metrics
            });
        }
        catch (JsonException exception)
        {
            return Result<ExternalComparisonExpectedOutput?>.Failure(
                $"External comparison output file could not be parsed as JSON: {exception.Message}");
        }
    }
}
