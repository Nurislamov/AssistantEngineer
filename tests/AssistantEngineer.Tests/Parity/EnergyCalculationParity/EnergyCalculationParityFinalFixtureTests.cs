using AssistantEngineer.Modules.Calculations.Application.Contracts.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.EquipmentSizing;
using AssistantEngineer.Modules.Calculations.Application.Contracts.InternalGains;
using AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.EquipmentSizing;
using AssistantEngineer.Modules.Calculations.Application.Services.InternalGains;
using AssistantEngineer.Modules.Calculations.Application.Services.RoomLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity;

public class EnergyCalculationParityFinalFixtureTests
{
    [Fact]
    public void AllJsonFixturesLoadAndHaveRequiredMetadata()
    {
        foreach (var file in Directory.GetFiles(FixtureDirectory(), "*.json"))
        {
            var fixture = EnergyCalculationParityFixtureLoader.Load(Path.GetFileName(file));

            Assert.False(string.IsNullOrWhiteSpace(fixture.FixtureName), file);
            Assert.False(string.IsNullOrWhiteSpace(fixture.Description), fixture.FixtureName);
            Assert.False(string.IsNullOrWhiteSpace(fixture.ReferenceType), fixture.FixtureName);
            Assert.False(string.IsNullOrWhiteSpace(fixture.Method), fixture.FixtureName);
            Assert.NotEmpty(fixture.Assumptions);
            Assert.NotNull(fixture.Input);
            Assert.NotNull(fixture.Expected);
            Assert.NotNull(fixture.Tolerances);
        }
    }

    [Theory]
    [InlineData("internal-gains-occupancy-sensible.json")]
    [InlineData("internal-gains-lighting-by-area.json")]
    [InlineData("internal-gains-equipment-by-area.json")]
    [InlineData("internal-gains-process-with-schedule.json")]
    [InlineData("internal-gains-room-aggregation.json")]
    [InlineData("internal-gains-zero-schedule.json")]
    [InlineData("internal-gains-invalid-schedule-factor.json")]
    [InlineData("internal-gains-negative-power-density.json")]
    public void InternalGainFixturesPass(string fileName)
    {
        var fixture = EnergyCalculationParityFixtureLoader.Load(fileName);
        var input = AssertRequired(fixture.Input.InternalGainCalculation);
        var result = new InternalGainEngine().Calculate(CreateInternalGainInput(input));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(fixture.Expected.HasErrors, result.Value.HasErrors);
        AssertClose(fixture.Expected.OccupancySensibleGainW, result.Value.OccupancySensibleGainW, fixture.Tolerances.HourlyLoadW);
        AssertClose(fixture.Expected.LightingGainW, result.Value.LightingGainW, fixture.Tolerances.HourlyLoadW);
        AssertClose(fixture.Expected.EquipmentGainW, result.Value.EquipmentGainW, fixture.Tolerances.HourlyLoadW);
        AssertClose(fixture.Expected.ProcessSensibleGainW, result.Value.ProcessSensibleGainW, fixture.Tolerances.HourlyLoadW);
        AssertClose(fixture.Expected.TotalSensibleGainW, result.Value.TotalSensibleGainW, fixture.Tolerances.HourlyLoadW);

        foreach (var code in fixture.Expected.ExpectedDiagnosticCodes)
            Assert.Contains(result.Value.Diagnostics, diagnostic => diagnostic.Code == code);
    }

    [Theory]
    [InlineData("room-load-heating-transmission-only.json")]
    [InlineData("room-load-heating-transmission-ventilation-infiltration.json")]
    [InlineData("room-load-cooling-solar-internal-ventilation.json")]
    [InlineData("room-load-does-not-go-negative.json")]
    public void RoomLoadFixturesPass(string fileName)
    {
        var fixture = EnergyCalculationParityFixtureLoader.Load(fileName);
        var input = AssertRequired(fixture.Input.RoomLoad);
        var result = new RoomLoadCalculationEngine().Calculate(CreateRoomLoadInput(input));

        Assert.True(result.IsSuccess, result.Error);
        AssertClose(fixture.Expected.HeatingLoadW, result.Value.HeatingLoadW, fixture.Tolerances.HourlyLoadW);
        AssertClose(fixture.Expected.CoolingLoadW, result.Value.CoolingLoadW, fixture.Tolerances.HourlyLoadW);
        AssertClose(fixture.Expected.HeatingLoadWPerM2, result.Value.HeatingLoadWPerM2, fixture.Tolerances.HourlyLoadW);
        AssertClose(fixture.Expected.CoolingLoadWPerM2, result.Value.CoolingLoadWPerM2, fixture.Tolerances.HourlyLoadW);

        foreach (var code in fixture.Expected.ExpectedDiagnosticCodes)
            Assert.Contains(result.Value.Diagnostics, diagnostic => diagnostic.Code == code);
    }

    [Theory]
    [InlineData("aggregation-floor-two-rooms.json")]
    [InlineData("aggregation-building-two-floors.json")]
    [InlineData("aggregation-thermal-zone-no-double-count.json")]
    public void AggregationFixturesPass(string fileName)
    {
        var fixture = EnergyCalculationParityFixtureLoader.Load(fileName);
        var input = AssertRequired(fixture.Input.Aggregation);
        var result = new LoadAggregationEngine().Aggregate(CreateAggregationInput(input));

        Assert.True(result.IsSuccess, result.Error);
        AssertClose(fixture.Expected.HeatingLoadW, result.Value.HeatingLoadW, fixture.Tolerances.HourlyLoadW);
        AssertClose(fixture.Expected.CoolingLoadW, result.Value.CoolingLoadW, fixture.Tolerances.HourlyLoadW);
        if (fixture.Expected.TotalAreaM2 != 0)
            AssertClose(fixture.Expected.TotalAreaM2, result.Value.TotalAreaM2, fixture.Tolerances.HourlyLoadW);
        if (fixture.Expected.HeatingLoadWPerM2 != 0)
            AssertClose(fixture.Expected.HeatingLoadWPerM2, result.Value.HeatingLoadWPerM2, fixture.Tolerances.HourlyLoadW);
        if (fixture.Expected.CoolingLoadWPerM2 != 0)
            AssertClose(fixture.Expected.CoolingLoadWPerM2, result.Value.CoolingLoadWPerM2, fixture.Tolerances.HourlyLoadW);
    }

    [Theory]
    [InlineData("annual-constant-heating-load.json")]
    [InlineData("annual-constant-cooling-load.json")]
    [InlineData("annual-monthly-aggregation-consistency.json")]
    [InlineData("annual-energy-use-intensity.json")]
    public void AnnualEnergyFixturesPass(string fileName)
    {
        var fixture = EnergyCalculationParityFixtureLoader.Load(fileName);
        var input = AssertRequired(fixture.Input.AnnualEnergyBalance);
        var result = new AnnualEnergyBalanceEngine().Calculate(CreateAnnualInput(input));

        Assert.True(result.IsSuccess, result.Error);
        if (fixture.ExpectedAnnualResults.HourCount > 0 ||
            fixture.ExpectedAnnualResults.HeatingDemandKWh != 0 ||
            fixture.ExpectedAnnualResults.CoolingDemandKWh != 0)
        {
            AssertClose(fixture.ExpectedAnnualResults.HeatingDemandKWh, result.Value.AnnualHeatingDemandKWh, fixture.Tolerances.AnnualDemandKWh);
            AssertClose(fixture.ExpectedAnnualResults.CoolingDemandKWh, result.Value.AnnualCoolingDemandKWh, fixture.Tolerances.AnnualDemandKWh);
        }
        AssertClose(
            result.Value.AnnualTotalDemandKWh,
            result.Value.MonthlyResults.Sum(month => month.TotalKWh),
            fixture.Tolerances.AnnualDemandKWh);

        if (fixture.FixtureName == "EnergyUseIntensity")
            AssertClose(50, result.Value.EnergyUseIntensityKWhPerM2Year, fixture.Tolerances.AnnualDemandKWh);
    }

    [Theory]
    [InlineData("dhw-residential-simple.json")]
    [InlineData("dhw-zero-occupancy.json")]
    public void DhwFixturesPass(string fileName)
    {
        var fixture = EnergyCalculationParityFixtureLoader.Load(fileName);
        var input = AssertRequired(fixture.Input.Dhw);
        var result = new DomesticHotWaterDemandService().Calculate(new DomesticHotWaterDemandRequest
        {
            PeopleCount = input.PeopleCount,
            LitersPerPersonDay = input.LitersPerPersonDay,
            ColdWaterTemperatureC = input.ColdWaterTemperatureC,
            HotWaterTemperatureC = input.HotWaterTemperatureC,
            DistributionLossFactor = input.DistributionLossFactor
        });

        Assert.True(result.IsSuccess, result.Error);
        AssertClose(fixture.Expected.DailyVolumeLiters, result.Value.DailyVolumeLiters, fixture.Tolerances.AnnualDemandKWh);
        AssertClose(fixture.Expected.DailyEnergyKWh, result.Value.DailyEnergyKWh, fixture.Tolerances.AnnualDemandKWh);
        if (fixture.Expected.AnnualEnergyKWh != 0 || fixture.FixtureName == "DhwZeroOccupancy")
            AssertClose(fixture.Expected.AnnualEnergyKWh, result.Value.AnnualEnergyKWh, fixture.Tolerances.AnnualDemandKWh);
    }

    [Theory]
    [InlineData("system-heating-efficiency.json")]
    [InlineData("system-cooling-cop.json")]
    [InlineData("system-total-energy.json")]
    public void SystemEnergyFixturesPass(string fileName)
    {
        var fixture = EnergyCalculationParityFixtureLoader.Load(fileName);
        var input = AssertRequired(fixture.Input.SystemEnergy);
        var result = new SystemEnergyEngine().Calculate(new SystemEnergyInput(
            input.UsefulHeatingEnergyKWh,
            input.UsefulCoolingEnergyKWh,
            input.UsefulDhwEnergyKWh,
            HeatingEfficiency: input.HeatingEfficiency,
            CoolingCop: input.CoolingCop,
            DhwEfficiency: input.DhwEfficiency,
            FanEnergyKWh: input.FanEnergyKWh));

        Assert.True(result.IsSuccess, result.Error);
        if (fixture.Expected.FinalHeatingEnergyKWh != 0)
            AssertClose(fixture.Expected.FinalHeatingEnergyKWh, result.Value.FinalHeatingEnergyKWh, fixture.Tolerances.AnnualDemandKWh);
        if (fixture.Expected.FinalCoolingEnergyKWh != 0)
            AssertClose(fixture.Expected.FinalCoolingEnergyKWh, result.Value.FinalCoolingEnergyKWh, fixture.Tolerances.AnnualDemandKWh);
        if (fixture.Expected.FinalDhwEnergyKWh != 0)
            AssertClose(fixture.Expected.FinalDhwEnergyKWh, result.Value.FinalDhwEnergyKWh, fixture.Tolerances.AnnualDemandKWh);
        if (fixture.Expected.TotalFinalEnergyKWh != 0)
            AssertClose(fixture.Expected.TotalFinalEnergyKWh, result.Value.TotalFinalEnergyKWh, fixture.Tolerances.AnnualDemandKWh);
    }

    [Theory]
    [InlineData("equipment-sizing-cooling-simple.json")]
    [InlineData("equipment-candidate-accepted.json")]
    [InlineData("equipment-candidate-rejected.json")]
    [InlineData("equipment-no-equipment-found.json")]
    public void EquipmentSizingFixturesPass(string fileName)
    {
        var fixture = EnergyCalculationParityFixtureLoader.Load(fileName);
        var input = AssertRequired(fixture.Input.EquipmentSizing);
        var result = new EquipmentSizingEngine().Calculate(CreateEquipmentSizingInput(input));

        Assert.True(result.IsSuccess, result.Error);
        if (fixture.Expected.RequiredCoolingCapacityWithReserveW != 0)
            AssertClose(fixture.Expected.RequiredCoolingCapacityWithReserveW, result.Value.RequiredCoolingCapacityWithReserveW, fixture.Tolerances.HourlyLoadW);

        if (fixture.Expected.HasAcceptedCandidate)
        {
            var accepted = Assert.Single(result.Value.RecommendedEquipment);
            AssertClose(fixture.Expected.CoolingMarginW, accepted.CoolingMarginW, fixture.Tolerances.HourlyLoadW);
            AssertClose(fixture.Expected.CoolingMarginPercent, accepted.CoolingMarginPercent!.Value, fixture.Tolerances.HourlyLoadW);
        }

        if (fixture.Expected.HasRejectedCandidate)
        {
            var rejected = Assert.Single(result.Value.RejectedEquipment);
            Assert.Contains(fixture.Expected.ExpectedRejectReason, rejected.Reasons);
        }

        foreach (var code in fixture.Expected.ExpectedDiagnosticCodes)
            Assert.Contains(result.Value.Diagnostics, diagnostic => diagnostic.Code == code);
    }

    private static RoomLoadCalculationInput CreateRoomLoadInput(RoomLoadFixtureInput input) =>
        new(
            input.RoomId,
            input.RoomCode,
            input.RoomName,
            input.AreaM2,
            input.VolumeM3,
            input.HeatingSetpointC,
            input.CoolingSetpointC,
            input.OutdoorDesignHeatingTemperatureC,
            input.OutdoorDesignCoolingTemperatureC,
            FixedComponents: new RoomLoadFixedComponentInput(
                input.FixedComponents.HeatingTransmissionW,
                input.FixedComponents.HeatingWindowTransmissionW,
                input.FixedComponents.HeatingGroundW,
                input.FixedComponents.HeatingVentilationW,
                input.FixedComponents.HeatingInfiltrationW,
                input.FixedComponents.CoolingTransmissionW,
                input.FixedComponents.CoolingWindowTransmissionW,
                input.FixedComponents.CoolingGroundW,
                input.FixedComponents.CoolingVentilationW,
                input.FixedComponents.CoolingInfiltrationW,
                input.FixedComponents.CoolingSolarW,
                input.FixedComponents.CoolingInternalGainsW));

    private static InternalGainInput CreateInternalGainInput(InternalGainsFixtureInput input) =>
        new(
            input.RoomId,
            input.AreaM2,
            input.OccupancyPeople,
            input.SensibleGainPerPersonW,
            input.LatentGainPerPersonW,
            input.LightingLoadW,
            input.LightingPowerDensityWPerM2,
            input.EquipmentLoadW,
            input.EquipmentPowerDensityWPerM2,
            input.ProcessSensibleGainW,
            input.ProcessLatentGainW,
            input.CustomSensibleGainW,
            input.CustomLatentGainW,
            input.OccupancyScheduleFactor,
            input.LightingScheduleFactor,
            input.EquipmentScheduleFactor,
            input.ProcessScheduleFactor,
            input.CustomScheduleFactor,
            input.DiagnosticsContext);

    private static LoadAggregationInput CreateAggregationInput(LoadAggregationFixtureInput input) =>
        new(
            input.TargetId,
            Enum.Parse<LoadAggregationTargetType>(input.TargetType),
            input.Rooms.Select(room => new AggregationRoomLoadInput(
                room.RoomId,
                room.RoomName,
                room.ThermalZoneId,
                room.FloorId,
                room.BuildingId,
                room.AreaM2,
                room.HeatingLoadW,
                room.CoolingLoadW)).ToArray(),
            Enum.Parse<LoadAggregationMode>(input.Mode));

    private static AnnualEnergyBalanceInput CreateAnnualInput(AnnualEnergyBalanceFixtureInput input)
    {
        var hours = new List<AnnualEnergyBalanceHourInput>();
        var hourIndex = 0;
        foreach (var month in input.Months)
        {
            for (var i = 0; i < month.Hours; i++)
            {
                hours.Add(new AnnualEnergyBalanceHourInput(
                    hourIndex++,
                    month.Month,
                    month.HeatingLoadW,
                    month.CoolingLoadW));
            }
        }

        return new AnnualEnergyBalanceInput(
            input.BuildingId,
            input.BuildingName,
            input.BuildingAreaM2,
            input.Year,
            hours);
    }

    private static EquipmentSizingInput CreateEquipmentSizingInput(EquipmentSizingFixtureInput input) =>
        new(
            input.TargetId,
            Enum.Parse<EquipmentSizingTargetType>(input.TargetType),
            input.RequiredHeatingLoadW,
            input.RequiredCoolingLoadW,
            input.SafetyFactor,
            input.Candidates.Select(candidate => new EquipmentSizingCandidateInput(
                candidate.EquipmentId,
                candidate.Name,
                candidate.Model,
                candidate.EquipmentType,
                candidate.HeatingCapacityW,
                candidate.CoolingCapacityW,
                candidate.IsActive)).ToArray(),
            input.EquipmentType);

    private static string FixtureDirectory() =>
        Path.Combine(
            TestPaths.RepoRoot,
            "tests",
            "AssistantEngineer.Tests",
            "Parity",
            "EnergyCalculationParity",
            "Fixtures");

    private static T AssertRequired<T>(T? value)
        where T : class
    {
        Assert.NotNull(value);
        return value;
    }

    private static void AssertClose(double expected, double actual, double tolerance) =>
        Assert.InRange(actual, expected - tolerance, expected + tolerance);
}
