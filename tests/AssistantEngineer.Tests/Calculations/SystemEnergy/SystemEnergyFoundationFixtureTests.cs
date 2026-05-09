using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyFoundationFixtureTests
{
    private readonly SystemEnergyFoundationCalculator _calculator = new(
        new SystemEnergyEmissionCalculator(),
        new SystemEnergyDistributionCalculator(),
        new SystemEnergyStorageCalculator(),
        new SystemEnergyGenerationCalculator());

    [Fact]
    public void Fixtures_AreDeterministicAndMeetExpectedAnchors()
    {
        var fixtures = SystemEnergyFoundationFixtureLoader.LoadAll();
        foreach (var fixture in fixtures)
        {
            var result = _calculator.Calculate(new(
                CalculationId: fixture.Id,
                LoadInputs: fixture.LoadInputs,
                StageDefinitions: fixture.StageDefinitions,
                GeneratorDefinitions: fixture.GeneratorDefinitions,
                FactorCatalog: fixture.FactorCatalog,
                TimeStepHours: 1.0,
                OutputResolution: AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.SystemEnergyProfileShape.Hourly8760,
                OwnershipPolicy: fixture.OwnershipPolicy,
                StrictFactorMode: fixture.StrictFactorMode));

            if (fixture.Expected.AnnualFinalEnergyKWh is { } final)
            {
                Assert.InRange(result.AnnualSummary.FinalEnergyKWh, final - 1e-6, final + 1e-6);
            }

            if (fixture.Expected.AnnualPrimaryEnergyKWh is { } primary)
            {
                Assert.InRange(result.AnnualSummary.PrimaryEnergyKWh, primary - 1e-6, primary + 1e-6);
            }

            if (fixture.Expected.AnnualCo2Kg is { } co2)
            {
                Assert.InRange(result.AnnualSummary.Co2Kg, co2 - 1e-6, co2 + 1e-6);
            }

            if (fixture.ExpectedDiagnosticCodes is { Count: > 0 } codes)
            {
                foreach (var code in codes)
                {
                    Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == code);
                }
            }
        }
    }
}
