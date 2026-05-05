namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;

/// <summary>
/// Application-facing room model strategy selector.
/// ReducedMatrix remains the default legacy-safe path; PhysicalNodeModel must be selected explicitly.
/// </summary>
public enum Iso52016PhysicalModelSelectionStrategy
{
    ReducedMatrix = 0,
    PhysicalNodeModel = 1
}