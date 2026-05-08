using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterUsefulDemandInput(
    string CalculationId,
    string? BuildingId,
    string? ZoneId,
    string? RoomId,
    DomesticHotWaterDemandBasisInput Demand,
    DomesticHotWaterTemperatureModel TemperatureModel,
    DomesticHotWaterDrawProfileInput DrawProfile,
    double? WaterDensityKgPerLiter,
    double? WaterSpecificHeatJPerKgKelvin,
    StandardCalculationDisclosure? DisclosureOverride,
    string? Source);
