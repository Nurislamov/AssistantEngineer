using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater;

public sealed class DomesticHotWaterDrawOffProfileFoundationTests
{
    private readonly DomesticHotWaterDrawOffProfileBuilder _builder = new();

    [Fact]
    public void ConstantPeopleDemand_BuildsDeterministicHourlyProfile()
    {
        var definition = CreateDefinition() with
        {
            DemandBasis = DomesticHotWaterDemandBasis.PerPerson,
            OccupantCount = 4,
            DailyVolumeLitersPerPerson = 50
        };

        var result = _builder.Build(new DomesticHotWaterDrawOffProfileRequest(
            DemandDefinition: definition,
            Resolution: DomesticHotWaterDrawOffProfileResolution.Hourly,
            NumberOfSteps: 8760,
            Schedule: null,
            NormalizationMode: DomesticHotWaterScheduleNormalizationMode.NormalizeToUnity,
            FallbackProfileMode: DomesticHotWaterFallbackProfileMode.DeterministicByUseKind,
            DiagnosticsMode: DomesticHotWaterDiagnosticsMode.Verbose));

        Assert.Equal(8760, result.VolumeProfileLiters.Count);
        Assert.Equal(8760, result.UsefulEnergyProfileKWh.Count);
        Assert.Equal(4 * 50 * 365, result.TotalVolumeLiters, precision: 3);
        Assert.True(result.TotalUsefulEnergyKWh > 0);
        Assert.Contains(result.Diagnostics, item => item.Code == "AE-DHW-DRAWOFF-DEFAULT-PROFILE-USED");
    }

    [Fact]
    public void ScheduledEnergy_UsesProfileDirectly()
    {
        var scheduled = Enumerable.Repeat(1.25, 12).ToArray();
        var definition = CreateDefinition() with
        {
            DemandBasis = DomesticHotWaterDemandBasis.ScheduledEnergy,
            ScheduledUsefulEnergyProfileKWh = scheduled
        };

        var result = _builder.Build(new DomesticHotWaterDrawOffProfileRequest(
            DemandDefinition: definition,
            Resolution: DomesticHotWaterDrawOffProfileResolution.Monthly,
            NumberOfSteps: 12,
            Schedule: Enumerable.Repeat(1.0, 12).ToArray(),
            NormalizationMode: DomesticHotWaterScheduleNormalizationMode.NormalizeToUnity,
            FallbackProfileMode: DomesticHotWaterFallbackProfileMode.DeterministicByUseKind,
            DiagnosticsMode: DomesticHotWaterDiagnosticsMode.Verbose));

        Assert.Equal(12, result.UsefulEnergyProfileKWh.Count);
        Assert.Equal(scheduled.Sum(), result.TotalUsefulEnergyKWh, 6);
    }

    [Fact]
    public void InvalidScheduleLength_EmitsDiagnostic()
    {
        var result = _builder.Build(new DomesticHotWaterDrawOffProfileRequest(
            DemandDefinition: CreateDefinition(),
            Resolution: DomesticHotWaterDrawOffProfileResolution.Hourly,
            NumberOfSteps: 24,
            Schedule: Enumerable.Repeat(1.0, 12).ToArray(),
            NormalizationMode: DomesticHotWaterScheduleNormalizationMode.NormalizeToUnity,
            FallbackProfileMode: DomesticHotWaterFallbackProfileMode.DeterministicByUseKind,
            DiagnosticsMode: DomesticHotWaterDiagnosticsMode.Verbose));

        Assert.Contains(result.Diagnostics, item => item.Code == "AE-DHW-DRAWOFF-SCHEDULE-LENGTH-MISMATCH");
    }

    private static DomesticHotWaterDemandDefinition CreateDefinition() =>
        new(
            DemandId: "DHW-DEF-1",
            BuildingId: "B1",
            ZoneId: "Z1",
            UseKind: DomesticHotWaterBuildingUseKind.Residential,
            DemandBasis: DomesticHotWaterDemandBasis.PerPerson,
            OccupantCount: 3,
            FloorAreaSquareMeters: null,
            DwellingCount: null,
            FixtureCount: null,
            DailyVolumeLitersPerPerson: 45,
            DailyVolumeLitersPerSquareMeter: null,
            DailyVolumeLitersPerDwelling: null,
            ColdWaterTemperatureCelsius: 10,
            HotWaterSetpointTemperatureCelsius: 55,
            ReferenceDrawOffTemperatureCelsius: null,
            TimeStepHours: 1.0,
            HourlySchedule: null,
            MonthlySchedule: null,
            ScheduledVolumeProfile: null,
            ScheduledUsefulEnergyProfileKWh: null,
            AnnualOperatingDays: 365,
            Source: "UnitTest",
            Diagnostics: []);
}
