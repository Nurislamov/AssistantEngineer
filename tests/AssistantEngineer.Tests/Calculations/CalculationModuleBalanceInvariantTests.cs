using AssistantEngineer.Modules.Calculations.Application.Contracts.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Services.RoomLoads;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Calculations;

public class CalculationModuleBalanceInvariantTests
{
    [Fact]
    public void RoomHeatingLoadEqualsHeatingBreakdownTotalAndAreaIntensity()
    {
        var engine = new RoomLoadCalculationEngine();

        var result = engine.Calculate(new RoomLoadCalculationInput(
            RoomId: 101,
            RoomCode: "R-101",
            RoomName: "Heating invariant room",
            AreaM2: 20,
            VolumeM3: 60,
            HeatingSetpointC: 21,
            CoolingSetpointC: 24,
            OutdoorDesignHeatingTemperatureC: -10,
            OutdoorDesignCoolingTemperatureC: 35,
            FixedComponents: new RoomLoadFixedComponentInput(
                HeatingTransmissionW: 1000,
                HeatingWindowTransmissionW: 200,
                HeatingGroundW: 150,
                HeatingVentilationW: 300,
                HeatingInfiltrationW: 50)));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(1700, result.Value.HeatingBreakdown.TotalW, precision: 6);
        Assert.Equal(result.Value.HeatingBreakdown.TotalW, result.Value.HeatingLoadW, precision: 6);
        Assert.Equal(85, result.Value.HeatingLoadWPerM2, precision: 6);
        Assert.Equal("transmission", result.Value.DominantHeatingComponent);
    }

    [Fact]
    public void RoomCoolingLoadEqualsCoolingBreakdownTotalAndAreaIntensity()
    {
        var engine = new RoomLoadCalculationEngine();

        var result = engine.Calculate(new RoomLoadCalculationInput(
            RoomId: 102,
            RoomCode: "R-102",
            RoomName: "Cooling invariant room",
            AreaM2: 25,
            VolumeM3: 75,
            HeatingSetpointC: 21,
            CoolingSetpointC: 24,
            OutdoorDesignHeatingTemperatureC: -10,
            OutdoorDesignCoolingTemperatureC: 35,
            FixedComponents: new RoomLoadFixedComponentInput(
                CoolingTransmissionW: 300,
                CoolingWindowTransmissionW: 100,
                CoolingGroundW: 50,
                CoolingVentilationW: 200,
                CoolingInfiltrationW: 75,
                CoolingSolarW: 500,
                CoolingInternalGainsW: 325)));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(1550, result.Value.CoolingBreakdown.TotalW, precision: 6);
        Assert.Equal(result.Value.CoolingBreakdown.TotalW, result.Value.CoolingLoadW, precision: 6);
        Assert.Equal(62, result.Value.CoolingLoadWPerM2, precision: 6);
        Assert.Equal("solar", result.Value.DominantCoolingComponent);
    }

    [Fact]
    public void NegativeFixedComponentsAreClampedAndReportedWithoutNegativeTotals()
    {
        var engine = new RoomLoadCalculationEngine();

        var result = engine.Calculate(new RoomLoadCalculationInput(
            RoomId: 103,
            RoomCode: "R-103",
            RoomName: "Negative fixed component room",
            AreaM2: 10,
            VolumeM3: 30,
            HeatingSetpointC: 21,
            CoolingSetpointC: 24,
            OutdoorDesignHeatingTemperatureC: -10,
            OutdoorDesignCoolingTemperatureC: 35,
            FixedComponents: new RoomLoadFixedComponentInput(
                HeatingTransmissionW: -100,
                CoolingSolarW: -50)));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.HeatingLoadW, precision: 6);
        Assert.Equal(0, result.Value.CoolingLoadW, precision: 6);

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Warning &&
            diagnostic.Code == "RoomLoad.NegativeFixedComponentClamped");
    }

    [Fact]
    public void DesignPointAggregationEqualsSumOfSelectedRoomLoadsAndAreas()
    {
        var engine = new LoadAggregationEngine();

        var result = engine.Aggregate(new LoadAggregationInput(
            TargetId: 900,
            TargetType: LoadAggregationTargetType.Building,
            TargetName: "Building invariant",
            Rooms:
            [
                new AggregationRoomLoadInput(
                    RoomId: 1,
                    RoomName: "Room 1",
                    ThermalZoneId: 10,
                    FloorId: 20,
                    BuildingId: 900,
                    AreaM2: 20,
                    HeatingLoadW: 1700,
                    CoolingLoadW: 1550,
                    HeatingBreakdown: new RoomHeatingLoadBreakdown(
                        TransmissionW: 1000,
                        WindowTransmissionW: 200,
                        GroundW: 150,
                        VentilationW: 300,
                        InfiltrationW: 50,
                        UsefulSolarGainOffsetW: 0,
                        UsefulInternalGainOffsetW: 0),
                    CoolingBreakdown: new RoomCoolingLoadBreakdown(
                        TransmissionW: 300,
                        WindowTransmissionW: 100,
                        SolarW: 500,
                        VentilationW: 200,
                        InfiltrationW: 75,
                        InternalGainsW: 325,
                        GroundW: 50)),

                new AggregationRoomLoadInput(
                    RoomId: 2,
                    RoomName: "Room 2",
                    ThermalZoneId: 10,
                    FloorId: 20,
                    BuildingId: 900,
                    AreaM2: 30,
                    HeatingLoadW: 900,
                    CoolingLoadW: 600,
                    HeatingBreakdown: new RoomHeatingLoadBreakdown(
                        TransmissionW: 600,
                        WindowTransmissionW: 0,
                        GroundW: 100,
                        VentilationW: 150,
                        InfiltrationW: 50,
                        UsefulSolarGainOffsetW: 0,
                        UsefulInternalGainOffsetW: 0),
                    CoolingBreakdown: new RoomCoolingLoadBreakdown(
                        TransmissionW: 200,
                        WindowTransmissionW: 0,
                        SolarW: 100,
                        VentilationW: 100,
                        InfiltrationW: 50,
                        InternalGainsW: 150,
                        GroundW: 0))
            ]));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);

        Assert.Equal(2, result.Value.RoomCount);
        Assert.Equal(50, result.Value.TotalAreaM2, precision: 6);
        Assert.Equal(2600, result.Value.HeatingLoadW, precision: 6);
        Assert.Equal(2150, result.Value.CoolingLoadW, precision: 6);
        Assert.Equal(52, result.Value.HeatingLoadWPerM2, precision: 6);
        Assert.Equal(43, result.Value.CoolingLoadWPerM2, precision: 6);
    }

    [Fact]
    public void AggregationComponentBreakdownPreservesComponentBuckets()
    {
        var engine = new LoadAggregationEngine();

        var result = engine.Aggregate(new LoadAggregationInput(
            TargetId: 900,
            TargetType: LoadAggregationTargetType.Building,
            TargetName: "Building component invariant",
            Rooms:
            [
                new AggregationRoomLoadInput(
                    RoomId: 1,
                    RoomName: "Room 1",
                    ThermalZoneId: 10,
                    FloorId: 20,
                    BuildingId: 900,
                    AreaM2: 20,
                    HeatingLoadW: 1700,
                    CoolingLoadW: 1550,
                    HeatingBreakdown: new RoomHeatingLoadBreakdown(1000, 200, 150, 300, 50, 0, 0),
                    CoolingBreakdown: new RoomCoolingLoadBreakdown(300, 100, 500, 200, 75, 325, 50)),

                new AggregationRoomLoadInput(
                    RoomId: 2,
                    RoomName: "Room 2",
                    ThermalZoneId: 10,
                    FloorId: 20,
                    BuildingId: 900,
                    AreaM2: 30,
                    HeatingLoadW: 900,
                    CoolingLoadW: 600,
                    HeatingBreakdown: new RoomHeatingLoadBreakdown(600, 0, 100, 150, 50, 0, 0),
                    CoolingBreakdown: new RoomCoolingLoadBreakdown(200, 0, 100, 100, 50, 150, 0))
            ]));

        Assert.True(result.IsSuccess);

        Assert.Equal(2400, result.Value.ComponentBreakdown.TransmissionW, precision: 6);
        Assert.Equal(600, result.Value.ComponentBreakdown.SolarW, precision: 6);
        Assert.Equal(750, result.Value.ComponentBreakdown.VentilationW, precision: 6);
        Assert.Equal(225, result.Value.ComponentBreakdown.InfiltrationW, precision: 6);
        Assert.Equal(475, result.Value.ComponentBreakdown.InternalW, precision: 6);
        Assert.Equal(300, result.Value.ComponentBreakdown.GroundW, precision: 6);
    }

    [Fact]
    public void HourlyAggregationWithoutCompleteHourlyProfilesFallsBackToDesignPointWithWarning()
    {
        var engine = new LoadAggregationEngine();

        var result = engine.Aggregate(new LoadAggregationInput(
            TargetId: 900,
            TargetType: LoadAggregationTargetType.Building,
            TargetName: "Building hourly fallback invariant",
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
                    HeatingLoadW: 1700,
                    CoolingLoadW: 1550)
            ]));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasErrors);
        Assert.Equal(1700, result.Value.HeatingLoadW, precision: 6);
        Assert.Equal(1550, result.Value.CoolingLoadW, precision: 6);

        Assert.Contains(result.Value.Diagnostics, diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Warning &&
            diagnostic.Code == "Aggregation.HourlyUnavailable");
    }

    [Fact]
    public void BalanceInvariantDocumentationAndVerificationScriptExist()
    {
        Assert.True(File.Exists(BalanceInvariantDocumentPath));
        Assert.True(File.Exists(VerifyScriptPath));

        var document = File.ReadAllText(BalanceInvariantDocumentPath);
        Assert.Contains("Calculation Module Balance Invariants", document, StringComparison.Ordinal);
        Assert.Contains("room load must equal the positive heating breakdown total", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Aggregation.HourlyUnavailable", document, StringComparison.Ordinal);
        Assert.Contains("does not claim exact EnergyPlus numerical parity", document, StringComparison.OrdinalIgnoreCase);

        var wrapper = File.ReadAllText(VerifyScriptPath);
        Assert.Contains("AssistantEngineer.Tools.EngineeringCore.csproj", wrapper, StringComparison.Ordinal);
        Assert.Contains("verify-calculation-module-balance-invariants", wrapper, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet test", wrapper, StringComparison.Ordinal);

        var tool = File.ReadAllText(ToolProgramPath);
        Assert.Contains("CalculationModuleBalanceInvariantTests", tool, StringComparison.Ordinal);
    }
    private static string BalanceInvariantDocumentPath =>
            Path.Combine(TestPaths.RepoRoot, "docs", "calculations", "CalculationModuleBalanceInvariants.md");

    private static string VerifyScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "verify-calculation-module-balance-invariants.ps1");
    private static string ToolProgramPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "tools",
            "AssistantEngineer.Tools.EngineeringCore",
            "Program.cs");
}

