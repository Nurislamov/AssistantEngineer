using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater.Iso12831;

namespace AssistantEngineer.Modules.Calculations.Application.Options;

public sealed class DomesticHotWaterOptions
{
    public bool UseIso12831InspiredCalculator { get; init; } = false;

    public Iso12831DomesticHotWaterUsageCategory DefaultUsageCategory { get; init; } =
        Iso12831DomesticHotWaterUsageCategory.ResidentialApartment;

    public Iso12831DomesticHotWaterReferenceMode DefaultReferenceMode { get; init; } =
        Iso12831DomesticHotWaterReferenceMode.PeopleBased;

    public Iso12831DomesticHotWaterDrawProfileKind DefaultDrawProfileKind { get; init; } =
        Iso12831DomesticHotWaterDrawProfileKind.ResidentialWeekdayWeekend;
}
