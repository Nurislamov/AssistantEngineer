namespace AssistantEngineer.Modules.Calculations.Application.Models.Profiles;

public sealed class RoomAnnualProfileSet
{
    public required double[] Occupancy { get; init; }
    public required double[] Equipment { get; init; }
    public required double[] Lighting { get; init; }
    public required double[] Dhw { get; init; }

    public int TotalHours => Occupancy.Length;
}