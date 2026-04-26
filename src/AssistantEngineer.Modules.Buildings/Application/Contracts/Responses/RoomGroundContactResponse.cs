using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;

public sealed class RoomGroundContactResponse
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public GroundContactTypeDto ContactType { get; set; }
    public double ExposedPerimeterM { get; set; }
    public double BurialDepthM { get; set; }
    public double WallHeightBelowGradeM { get; set; }
    public double HorizontalInsulationWidthM { get; set; }
    public double PerimeterInsulationDepthM { get; set; }
    public double UnderfloorVentilationAirChangesPerHour { get; set; }
}