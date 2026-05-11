using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Physical;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Physical;

public sealed class Iso52016PhysicalRoomModelBuilder : ISo52016PhysicalRoomModelBuilder
{
    public Result<Iso52016MatrixHourlySolverRequest> Build(
        Iso52016PhysicalRoomModelRequest request)
    {
        var validation = Iso52016PhysicalRoomModelValidation.Validate(request);

        if (validation.IsFailure)
            return Result<Iso52016MatrixHourlySolverRequest>.Failure(validation);

        var hourlyInputProfile = request.HourlyInputProfile;
        var heatBalanceOptions = request.HeatBalanceOptions ?? new Iso52016RoomHeatBalanceOptions();
        var modelOptions = request.ModelOptions ?? new Iso52016PhysicalNodeModelOptions();
        var surfaces = (request.Surfaces ?? Array.Empty<Iso52016PhysicalSurface>()).ToArray();
        var surfaceBoundaryConditions = (request.SurfaceBoundaryConditions ?? Array.Empty<Iso52016PhysicalSurfaceHourlyBoundaryCondition>()).ToArray();
        var operationConditions = (request.OperationConditions ?? Array.Empty<Iso52016PhysicalHourlyOperationCondition>()).ToArray();
        var operationConditionsByHour = Iso52016PhysicalRoomModelMapping.BuildOperationConditionLookup(operationConditions);

        if (surfaces.Length == 0)
        {
            return Iso52016PhysicalThreeNodeRequestBuilder.Build(
                hourlyInputProfile,
                heatBalanceOptions,
                modelOptions,
                operationConditionsByHour);
        }

        return Iso52016PhysicalSurfaceExpandedRequestBuilder.Build(
            hourlyInputProfile,
            heatBalanceOptions,
            modelOptions,
            surfaces,
            surfaceBoundaryConditions,
            operationConditionsByHour);
    }
}
