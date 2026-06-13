namespace AssistantEngineer.Api.Services.OperationalDiagnostics;

public sealed class OperationalCorrelationOptions
{
    public const string SectionName = "OperationalCorrelation";
    public const string DefaultHeaderName = "X-Correlation-ID";

    public bool Enabled { get; set; } = true;
    public string HeaderName { get; set; } = DefaultHeaderName;
    public int MaxLength { get; set; } = 128;
}
