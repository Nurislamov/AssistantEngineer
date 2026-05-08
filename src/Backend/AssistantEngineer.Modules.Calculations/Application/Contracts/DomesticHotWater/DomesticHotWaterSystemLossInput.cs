using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterSystemLossInput(
    string CalculationId,
    DomesticHotWaterUsefulDemandResult UsefulDemand,
    DomesticHotWaterStorageLossInput Storage,
    DomesticHotWaterDistributionLossInput Distribution,
    DomesticHotWaterCirculationLossInput Circulation,
    double? DefaultAmbientTemperatureCelsius,
    double? DefaultRecoverableFraction,
    StandardCalculationDisclosure? DisclosureOverride,
    string? Source);
