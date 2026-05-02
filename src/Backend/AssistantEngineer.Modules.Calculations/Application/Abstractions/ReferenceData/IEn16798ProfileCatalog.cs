using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;

public interface IEn16798ProfileCatalog
{
    En16798RoomUsageProfile GetProfile(RoomType roomType, En16798ProfileCategory category);
}