using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Physical;

/// <summary>
/// Selects between the legacy-safe reduced Matrix model and the explicit ISO52016-inspired physical node model.
/// This is an adapter over existing builders and the existing Matrix solver; it is not a new solver.
/// </summary>
public interface ISo52016PhysicalModelSelectionService
{
    Result<Iso52016PhysicalModelSelectionResult> Simulate(
        Iso52016PhysicalModelSelectionRequest request);
}