using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class EngineeringReportJsonExporter : IEngineeringReportJsonExporter
{
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly JsonSerializerOptions CompactOptions = new()
    {
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    public string Export(
        EngineeringReportDocument report,
        bool indented = true)
    {
        ArgumentNullException.ThrowIfNull(report);
        return JsonSerializer.Serialize(report, indented ? IndentedOptions : CompactOptions);
    }
}
