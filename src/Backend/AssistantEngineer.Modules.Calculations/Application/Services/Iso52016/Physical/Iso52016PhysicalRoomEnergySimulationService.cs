using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Physical;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical;

public sealed class Iso52016PhysicalRoomEnergySimulationService : IIso52016PhysicalRoomEnergySimulationService
{
    private readonly IIso52016PhysicalRoomModelBuilder _physicalRoomModelBuilder;
    private readonly IIso52016MatrixHourlySolver _matrixHourlySolver;

    public Iso52016PhysicalRoomEnergySimulationService(
        IIso52016PhysicalRoomModelBuilder physicalRoomModelBuilder,
        IIso52016MatrixHourlySolver matrixHourlySolver)
    {
        _physicalRoomModelBuilder = physicalRoomModelBuilder;
        _matrixHourlySolver = matrixHourlySolver;
    }

    public Result<Iso52016PhysicalRoomEnergySimulationResult> Simulate(
        Iso52016PhysicalRoomModelRequest request)
    {
        if (request is null)
            return Result<Iso52016PhysicalRoomEnergySimulationResult>.Validation("ISO 52016 physical room energy simulation request is required.");

        var matrixRequestResult = _physicalRoomModelBuilder.Build(request);

        if (matrixRequestResult.IsFailure)
            return Result<Iso52016PhysicalRoomEnergySimulationResult>.Failure(matrixRequestResult);

        var matrixProfileResult = _matrixHourlySolver.Solve(matrixRequestResult.Value);

        if (matrixProfileResult.IsFailure)
            return Result<Iso52016PhysicalRoomEnergySimulationResult>.Failure(matrixProfileResult);

        var roomCode = request.HourlyInputProfile.RoomCode.Trim();

        return Result<Iso52016PhysicalRoomEnergySimulationResult>.Success(
            new Iso52016PhysicalRoomEnergySimulationResult(
                RoomCode: roomCode,
                PhysicalModelRequest: request,
                MatrixSolverRequest: matrixRequestResult.Value,
                MatrixSolverProfile: matrixProfileResult.Value));
    }
}