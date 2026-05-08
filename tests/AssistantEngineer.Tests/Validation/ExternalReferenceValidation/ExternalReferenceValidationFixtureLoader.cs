using System.Text.Json;

namespace AssistantEngineer.Tests.Validation.ExternalReferenceValidation;

internal static class ExternalReferenceValidationFixtureLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static ExternalReferenceValidationFixture Load(string fixtureFileName)
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "tests",
            "AssistantEngineer.Tests",
            "Validation",
            "ExternalReferenceValidation",
            "Fixtures",
            fixtureFileName);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Energy calculation equivalence fixture '{fixtureFileName}' was not found.", path);

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ExternalReferenceValidationFixture>(json, JsonOptions) ??
               throw new InvalidOperationException($"Energy calculation equivalence fixture '{fixtureFileName}' could not be deserialized.");
    }
}

internal sealed class ExternalReferenceValidationFixture
{
    public string FixtureName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public List<string> Assumptions { get; set; } = [];
    public List<string> Notes { get; set; } = [];
    public EnergyCalculationFixtureInput Input { get; set; } = new();
    public EnergyCalculationFixtureExpected Expected { get; set; } = new();
    public EnergyCalculationExpectedHourlyResults ExpectedHourlyResults { get; set; } = new();
    public List<EnergyCalculationExpectedMonthlyResult> ExpectedMonthlyResults { get; set; } = [];
    public EnergyCalculationExpectedAnnualResults ExpectedAnnualResults { get; set; } = new();
    public EnergyCalculationTolerances Tolerances { get; set; } = new();
}

internal sealed class EnergyCalculationFixtureInput
{
    public string CalculationBasis { get; set; } = string.Empty;
    public EnergyCalculationEnvelopeInput? Envelope { get; set; }
    public TransmissionFixtureInput? Transmission { get; set; }
    public WindowSolarGainsFixtureInput? WindowSolarGains { get; set; }
    public VentilationInfiltrationFixtureInput? VentilationInfiltration { get; set; }
    public EnergyCalculationInternalGainsInput? InternalGains { get; set; }
    public InternalGainsFixtureInput? InternalGainCalculation { get; set; }
    public RoomLoadFixtureInput? RoomLoad { get; set; }
    public LoadAggregationFixtureInput? Aggregation { get; set; }
    public AnnualEnergyBalanceFixtureInput? AnnualEnergyBalance { get; set; }
    public DhwFixtureInput? Dhw { get; set; }
    public SystemEnergyFixtureInput? SystemEnergy { get; set; }
    public EquipmentSizingFixtureInput? EquipmentSizing { get; set; }
    public EnergyCalculationSolarInput? Solar { get; set; }
    public EnergyCalculationSimulationInput? Simulation { get; set; }
    public EnergyCalculationAnnualAggregationInput? AnnualAggregation { get; set; }
}

internal sealed class TransmissionFixtureInput
{
    public List<TransmissionFixtureElementInput> Elements { get; set; } = [];
}

internal sealed class WindowSolarGainsFixtureInput
{
    public int RoomId { get; set; }
    public List<WindowSolarGainFixtureWindowInput> Windows { get; set; } = [];
}

internal sealed class VentilationInfiltrationFixtureInput
{
    public int RoomId { get; set; }
    public double AreaM2 { get; set; }
    public double VolumeM3 { get; set; }
    public int OccupancyPeople { get; set; }
    public double IndoorTemperatureC { get; set; }
    public double OutdoorTemperatureC { get; set; }
    public double? MechanicalAirflowM3PerHour { get; set; }
    public double? AirflowLitersPerSecond { get; set; }
    public double? AirflowPerPersonLps { get; set; }
    public double? AirflowPerAreaLpsM2 { get; set; }
    public double? AirChangesPerHour { get; set; }
    public double? InfiltrationAirChangesPerHour { get; set; }
    public double? InfiltrationAirflowM3PerHour { get; set; }
    public double? NaturalVentilationAirflowM3PerHour { get; set; }
    public double? HeatRecoveryEfficiency { get; set; }
    public double ScheduleFactor { get; set; } = 1.0;
    public double? AirDensityKgPerM3 { get; set; }
    public double? AirSpecificHeatJPerKgK { get; set; }
    public string? DiagnosticsContext { get; set; }
}

internal sealed class WindowSolarGainFixtureWindowInput
{
    public int WindowId { get; set; }
    public int RoomId { get; set; }
    public double AreaM2 { get; set; }
    public double OrientationAzimuthDeg { get; set; }
    public double TiltDeg { get; set; }
    public double? Shgc { get; set; }
    public double? FrameFactor { get; set; }
    public double InternalShadingFactor { get; set; } = 1.0;
    public double ExternalShadingFactor { get; set; } = 1.0;
    public double FixedShadingFactor { get; set; } = 1.0;
    public double? IncidentIrradianceWPerM2 { get; set; }
    public double? DirectIrradianceWPerM2 { get; set; }
    public double? DiffuseIrradianceWPerM2 { get; set; }
    public double? GroundReflectedIrradianceWPerM2 { get; set; }
    public int? HourIndex { get; set; }
    public bool IsNight { get; set; }
    public string? DiagnosticsContext { get; set; }
}

internal sealed class TransmissionFixtureElementInput
{
    public int ElementId { get; set; }
    public string ElementType { get; set; } = string.Empty;
    public int RoomId { get; set; }
    public double AreaM2 { get; set; }
    public double UValueWPerM2K { get; set; }
    public double IndoorTemperatureC { get; set; }
    public double? OutdoorTemperatureC { get; set; }
    public double? BoundaryTemperatureC { get; set; }
    public double? AdjacentTemperatureC { get; set; }
    public double? GroundTemperatureC { get; set; }
    public string BoundaryType { get; set; } = string.Empty;
    public double? CorrectionFactor { get; set; }
    public string? DiagnosticsContext { get; set; }
}

internal sealed class EnergyCalculationFixtureExpected
{
    public double TotalHeatFlowW { get; set; }
    public double TotalHeatLossW { get; set; }
    public double TotalHeatGainW { get; set; }
    public double TotalRoomSolarGainW { get; set; }
    public double OccupancySensibleGainW { get; set; }
    public double LightingGainW { get; set; }
    public double EquipmentGainW { get; set; }
    public double ProcessSensibleGainW { get; set; }
    public double TotalSensibleGainW { get; set; }
    public double HeatingLoadW { get; set; }
    public double CoolingLoadW { get; set; }
    public double HeatingLoadWPerM2 { get; set; }
    public double CoolingLoadWPerM2 { get; set; }
    public double TotalAreaM2 { get; set; }
    public double DailyVolumeLiters { get; set; }
    public double DailyEnergyKWh { get; set; }
    public double AnnualEnergyKWh { get; set; }
    public double FinalHeatingEnergyKWh { get; set; }
    public double FinalCoolingEnergyKWh { get; set; }
    public double FinalDhwEnergyKWh { get; set; }
    public double TotalFinalEnergyKWh { get; set; }
    public double RequiredCoolingCapacityWithReserveW { get; set; }
    public double CoolingMarginW { get; set; }
    public double CoolingMarginPercent { get; set; }
    public bool HasErrors { get; set; }
    public bool HasAcceptedCandidate { get; set; }
    public bool HasRejectedCandidate { get; set; }
    public string ExpectedRejectReason { get; set; } = string.Empty;
    public List<string> ExpectedDiagnosticCodes { get; set; } = [];
    public VentilationInfiltrationFixtureExpected? VentilationInfiltration { get; set; }
    public List<TransmissionFixtureExpectedElement> Elements { get; set; } = [];
    public List<WindowSolarGainFixtureExpectedWindow> WindowSolarGains { get; set; } = [];
}

internal sealed class VentilationInfiltrationFixtureExpected
{
    public double DeltaTC { get; set; }
    public double MechanicalAirflowM3PerHour { get; set; }
    public double MechanicalAirflowM3PerSecond { get; set; }
    public double RawMechanicalHeatingLoadW { get; set; }
    public double RawMechanicalCoolingLoadW { get; set; }
    public double EffectiveMechanicalHeatingLoadW { get; set; }
    public double EffectiveMechanicalCoolingLoadW { get; set; }
    public double InfiltrationAirChangesPerHour { get; set; }
    public double InfiltrationAirflowM3PerHour { get; set; }
    public double InfiltrationAirflowM3PerSecond { get; set; }
    public double InfiltrationHeatingLoadW { get; set; }
    public double InfiltrationCoolingLoadW { get; set; }
    public double TotalHeatingLoadW { get; set; }
    public double TotalCoolingLoadW { get; set; }
    public bool HasErrors { get; set; }
    public List<string> ExpectedDiagnosticCodes { get; set; } = [];
}

internal sealed class WindowSolarGainFixtureExpectedWindow
{
    public int WindowId { get; set; }
    public double EffectiveSolarFactor { get; set; }
    public double SolarGainW { get; set; }
    public bool IsIncludedInLoad { get; set; } = true;
    public List<string> ExpectedDiagnosticCodes { get; set; } = [];
}

internal sealed class TransmissionFixtureExpectedElement
{
    public int ElementId { get; set; }
    public string ElementType { get; set; } = string.Empty;
    public string BoundaryType { get; set; } = string.Empty;
    public double AreaM2 { get; set; }
    public double UValueWPerM2K { get; set; }
    public double DeltaTC { get; set; }
    public double HeatFlowW { get; set; }
    public bool IsIncludedInLoad { get; set; }
    public List<string> ExpectedDiagnosticCodes { get; set; } = [];
}

internal sealed class EnergyCalculationEnvelopeInput
{
    public double TransmissionHeatTransferCoefficientWPerK { get; set; }
    public double VentilationHeatTransferCoefficientWPerK { get; set; }
    public double ThermalCapacityJPerK { get; set; }
}

internal sealed class EnergyCalculationInternalGainsInput
{
    public double ConstantInternalGainsW { get; set; }
}

internal sealed class InternalGainsFixtureInput
{
    public int RoomId { get; set; }
    public double? AreaM2 { get; set; }
    public int? OccupancyPeople { get; set; }
    public double? SensibleGainPerPersonW { get; set; }
    public double? LatentGainPerPersonW { get; set; }
    public double? LightingLoadW { get; set; }
    public double? LightingPowerDensityWPerM2 { get; set; }
    public double? EquipmentLoadW { get; set; }
    public double? EquipmentPowerDensityWPerM2 { get; set; }
    public double? ProcessSensibleGainW { get; set; }
    public double? ProcessLatentGainW { get; set; }
    public double? CustomSensibleGainW { get; set; }
    public double? CustomLatentGainW { get; set; }
    public double OccupancyScheduleFactor { get; set; } = 1.0;
    public double LightingScheduleFactor { get; set; } = 1.0;
    public double EquipmentScheduleFactor { get; set; } = 1.0;
    public double ProcessScheduleFactor { get; set; } = 1.0;
    public double CustomScheduleFactor { get; set; } = 1.0;
    public string? DiagnosticsContext { get; set; }
}

internal sealed class RoomLoadFixtureInput
{
    public int RoomId { get; set; }
    public string? RoomCode { get; set; }
    public string? RoomName { get; set; }
    public double AreaM2 { get; set; }
    public double VolumeM3 { get; set; }
    public double HeatingSetpointC { get; set; }
    public double CoolingSetpointC { get; set; }
    public double OutdoorDesignHeatingTemperatureC { get; set; }
    public double OutdoorDesignCoolingTemperatureC { get; set; }
    public RoomLoadFixedComponentsFixtureInput FixedComponents { get; set; } = new();
}

internal sealed class RoomLoadFixedComponentsFixtureInput
{
    public double HeatingTransmissionW { get; set; }
    public double HeatingWindowTransmissionW { get; set; }
    public double HeatingGroundW { get; set; }
    public double HeatingVentilationW { get; set; }
    public double HeatingInfiltrationW { get; set; }
    public double CoolingTransmissionW { get; set; }
    public double CoolingWindowTransmissionW { get; set; }
    public double CoolingGroundW { get; set; }
    public double CoolingVentilationW { get; set; }
    public double CoolingInfiltrationW { get; set; }
    public double CoolingSolarW { get; set; }
    public double CoolingInternalGainsW { get; set; }
}

internal sealed class LoadAggregationFixtureInput
{
    public int TargetId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public string Mode { get; set; } = "DesignPoint";
    public List<LoadAggregationFixtureRoomInput> Rooms { get; set; } = [];
}

internal sealed class LoadAggregationFixtureRoomInput
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public int? ThermalZoneId { get; set; }
    public int? FloorId { get; set; }
    public int BuildingId { get; set; }
    public double AreaM2 { get; set; }
    public double HeatingLoadW { get; set; }
    public double CoolingLoadW { get; set; }
}

internal sealed class AnnualEnergyBalanceFixtureInput
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public double BuildingAreaM2 { get; set; }
    public int Year { get; set; }
    public List<AnnualEnergyBalanceFixtureMonthInput> Months { get; set; } = [];
}

internal sealed class AnnualEnergyBalanceFixtureMonthInput
{
    public int Month { get; set; }
    public int Hours { get; set; }
    public double HeatingLoadW { get; set; }
    public double CoolingLoadW { get; set; }
}

internal sealed class DhwFixtureInput
{
    public int PeopleCount { get; set; }
    public double LitersPerPersonDay { get; set; }
    public double ColdWaterTemperatureC { get; set; }
    public double HotWaterTemperatureC { get; set; }
    public double DistributionLossFactor { get; set; }
}

internal sealed class SystemEnergyFixtureInput
{
    public double UsefulHeatingEnergyKWh { get; set; }
    public double UsefulCoolingEnergyKWh { get; set; }
    public double UsefulDhwEnergyKWh { get; set; }
    public double? HeatingEfficiency { get; set; }
    public double? CoolingCop { get; set; }
    public double? DhwEfficiency { get; set; }
    public double FanEnergyKWh { get; set; }
}

internal sealed class EquipmentSizingFixtureInput
{
    public int TargetId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public double RequiredHeatingLoadW { get; set; }
    public double RequiredCoolingLoadW { get; set; }
    public double? SafetyFactor { get; set; }
    public string? EquipmentType { get; set; }
    public List<EquipmentSizingFixtureCandidateInput> Candidates { get; set; } = [];
}

internal sealed class EquipmentSizingFixtureCandidateInput
{
    public int EquipmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string EquipmentType { get; set; } = string.Empty;
    public double? HeatingCapacityW { get; set; }
    public double? CoolingCapacityW { get; set; }
    public bool IsActive { get; set; } = true;
}

internal sealed class EnergyCalculationSolarInput
{
    public double ConstantSolarGainsW { get; set; }
    public EnergyCalculationSolarWindowInput? Window { get; set; }
    public List<EnergyCalculationSolarHourInput> HourlySouthSurfaceIrradiance { get; set; } = [];
}

internal sealed class EnergyCalculationSolarWindowInput
{
    public string Orientation { get; set; } = string.Empty;
    public double AreaM2 { get; set; }
    public double SolarHeatGainCoefficient { get; set; }
    public double FrameFraction { get; set; }
    public double ShadingFactor { get; set; }
}

internal sealed class EnergyCalculationSolarHourInput
{
    public int HourOfYear { get; set; }
    public double BeamIrradianceWm2 { get; set; }
    public double DiffuseSkyIrradianceWm2 { get; set; }
    public double GroundReflectedIrradianceWm2 { get; set; }

    public double TotalIrradianceWm2 =>
        BeamIrradianceWm2 + DiffuseSkyIrradianceWm2 + GroundReflectedIrradianceWm2;
}

internal sealed class EnergyCalculationSimulationInput
{
    public int HourCount { get; set; }
    public double OutdoorTemperatureC { get; set; }
    public double HeatingSetpointC { get; set; }
    public double CoolingSetpointC { get; set; }
    public double InitialIndoorTemperatureC { get; set; }
    public double TimeStepSeconds { get; set; } = 3600;
}

internal sealed class EnergyCalculationAnnualAggregationInput
{
    public string HourlyPattern { get; set; } = string.Empty;
    public List<EnergyCalculationAnnualAggregationMonthInput> Months { get; set; } = [];
}

internal sealed class EnergyCalculationAnnualAggregationMonthInput
{
    public int Month { get; set; }
    public int Hours { get; set; }
    public double HeatingLoadW { get; set; }
    public double CoolingLoadW { get; set; }
    public double InternalGainsW { get; set; }
    public double SolarGainsW { get; set; }
}

internal sealed class EnergyCalculationExpectedHourlyResults
{
    public int HourCount { get; set; }
    public List<EnergyCalculationExpectedHour> RepresentativeHours { get; set; } = [];
    public EnergyCalculationCompactHourlySummary? CompactSummary { get; set; }
}

internal sealed class EnergyCalculationCompactHourlySummary
{
    public string Pattern { get; set; } = string.Empty;
    public double PeakHeatingLoadW { get; set; }
    public double PeakCoolingLoadW { get; set; }
}

internal sealed class EnergyCalculationExpectedHour
{
    public int HourOfYear { get; set; }
    public double? HeatingLoadW { get; set; }
    public double? CoolingLoadW { get; set; }
    public double? SolarGainsW { get; set; }
    public double? BeamSolarGainW { get; set; }
    public double? DiffuseSkySolarGainW { get; set; }
    public double? GroundReflectedSolarGainW { get; set; }
    public double? InternalGainsW { get; set; }
    public double? IndoorTemperatureAfterHvacC { get; set; }
}

internal sealed class EnergyCalculationExpectedMonthlyResult
{
    public int Month { get; set; }
    public int Hours { get; set; }
    public double HeatingDemandKWh { get; set; }
    public double CoolingDemandKWh { get; set; }
    public double SolarGainsKWh { get; set; }
    public double InternalGainsKWh { get; set; }
    public double PeakHeatingLoadW { get; set; }
    public double PeakCoolingLoadW { get; set; }
}

internal sealed class EnergyCalculationExpectedAnnualResults
{
    public double HeatingDemandKWh { get; set; }
    public double CoolingDemandKWh { get; set; }
    public double SolarGainsKWh { get; set; }
    public double InternalGainsKWh { get; set; }
    public double? TransmissionHeatTransferCoefficientWPerK { get; set; }
    public double? VentilationHeatTransferCoefficientWPerK { get; set; }
    public double PeakHeatingLoadW { get; set; }
    public double PeakCoolingLoadW { get; set; }
    public int HourCount { get; set; }
}

internal sealed class EnergyCalculationTolerances
{
    public double HourlyTemperatureC { get; set; }
    public double HourlyLoadW { get; set; }
    public double MonthlyDemandKWh { get; set; }
    public double AnnualDemandKWh { get; set; }
    public double HeatTransferCoefficientWPerK { get; set; }
}

