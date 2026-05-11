using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical;

internal static class Iso52016PhysicalRoomModelRequestFactory
{
    internal static Result<Iso52016MatrixHourlySolverRequest> CreateSuccessRequest(
        Iso52016RoomHourlyInputProfile hourlyInputProfile,
        Iso52016RoomHeatBalanceOptions heatBalanceOptions,
        string airNodeId,
        IReadOnlyList<Iso52016MatrixNodeDefinition> nodes,
        IReadOnlyList<Iso52016MatrixConductanceLink> internalConductances,
        IReadOnlyList<Iso52016MatrixBoundaryConductance> boundaryConductances,
        IReadOnlyList<Iso52016MatrixHourlyInputRecord> hours)
    {
        var solverOptions = new Iso52016MatrixHourlySolverOptions(
            TimeStepSeconds: heatBalanceOptions.TimeStepSeconds,
            AirNodeId: airNodeId,
            DefaultHeatingSetpointC: hourlyInputProfile.HeatingSetpointC,
            DefaultCoolingSetpointC: hourlyInputProfile.CoolingSetpointC);

        return Result<Iso52016MatrixHourlySolverRequest>.Success(
            new Iso52016MatrixHourlySolverRequest(
                ZoneCode: hourlyInputProfile.RoomCode.Trim(),
                Nodes: nodes,
                InternalConductances: internalConductances,
                BoundaryConductances: boundaryConductances,
                Hours: hours,
                Options: solverOptions));
    }
}
