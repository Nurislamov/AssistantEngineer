using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy;

namespace AssistantEngineer.Tests.Calculations;

public class AnnualEnergyMonthlyCoverageDiagnosticsTests
{
    [Fact]
    public void AnnualEnergyWarnsWhenHourlyInputsDoNotCoverAllCalendarMonths()
    {
        var engine = new AnnualEnergyBalanceEngine();

        var result = engine.Calculate(new AnnualEnergyBalanceInput(
            BuildingId: 501,
            BuildingName: "Partial annual coverage building",
            BuildingAreaM2: 100,
            Year: 2026,
            Hours:
            [
                new AnnualEnergyBalanceHourInput(
                    HourIndex: 0,
                    Month: 1,
                    HeatingLoadW: 1000,
                    CoolingLoadW: 0),

                new AnnualEnergyBalanceHourInput(
                    HourIndex: 1440,
                    Month: 3,
                    HeatingLoadW: 0,
                    CoolingLoadW: 500)
            ],
            EnergyDataSource: "DeterministicFixture",
            DiagnosticsContext: "annual-monthly-coverage-partial"));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(1, result.Value.MonthlyResults.Single(month => month.Month == 1).HeatingKWh, precision: 6);
        Assert.Equal(0.5, result.Value.MonthlyResults.Single(month => month.Month == 3).CoolingKWh, precision: 6);
        Assert.Equal(1.5, result.Value.AnnualTotalDemandKWh, precision: 6);

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Warning &&
            diagnostic.Code == "AnnualEnergy.MonthlyCoverageIncomplete" &&
            diagnostic.Context == "annual-monthly-coverage-partial" &&
            diagnostic.Message.Contains("2", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("12", StringComparison.Ordinal));

        Assert.DoesNotContain(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.MonthlyCoverageComplete");
    }

    [Fact]
    public void AnnualEnergyReportsCompleteMonthlyCoverageWhenAllMonthsArePresent()
    {
        var engine = new AnnualEnergyBalanceEngine();

        var hours = Enumerable
            .Range(1, 12)
            .Select(month => new AnnualEnergyBalanceHourInput(
                HourIndex: (month - 1) * 730,
                Month: month,
                HeatingLoadW: month * 100,
                CoolingLoadW: month * 50))
            .ToArray();

        var result = engine.Calculate(new AnnualEnergyBalanceInput(
            BuildingId: 502,
            BuildingName: "Complete annual coverage building",
            BuildingAreaM2: 120,
            Year: 2026,
            Hours: hours,
            EnergyDataSource: "DeterministicFixture",
            DiagnosticsContext: "annual-monthly-coverage-complete"));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(12, result.Value.MonthlyResults.Count);
        Assert.Equal(7.8, result.Value.AnnualHeatingDemandKWh, precision: 6);
        Assert.Equal(3.9, result.Value.AnnualCoolingDemandKWh, precision: 6);

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Info &&
            diagnostic.Code == "AnnualEnergy.MonthlyCoverageComplete" &&
            diagnostic.Context == "annual-monthly-coverage-complete");

        Assert.DoesNotContain(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "AnnualEnergy.MonthlyCoverageIncomplete");
    }

    [Fact]
    public void AnnualEnergyDoesNotEmitCoverageDiagnosticWhenValidationFailsBeforeCoverageCheck()
    {
        var engine = new AnnualEnergyBalanceEngine();

        var result = engine.Calculate(new AnnualEnergyBalanceInput(
            BuildingId: 503,
            BuildingName: "Invalid annual coverage building",
            BuildingAreaM2: 0,
            Year: 2026,
            Hours:
            [
                new AnnualEnergyBalanceHourInput(
                    HourIndex: 0,
                    Month: 1,
                    HeatingLoadW: 1000,
                    CoolingLoadW: 0)
            ]));

        Assert.True(result.IsFailure);
        Assert.Contains("AnnualEnergy.InvalidArea", result.Error);
    }
}
