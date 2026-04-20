using AssistantEngineer.Domain.Primitives;

namespace AssistantEngineer.Domain.Models;

public class CalculationPreferences
{
    public int Id { get; private set; }
    public double CoolingSafetyFactor { get; private set; }
    public double HeatingSafetyFactor { get; private set; }
    public double Iso52016InternalHeatCapacityJPerM2K { get; private set; }
    public double Iso52016SolarUtilizationFactor { get; private set; }
    public double Iso52016WindowFrameAreaFraction { get; private set; }
    public double Iso52016DirectSolarShadingReductionFactor { get; private set; }
    public double Iso52016DiffuseSolarShareUnaffectedByShading { get; private set; }
    public double Iso52016DefaultAirChangesPerHour { get; private set; }

    public int ProjectId { get; private set; }
    public Project Project { get; private set; } = null!;

    private CalculationPreferences() { }

    private CalculationPreferences(
        double coolingSafetyFactor,
        double heatingSafetyFactor,
        double iso52016InternalHeatCapacityJPerM2K,
        double iso52016SolarUtilizationFactor,
        double iso52016WindowFrameAreaFraction,
        double iso52016DirectSolarShadingReductionFactor,
        double iso52016DiffuseSolarShareUnaffectedByShading,
        double iso52016DefaultAirChangesPerHour)
    {
        CoolingSafetyFactor = coolingSafetyFactor;
        HeatingSafetyFactor = heatingSafetyFactor;
        Iso52016InternalHeatCapacityJPerM2K = iso52016InternalHeatCapacityJPerM2K;
        Iso52016SolarUtilizationFactor = iso52016SolarUtilizationFactor;
        Iso52016WindowFrameAreaFraction = iso52016WindowFrameAreaFraction;
        Iso52016DirectSolarShadingReductionFactor = iso52016DirectSolarShadingReductionFactor;
        Iso52016DiffuseSolarShareUnaffectedByShading = iso52016DiffuseSolarShareUnaffectedByShading;
        Iso52016DefaultAirChangesPerHour = iso52016DefaultAirChangesPerHour;
    }

    public static Result<CalculationPreferences> Create(
        double coolingSafetyFactor,
        double heatingSafetyFactor,
        double iso52016InternalHeatCapacityJPerM2K = 10_000,
        double iso52016SolarUtilizationFactor = 0.75,
        double iso52016WindowFrameAreaFraction = 0.25,
        double iso52016DirectSolarShadingReductionFactor = 1.0,
        double iso52016DiffuseSolarShareUnaffectedByShading = 0.3,
        double iso52016DefaultAirChangesPerHour = 0.5)
    {
        var coolingCheck = Guard.AgainstRange(coolingSafetyFactor, 1.0, 2.0, "Cooling safety factor");
        if (coolingCheck.IsFailure) return Result<CalculationPreferences>.Failure(coolingCheck);

        var heatingCheck = Guard.AgainstRange(heatingSafetyFactor, 1.0, 2.0, "Heating safety factor");
        if (heatingCheck.IsFailure) return Result<CalculationPreferences>.Failure(heatingCheck);

        var capacityCheck = Guard.AgainstRange(iso52016InternalHeatCapacityJPerM2K, 1_000, 500_000, "ISO 52016 internal heat capacity");
        if (capacityCheck.IsFailure) return Result<CalculationPreferences>.Failure(capacityCheck);

        var solarUtilizationCheck = Guard.AgainstRange(iso52016SolarUtilizationFactor, 0, 1, "ISO 52016 solar utilization factor");
        if (solarUtilizationCheck.IsFailure) return Result<CalculationPreferences>.Failure(solarUtilizationCheck);

        var frameCheck = Guard.AgainstRange(iso52016WindowFrameAreaFraction, 0, 0.9, "ISO 52016 window frame area fraction");
        if (frameCheck.IsFailure) return Result<CalculationPreferences>.Failure(frameCheck);

        var directShadingCheck = Guard.AgainstRange(iso52016DirectSolarShadingReductionFactor, 0, 1, "ISO 52016 direct solar shading reduction factor");
        if (directShadingCheck.IsFailure) return Result<CalculationPreferences>.Failure(directShadingCheck);

        var diffuseShareCheck = Guard.AgainstRange(iso52016DiffuseSolarShareUnaffectedByShading, 0, 1, "ISO 52016 diffuse solar share unaffected by shading");
        if (diffuseShareCheck.IsFailure) return Result<CalculationPreferences>.Failure(diffuseShareCheck);

        var ventilationCheck = Guard.AgainstRange(iso52016DefaultAirChangesPerHour, 0, 20, "ISO 52016 default air changes per hour");
        if (ventilationCheck.IsFailure) return Result<CalculationPreferences>.Failure(ventilationCheck);

        return Result<CalculationPreferences>.Success(new CalculationPreferences(
            coolingSafetyFactor,
            heatingSafetyFactor,
            iso52016InternalHeatCapacityJPerM2K,
            iso52016SolarUtilizationFactor,
            iso52016WindowFrameAreaFraction,
            iso52016DirectSolarShadingReductionFactor,
            iso52016DiffuseSolarShareUnaffectedByShading,
            iso52016DefaultAirChangesPerHour));
    }

    public static CalculationPreferences Default() => new(
        coolingSafetyFactor: 1.10,
        heatingSafetyFactor: 1.15,
        iso52016InternalHeatCapacityJPerM2K: 10_000,
        iso52016SolarUtilizationFactor: 0.75,
        iso52016WindowFrameAreaFraction: 0.25,
        iso52016DirectSolarShadingReductionFactor: 1.0,
        iso52016DiffuseSolarShareUnaffectedByShading: 0.3,
        iso52016DefaultAirChangesPerHour: 0.5);
}
