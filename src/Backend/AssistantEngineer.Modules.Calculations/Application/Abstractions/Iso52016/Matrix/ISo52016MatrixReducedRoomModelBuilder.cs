using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix;

public interface ISo52016MatrixReducedRoomModelBuilder
{
    Result<Iso52016MatrixHourlySolverRequest> Build(
        Iso52016MatrixReducedRoomModelRequest request);
}