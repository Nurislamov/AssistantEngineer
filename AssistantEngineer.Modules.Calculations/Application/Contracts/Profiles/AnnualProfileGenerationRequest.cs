using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Profiles;

public sealed class AnnualProfileGenerationRequest
{
    public int Year { get; set; }
    public string CountryCode { get; set; } = "UZ";
    public RoomTypeDto RoomType { get; set; } = RoomTypeDto.Other;
    public AnnualProfileKindDto ProfileKind { get; set; } = AnnualProfileKindDto.Occupancy;
}