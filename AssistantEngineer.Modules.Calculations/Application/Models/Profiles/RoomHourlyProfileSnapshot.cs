namespace AssistantEngineer.Modules.Calculations.Application.Models.Profiles;

public sealed class RoomHourlyProfileSnapshot
{
    public double Occupancy { get; init; }
    public double Equipment { get; init; }
    public double Lighting { get; init; }
    public double Dhw { get; init; }
}