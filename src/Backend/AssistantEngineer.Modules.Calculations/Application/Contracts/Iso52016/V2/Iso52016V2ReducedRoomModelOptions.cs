namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

/// <summary>
/// Options for the reduced room model adapter that bridges the existing ISO 52016 room input profile
/// to the V2 node/matrix hourly solver. This is intentionally conservative: one air node, one outdoor boundary.
/// More detailed surface/mass node generation can be added once room envelope layers are available at this level.
/// </summary>
public sealed record Iso52016V2ReducedRoomModelOptions(
    string AirNodeId = "air",
    string OutdoorBoundaryId = "outdoor");