namespace AssistantEngineer.Api.Services.Calculations;

public sealed class EngineeringCalculationJobWorkerOptions
{
    public const string SectionName = "EngineeringCalculationJobs:Worker";

    public bool Enabled { get; set; } = true;

    public int PollIntervalSeconds { get; set; } = 5;

    public int BatchSize { get; set; } = 3;

    public int LeaseDurationSeconds { get; set; } = 300;

    public string? WorkerId { get; set; }

    public bool StaleRunningJobRecoveryEnabled { get; set; } = false;
}
