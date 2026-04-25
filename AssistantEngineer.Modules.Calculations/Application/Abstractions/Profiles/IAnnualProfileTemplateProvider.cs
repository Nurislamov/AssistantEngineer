using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Models.Profiles;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;

public interface IAnnualProfileTemplateProvider
{
    DailyProfileTemplate GetTemplate(RoomType roomType, AnnualProfileKind profileKind);
}

public enum AnnualProfileKind
{
    Occupancy = 1,
    Equipment = 2,
    Lighting = 3,
    Dhw = 4
}