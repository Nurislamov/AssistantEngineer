using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Physical;

/// <summary>
/// Builds an ISO 52016-inspired physical room node model on top of the existing Matrix hourly solver.
/// This is an internal engineering builder stage, not an external equivalence/validation claim.
/// </summary>
public interface ISo52016PhysicalRoomModelBuilder
{
    Result<Iso52016MatrixHourlySolverRequest> Build(
        Iso52016PhysicalRoomModelRequest request);
}