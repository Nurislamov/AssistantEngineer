namespace AssistantEngineer.Api.Services.Calculations;

public sealed class EngineeringCalculationJobWorkerOptions
{
    public const string SectionName = "EngineeringCalculationJobs:Worker";

    public bool Enabled { get; set; } = true;

    public int PollIntervalSeconds { get; set; } = 5;

    public int BatchSize { get; set; } = 3;
}