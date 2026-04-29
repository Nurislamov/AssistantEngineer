using System.Text.Json;

namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity;

internal static class EnergyCalculationParityFixtureLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static EnergyCalculationParityFixture Load(string fixtureFileName)
    {
        var path = Path.Combine(
            TestPaths.RepoRoot,
            "tests",
            "AssistantEngineer.Tests",
            "Parity",
            "EnergyCalculationParity",
            "Fixtures",
            fixtureFileName);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Energy calculation parity fixture '{fixtureFileName}' was not found.", path);

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<EnergyCalculationParityFixture>(json, JsonOptions) ??
               throw new InvalidOperationException($"Energy calculation parity fixture '{fixtureFileName}' could not be deserialized.");
    }
}

internal sealed class EnergyCalculationParityFixture
{
    public string FixtureName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
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
