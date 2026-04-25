using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Mappers;
using AssistantEngineer.Modules.Buildings.Domain.Ground;
using AssistantEngineer.SharedKernel.Abstractions;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Application.Services.Rooms;

public sealed class RoomGroundContactService
{
    private readonly IRoomRepository _rooms;
    private readonly IUnitOfWork _unitOfWork;

    public RoomGroundContactService(
        IRoomRepository rooms,
        IUnitOfWork unitOfWork)
    {
        _rooms = rooms;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RoomGroundContactResponse>> GetAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        var room = await _rooms.GetByIdAsync(roomId, cancellationToken);
        if (room is null)
            return Result<RoomGroundContactResponse>.NotFound($"Room with id {roomId} not found.");

        if (room.GroundContactMetadata is null)
            return Result<RoomGroundContactResponse>.NotFound("Ground-contact metadata is not set for this room.");

        return Result<RoomGroundContactResponse>.Success(
            room.GroundContactMetadata.ToResponse(room.Id, room.Name));
    }

    public async Task<Result<RoomGroundContactResponse>> UpsertAsync(
        int roomId,
        UpsertRoomGroundContactRequest request,
        CancellationToken cancellationToken = default)
    {
        var room = await _rooms.GetByIdAsync(roomId, cancellationToken);
        if (room is null)
            return Result<RoomGroundContactResponse>.NotFound($"Room with id {roomId} not found.");

        if (room.GroundContactMetadata is null)
        {
            var createResult = GroundContactMetadata.Create(
                request.ContactType.ToDomain(),
                request.ExposedPerimeterM,
                request.BurialDepthM,
                request.WallHeightBelowGradeM,
                request.HorizontalInsulationWidthM,
                request.PerimeterInsulationDepthM,
                request.UnderfloorVentilationAirChangesPerHour);

            if (createResult.IsFailure)
                return Result<RoomGroundContactResponse>.Failure(createResult.Error, createResult.ErrorType);

            var setResult = room.SetGroundContactMetadata(createResult.Value);
            if (setResult.IsFailure)
                return Result<RoomGroundContactResponse>.Failure(setResult.Error, setResult.ErrorType);
        }
        else
        {
            var updateResult = room.GroundContactMetadata.Update(
                request.ContactType.ToDomain(),
                request.ExposedPerimeterM,
                request.BurialDepthM,
                request.WallHeightBelowGradeM,
                request.HorizontalInsulationWidthM,
                request.PerimeterInsulationDepthM,
                request.UnderfloorVentilationAirChangesPerHour);

            if (updateResult.IsFailure)
                return Result<RoomGroundContactResponse>.Failure(updateResult.Error, updateResult.ErrorType);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<RoomGroundContactResponse>.Success(
            room.GroundContactMetadata!.ToResponse(room.Id, room.Name));
    }

    public async Task<Result> DeleteAsync(
        int roomId,
        CancellationToken cancellationToken = default)
    {
        var room = await _rooms.GetByIdAsync(roomId, cancellationToken);
        if (room is null)
            return Result.NotFound($"Room with id {roomId} not found.");

        var clearResult = room.ClearGroundContactMetadata();
        if (clearResult.IsFailure)
            return clearResult;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}