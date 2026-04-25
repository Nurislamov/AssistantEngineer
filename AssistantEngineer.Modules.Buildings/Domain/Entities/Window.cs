using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Modules.Buildings.Domain.Entities;

public class Window
{
    public int Id { get; private set; }
    public Area Area { get; private set; } = null!;
    public ThermalTransmittance UValue { get; private set; } = null!;
    public SolarHeatGainCoefficient Shgc { get; private set; } = null!;
    public CardinalDirection Orientation { get; private set; }
    public WindowShadingParameters Shading { get; private set; } = WindowShadingParameters.None;

    public int RoomId { get; private set; }
    public Room Room { get; private set; } = null!;

    private Window() { }

    private Window(
        Area area, 
        ThermalTransmittance uValue, 
        SolarHeatGainCoefficient shgc, 
        CardinalDirection orientation,
        WindowShadingParameters shading,
        Room room)
    {
        Area = area;
        UValue = uValue;
        Shgc = shgc;
        Orientation = orientation;
        Shading = shading;
        Room = room;
        RoomId = room.Id;
    }

    public static Result<Window> Create(
        Area area,
        ThermalTransmittance uValue,
        SolarHeatGainCoefficient shgc,
        CardinalDirection orientation,
        Room room,
        WindowShadingParameters? shading = null)
    {
        return Result<Window>.Success(new Window(
            area,
            uValue,
            shgc,
            orientation,
            shading ?? WindowShadingParameters.None,
            room));
    }
    
    public Result Resize(Area newArea)
    {
        Area = newArea;
        return Result.Success();
    }
}
