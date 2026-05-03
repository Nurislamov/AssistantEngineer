namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

/// <summary>
/// Conductive/convective coupling between two thermal nodes.
/// </summary>
public sealed record Iso52016V2ConductanceLink(
    string FromNodeId,
    string ToNodeId,
    double ConductanceWPerK);