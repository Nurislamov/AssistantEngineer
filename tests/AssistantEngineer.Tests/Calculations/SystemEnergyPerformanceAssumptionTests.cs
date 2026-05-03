using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations;

public class SystemEnergyPerformanceAssumptionTests
{
    [Fact]
    public void HeatingEfficiencyTakesPrecedenceOverHeatingCopWithWarningWhenBothAreSupplied()
    {
        var engine = new SystemEnergyEngine();

        var result = engine.Calculate(new SystemEnergyInput(
            UsefulHeatingEnergyKWh: 1_000,
            UsefulCoolingEnergyKWh: 0,
            UsefulDhwEnergyKWh: 0,
            HeatingEfficiency: 0.8,
            HeatingCop: 3.0,
            FanEnergyKWh: 100,
            PrimaryEnergyFactor: 2.0,
            DiagnosticsContext: "system-energy-dual-heating"));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(1_000, result.Value.UsefulHeatingKWh, precision: 6);
        Assert.Equal(1_250, result.Value.FinalHeatingEnergyKWh, precision: 6);
        Assert.Equal(100, result.Value.FinalFanEnergyKWh, precision: 6);
        Assert.Equal(1_350, result.Value.TotalFinalEnergyKWh, precision: 6);
        Assert.Equal(2_700, result.Value.PrimaryEnergyKWh!.Value, precision: 6);

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Warning &&
            diagnostic.Code == "SystemEnergy.HeatingDualPerformanceAssumption" &&
            diagnostic.Context == "system-energy-dual-heating");

        Assert.Contains(result.Value.AssumptionsUsed, assumption =>
            assumption.Contains("efficiency takes precedence", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DhwEfficiencyTakesPrecedenceOverDhwCopWithWarningWhenBothAreSupplied()
    {
        var engine = new SystemEnergyEngine();

        var result = engine.Calculate(new SystemEnergyInput(
            UsefulDhwEnergyKWh: 600,
            DhwEfficiency: 0.75,
            DhwCop: 2.5,
            PrimaryEnergyFactor: 1.5,
            DiagnosticsContext: "system-energy-dual-dhw"));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(600, result.Value.UsefulDhwKWh, precision: 6);
        Assert.Equal(800, result.Value.FinalDhwEnergyKWh, precision: 6);
        Assert.Equal(800, result.Value.TotalFinalEnergyKWh, precision: 6);
        Assert.Equal(1_200, result.Value.PrimaryEnergyKWh!.Value, precision: 6);

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Warning &&
            diagnostic.Code == "SystemEnergy.DhwDualPerformanceAssumption" &&
            diagnostic.Context == "system-energy-dual-dhw");
    }

    [Fact]
    public void DualPerformanceAssumptionWarningIsNotEmittedWhenUsefulEnergyIsZero()
    {
        var engine = new SystemEnergyEngine();

        var result = engine.Calculate(new SystemEnergyInput(
            UsefulHeatingEnergyKWh: 0,
            UsefulDhwEnergyKWh: 0,
            HeatingEfficiency: 0.8,
            HeatingCop: 3.0,
            DhwEfficiency: 0.75,
            DhwCop: 2.5,
            PrimaryEnergyFactor: 2.0));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);
        Assert.Equal(0, result.Value.TotalFinalEnergyKWh, precision: 6);
        Assert.Equal(0, result.Value.PrimaryEnergyKWh!.Value, precision: 6);

        Assert.DoesNotContain(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "SystemEnergy.HeatingDualPerformanceAssumption" ||
            diagnostic.Code == "SystemEnergy.DhwDualPerformanceAssumption");
    }

    [Fact]
    public void MissingPerformanceAssumptionStillCarriesUsefulEnergyAsFinalEnergyWithWarning()
    {
        var engine = new SystemEnergyEngine();

        var result = engine.Calculate(new SystemEnergyInput(
            UsefulHeatingEnergyKWh: 500,
            UsefulCoolingEnergyKWh: 300,
            UsefulDhwEnergyKWh: 200));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(500, result.Value.FinalHeatingEnergyKWh, precision: 6);
        Assert.Equal(300, result.Value.FinalCoolingEnergyKWh, precision: 6);
        Assert.Equal(200, result.Value.FinalDhwEnergyKWh, precision: 6);
        Assert.Equal(1_000, result.Value.TotalFinalEnergyKWh, precision: 6);
        Assert.Null(result.Value.PrimaryEnergyKWh);

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "SystemEnergy.HeatingAssumptionMissing");

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "SystemEnergy.CoolingAssumptionMissing");

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Code == "SystemEnergy.DhwAssumptionMissing");
    }
}
