using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Api.Facades;

public interface IRoomsFacade
{
    Task<Result<RoomResponse>> CreateAsync(CreateRoomRequest request, CancellationToken cancellationToken);
    Task<Result<List<RoomResponse>>> GetAllAsync(CancellationToken cancellationToken);
    Task<Result<RoomResponse>> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<Result<RoomCalculationResult>> CalculateAsync(
        int id,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken);

    Task<Result<RoomHeatingLoadResult>> CalculateHeatingLoadAsync(
        int id,
        HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken);

    Task<Result<WindowResponse>> AddWindowAsync(
        int id,
        CreateWindowRequest request,
        CancellationToken cancellationToken);

    Task<Result<WallResponse>> AddWallAsync(
        int id,
        CreateWallRequest request,
        CancellationToken cancellationToken);

    Task<Result<EquipmentSelectionResult>> SelectEquipmentAsync(
        int id,
        EquipmentSelectionRequest request,
        CoolingLoadCalculationMethodDto method,
        CancellationToken cancellationToken);

    Task<Result<List<WindowResponse>>> GetWindowsAsync(int id, CancellationToken cancellationToken);
    Task<Result<List<WallResponse>>> GetWallsAsync(int id, CancellationToken cancellationToken);
}
