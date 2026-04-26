using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Services.Profiles;

namespace AssistantEngineer.Modules.Calculations.Application.Facades;

public sealed class ProfilesFacade : IProfilesFacade
{
    private readonly En16798ProfileService _profiles;
    private readonly AnnualProfileGenerationService _annualProfiles;

    public ProfilesFacade(
        En16798ProfileService profiles,
        AnnualProfileGenerationService annualProfiles)
    {
        _profiles = profiles;
        _annualProfiles = annualProfiles;
    }

    public En16798RoomUsageProfileResponse GetRoomUsageProfile(
        RoomTypeDto roomType,
        En16798ProfileCategory category) =>
        _profiles.GetRoomUsageProfile(roomType, category);

    public AnnualProfileResponse GenerateAnnualProfile(
        AnnualProfileGenerationRequest request) =>
        _annualProfiles.Generate(request);
}