using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations;

public interface IEngineeringCalculationScenarioModuleExecutor
{
    ScenarioModuleRunOutcome Execute(
        string moduleKind,
        string stepName,
        Func<ScenarioModuleExecution> execute);

    Task<ScenarioModuleRunOutcome> ExecuteAsync(
        string moduleKind,
        string stepName,
        Func<Task<ScenarioModuleExecution>> execute);

    void AddModuleOutcome(
        ScenarioModuleRunOutcome outcome,
        ICollection<EngineeringCalculationModuleExecutionResultDto> moduleResults,
        ICollection<EngineeringCalculationModuleTimingDto> timings,
        ICollection<string> executedModules,
        ICollection<string> skippedModules,
        ICollection<string> unavailableModules);
}