namespace AssistantEngineer.Modules.Calculations.Application.Models.Profiles;

public sealed class DailyProfileTemplate
{
    public required double[] WorkingDay { get; init; }
    public required double[] Weekend { get; init; }
    public required double[] Holiday { get; init; }
}