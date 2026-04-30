namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Aggregation;

public sealed record LoadAggregationInput(
    int TargetId,
    LoadAggregationTargetType TargetType,
    IReadOnlyList<AggregationRoomLoadInput> Rooms,
    LoadAggregationMode Mode = LoadAggregationMode.DesignPoint,
    string? TargetName = null,
    string? DiagnosticsContext = null);
