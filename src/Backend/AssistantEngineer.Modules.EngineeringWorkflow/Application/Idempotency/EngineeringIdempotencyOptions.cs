namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Idempotency;

public sealed class EngineeringIdempotencyOptions
{
    public const string SectionName = "EngineeringIdempotency";

    public bool Enabled { get; set; } = true;

    public int TtlMinutes { get; set; } = 1440;

    public int MaxEntries { get; set; } = 1000;

    public int MaxCachedResponseBytes { get; set; } = 262144;
}
