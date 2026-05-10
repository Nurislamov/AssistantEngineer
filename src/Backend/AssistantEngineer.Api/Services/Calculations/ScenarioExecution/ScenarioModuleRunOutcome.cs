namespace AssistantEngineer.Api.Services.Calculations;

public sealed record ScenarioModuleRunOutcome(
    string ModuleKind,
    string StepName,
    ScenarioModuleExecution Execution,
    double DurationMilliseconds);