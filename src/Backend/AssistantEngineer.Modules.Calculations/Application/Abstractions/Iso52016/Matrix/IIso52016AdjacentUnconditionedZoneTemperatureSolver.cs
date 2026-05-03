using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix;

public interface IIso52016AdjacentUnconditionedZoneTemperatureSolver
{
    Result<Iso52016AdjacentUnconditionedZoneTemperatureResult> Solve(
        Iso52016AdjacentUnconditionedZoneTemperatureRequest request);
}