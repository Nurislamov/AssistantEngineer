namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;

/// <summary>
/// Conductive/convective coupling between two thermal nodes.
/// </summary>
public sealed record Iso52016MatrixConductanceLink(
    string FromNodeId,
    string ToNodeId,
    double ConductanceWPerK);