using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Api.Services.Calculations;

namespace AssistantEngineer.Tests.Calculations;

public class EngineeringCalculationScenarioModuleExecutorTests
{
    [Fact]
    public void ExecuteRecordsExecutedModuleAndAddsLedgerEntries()
    {
        var executor = new EngineeringCalculationScenarioModuleExecutor();

        var outcome = executor.Execute(
            "WeatherSolar",
            "Weather and solar readiness",
            () => ScenarioModuleExecution.Execute(
                [new EngineeringCalculationModuleValueDto("weather_status", "Weather source status", "Available")],
                "UnitTestSource"));

        var moduleResults = new List<EngineeringCalculationModuleExecutionResultDto>();
        var timings = new List<EngineeringCalculationModuleTimingDto>();
        var executed = new List<string>();
        var skipped = new List<string>();
        var unavailable = new List<string>();

        executor.AddModuleOutcome(outcome, moduleResults, timings, executed, skipped, unavailable);

        Assert.Equal("WeatherSolar", outcome.ModuleKind);
        Assert.True(outcome.DurationMilliseconds >= 0.0);
        Assert.Equal(["WeatherSolar"], executed);
        Assert.Empty(skipped);
        Assert.Empty(unavailable);

        var result = Assert.Single(moduleResults);
        Assert.Equal(EngineeringCalculationModuleExecutionStatus.Executed, result.Status);
        Assert.Equal("UnitTestSource", result.SourceServiceName);
        Assert.Single(result.SummaryValues);

        var timing = Assert.Single(timings);
        Assert.Equal("WeatherSolar", timing.ModuleKind);
        Assert.True(timing.DurationMilliseconds >= 0.0);
    }

    [Fact]
    public void SkipCreatesWarningDiagnosticAndSkippedLedgerEntry()
    {
        var executor = new EngineeringCalculationScenarioModuleExecutor();

        var outcome = executor.Execute(
            "Ventilation",
            "Natural ventilation execution",
            () => ScenarioModuleExecution.Skip(
                "No natural ventilation openings are configured.",
                "Configure natural ventilation openings to execute this module."));

        var moduleResults = new List<EngineeringCalculationModuleExecutionResultDto>();
        var timings = new List<EngineeringCalculationModuleTimingDto>();
        var executed = new List<string>();
        var skipped = new List<string>();
        var unavailable = new List<string>();

        executor.AddModuleOutcome(outcome, moduleResults, timings, executed, skipped, unavailable);

        Assert.Empty(executed);
        Assert.Equal(["Ventilation"], skipped);
        Assert.Empty(unavailable);

        var result = Assert.Single(moduleResults);
        Assert.Equal(EngineeringCalculationModuleExecutionStatus.Skipped, result.Status);
        Assert.Equal("EngineeringCalculationScenarioRunner", result.SourceServiceName);
        Assert.Equal("No natural ventilation openings are configured.", Assert.Single(result.Warnings));

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("warning", diagnostic.Severity);
        Assert.Equal("SCENARIO_MODULE_SKIPPED", diagnostic.Code);
    }

    [Fact]
    public void FailCreatesErrorDiagnosticAndUnavailableLedgerEntry()
    {
        var executor = new EngineeringCalculationScenarioModuleExecutor();

        var outcome = executor.Execute(
            "HeatingCooling",
            "ISO52016/MultiZone heating-cooling load",
            () => ScenarioModuleExecution.Fail(
                "Heating/cooling module failed.",
                "Provide a valid building model."));

        var moduleResults = new List<EngineeringCalculationModuleExecutionResultDto>();
        var timings = new List<EngineeringCalculationModuleTimingDto>();
        var executed = new List<string>();
        var skipped = new List<string>();
        var unavailable = new List<string>();

        executor.AddModuleOutcome(outcome, moduleResults, timings, executed, skipped, unavailable);

        Assert.Empty(executed);
        Assert.Empty(skipped);
        Assert.Equal(["HeatingCooling"], unavailable);

        var result = Assert.Single(moduleResults);
        Assert.Equal(EngineeringCalculationModuleExecutionStatus.Failed, result.Status);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("error", diagnostic.Severity);
        Assert.Equal("SCENARIO_MODULE_FAILED", diagnostic.Code);
    }

    [Fact]
    public async Task ExecuteAsyncRecordsAsyncExecution()
    {
        var executor = new EngineeringCalculationScenarioModuleExecutor();

        var outcome = await executor.ExecuteAsync(
            "DomesticHotWater",
            "Domestic hot water execution",
            () => Task.FromResult(ScenarioModuleExecution.Execute(
                [new EngineeringCalculationModuleValueDto("dhw_annual_useful_kwh", "Annual useful DHW demand", 1200.0, "kWh")],
                "AsyncUnitTestSource")));

        Assert.Equal("DomesticHotWater", outcome.ModuleKind);
        Assert.Equal("Domestic hot water execution", outcome.StepName);
        Assert.Equal(EngineeringCalculationModuleExecutionStatus.Executed, outcome.Execution.Status);
        Assert.Equal("AsyncUnitTestSource", outcome.Execution.SourceServiceName);
    }
}