using AssistantEngineer.Modules.Buildings.Application.Mappers;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Mappers;
using AssistantEngineer.Modules.Calculations.Application.Services.Buildings;
using AssistantEngineer.Modules.Calculations.Application.Services.Floors;
using AssistantEngineer.Modules.Calculations.Application.Services.Rooms;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public sealed class LoadCalculationsFacade : ILoadCalculationsFacade
{
    private readonly BuildingCoolingLoadService _buildingCooling;
    private readonly BuildingHeatingLoadService _buildingHeating;
    private readonly BuildingEnergyBalanceService _buildingEnergyBalance;
    private readonly FloorCalculationService _floorCalculation;
    private readonly RoomCalculationService _roomCalculation;

    public LoadCalculationsFacade(
        BuildingCoolingLoadService buildingCooling,
        BuildingHeatingLoadService buildingHeating,
        BuildingEnergyBalanceService buildingEnergyBalance,
        FloorCalculationService floorCalculation,
        RoomCalculationService roomCalculation)
    {
        _buildingCooling = buildingCooling;
        _buildingHeating = buildingHeating;
        _buildingEnergyBalance = buildingEnergyBalance;
        _floorCalculation = floorCalculation;
        _roomCalculation = roomCalculation;
    }

    public Task<Result<BuildingCalculationResult>> CalculateBuildingCoolingLoadAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _buildingCooling.CalculateAsync(
            buildingId,
            method.ToDomain(),
            cancellationToken);

    public Task<Result<BuildingHeatingLoadResult>> CalculateBuildingHeatingLoadAsync(
        int buildingId,
        HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _buildingHeating.CalculateAsync(
            buildingId,
            method.ToDomain(),
            cancellationToken);

    public Task<Result<BuildingEnergyBalanceResult>> CalculateBuildingEnergyBalanceAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto coolingMethod,
        HeatingLoadCalculationMethodDto heatingMethod,
        CancellationToken cancellationToken) =>
        _buildingEnergyBalance.CalculateAsync(
            buildingId,
            coolingMethod.ToDomain(),
            heatingMethod.ToDomain(),
            cancellationToken);

    public Task<Result<FloorCalculationResult>> CalculateFloorCoolingLoadAsync(
        int floorId,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _floorCalculation.CalculateAsync(
            floorId,
            method.ToDomain(),
            cancellationToken);

    public Task<Result<RoomCalculationResult>> CalculateRoomCoolingLoadAsync(
        int roomId,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _roomCalculation.CalculateAsync(
            roomId,
            method.ToDomain(),
            cancellationToken);

    public Task<Result<RoomHeatingLoadResult>> CalculateRoomHeatingLoadAsync(
        int roomId,
        HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _roomCalculation.CalculateHeatingLoadAsync(
            roomId,
            method.ToDomain(),
            cancellationToken);
}