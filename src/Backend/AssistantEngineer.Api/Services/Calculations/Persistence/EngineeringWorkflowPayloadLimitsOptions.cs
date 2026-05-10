namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public sealed class EngineeringWorkflowPayloadLimitsOptions
{
    public bool Enabled { get; set; } = true;

    public int RequestJsonMaxBytes { get; set; } = 262_144;

    public int StateJsonMaxBytes { get; set; } = 524_288;

    public int ResultSummaryJsonMaxBytes { get; set; } = 524_288;

    public int DiagnosticsJsonMaxBytes { get; set; } = 262_144;

    public int ArtifactContentMaxBytes { get; set; } = 2_097_152;

    public string TruncationMarker { get; set; } = "[TRUNCATED_BY_ASSISTANT_ENGINEER_PAYLOAD_LIMIT]";
}
