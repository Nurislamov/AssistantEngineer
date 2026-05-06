using AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.BuildingInput;
using AssistantEngineer.Modules.Calculations.Application.Services.Validation.BuildingInput;

namespace AssistantEngineer.Tests.Calculations.Validation.BuildingInput;

public sealed class BuildingInputValidationReadinessTests
{
    private readonly BuildingInputValidationService _service = new();

    [Fact]
    public void Readiness_WhenNoErrorsAndWarningsPresent_IsReadyWithWarnings()
    {
        var result = _service.Validate(new BuildingInputValidationRequest(
            BuildingInputValidationScenarioBuilder.BuildValidSimpleRoom()));

        Assert.Equal(BuildingInputValidationReadinessStatus.ReadyWithWarnings, result.ReadinessStatus);
    }

    [Fact]
    public void Readiness_WhenErrorsPresent_IsBlockedByErrors()
    {
        var result = _service.Validate(new BuildingInputValidationRequest(
            BuildingInputValidationScenarioBuilder.BuildInvalidWallUValue()));

        Assert.Equal(BuildingInputValidationReadinessStatus.BlockedByErrors, result.ReadinessStatus);
        Assert.True(result.ErrorCount > 0);
    }

    [Fact]
    public void Readiness_WhenEvaluationSkippedAndNoErrors_IsNotEvaluated()
    {
        var result = _service.Validate(new BuildingInputValidationRequest(
            BuildingInputValidationScenarioBuilder.BuildValidSimpleRoom(),
            EvaluateIso52016Readiness: false));

        Assert.Equal(BuildingInputValidationReadinessStatus.NotEvaluated, result.ReadinessStatus);
    }
}
