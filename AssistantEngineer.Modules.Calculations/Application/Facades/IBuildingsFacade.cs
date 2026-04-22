using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public interface IBuildingsFacade
{
    Task<Result<BuildingResponse>> CreateAsync(
        int projectId,
        CreateBuildingRequest request,
        CancellationToken cancellationToken);

    Task<Result<BuildingResponse>> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<Result<List<BuildingResponse>>> GetByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken);

    Task<Result<BuildingCalculationResult>> CalculateCoolingLoadAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken);

    Task<Result<BuildingHeatingLoadResult>> CalculateHeatingLoadAsync(
        int buildingId,
        HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken);

    Task<Result<BuildingEnergyBalanceResult>> CalculateEnergyBalanceAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto coolingMethod,
        HeatingLoadCalculationMethodDto heatingMethod,
        CancellationToken cancellationToken);
}
