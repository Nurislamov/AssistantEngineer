using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Profiles;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public interface IProfilesFacade
{
    En16798RoomUsageProfileResponse GetRoomUsageProfile(
        RoomTypeDto roomType,
        En16798ProfileCategory category);

    AnnualProfileResponse GenerateAnnualProfile(
        AnnualProfileGenerationRequest request);
}