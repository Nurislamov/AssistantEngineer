using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using System.Collections.ObjectModel;

namespace AssistantEngineer.Modules.Buildings.Domain.ThermalZones;

public class ThermalZone
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int BuildingId { get; private set; }
    public Building Building { get; private set; } = null!;

    private readonly List<ThermalZoneRoom> _rooms = new();
    public IReadOnlyCollection<ThermalZoneRoom> Rooms => new ReadOnlyCollection<ThermalZoneRoom>(_rooms);
    public IReadOnlyList<int> RoomIds => _rooms.Select(room => room.RoomId).ToArray();

    private ThermalZone() { }

    private ThermalZone(string name, IEnumerable<int> roomIds, Building? building = null)
    {
        Name = name;
        if (building is not null)
        {
            Building = building;
            BuildingId = building.Id;
        }

        foreach (var roomId in roomIds)
            _rooms.Add(ThermalZoneRoom.Create(roomId));
    }

    public static Result<ThermalZone> Create(string name, IEnumerable<int> roomIds, Building? building = null)
    {
        var nameResult = name.ToRequiredTrimmed("Thermal zone name");
        if (nameResult.IsFailure) return Result<ThermalZone>.Failure(nameResult);

        var ids = roomIds.Distinct().ToArray();
        if (ids.Length == 0)
            return Result<ThermalZone>.Validation("Thermal zone must contain at least one room.");

        if (ids.Any(id => id <= 0))
            return Result<ThermalZone>.Validation("Thermal zone room ids must be positive.");

        return Result<ThermalZone>.Success(new ThermalZone(nameResult.Value, ids, building));
    }
    
    public IReadOnlyCollection<Room> GetRooms()
    {
        return Building.Floors
            .SelectMany(f => f.Rooms)
            .Where(r => RoomIds.Contains(r.Id))
            .ToList();
    }
    
    public double GetTotalFloorArea()
    {
        return GetRooms().Sum(r => r.Area.SquareMeters);
    }
    
    public double GetTotalVolume()
    {
        return GetRooms().Sum(r => r.CalculateVolume());
    }
    
    public double GetTotalInternalHeatCapacity()
    {
        return GetRooms().Sum(r => r.CalculateInternalHeatCapacityKjPerK());
    }
    
    public double GetTotalHtrOp()
    {
        var total = 0.0;
        foreach (var room in GetRooms())
        {
            foreach (var wall in room.Walls.Where(w => w.IsExternal))
            {
                double u = wall.ConstructionAssembly?.UValueWPerM2K ?? wall.UValue.Value;
                total += wall.Area.SquareMeters * u;
            }
            
            foreach (var window in room.Windows)
            {
                total += window.Area.SquareMeters * window.UValue.Value;
            }
            
            total += room.Area.SquareMeters * room.GetFloorUValue();
            total += room.Area.SquareMeters * room.GetCeilingUValue();
        }
        return total;
    }
    
    public double GetTotalHve()
    {
        const double airHeatCapacity = 0.34; // Wh/(m³·K)
        double total = 0;
        foreach (var room in GetRooms())
        {
            double ach = room.VentilationParameters is null
                ? 0.5
                : room.VentilationParameters.AirChangesPerHour +
                room.VentilationParameters.InfiltrationAirChangesPerHour;
            total += airHeatCapacity * ach * room.CalculateVolume();
        }
        return total;
    }
}
