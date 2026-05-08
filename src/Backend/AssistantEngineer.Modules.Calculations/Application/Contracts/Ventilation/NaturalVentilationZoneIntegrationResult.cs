using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record NaturalVentilationZoneIntegrationResult(
    string CalculationId,
    IReadOnlyList<NaturalVentilationHourlyZoneResult> HourlyZones,
    IReadOnlyList<NaturalVentilationHourlyRoomResult> UnassignedRooms,
    IReadOnlyList<NaturalVentilationHourlyOpeningCalculationResult> UnassignedOpenings,
    IReadOnlyDictionary<string, IReadOnlyList<double>> ZoneAirflowCubicMetersPerHourProfiles,
    IReadOnlyDictionary<string, IReadOnlyList<double>> ZoneVentilationHeatTransferCoefficientProfilesWPerKelvin,
    IReadOnlyDictionary<string, IReadOnlyList<double>> ZoneSensibleVentilationLoadProfilesWatts,
    IReadOnlyDictionary<string, IReadOnlyList<double>> ZoneAirChangesPerHourProfiles,
    StandardCalculationDisclosure Disclosure,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
