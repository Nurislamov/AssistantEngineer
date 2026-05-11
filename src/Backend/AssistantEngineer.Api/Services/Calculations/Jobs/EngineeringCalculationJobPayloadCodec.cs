using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations;

internal sealed class EngineeringCalculationJobPayloadCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, JsonOptions);

    public EngineeringCalculationJobRequestDto? DeserializeJobRequest(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<EngineeringCalculationJobRequestDto>(raw, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public EngineeringCalculationScenarioResultDto? DeserializeScenarioResult(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<EngineeringCalculationScenarioResultDto>(raw, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public IReadOnlyList<EngineeringWorkflowDiagnosticDto> DeserializeDiagnostics(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<EngineeringWorkflowDiagnosticDto>>(raw, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public IReadOnlyList<EngineeringWorkflowDiagnosticDto> SortAndDistinctDiagnostics(
        IEnumerable<EngineeringWorkflowDiagnosticDto> diagnostics)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        return diagnostics
            .OrderByDescending(item => SeverityRank(item.Severity))
            .ThenBy(item => item.SourceStep, StringComparer.Ordinal)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.Message, StringComparer.Ordinal)
            .Where(item => seen.Add($"{item.SourceStep}|{item.Code}|{item.Message}|{item.TargetField}"))
            .ToArray();
    }

    public IReadOnlyList<string> SortAndDistinctText(
        IEnumerable<string> values)
    {
        return values
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();
    }

    private static int SeverityRank(string severity)
    {
        if (severity.Equals("error", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (severity.Equals("assumption", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return 1;
    }
}
