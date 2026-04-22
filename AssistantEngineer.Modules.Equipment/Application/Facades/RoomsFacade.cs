using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Services.Rooms;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Mappers;
using AssistantEngineer.Modules.Calculations.Application.Services.Rooms;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Application.Services;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Equipment.Application.Facades;

public sealed class RoomsFacade : IRoomsFacade
{
    private readonly RoomCommandService _command;
    private readonly RoomQueryService _query;
    private readonly RoomCalculationService _calculation;
    private readonly EquipmentSelectionService _equipmentSelectionService;

    public RoomsFacade(
        RoomCommandService command,
        RoomQueryService query,
        RoomCalculationService calculation,
        EquipmentSelectionService equipmentSelectionService)
    {
        _command = command;
        _query = query;
        _calculation = calculation;
        _equipmentSelectionService = equipmentSelectionService;
    }

    public Task<Result<RoomResponse>> CreateAsync(CreateRoomRequest request, CancellationToken cancellationToken) =>
        _command.CreateAsync(request, cancellationToken);

    public Task<Result<List<RoomResponse>>> GetAllAsync(CancellationToken cancellationToken) =>
        _query.GetAllAsync(cancellationToken);

    public Task<Result<RoomResponse>> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _query.GetByIdAsync(id, cancellationToken);

    public Task<Result<RoomCalculationResult>> CalculateCoolingLoadAsync(
        int id,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _calculation.CalculateAsync(id, method.ToDomain(), cancellationToken);

    public Task<Result<RoomHeatingLoadResult>> CalculateHeatingLoadAsync(
        int id,
        HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _calculation.CalculateHeatingLoadAsync(id, method.ToDomain(), cancellationToken);

    public Task<Result<WindowResponse>> AddWindowAsync(
        int id,
        CreateWindowRequest request,
        CancellationToken cancellationToken) =>
        _command.AddWindowAsync(id, request, cancellationToken);

    public Task<Result<WallResponse>> AddWallAsync(
        int id,
        CreateWallRequest request,
        CancellationToken cancellationToken) =>
        _command.AddWallAsync(id, request, cancellationToken);

    public Task<Result<EquipmentSelectionResult>> SelectEquipmentAsync(
        int id,
        EquipmentSelectionRequest request,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _equipmentSelectionService.SelectForRoomAsync(
            id,
            request,
            method.ToDomain(),
            cancellationToken);

    public Task<Result<List<WindowResponse>>> GetWindowsAsync(int id, CancellationToken cancellationToken) =>
        _query.GetWindowsAsync(id, cancellationToken);

    public Task<Result<List<WallResponse>>> GetWallsAsync(int id, CancellationToken cancellationToken) =>
        _query.GetWallsAsync(id, cancellationToken);
}
