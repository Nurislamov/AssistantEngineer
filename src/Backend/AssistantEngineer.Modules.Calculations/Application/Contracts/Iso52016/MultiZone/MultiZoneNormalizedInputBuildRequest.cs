using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

public sealed record MultiZoneNormalizedInputBuildRequest(
    string BuildingId,
    IReadOnlyList<ThermalZoneDefinition> Zones,
    IReadOnlyList<MultiZoneZoneHourlyProfile> ZoneHourlyProfiles,
    IReadOnlyList<double> ExteriorTemperatureProfileCelsius,
    IReadOnlyDictionary<string, IReadOnlyList<double>>? GroundTemperatureProfilesByBoundaryId = null,
    IReadOnlyDictionary<string, IReadOnlyList<double>>? AdjacentUnconditionedTemperatureProfilesByBoundaryId = null,
    IReadOnlyDictionary<string, double>? AdjacentUnconditionedReductionFactorByBoundaryId = null,
    bool AllowUnknownExposure = false,
    bool TreatSameUseAdjacentAsAdiabatic = true,
    double SameUseAdjacentConductanceFactor = 0.0,
    double AdjacentUnconditionedFallbackExteriorWeight = 0.7,
    double AdjacentUnconditionedFallbackOffsetCelsius = 0.0,
    IReadOnlyList<string>? ClaimFlags = null,
    NaturalVentilationZoneIntegrationResult? NaturalVentilationZoneIntegration = null,
    IReadOnlyDictionary<string, IReadOnlyList<double>>? InfiltrationVentilationConductanceProfilesByZoneId = null,
    IReadOnlyDictionary<string, IReadOnlyList<double>>? MechanicalVentilationConductanceProfilesByZoneId = null,
    IReadOnlyDictionary<string, IReadOnlyList<double>>? CustomVentilationConductanceProfilesByZoneId = null,
    NaturalVentilationVentilationLaneMergeMode VentilationLaneMergeMode = NaturalVentilationVentilationLaneMergeMode.NoDoubleCountingMax);
