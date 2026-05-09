namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater.Iso12831;

public sealed record DomesticHotWaterDrawProfileTable(
    string ProfileId,
    Iso12831DomesticHotWaterDrawProfileKind DrawProfileKind,
    IReadOnlyList<double> WeekdayWeights24,
    IReadOnlyList<double> WeekendWeights24);
