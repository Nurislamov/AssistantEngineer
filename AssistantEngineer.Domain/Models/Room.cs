using AssistantEngineer.Domain.Primitives;
using AssistantEngineer.Domain.ValueObjects;
using AssistantEngineer.Domain.Models.Schedules;
using AssistantEngineer.Domain.Models.Ventilation;
using System.Collections.ObjectModel;

namespace AssistantEngineer.Domain.Models;

public class Room
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public Area Area { get; private set; } = null!;
    public double HeightM { get; private set; }

    public Temperature IndoorTemperature { get; private set; } = null!;
    public Temperature OutdoorTemperature { get; private set; } = null!;

    public int PeopleCount { get; private set; }
    public Power EquipmentLoad { get; private set; } = Power.FromWatts(0).Value;
    public Power LightingLoad { get; private set; } = Power.FromWatts(0).Value;

    public RoomType Type { get; private set; }

    private readonly List<Window> _windows = new();
    private readonly List<Wall> _walls = new();
    public double GetFloorUValue() => 0.3;
    public double GetCeilingUValue() => 0.2;
    public double GetFloorHeatCapacityKjPerM2K() => 50.0;
    public double GetCeilingHeatCapacityKjPerM2K() => 30.0;
    public IReadOnlyCollection<Window> Windows => new ReadOnlyCollection<Window>(_windows);
    public IReadOnlyCollection<Wall> Walls => new ReadOnlyCollection<Wall>(_walls);

    public int FloorId { get; private set; }
    public Floor Floor { get; private set; } = null!;
    
    public int? OccupancyScheduleId { get; private set; }
    public HourlySchedule? OccupancySchedule { get; private set; }
    public int? EquipmentScheduleId { get; private set; }
    public HourlySchedule? EquipmentSchedule { get; private set; }
    public int? LightingScheduleId { get; private set; }
    public HourlySchedule? LightingSchedule { get; private set; }
    public int? VentilationParametersId { get; private set; }
    public VentilationParameters? VentilationParameters { get; private set; }

    private Room() { }

    private Room(
        string name,
        Area area,
        double heightM,
        Temperature indoorTemp,
        Temperature outdoorTemp,
        Floor floor,
        int peopleCount,
        Power equipmentLoad,
        Power lightingLoad,
        RoomType type)
    {
        Name = name;
        Area = area;
        HeightM = heightM;
        IndoorTemperature = indoorTemp;
        OutdoorTemperature = outdoorTemp;
        Floor = floor;
        FloorId = floor.Id;
        PeopleCount = peopleCount;
        EquipmentLoad = equipmentLoad;
        LightingLoad = lightingLoad;
        Type = type;
    }

    public static Result<Room> Create(
        string name,
        Area area,
        double heightM,
        Temperature indoorTemp,
        Temperature outdoorTemp,
        Floor floor,
        int peopleCount = 0,
        Power? equipmentLoad = null,
        Power? lightingLoad = null,
        RoomType type = RoomType.Office)
    {
        var nameResult = name.ToRequiredTrimmed("Room name", maxLength: 100);
        if (nameResult.IsFailure) return Result<Room>.Failure(nameResult);

        var heightCheck = Guard.AgainstZeroOrNegative(heightM, "Height");
        if (heightCheck.IsFailure) return Result<Room>.Failure(heightCheck);

        var peopleCheck = Guard.AgainstNegative(peopleCount, "People count");
        if (peopleCheck.IsFailure) return Result<Room>.Failure(peopleCheck);

        var equip = equipmentLoad ?? Power.FromWatts(0).Value;
        var light = lightingLoad ?? Power.FromWatts(0).Value;

        return Result<Room>.Success(new Room(
            nameResult.Value, area, heightM, indoorTemp, outdoorTemp, floor,
            peopleCount, equip, light, type));
    }

    public double CalculateVolume() => Area.SquareMeters * HeightM;

    public Result<Window> AddWindow(
        Area area,
        ThermalTransmittance uValue,
        SolarHeatGainCoefficient shgc,
        CardinalDirection orientation,
        WindowShadingParameters? shading = null)
    {
        if (!HasExternalWallForWindow(orientation))
            return Result<Window>.Validation("Window orientation must match an existing external wall.");

        var window = Window.Create(area, uValue, shgc, orientation, this, shading);
        if (window.IsFailure)
            return Result<Window>.Failure(window);

        if (!IsWindowAreaValid(area))
            return Result<Window>.Validation("Total window area would exceed 80% of floor area.");

        _windows.Add(window.Value);
        return Result<Window>.Success(window.Value);
    }

    public Result RemoveWindow(int windowId)
    {
        var window = _windows.FirstOrDefault(w => w.Id == windowId);
        if (window == null)
            return Result.NotFound($"Window with id {windowId} not found.");

        _windows.Remove(window);
        return Result.Success();
    }

    public Result<Wall> AddWall(Area area, bool isExternal, ThermalTransmittance uValue, CardinalDirection orientation)
    {
        var wall = Wall.Create(area, isExternal, uValue, orientation, this);
        if (wall.IsFailure)
            return Result<Wall>.Failure(wall);

        _walls.Add(wall.Value);
        return Result<Wall>.Success(wall.Value);
    }

    public Result RemoveWall(int wallId)
    {
        var wall = _walls.FirstOrDefault(w => w.Id == wallId);
        if (wall == null)
            return Result.NotFound($"Wall with id {wallId} not found.");

        _walls.Remove(wall);
        return Result.Success();
    }

    public Result UpdateIndoorTemperature(Temperature newTemp)
    {
        IndoorTemperature = newTemp;
        return Result.Success();
    }

    public Result SetOccupancySchedule(HourlySchedule? schedule)
    {
        var validation = ValidateSchedule(schedule, "Occupancy schedule");
        if (validation.IsFailure)
            return validation;

        OccupancySchedule = schedule;
        OccupancyScheduleId = schedule?.Id;
        return Result.Success();
    }

    public Result SetEquipmentSchedule(HourlySchedule? schedule)
    {
        var validation = ValidateSchedule(schedule, "Equipment schedule");
        if (validation.IsFailure)
            return validation;

        EquipmentSchedule = schedule;
        EquipmentScheduleId = schedule?.Id;
        return Result.Success();
    }

    public Result SetLightingSchedule(HourlySchedule? schedule)
    {
        var validation = ValidateSchedule(schedule, "Lighting schedule");
        if (validation.IsFailure)
            return validation;

        LightingSchedule = schedule;
        LightingScheduleId = schedule?.Id;
        return Result.Success();
    }

    public Result SetVentilationParameters(VentilationParameters? ventilationParameters)
    {
        var validation = ValidateVentilationParameters(ventilationParameters);
        if (validation.IsFailure)
            return validation;

        VentilationParameters = ventilationParameters;
        VentilationParametersId = ventilationParameters?.Id;
        return Result.Success();
    }
    
    public double CalculateInternalHeatCapacityKjPerK()
    {
        var total = 0.0;
        
        foreach (var wall in Walls.Where(w => w.ConstructionAssembly != null))
        {
            total += wall.Area.SquareMeters * wall.ConstructionAssembly!.InternalHeatCapacityKjPerM2K;
        }
        
        total += Area.SquareMeters * GetFloorHeatCapacityKjPerM2K();
        total += Area.SquareMeters * GetCeilingHeatCapacityKjPerM2K();

        return total;
    }

    private bool IsWindowAreaValid(Area additionalArea)
    {
        var totalWindowArea = _windows.Sum(w => w.Area.SquareMeters) + additionalArea.SquareMeters;
        return totalWindowArea <= Area.SquareMeters * 0.8;
    }

    private bool HasExternalWallForWindow(CardinalDirection orientation) =>
        _walls.Any(wall => wall.IsExternal && IsSameFacade(wall.Orientation, orientation));

    private static bool IsSameFacade(CardinalDirection wallOrientation, CardinalDirection windowOrientation) =>
        NormalizeFacade(wallOrientation) == NormalizeFacade(windowOrientation);

    private static CardinalDirection NormalizeFacade(CardinalDirection orientation) =>
        orientation switch
        {
            CardinalDirection.North or CardinalDirection.NorthEast or CardinalDirection.NorthWest => CardinalDirection.North,
            CardinalDirection.East or CardinalDirection.SouthEast => CardinalDirection.East,
            CardinalDirection.West or CardinalDirection.SouthWest => CardinalDirection.West,
            _ => CardinalDirection.South
        };

    private static Result ValidateSchedule(HourlySchedule? schedule, string scheduleName)
    {
        if (schedule is null)
            return Result.Success();

        if (schedule.Factors.Count != 24)
            return Result.Validation($"{scheduleName} must contain exactly 24 factors.");

        if (schedule.Factors.Any(factor => factor is < 0 or > 1))
            return Result.Validation($"{scheduleName} factors must be between 0 and 1.");

        return Result.Success();
    }

    private static Result ValidateVentilationParameters(VentilationParameters? ventilationParameters)
    {
        if (ventilationParameters is null)
            return Result.Success();

        if (ventilationParameters.AirChangesPerHour < 0)
            return Result.Validation("Air changes per hour cannot be negative.");

        if (ventilationParameters.HeatRecoveryEfficiency is < 0 or > 1)
            return Result.Validation("Heat recovery efficiency must be between 0 and 1.");

        if (ventilationParameters.InfiltrationAirChangesPerHour < 0)
            return Result.Validation("Infiltration air changes per hour cannot be negative.");

        if (ventilationParameters.WindExposureFactor is < 0 or > 5)
            return Result.Validation("Wind exposure factor must be between 0 and 5.");

        if (ventilationParameters.StackCoefficient is < 0 or > 1)
            return Result.Validation("Stack coefficient must be between 0 and 1.");

        if (ventilationParameters.WindCoefficient is < 0 or > 1)
            return Result.Validation("Wind coefficient must be between 0 and 1.");

        return Result.Success();
    }
}
