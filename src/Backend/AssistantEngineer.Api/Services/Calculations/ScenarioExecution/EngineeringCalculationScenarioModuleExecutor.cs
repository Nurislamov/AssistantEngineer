using AssistantEngineer.Api.Contracts.Calculations;
using System.Diagnostics;

namespace AssistantEngineer.Api.Services.Calculations;

public sealed class EngineeringCalculationScenarioModuleExecutor : IEngineeringCalculationScenarioModuleExecutor
{
    public ScenarioModuleRunOutcome Execute(
        string moduleKind,
        string stepName,
        Func<ScenarioModuleExecution> execute)
    {
        ArgumentNullException.ThrowIfNull(execute);

        var stopwatch = Stopwatch.StartNew();
        var outcome = execute();
        stopwatch.Stop();

        return new ScenarioModuleRunOutcome(moduleKind, stepName, outcome, stopwatch.Elapsed.TotalMilliseconds);
    }

    public async Task<ScenarioModuleRunOutcome> ExecuteAsync(
        string moduleKind,
        string stepName,
        Func<Task<ScenarioModuleExecution>> execute)
    {
        ArgumentNullException.ThrowIfNull(execute);

        var stopwatch = Stopwatch.StartNew();
        var outcome = await execute();
        stopwatch.Stop();

        return new ScenarioModuleRunOutcome(moduleKind, stepName, outcome, stopwatch.Elapsed.TotalMilliseconds);
    }

    public void AddModuleOutcome(
        ScenarioModuleRunOutcome outcome,
        ICollection<EngineeringCalculationModuleExecutionResultDto> moduleResults,
        ICollection<EngineeringCalculationModuleTimingDto> timings,
        ICollection<string> executedModules,
        ICollection<string> skippedModules,
        ICollection<string> unavailableModules)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentNullException.ThrowIfNull(moduleResults);
        ArgumentNullException.ThrowIfNull(timings);
        ArgumentNullException.ThrowIfNull(executedModules);
        ArgumentNullException.ThrowIfNull(skippedModules);
        ArgumentNullException.ThrowIfNull(unavailableModules);

        var status = outcome.Execution.Status;
        if (status == EngineeringCalculationModuleExecutionStatus.Executed)
        {
            executedModules.Add(outcome.ModuleKind);
        }
        else if (status == EngineeringCalculationModuleExecutionStatus.Skipped)
        {
            skippedModules.Add(outcome.ModuleKind);
        }
        else
        {
            unavailableModules.Add(outcome.ModuleKind);
        }

        timings.Add(new EngineeringCalculationModuleTimingDto(outcome.ModuleKind, Round(outcome.DurationMilliseconds)));
        moduleResults.Add(new EngineeringCalculationModuleExecutionResultDto(
            ModuleKind: outcome.ModuleKind,
            Status: status,
            SummaryValues: outcome.Execution.Values,
            Diagnostics: outcome.Execution.Diagnostics,
            Assumptions: outcome.Execution.Assumptions,
            Warnings: outcome.Execution.Warnings,
            DurationMilliseconds: Round(outcome.DurationMilliseconds),
            SourceServiceName: outcome.Execution.SourceServiceName));
    }

    private static double Round(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);
}