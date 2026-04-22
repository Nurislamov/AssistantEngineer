using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Mappers;
using AssistantEngineer.Modules.Calculations.Application.Services.Buildings;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public sealed class BuildingsFacade : IBuildingsFacade
{
    private readonly BuildingCommandService _command;
    private readonly BuildingQueryService _query;
    private readonly BuildingCoolingLoadService _coolingLoadService;
    private readonly BuildingHeatingLoadService _heatingLoadService;
    private readonly BuildingEnergyBalanceService _energyBalanceService;

    public BuildingsFacade(
        BuildingCommandService command,
        BuildingQueryService query,
        BuildingCoolingLoadService coolingLoadService,
        BuildingHeatingLoadService heatingLoadService,
        BuildingEnergyBalanceService energyBalanceService)
    {
        _command = command;
        _query = query;
        _coolingLoadService = coolingLoadService;
        _heatingLoadService = heatingLoadService;
        _energyBalanceService = energyBalanceService;
    }

    public Task<Result<BuildingResponse>> CreateAsync(
        int projectId,
        CreateBuildingRequest request,
        CancellationToken cancellationToken) =>
        _command.CreateAsync(projectId, request, cancellationToken);

    public Task<Result<BuildingResponse>> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _query.GetByIdAsync(id, cancellationToken);

    public Task<Result<List<BuildingResponse>>> GetByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken) =>
        _query.GetByProjectIdAsync(projectId, cancellationToken);

    public Task<Result<BuildingCalculationResult>> CalculateCoolingLoadAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _coolingLoadService.CalculateAsync(buildingId, method.ToDomain(), cancellationToken);

    public Task<Result<BuildingHeatingLoadResult>> CalculateHeatingLoadAsync(
        int buildingId,
        HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _heatingLoadService.CalculateAsync(buildingId, method.ToDomain(), cancellationToken);

    public Task<Result<BuildingEnergyBalanceResult>> CalculateEnergyBalanceAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto coolingMethod,
        HeatingLoadCalculationMethodDto heatingMethod,
        CancellationToken cancellationToken) =>
        _energyBalanceService.CalculateAsync(
            buildingId,
            coolingMethod.ToDomain(),
            heatingMethod.ToDomain(),
            cancellationToken);
}
