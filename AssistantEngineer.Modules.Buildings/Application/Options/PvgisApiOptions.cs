namespace AssistantEngineer.Modules.Buildings.Application.Options;

public sealed class PvgisApiOptions
{
    public const string SectionName = "Buildings:Pvgis";

    public string BaseUrl { get; set; } = "https://re.jrc.ec.europa.eu/api/";
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxRetryAttempts { get; set; } = 3;
    public int InitialRetryDelayMilliseconds { get; set; } = 500;
    public int CircuitBreakerFailureThreshold { get; set; } = 5;
    public int CircuitBreakerBreakDurationSeconds { get; set; } = 30;
}
