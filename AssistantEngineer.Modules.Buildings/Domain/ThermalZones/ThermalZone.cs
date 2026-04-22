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
    public IReadOnlyCollection<Room> AssignedRooms => _rooms.Select(room => room.Room).ToArray();
    public IReadOnlyList<int> RoomIds => _rooms
        .Select(room => room.Room.Id > 0 ? room.Room.Id : room.RoomId)
        .Where(roomId => roomId > 0)
        .ToArray();

    private ThermalZone() { }

    private ThermalZone(string name, IEnumerable<Room> rooms, Building? building = null)
    {
        Name = name;
        if (building is not null)
        {
            Building = building;
            BuildingId = building.Id;
        }

        foreach (var room in rooms)
            _rooms.Add(ThermalZoneRoom.Create(room));
    }

    public static Result<ThermalZone> Create(string name, IEnumerable<Room> rooms, Building? building = null)
    {
        var nameResult = name.ToRequiredTrimmed("Thermal zone name");
        if (nameResult.IsFailure) return Result<ThermalZone>.Failure(nameResult);

        var assignedRooms = rooms.Distinct().ToArray();
        if (assignedRooms.Length == 0)
            return Result<ThermalZone>.Validation("Thermal zone must contain at least one room.");

        if (assignedRooms.Any(room => room is null))
            return Result<ThermalZone>.Validation("Thermal zone rooms cannot contain null values.");

        return Result<ThermalZone>.Success(new ThermalZone(nameResult.Value, assignedRooms, building));
    }
    
    public IReadOnlyCollection<Room> GetRooms()
    {
        return AssignedRooms.ToList();
    }
    
    public double GetTotalFloorArea()
    {
        return GetRooms().Sum(r => r.Area.SquareMeters);
    }
    
    public double GetTotalVolume()
    {
        return GetRooms().Sum(r => r.CalculateVolume());
    }
    
    public double GetTotalInternalHeatCapacity(
        double floorHeatCapacityKjPerM2K,
        double ceilingHeatCapacityKjPerM2K)
    {
        return GetRooms().Sum(r => r.CalculateInternalHeatCapacityKjPerK(
            floorHeatCapacityKjPerM2K,
            ceilingHeatCapacityKjPerM2K));
    }
    
    public double GetTotalHtrOp(
        double floorUValueWPerM2K,
        double ceilingUValueWPerM2K)
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
            
            total += room.Area.SquareMeters * floorUValueWPerM2K;
            total += room.Area.SquareMeters * ceilingUValueWPerM2K;
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
