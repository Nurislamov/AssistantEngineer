using AssistantEngineer.Modules.Calculations.Application.Services.Validation.BuildingInput;

namespace AssistantEngineer.Tests.Calculations.Validation.BuildingInput;

public sealed class BuildingInputValidationFixtureTests
{
    private readonly BuildingInputValidationService _service = new();

    [Fact]
    public void Fixtures_ParseAndExpectedDiagnosticsArePresent()
    {
        var fixtures = BuildingInputValidationFixtureLoader.LoadAll();
        Assert.NotEmpty(fixtures);

        foreach (var fixture in fixtures)
        {
            var request = BuildingInputValidationScenarioBuilder.BuildRequest(fixture.Scenario);
            var result = _service.Validate(request);

            Assert.Equal(fixture.ExpectedReadinessStatus, result.ReadinessStatus);
            foreach (var expectedCode in fixture.ExpectedDiagnosticCodes)
            {
                Assert.Contains(result.Diagnostics, item => item.Code == expectedCode);
            }

            foreach (var expectedCorrectionId in fixture.ExpectedCorrectionIds)
            {
                Assert.Contains(result.SuggestedCorrections, item => item.CorrectionId == expectedCorrectionId);
            }
        }
    }
}
