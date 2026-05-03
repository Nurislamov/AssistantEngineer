namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

/// <summary>
/// Thermal node used by the ISO 52016 V2 implicit hourly matrix solver.
/// A node can represent zone air, an internal mass node, a surface layer node, or a reduced equivalent node.
/// </summary>
public sealed record Iso52016V2NodeDefinition(
    string NodeId,
    double HeatCapacityJPerK,
    double InitialTemperatureC,
    bool IsAirNode = false);