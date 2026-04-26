using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Options;

public sealed class CoolingLoadCalculationOptionsValidator : IValidateOptions<CoolingLoadCalculationOptions>
{
    public ValidateOptionsResult Validate(string? name, CoolingLoadCalculationOptions options)
    {
        var failures = new List<string>();

        RequirePositive(options.DefaultCoolingSafetyFactor, "Calculations:CoolingLoad:DefaultCoolingSafetyFactor", failures);
        RequirePositive(options.SimplifiedVolumeLoadWPerM3, "Calculations:CoolingLoad:SimplifiedVolumeLoadWPerM3", failures);
        RequirePositive(options.SimplifiedInternalWallLoadWPerM2, "Calculations:CoolingLoad:SimplifiedInternalWallLoadWPerM2", failures);
        RequirePositive(options.SimplifiedNorthExternalWallLoadWPerM2, "Calculations:CoolingLoad:SimplifiedNorthExternalWallLoadWPerM2", failures);
        RequirePositive(options.SimplifiedExternalWallLoadWPerM2, "Calculations:CoolingLoad:SimplifiedExternalWallLoadWPerM2", failures);
        RequireRange(options.DefaultOutdoorCoolingDesignTemperatureC, -100, 100, "Calculations:CoolingLoad:DefaultOutdoorCoolingDesignTemperatureC", failures);

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void RequirePositive(double value, string path, List<string> failures) =>
        RequireRange(value, double.Epsilon, double.MaxValue, path, failures);

    private static void RequireRange(double value, double min, double max, string path, List<string> failures)
    {
        if (!double.IsFinite(value) || value < min || value > max)
            failures.Add($"{path} must be between {min} and {max}. Actual value: {value}.");
    }
}

public sealed class Iso52016CoolingLoadOptionsValidator : IValidateOptions<Iso52016CoolingLoadOptions>
{
    public ValidateOptionsResult Validate(string? name, Iso52016CoolingLoadOptions options)
    {
        var failures = new List<string>();

        RequireRange(options.DefaultDesignMonth, 1, 12, "Calculations:Iso52016Cooling:DefaultDesignMonth", failures);
        RequirePositive(options.DefaultThermalMassWhPerM2K, "Calculations:Iso52016Cooling:DefaultThermalMassWhPerM2K", failures);
        RequireNonNegative(options.DefaultVentilationAirChangesPerHour, "Calculations:Iso52016Cooling:DefaultVentilationAirChangesPerHour", failures);
        RequirePositive(options.AirHeatCapacityWhPerM3K, "Calculations:Iso52016Cooling:AirHeatCapacityWhPerM3K", failures);
        RequireRatio(options.DefaultSolarUtilizationFactor, "Calculations:Iso52016Cooling:DefaultSolarUtilizationFactor", failures);
        RequirePositive(options.DefaultCoolingSafetyFactor, "Calculations:Iso52016Cooling:DefaultCoolingSafetyFactor", failures);

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void RequireRange(int value, int min, int max, string path, List<string> failures)
    {
        if (value < min || value > max)
            failures.Add($"{path} must be between {min} and {max}. Actual value: {value}.");
    }

    private static void RequirePositive(double value, string path, List<string> failures) =>
        RequireRange(value, double.Epsilon, double.MaxValue, path, failures);

    private static void RequireNonNegative(double value, string path, List<string> failures) =>
        RequireRange(value, 0, double.MaxValue, path, failures);

    private static void RequireRatio(double value, string path, List<string> failures) =>
        RequireRange(value, 0, 1, path, failures);

    private static void RequireRange(double value, double min, double max, string path, List<string> failures)
    {
        if (!double.IsFinite(value) || value < min || value > max)
            failures.Add($"{path} must be between {min} and {max}. Actual value: {value}.");
    }
}

public sealed class Iso52016EnergyNeedOptionsValidator : IValidateOptions<Iso52016EnergyNeedOptions>
{
    public ValidateOptionsResult Validate(string? name, Iso52016EnergyNeedOptions options)
    {
        var failures = new List<string>();

        RequireRange(options.DefaultWeatherYear, 1900, 2100, "Calculations:Iso52016EnergyNeed:DefaultWeatherYear", failures);
        RequireRange(options.DefaultHeatingSetbackC, -50, 50, "Calculations:Iso52016EnergyNeed:DefaultHeatingSetbackC", failures);
        RequireRange(options.DefaultCoolingSetpointC, -50, 80, "Calculations:Iso52016EnergyNeed:DefaultCoolingSetpointC", failures);
        RequireRange(options.DefaultCoolingSetbackC, -50, 80, "Calculations:Iso52016EnergyNeed:DefaultCoolingSetbackC", failures);
        RequireNonNegative(options.DefaultAirChangesPerHour, "Calculations:Iso52016EnergyNeed:DefaultAirChangesPerHour", failures);
        RequirePositive(options.AirHeatCapacityWhPerM3K, "Calculations:Iso52016EnergyNeed:AirHeatCapacityWhPerM3K", failures);
        RequirePositive(options.InternalHeatCapacityJPerM2K, "Calculations:Iso52016EnergyNeed:InternalHeatCapacityJPerM2K", failures);
        RequireRatio(options.DefaultSolarUtilizationFactor, "Calculations:Iso52016EnergyNeed:DefaultSolarUtilizationFactor", failures);
        RequireRatio(options.DefaultWindowFrameAreaFraction, "Calculations:Iso52016EnergyNeed:DefaultWindowFrameAreaFraction", failures);
        RequireRatio(options.DefaultDirectSolarShadingReductionFactor, "Calculations:Iso52016EnergyNeed:DefaultDirectSolarShadingReductionFactor", failures);
        RequireNonNegative(options.DefaultOverhangDepthM, "Calculations:Iso52016EnergyNeed:DefaultOverhangDepthM", failures);
        RequireNonNegative(options.DefaultSideFinDepthM, "Calculations:Iso52016EnergyNeed:DefaultSideFinDepthM", failures);
        RequireNonNegative(options.DefaultWindowRevealDepthM, "Calculations:Iso52016EnergyNeed:DefaultWindowRevealDepthM", failures);
        RequirePositive(options.DefaultWindowHeightM, "Calculations:Iso52016EnergyNeed:DefaultWindowHeightM", failures);
        RequirePositive(options.DefaultWindowWidthM, "Calculations:Iso52016EnergyNeed:DefaultWindowWidthM", failures);
        RequireRatio(options.MinimumDirectSolarShadingReductionFactor, "Calculations:Iso52016EnergyNeed:MinimumDirectSolarShadingReductionFactor", failures);
        RequireRatio(options.DiffuseSolarShareUnaffectedByShading, "Calculations:Iso52016EnergyNeed:DiffuseSolarShareUnaffectedByShading", failures);
        RequireRange(options.LatitudeDegrees, -90, 90, "Calculations:Iso52016EnergyNeed:LatitudeDegrees", failures);
        RequireRange(options.DefaultGroundBoundaryTemperatureC, -100, 100, "Calculations:Iso52016EnergyNeed:DefaultGroundBoundaryTemperatureC", failures);
        RequireRatio(options.AdjacentUnconditionedTemperatureWeight, "Calculations:Iso52016EnergyNeed:AdjacentUnconditionedTemperatureWeight", failures);

        if (double.IsFinite(options.DefaultCoolingSetpointC) &&
            double.IsFinite(options.DefaultCoolingSetbackC) &&
            options.DefaultCoolingSetbackC < options.DefaultCoolingSetpointC)
        {
            failures.Add("Calculations:Iso52016EnergyNeed:DefaultCoolingSetbackC must be greater than or equal to DefaultCoolingSetpointC.");
        }

        if (double.IsFinite(options.MinimumDirectSolarShadingReductionFactor) &&
            double.IsFinite(options.DefaultDirectSolarShadingReductionFactor) &&
            options.MinimumDirectSolarShadingReductionFactor > options.DefaultDirectSolarShadingReductionFactor)
        {
            failures.Add("Calculations:Iso52016EnergyNeed:MinimumDirectSolarShadingReductionFactor cannot exceed DefaultDirectSolarShadingReductionFactor.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void RequireRange(int value, int min, int max, string path, List<string> failures)
    {
        if (value < min || value > max)
            failures.Add($"{path} must be between {min} and {max}. Actual value: {value}.");
    }

    private static void RequirePositive(double value, string path, List<string> failures) =>
        RequireRange(value, double.Epsilon, double.MaxValue, path, failures);

    private static void RequireNonNegative(double value, string path, List<string> failures) =>
        RequireRange(value, 0, double.MaxValue, path, failures);

    private static void RequireRatio(double value, string path, List<string> failures) =>
        RequireRange(value, 0, 1, path, failures);

    private static void RequireRange(double value, double min, double max, string path, List<string> failures)
    {
        if (!double.IsFinite(value) || value < min || value > max)
            failures.Add($"{path} must be between {min} and {max}. Actual value: {value}.");
    }
}

public sealed class Iso52016MonthlyEnergyNeedOptionsValidator : IValidateOptions<Iso52016MonthlyEnergyNeedOptions>
{
    public ValidateOptionsResult Validate(string? name, Iso52016MonthlyEnergyNeedOptions options)
    {
        var failures = new List<string>();

        RequireRatio(options.HeatingGainUtilizationFactor, "Calculations:Iso52016MonthlyEnergyNeed:HeatingGainUtilizationFactor", failures);
        RequireRatio(options.CoolingGainUtilizationFactor, "Calculations:Iso52016MonthlyEnergyNeed:CoolingGainUtilizationFactor", failures);
        RequireNonNegative(options.MinimumMonthlyDemandKWh, "Calculations:Iso52016MonthlyEnergyNeed:MinimumMonthlyDemandKWh", failures);

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void RequireNonNegative(double value, string path, List<string> failures) =>
        RequireRange(value, 0, double.MaxValue, path, failures);

    private static void RequireRatio(double value, string path, List<string> failures) =>
        RequireRange(value, 0, 1, path, failures);

    private static void RequireRange(double value, double min, double max, string path, List<string> failures)
    {
        if (!double.IsFinite(value) || value < min || value > max)
            failures.Add($"{path} must be between {min} and {max}. Actual value: {value}.");
    }
}

public sealed class En12831HeatingLoadOptionsValidator : IValidateOptions<En12831HeatingLoadOptions>
{
    public ValidateOptionsResult Validate(string? name, En12831HeatingLoadOptions options)
    {
        var failures = new List<string>();

        RequireNonNegative(options.DefaultAirChangesPerHour, "Calculations:HeatingLoad:DefaultAirChangesPerHour", failures);
        RequirePositive(options.AirHeatCapacityWhPerM3K, "Calculations:HeatingLoad:AirHeatCapacityWhPerM3K", failures);
        RequireRange(options.DefaultOutdoorHeatingDesignTemperatureC, -100, 100, "Calculations:HeatingLoad:DefaultOutdoorHeatingDesignTemperatureC", failures);

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void RequirePositive(double value, string path, List<string> failures) =>
        RequireRange(value, double.Epsilon, double.MaxValue, path, failures);

    private static void RequireNonNegative(double value, string path, List<string> failures) =>
        RequireRange(value, 0, double.MaxValue, path, failures);

    private static void RequireRange(double value, double min, double max, string path, List<string> failures)
    {
        if (!double.IsFinite(value) || value < min || value > max)
            failures.Add($"{path} must be between {min} and {max}. Actual value: {value}.");
    }
}

public sealed class En16798ProfileOptionsValidator : IValidateOptions<En16798ProfileOptions>
{
    public ValidateOptionsResult Validate(string? name, En16798ProfileOptions options) =>
        Enum.IsDefined(options.DefaultCategory)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("Calculations:En16798Profiles:DefaultCategory must be a defined En16798ProfileCategory value.");
}

public sealed class NaturalVentilationOptionsValidator : IValidateOptions<NaturalVentilationOptions>
{
    public ValidateOptionsResult Validate(string? name, NaturalVentilationOptions options)
    {
        var failures = new List<string>();

        RequireRange(options.MinimumOutdoorTemperatureC, -100, 100, "Calculations:NaturalVentilation:MinimumOutdoorTemperatureC", failures);
        RequireRange(options.MaximumOutdoorTemperatureC, -100, 100, "Calculations:NaturalVentilation:MaximumOutdoorTemperatureC", failures);
        RequireRatio(options.MinimumDemandFactor, "Calculations:NaturalVentilation:MinimumDemandFactor", failures);
        RequireRatio(options.OperableWindowAreaFraction, "Calculations:NaturalVentilation:OperableWindowAreaFraction", failures);
        RequireRatio(options.OpeningDischargeCoefficient, "Calculations:NaturalVentilation:OpeningDischargeCoefficient", failures);
        RequirePositive(options.MaximumAirChangesPerHour, "Calculations:NaturalVentilation:MaximumAirChangesPerHour", failures);
        RequireRange(options.IndoorTemperatureThresholdC, -100, 100, "Calculations:NaturalVentilation:IndoorTemperatureThresholdC", failures);
        RequireNonNegative(options.MinimumIndoorOutdoorDeltaC, "Calculations:NaturalVentilation:MinimumIndoorOutdoorDeltaC", failures);
        RequirePositive(options.MaximumWindSpeedForOpeningMPerS, "Calculations:NaturalVentilation:MaximumWindSpeedForOpeningMPerS", failures);
        RequireRange(options.NightCoolingStartHour, 0, 23, "Calculations:NaturalVentilation:NightCoolingStartHour", failures);
        RequireRange(options.NightCoolingEndHour, 0, 23, "Calculations:NaturalVentilation:NightCoolingEndHour", failures);
        RequireRange(options.NightCoolingIndoorTemperatureThresholdC, -100, 100, "Calculations:NaturalVentilation:NightCoolingIndoorTemperatureThresholdC", failures);
        RequireRatio(options.MinimumNightOpeningFactor, "Calculations:NaturalVentilation:MinimumNightOpeningFactor", failures);

        if (double.IsFinite(options.MinimumOutdoorTemperatureC) &&
            double.IsFinite(options.MaximumOutdoorTemperatureC) &&
            options.MinimumOutdoorTemperatureC > options.MaximumOutdoorTemperatureC)
        {
            failures.Add("Calculations:NaturalVentilation:MinimumOutdoorTemperatureC cannot exceed MaximumOutdoorTemperatureC.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void RequireRange(int value, int min, int max, string path, List<string> failures)
    {
        if (value < min || value > max)
            failures.Add($"{path} must be between {min} and {max}. Actual value: {value}.");
    }

    private static void RequirePositive(double value, string path, List<string> failures) =>
        RequireRange(value, double.Epsilon, double.MaxValue, path, failures);

    private static void RequireNonNegative(double value, string path, List<string> failures) =>
        RequireRange(value, 0, double.MaxValue, path, failures);

    private static void RequireRatio(double value, string path, List<string> failures) =>
        RequireRange(value, 0, 1, path, failures);

    private static void RequireRange(double value, double min, double max, string path, List<string> failures)
    {
        if (!double.IsFinite(value) || value < min || value > max)
            failures.Add($"{path} must be between {min} and {max}. Actual value: {value}.");
    }
}
