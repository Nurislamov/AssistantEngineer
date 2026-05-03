using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2;

public interface IIso52016V2HourlySolver
{
    Result<Iso52016V2HourlySolverProfile> Solve(
        Iso52016V2HourlySolverRequest request);
}