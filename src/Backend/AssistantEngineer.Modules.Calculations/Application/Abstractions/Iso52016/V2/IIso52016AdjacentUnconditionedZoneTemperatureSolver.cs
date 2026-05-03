using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2;

public interface IIso52016AdjacentUnconditionedZoneTemperatureSolver
{
    Result<Iso52016AdjacentUnconditionedZoneTemperatureResult> Solve(
        Iso52016AdjacentUnconditionedZoneTemperatureRequest request);
}