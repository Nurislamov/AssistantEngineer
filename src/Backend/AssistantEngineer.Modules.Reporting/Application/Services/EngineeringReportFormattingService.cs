namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class EngineeringReportFormattingService
{
    public string FormatFixed2(double value) => value.ToString("F2");

    public string FormatNullableFixed2(double? value, string nullValue = "n/a") =>
        value.HasValue ? value.Value.ToString("F2") : nullValue;

    public string FormatIsoUtc(DateTimeOffset value) => value.ToString("O");
}
