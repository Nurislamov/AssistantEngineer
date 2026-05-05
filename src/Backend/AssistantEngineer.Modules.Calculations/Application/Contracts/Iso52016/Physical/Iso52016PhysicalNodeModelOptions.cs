namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;

/// <summary>
/// Options for the ISO52016-inspired physical room node-model stage.
/// The defaults intentionally preserve the Step 01 deterministic three-node fallback
/// and add deterministic defaults for explicit surface/construction, boundary profile,
/// and operation profile stages.
/// </summary>
public sealed record Iso52016PhysicalNodeModelOptions(
    string AirNodeId = "air",
    string InternalSurfaceNodeId = "internal-surface",
    string ThermalMassNodeId = "thermal-mass",
    string OutdoorBoundaryId = "outdoor",
    string GroundBoundaryId = "ground",
    string AdjacentBoundaryId = "adjacent-zone",
    string VentilationBoundaryId = "ventilation-air",
    double AirHeatCapacityFraction = 0.02,
    double InternalSurfaceHeatCapacityFraction = 0.08,
    double ThermalMassHeatCapacityFraction = 0.90,
    double OutdoorTransmissionConductanceFraction = 0.70,
    double GroundTransmissionConductanceFraction = 0.20,
    double AdjacentTransmissionConductanceFraction = 0.10,
    double AirToInternalSurfaceConductanceMultiplier = 2.0,
    double InternalSurfaceToThermalMassConductanceMultiplier = 3.0,
    double? AirToInternalSurfaceConductanceWPerK = null,
    double? InternalSurfaceToThermalMassConductanceWPerK = null,
    double InternalGainsConvectiveFraction = 0.50,
    double InternalRadiativeGainsToInternalSurfaceFraction = 0.70,
    double SolarGainsToInternalSurfaceFraction = 0.70,
    double AdjacentBoundaryTemperatureC = 20.0,
    double SurfaceNodeHeatCapacityFraction = 0.20,
    double DefaultSurfaceToAirConductanceWPerM2K = 3.0,
    double SurfaceToMassConductanceMultiplier = 2.0,
    double MinimumSurfaceNodeHeatCapacityJPerK = 1.0,
    double MinimumMassNodeHeatCapacityJPerK = 1.0,
    string AdjacentConditionedBoundaryId = "adjacent-conditioned-zone",
    string AdjacentUnconditionedBoundaryId = "adjacent-unconditioned-zone");
