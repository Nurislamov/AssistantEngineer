using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Services.Floors;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Mappers;
using AssistantEngineer.Modules.Calculations.Application.Services.Floors;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public sealed class FloorsFacade : IFloorsFacade
{
    private readonly FloorCommandService _command;
    private readonly FloorQueryService _query;
    private readonly FloorCalculationService _calculation;

    public FloorsFacade(
        FloorCommandService command,
        FloorQueryService query,
        FloorCalculationService calculation)
    {
        _command = command;
        _query = query;
        _calculation = calculation;
    }

    public Task<Result<FloorResponse>> CreateAsync(
        int buildingId,
        CreateFloorRequest request,
        CancellationToken cancellationToken) =>
        _command.CreateAsync(buildingId, request, cancellationToken);

    public Task<Result<List<FloorResponse>>> GetByBuildingIdAsync(
        int buildingId,
        CancellationToken cancellationToken) =>
        _query.GetByBuildingIdAsync(buildingId, cancellationToken);

    public Task<Result<FloorResponse>> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _query.GetByIdAsync(id, cancellationToken);

    public Task<Result<FloorCalculationResult>> CalculateCoolingLoadAsync(
        int id,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _calculation.CalculateAsync(id, method.ToDomain(), cancellationToken);
}
