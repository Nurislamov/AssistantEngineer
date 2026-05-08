using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;

namespace AssistantEngineer.Tests.Calculations.DomesticHotWater;

internal static class DomesticHotWaterSystemLossTestData
{
    public static DomesticHotWaterUsefulDemandResult CreateUsefulDemand(
        IReadOnlyList<double>? hourlyUsefulEnergy = null,
        IReadOnlyList<double>? hourlyVolume = null)
    {
        var usefulHourly = hourlyUsefulEnergy ?? Enumerable.Repeat(1.0, 8760).ToArray();
        var volumeHourly = hourlyVolume ?? Enumerable.Repeat(2.0, 8760).ToArray();

        return new DomesticHotWaterUsefulDemandResult(
            CalculationId: "DHW-USEFUL-1",
            BuildingId: "B1",
            ZoneId: "Z1",
            RoomId: "R1",
            DemandBasis: DomesticHotWaterDemandBasis.People,
            UseCategory: DomesticHotWaterUseCategory.Residential,
            DailyVolumeLiters: volumeHourly.Sum() / 365.0,
            AnnualVolumeLiters: volumeHourly.Sum(),
            MonthlyVolumeLiters: BuildMonthly(volumeHourly),
            HourlyVolumeLiters8760: volumeHourly,
            TemperatureRiseKelvin: 45.0,
            DailyUsefulEnergyKWh: usefulHourly.Sum() / 365.0,
            AnnualUsefulEnergyKWh: usefulHourly.Sum(),
            MonthlyUsefulEnergyKWh: BuildMonthly(usefulHourly),
            HourlyUsefulEnergyKWh8760: usefulHourly,
            Disclosure: new StandardCalculationDisclosureFactory().CreateDomesticHotWaterIso12831Disclosure(),
            Diagnostics: []);
    }

    public static DomesticHotWaterStorageLossInput CreateStorageInput(
        bool present = true,
        double? standingLossW = 100.0,
        double? coefficient = null,
        double? setpoint = 60.0,
        double? ambient = 20.0,
        double? operatingHours = 24.0,
        double? recoverableFraction = 0.0) =>
        new(
            IsStoragePresent: present,
            StorageVolumeLiters: 200.0,
            StorageSetpointTemperatureCelsius: setpoint,
            AmbientTemperatureCelsius: ambient,
            HourlyAmbientTemperaturesCelsius8760: null,
            StorageLossCoefficientWPerKelvin: coefficient,
            StandingLossWatts: standingLossW,
            OperatingHoursPerDay: operatingHours,
            RecoverableFraction: recoverableFraction,
            RecoveryMode: DomesticHotWaterLossRecoveryMode.PartiallyRecoverable,
            Source: "UnitTest",
            Diagnostics: []);

    public static DomesticHotWaterDistributionLossInput CreateDistributionInput(
        bool present = true,
        double? length = 20.0,
        double? linearLoss = 0.4,
        double? supply = 55.0,
        double? ambient = 20.0,
        double? operatingHours = 24.0,
        double? recoverableFraction = 0.0) =>
        new(
            IsDistributionPresent: present,
            PipeLengthMeters: length,
            PipeLinearLossCoefficientWPerMeterKelvin: linearLoss,
            SupplyTemperatureCelsius: supply,
            AmbientTemperatureCelsius: ambient,
            HourlyAmbientTemperaturesCelsius8760: null,
            OperatingHoursPerDay: operatingHours,
            RecoverableFraction: recoverableFraction,
            RecoveryMode: DomesticHotWaterLossRecoveryMode.PartiallyRecoverable,
            Source: "UnitTest",
            Diagnostics: []);

    public static DomesticHotWaterCirculationLossInput CreateCirculationInput(
        bool present = true,
        double? loopLength = 30.0,
        double? linearLoss = 0.5,
        double? supply = 55.0,
        double? ambient = 20.0,
        IReadOnlyList<double>? operation = null,
        double? operatingHours = 24.0,
        double? pumpPowerW = 50.0,
        double? recoverableFraction = 0.0) =>
        new(
            IsCirculationPresent: present,
            LoopLengthMeters: loopLength,
            LoopLinearLossCoefficientWPerMeterKelvin: linearLoss,
            SupplyTemperatureCelsius: supply,
            ReturnTemperatureCelsius: 45.0,
            AmbientTemperatureCelsius: ambient,
            HourlyAmbientTemperaturesCelsius8760: null,
            HourlyOperationFractions8760: operation,
            OperatingHoursPerDay: operatingHours,
            PumpPowerWatts: pumpPowerW,
            RecoverableFraction: recoverableFraction,
            RecoveryMode: DomesticHotWaterLossRecoveryMode.PartiallyRecoverable,
            Source: "UnitTest",
            Diagnostics: []);

    public static DomesticHotWaterSystemLossInput CreateSystemLossInput(
        DomesticHotWaterUsefulDemandResult? usefulDemand = null,
        DomesticHotWaterStorageLossInput? storage = null,
        DomesticHotWaterDistributionLossInput? distribution = null,
        DomesticHotWaterCirculationLossInput? circulation = null,
        double? defaultAmbient = 20.0,
        double? defaultRecoverableFraction = 0.0) =>
        new(
            CalculationId: "DHW-SYS-1",
            UsefulDemand: usefulDemand ?? CreateUsefulDemand(),
            Storage: storage ?? CreateStorageInput(),
            Distribution: distribution ?? CreateDistributionInput(),
            Circulation: circulation ?? CreateCirculationInput(),
            DefaultAmbientTemperatureCelsius: defaultAmbient,
            DefaultRecoverableFraction: defaultRecoverableFraction,
            DisclosureOverride: null,
            Source: "UnitTest");

    private static IReadOnlyList<double> BuildMonthly(IReadOnlyList<double> hourly)
    {
        int[] daysPerMonth = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
        var monthly = new double[12];
        var offset = 0;
        for (var month = 0; month < 12; month++)
        {
            var hours = daysPerMonth[month] * 24;
            monthly[month] = hourly.Skip(offset).Take(hours).Sum();
            offset += hours;
        }

        return monthly;
    }
}
