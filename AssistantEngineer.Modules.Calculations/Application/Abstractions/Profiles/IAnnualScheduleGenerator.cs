namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;

public interface IAnnualScheduleGenerator
{
    double[] Generate(
        int year,
        string countryCode,
        AssistantEngineer.Modules.Buildings.Domain.Enums.RoomType roomType,
        AnnualProfileKind profileKind);
}