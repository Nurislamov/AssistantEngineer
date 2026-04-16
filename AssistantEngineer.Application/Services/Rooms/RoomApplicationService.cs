using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Contracts.Requests;
using AssistantEngineer.Contracts.Responses;
using AssistantEngineer.Domain.Contracts.Calculations;
using AssistantEngineer.Domain.Models;
using AssistantEngineer.Domain.Services.Calculations;
using AssistantEngineer.Services.Calculations;

namespace AssistantEngineer.Application.Services.Rooms;

public class RoomApplicationService
{
    private readonly IAppDbContext _context;
    private readonly RoomCalculationService _roomCalculationService;

    public RoomApplicationService(
        IAppDbContext context,
        RoomCalculationService roomCalculationService)
    {
        _context = context;
        _roomCalculationService = roomCalculationService;
    }

    public async Task<List<RoomResponse>> GetAllAsync()
    {
        return _context.Rooms
            .Select(room => ToResponse(room))
            .ToList();
    }

    public async Task<RoomResponse?> GetByIdAsync(int id)
    {
        var room = _context.Rooms.FirstOrDefault(room => room.Id == id);
        return room == null ? null : ToResponse(room);
    }

    public async Task<RoomResponse?> CreateAsync(CreateRoomRequest request)
    {
        var floorExists = _context.Floors.Any(f => f.Id == request.FloorId);
        if (!floorExists)
            return null;

        var room = new Room
        {
            Name = request.Name,
            FloorId = request.FloorId,
            AreaM2 = request.AreaM2,
            HeightM = request.HeightM,
            VolumeM3 = request.AreaM2 * request.HeightM,
            IndoorTemperatureC = request.IndoorTemperatureC,
            OutdoorTemperatureC = request.OutdoorTemperatureC,
            PeopleCount = request.PeopleCount,
            EquipmentLoadW = request.EquipmentLoadW,
            LightingLoadW = request.LightingLoadW
        };

        _context.AddRoom(room);
        await _context.SaveChangesAsync();

        return ToResponse(room);
    }

    public async Task<RoomCalculationResult?> CalculateAsync(int id)
    {
        var room = _context.Rooms.FirstOrDefault(room => room.Id == id);

        if (room == null)
            return null;

        var windows = _context.Windows
            .Where(w => w.RoomId == id)
            .ToList();

        var walls = _context.Walls
            .Where(w => w.RoomId == id)
            .ToList();

        var result = _roomCalculationService.Calculate(room, windows, walls);
        room.DesignReserveFactor = result.DesignReserveFactor;
        room.DesignCapacityW = result.DesignCapacityW;
        room.DesignCapacityKw = result.DesignCapacityKw;

        await _context.SaveChangesAsync();

        return result;
    }

    public async Task<List<WindowResponse>?> GetWindowsAsync(int roomId)
    {
        var roomExists = _context.Rooms.Any(r => r.Id == roomId);
        if (!roomExists)
            return null;

        return _context.Windows
            .Where(w => w.RoomId == roomId)
            .Select(w => new WindowResponse
            {
                Id = w.Id,
                RoomId = w.RoomId,
                AreaM2 = w.AreaM2
            })
            .ToList();
    }

    public async Task<WindowResponse?> AddWindowAsync(int roomId, CreateWindowRequest request)
    {
        var roomExists = _context.Rooms.Any(r => r.Id == roomId);
        if (!roomExists)
            return null;

        var window = new Window
        {
            RoomId = roomId,
            AreaM2 = request.AreaM2
        };

        _context.AddWindow(window);
        await _context.SaveChangesAsync();

        return new WindowResponse
        {
            Id = window.Id,
            RoomId = window.RoomId,
            AreaM2 = window.AreaM2
        };
    }

    public async Task<List<WallResponse>?> GetWallsAsync(int roomId)
    {
        var roomExists = _context.Rooms.Any(r => r.Id == roomId);
        if (!roomExists)
            return null;

        return _context.Walls
            .Where(w => w.RoomId == roomId)
            .Select(w => new WallResponse
            {
                Id = w.Id,
                RoomId = w.RoomId,
                AreaM2 = w.AreaM2,
                IsExternal = w.IsExternal
            })
            .ToList();
    }

    public async Task<WallResponse?> AddWallAsync(int roomId, CreateWallRequest request)
    {
        var roomExists = _context.Rooms.Any(r => r.Id == roomId);
        if (!roomExists)
            return null;

        var wall = new Wall
        {
            RoomId = roomId,
            AreaM2 = request.AreaM2,
            IsExternal = request.IsExternal
        };

        _context.AddWall(wall);
        await _context.SaveChangesAsync();

        return new WallResponse
        {
            Id = wall.Id,
            RoomId = wall.RoomId,
            AreaM2 = wall.AreaM2,
            IsExternal = wall.IsExternal
        };
    }

    private static RoomResponse ToResponse(Room room)
    {
        return new RoomResponse
        {
            Id = room.Id,
            Name = room.Name,
            AreaM2 = room.AreaM2,
            HeightM = room.HeightM,
            VolumeM3 = room.VolumeM3,
            IndoorTemperatureC = room.IndoorTemperatureC,
            OutdoorTemperatureC = room.OutdoorTemperatureC,
            PeopleCount = room.PeopleCount,
            EquipmentLoadW = room.EquipmentLoadW,
            LightingLoadW = room.LightingLoadW,
            DesignReserveFactor = room.DesignReserveFactor,
            DesignCapacityW = room.DesignCapacityW,
            DesignCapacityKw = room.DesignCapacityKw,
            FloorId = room.FloorId
        };
    }
}
