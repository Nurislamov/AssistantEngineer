using System.Text.Json;

namespace AssistantEngineer.Tests.Validation.ExternalReferenceValidation.BenchmarkFixtures;

internal static class EnergyBenchmarkFixtureMetadata
{
    public static readonly ISet<string> ReferenceTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "InternalDeterministic",
        "BenchmarkReference",
        "ExternalReference"
    };

    public static readonly ISet<string> Statuses = new HashSet<string>(StringComparer.Ordinal)
    {
        "Active",
        "Pending",
        "Disabled"
    };

    public static readonly ISet<string> Categories = new HashSet<string>(StringComparer.Ordinal)
    {
        "Transmission",
        "SolarGains",
        "VentilationInfiltration",
        "InternalGains",
        "RoomLoad",
        "Aggregation",
        "AnnualEnergyBalance",
        "Dhw",
        "SystemEnergy",
        "EquipmentSizing",
        "HourlyEnergyBalance",
        "SignedComponentBalance",
        "WindowSolarGains",
        "SurfaceIrradiance"
    };
}

internal sealed class EnergyBenchmarkFixture
{
    public string FixtureName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public EnergyBenchmarkFixtureInput Input { get; set; } = new();
    public JsonElement Expected { get; set; }
    public EnergyBenchmarkToleranceSet Tolerances { get; set; } = new();
    public List<string> Assumptions { get; set; } = [];
    public List<string> Notes { get; set; } = [];
}

internal sealed class EnergyBenchmarkFixtureInput
{
    public string CalculationBasis { get; set; } = string.Empty;
    public int BuildingId { get; set; } = 1;
    public string BuildingName { get; set; } = "Benchmark building";
    public double BuildingAreaM2 { get; set; }
    public int Year { get; set; } = 2026;
    public string HourlyPattern { get; set; } = "constant";
    public int HourlyRecordCount { get; set; }
    public string EnergyDataSource { get; set; } = "TrueHourlySimulation";
    public bool IsTrueHourly8760 { get; set; }
    public EnergyBenchmarkHourlyValues HourlyValues { get; set; } = new();
    public EnergyBenchmarkWindowSolarGainInput? WindowSolarGain { get; set; }
    public EnergyBenchmarkSurfaceIrradianceInput? SurfaceIrradiance { get; set; }
}

internal sealed class EnergyBenchmarkHourlyValues
{
    public double HeatingLoadW { get; set; }
    public double CoolingLoadW { get; set; }
    public double MechanicalVentilationW { get; set; }
    public double NaturalVentilationW { get; set; }
    public double TransmissionW { get; set; }
    public double VentilationW { get; set; }
    public double InfiltrationW { get; set; }
    public double SolarGainsW { get; set; }
    public double InternalGainsW { get; set; }
    public double GroundW { get; set; }
    public double HourDurationH { get; set; } = 1.0;
    public double TransmissionBalanceW { get; set; }
    public double MechanicalVentilationBalanceW { get; set; }
    public double NaturalVentilationBalanceW { get; set; }
    public double VentilationBalanceW { get; set; }
    public double InfiltrationBalanceW { get; set; }
    public double GroundBalanceW { get; set; }
}

internal sealed class EnergyBenchmarkWindowSolarGainInput
{
    public int WindowId { get; set; } = 1;
    public int RoomId { get; set; } = 1;
    public double AreaM2 { get; set; }
    public double OrientationAzimuthDeg { get; set; } = 180;
    public double TiltDeg { get; set; } = 90;
    public double? Shgc { get; set; }
    public double? FrameFactor { get; set; } = 1;
    public double InternalShadingFactor { get; set; } = 1;
    public double ExternalShadingFactor { get; set; } = 1;
    public double FixedShadingFactor { get; set; } = 1;
    public double? IncidentIrradianceWPerM2 { get; set; }
    public double? DirectIrradianceWPerM2 { get; set; }
    public double? DiffuseIrradianceWPerM2 { get; set; }
    public double? GroundReflectedIrradianceWPerM2 { get; set; }
    public bool IsNight { get; set; }
}

internal sealed class EnergyBenchmarkSurfaceIrradianceInput
{
    public double SolarAltitudeDeg { get; set; }
    public double SolarAzimuthDeg { get; set; } = 180;
    public double SurfaceAzimuthDeg { get; set; } = 180;
    public double SurfaceTiltDeg { get; set; } = 90;
    public double DirectNormalIrradianceWPerM2 { get; set; }
    public double DiffuseHorizontalIrradianceWPerM2 { get; set; }
    public double GlobalHorizontalIrradianceWPerM2 { get; set; }
    public double GroundReflectance { get; set; } = 0.2;
}
