using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Physical;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical;

public sealed class Iso52016PhysicalModelSelectionService : ISo52016PhysicalModelSelectionService
{
    private readonly ISo52016MatrixReducedRoomModelBuilder _reducedRoomModelBuilder;
    private readonly ISo52016PhysicalRoomModelBuilder _physicalRoomModelBuilder;
    private readonly ISo52016MatrixHourlySolver _matrixHourlySolver;

    public Iso52016PhysicalModelSelectionService(
        ISo52016MatrixReducedRoomModelBuilder reducedRoomModelBuilder,
        ISo52016PhysicalRoomModelBuilder physicalRoomModelBuilder,
        ISo52016MatrixHourlySolver matrixHourlySolver)
    {
        _reducedRoomModelBuilder = reducedRoomModelBuilder;
        _physicalRoomModelBuilder = physicalRoomModelBuilder;
        _matrixHourlySolver = matrixHourlySolver;
    }

    public Result<Iso52016PhysicalModelSelectionResult> Simulate(
        Iso52016PhysicalModelSelectionRequest request)
    {
        var validation = Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016PhysicalModelSelectionResult>.Failure(validation);

        var matrixRequestResult = BuildMatrixRequest(request);

        if (matrixRequestResult.IsFailure)
            return Result<Iso52016PhysicalModelSelectionResult>.Failure(matrixRequestResult);

        var matrixResult = _matrixHourlySolver.Solve(matrixRequestResult.Value);

        if (matrixResult.IsFailure)
            return Result<Iso52016PhysicalModelSelectionResult>.Failure(matrixResult);

        return Result<Iso52016PhysicalModelSelectionResult>.Success(
            new Iso52016PhysicalModelSelectionResult(
                ZoneCode: matrixRequestResult.Value.ZoneCode,
                Strategy: request.Strategy,
                MatrixSolverRequest: matrixRequestResult.Value,
                MatrixSolverProfile: matrixResult.Value));
    }

    private Result<Iso52016MatrixHourlySolverRequest> BuildMatrixRequest(
        Iso52016PhysicalModelSelectionRequest request) =>
        request.Strategy switch
        {
            Iso52016PhysicalModelSelectionStrategy.ReducedMatrix =>
                _reducedRoomModelBuilder.Build(
                    new Iso52016MatrixReducedRoomModelRequest(
                        HourlyInputProfile: request.HourlyInputProfile,
                        HeatBalanceOptions: request.HeatBalanceOptions)),

            Iso52016PhysicalModelSelectionStrategy.PhysicalNodeModel =>
                _physicalRoomModelBuilder.Build(
                    new Iso52016PhysicalRoomModelRequest(
                        HourlyInputProfile: request.HourlyInputProfile,
                        HeatBalanceOptions: request.HeatBalanceOptions,
                        ModelOptions: request.ModelOptions,
                        Surfaces: request.Surfaces,
                        SurfaceBoundaryConditions: request.SurfaceBoundaryConditions,
                        OperationConditions: request.OperationConditions)),

            _ => Result<Iso52016MatrixHourlySolverRequest>.Validation(
                $"ISO 52016 physical model selection strategy '{request.Strategy}' is not supported.")
        };

    private static Result Validate(
        Iso52016PhysicalModelSelectionRequest request)
    {
        if (request is null)
            return Result.Validation("ISO 52016 physical model selection request is required.");

        if (request.HourlyInputProfile is null)
            return Result.Validation("ISO 52016 physical model selection requires an hourly input profile.");

        if (!Enum.IsDefined(request.Strategy))
            return Result.Validation($"ISO 52016 physical model selection strategy '{request.Strategy}' is not supported.");

        return Result.Success();
    }
}