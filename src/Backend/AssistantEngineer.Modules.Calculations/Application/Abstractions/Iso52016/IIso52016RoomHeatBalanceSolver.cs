using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;

public interface IIso52016RoomHeatBalanceSolver
{
    Result<Iso52016RoomHeatBalanceProfile> Solve(
        Iso52016RoomHeatBalanceRequest request);
}