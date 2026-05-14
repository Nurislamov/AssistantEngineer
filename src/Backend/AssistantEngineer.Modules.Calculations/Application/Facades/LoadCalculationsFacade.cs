using AssistantEngineer.Modules.Buildings.Application.Mappers;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Pipeline;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Contracts.EquipmentSizing;
using AssistantEngineer.Modules.Calculations.Application.Mappers;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public sealed class LoadCalculationsFacade : ILoadCalculationsFacade
{
    private readonly IEnergyCalculationPipeline _pipeline;

    public LoadCalculationsFacade(IEnergyCalculationPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public Task<Result<BuildingCalculationResult>> CalculateBuildingCoolingLoadAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _pipeline.CalculateBuildingCoolingLoadAsync(
            buildingId,
            method.ToDomain(),
            cancellationToken);

    public Task<Result<BuildingHeatingLoadResult>> CalculateBuildingHeatingLoadAsync(
        int buildingId,
        HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _pipeline.CalculateBuildingHeatingLoadAsync(
            buildingId,
            method.ToDomain(),
            cancellationToken);

    public Task<Result<BuildingEnergyBalanceResult>> CalculateBuildingEnergyBalanceAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto coolingMethod,
        HeatingLoadCalculationMethodDto heatingMethod,
        CancellationToken cancellationToken) =>
        _pipeline.CalculateBuildingEnergyBalanceAsync(
            buildingId,
            coolingMethod.ToDomain(),
            heatingMethod.ToDomain(),
            cancellationToken);

    public Task<Result<FloorCalculationResult>> CalculateFloorCoolingLoadAsync(
        int floorId,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _pipeline.CalculateFloorCoolingLoadAsync(
            floorId,
            method.ToDomain(),
            cancellationToken);

    public Task<Result<FloorCalculationResult>> CalculateFloorHeatingLoadAsync(
        int floorId,
        HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _pipeline.CalculateFloorHeatingLoadAsync(
            floorId,
            method.ToDomain(),
            cancellationToken);

    public Task<Result<RoomCalculationResult>> CalculateRoomCoolingLoadAsync(
        int roomId,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _pipeline.CalculateRoomCoolingLoadAsync(
            roomId,
            method.ToDomain(),
            cancellationToken);

    public Task<Result<RoomHeatingLoadResult>> CalculateRoomHeatingLoadAsync(
        int roomId,
        HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _pipeline.CalculateRoomHeatingLoadAsync(
            roomId,
            method.ToDomain(),
            cancellationToken);

    public Task<Result<EquipmentSizingResult>> CalculateRoomEquipmentSizingAsync(
        int roomId,
        string systemType,
        string unitType,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _pipeline.CalculateRoomEquipmentSizingAsync(
            roomId,
            systemType,
            unitType,
            method.ToDomain(),
            cancellationToken);
}
