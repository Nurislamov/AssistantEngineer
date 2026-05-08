using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record NaturalVentilationControlEvaluationResult(
    IReadOnlyList<NaturalVentilationOpeningOperationResult> Operations,
    IReadOnlyDictionary<string, IReadOnlyList<double>> OpeningFractionProfilesByOpeningId,
    IReadOnlyDictionary<string, IReadOnlyList<double>> RoomOpeningFractionProfilesByRoomId,
    IReadOnlyDictionary<string, IReadOnlyList<double>> ZoneOpeningFractionProfilesByZoneId,
    StandardCalculationDisclosure Disclosure,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
