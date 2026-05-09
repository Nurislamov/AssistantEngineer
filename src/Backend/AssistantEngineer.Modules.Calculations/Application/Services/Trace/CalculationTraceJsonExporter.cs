using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Trace;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Trace;

public sealed class CalculationTraceJsonExporter : ICalculationTraceJsonExporter
{
    private static readonly JsonSerializerOptions CompactOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public string Export(
        CalculationTraceDocument trace,
        bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(trace);
        return JsonSerializer.Serialize(trace, indented ? IndentedOptions : CompactOptions);
    }
}
