using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Physical;

/// <summary>
/// Simulates an ISO 52016-inspired physical room model by building a Matrix request and running the existing Matrix hourly solver.
/// This is an adapter/service stage over the existing solver, not a new solver and not an external parity claim.
/// </summary>
public interface IIso52016PhysicalRoomEnergySimulationService
{
    Result<Iso52016PhysicalRoomEnergySimulationResult> Simulate(
        Iso52016PhysicalRoomModelRequest request);
}