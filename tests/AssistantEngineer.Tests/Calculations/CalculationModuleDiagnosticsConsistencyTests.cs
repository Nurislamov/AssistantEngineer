using AssistantEngineer.Modules.Calculations.Application.Contracts.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.RoomLoads;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Calculations;

public class CalculationModuleDiagnosticsConsistencyTests
{
    [Fact]
    public void ValidRoomLoadResultDoesNotContainErrorDiagnostics()
    {
        var engine = new RoomLoadCalculationEngine();

        var result = engine.Calculate(new RoomLoadCalculationInput(
            RoomId: 1,
            RoomCode: "R-1",
            RoomName: "Valid room",
            AreaM2: 20,
            VolumeM3: 60,
            HeatingSetpointC: 21,
            CoolingSetpointC: 24,
            OutdoorDesignHeatingTemperatureC: -10,
            OutdoorDesignCoolingTemperatureC: 35,
            FixedComponents: new RoomLoadFixedComponentInput(
                HeatingTransmissionW: 1000,
                CoolingSolarW: 500)));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);
        Assert.DoesNotContain(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Error);
    }

    [Fact]
    public void InvalidRoomAreaRemainsVisibleAsErrorDiagnostic()
    {
        var engine = new RoomLoadCalculationEngine();

        var result = engine.Calculate(new RoomLoadCalculationInput(
            RoomId: 1,
            RoomCode: "R-1",
            RoomName: "Invalid room",
            AreaM2: 0,
            VolumeM3: 60,
            HeatingSetpointC: 21,
            CoolingSetpointC: 24,
            OutdoorDesignHeatingTemperatureC: -10,
            OutdoorDesignCoolingTemperatureC: 35,
            FixedComponents: new RoomLoadFixedComponentInput(
                HeatingTransmissionW: 1000)));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasErrors);

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Error &&
            diagnostic.Code == "RoomLoad.InvalidArea");
    }

    [Fact]
    public void InvalidAggregationRoomAreaFailsValidationWithDiagnosticCode()
    {
        var engine = new LoadAggregationEngine();

        var result = engine.Aggregate(new LoadAggregationInput(
            TargetId: 900,
            TargetType: LoadAggregationTargetType.Building,
            TargetName: "Invalid aggregation",
            Rooms:
            [
                new AggregationRoomLoadInput(
                    RoomId: 1,
                    RoomName: "Invalid room",
                    ThermalZoneId: 10,
                    FloorId: 20,
                    BuildingId: 900,
                    AreaM2: -1,
                    HeatingLoadW: 1000,
                    CoolingLoadW: 500)
            ]));

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Contains("Aggregation.InvalidRoomArea", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void HourlyAggregationFallbackIsVisibleAsWarning()
    {
        var engine = new LoadAggregationEngine();

        var result = engine.Aggregate(new LoadAggregationInput(
            TargetId: 900,
            TargetType: LoadAggregationTargetType.Building,
            TargetName: "Hourly fallback aggregation",
            Mode: LoadAggregationMode.Hourly,
            Rooms:
            [
                new AggregationRoomLoadInput(
                    RoomId: 1,
                    RoomName: "Room 1",
                    ThermalZoneId: 10,
                    FloorId: 20,
                    BuildingId: 900,
                    AreaM2: 20,
                    HeatingLoadW: 1000,
                    CoolingLoadW: 500)
            ]));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Warning &&
            diagnostic.Code == "Aggregation.HourlyUnavailable");
    }

    [Fact]
    public void SyntheticWeatherAndMonthlyAdapterStayVisibleAsWarnings()
    {
        var engine = new AnnualEnergyBalanceEngine();

        var result = engine.Calculate(new AnnualEnergyBalanceInput(
            BuildingId: 1,
            BuildingName: "Synthetic weather building",
            BuildingAreaM2: 100,
            Year: 2026,
            Hours:
            [
                new AnnualEnergyBalanceHourInput(
                    HourIndex: 0,
                    Month: 1,
                    HeatingLoadW: 1000,
                    CoolingLoadW: 0,
                    TransmissionW: 1000)
            ],
            UsesSyntheticWeather: true));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);
        Assert.False(result.Value.IsTrueHourly8760);
        Assert.Equal(1, result.Value.HourlyRecordCount);

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Warning &&
            diagnostic.Code == "AnnualEnergy.SyntheticWeather");

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Warning &&
            diagnostic.Code == "SolarWeather.SyntheticWeatherUsed");

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Warning &&
            diagnostic.Code == "AnnualEnergy.MonthlyBalanceAdapter");

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Warning &&
            diagnostic.Code == "AnnualEnergy.Not8760");
    }

    [Fact]
    public void PartialTrueHourlySimulationIsNotClaimedAsTrueHourly8760()
    {
        var engine = new AnnualEnergyBalanceEngine();

        var result = engine.Calculate(new AnnualEnergyBalanceInput(
            BuildingId: 1,
            BuildingName: "Partial hourly building",
            BuildingAreaM2: 100,
            Year: 2026,
            Hours:
            [
                new AnnualEnergyBalanceHourInput(
                    HourIndex: 0,
                    Month: 1,
                    HeatingLoadW: 1000,
                    CoolingLoadW: 0,
                    TransmissionW: 1000)
            ],
            EnergyDataSource: "TrueHourlySimulation",
            IsTrueHourly8760: true));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);
        Assert.Equal("TrueHourlySimulation", result.Value.EnergyDataSource);
        Assert.False(result.Value.IsTrueHourly8760);
        Assert.Equal(1, result.Value.HourlyRecordCount);

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Warning &&
            diagnostic.Code == "AnnualEnergy.TrueHourlySimulationPartial");
    }

    [Fact]
    public void InvalidAnnualEnergyMandatoryInputFailsValidationWithDiagnosticCode()
    {
        var engine = new AnnualEnergyBalanceEngine();

        var result = engine.Calculate(new AnnualEnergyBalanceInput(
            BuildingId: 1,
            BuildingName: "Invalid annual energy building",
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
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Contains("AnnualEnergy.InvalidArea", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void NegativeAnnualHourlyValuesAreClampedAndReportedAsWarnings()
    {
        var engine = new AnnualEnergyBalanceEngine();

        var result = engine.Calculate(new AnnualEnergyBalanceInput(
            BuildingId: 1,
            BuildingName: "Negative hourly building",
            BuildingAreaM2: 100,
            Year: 2026,
            Hours:
            [
                new AnnualEnergyBalanceHourInput(
                    HourIndex: 0,
                    Month: 1,
                    HeatingLoadW: -1000,
                    CoolingLoadW: 0,
                    TransmissionW: -500)
            ],
            EnergyDataSource: "DeterministicFixture"));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);
        Assert.Equal(0, result.Value.AnnualHeatingDemandKWh, precision: 6);

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Warning &&
            diagnostic.Code == "AnnualEnergy.NegativeHourlyValueClamped");
    }

    [Fact]
    public void DiagnosticsConsistencyDocumentationAndVerificationScriptExist()
    {
        Assert.True(File.Exists(DiagnosticsConsistencyDocumentPath));
        Assert.True(File.Exists(VerifyScriptPath));

        var document = File.ReadAllText(DiagnosticsConsistencyDocumentPath);

        Assert.Contains("Calculation Module Diagnostics Consistency", document, StringComparison.Ordinal);
        Assert.Contains("Invalid mandatory inputs must be visible", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Warning diagnostics", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AnnualEnergy.MonthlyBalanceAdapter", document, StringComparison.Ordinal);
        Assert.Contains("EnergyDataSource = TrueHourlySimulation", document, StringComparison.Ordinal);
        Assert.Contains("does not claim exact EnergyPlus numerical parity", document, StringComparison.OrdinalIgnoreCase);

        var wrapper = File.ReadAllText(VerifyScriptPath);
        Assert.Contains("AssistantEngineer.Tools.EngineeringCore.csproj", wrapper, StringComparison.Ordinal);
        Assert.Contains("verify-calculation-module-diagnostics-consistency", wrapper, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet test", wrapper, StringComparison.Ordinal);

        var tool = File.ReadAllText(ToolProgramPath);
        Assert.Contains("CalculationModuleDiagnosticsConsistencyTests", tool, StringComparison.Ordinal);
    }
private static string DiagnosticsConsistencyDocumentPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "CalculationModuleDiagnosticsConsistency.md");

    private static string VerifyScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "verify-calculation-module-diagnostics-consistency.ps1");
    private static string ToolProgramPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "tools",
            "AssistantEngineer.Tools.EngineeringCore",
            "Program.cs");
}

