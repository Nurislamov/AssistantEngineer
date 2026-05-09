using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;

namespace AssistantEngineer.Modules.Calculations.Application.Options;

public sealed class SystemEnergyOptions
{
    public bool UseEn15316InspiredChain { get; init; } = false;
    public bool UseEn15316CircuitLevelCalculator { get; init; } = false;

    public En15316GenerationTechnology DefaultHeatingTechnology { get; init; } =
        En15316GenerationTechnology.Boiler;

    public En15316GenerationTechnology DefaultCoolingTechnology { get; init; } =
        En15316GenerationTechnology.Chiller;

    public En15316GenerationTechnology DefaultDhwTechnology { get; init; } =
        En15316GenerationTechnology.Boiler;

    public En15316EnergyCarrier DefaultHeatingCarrier { get; init; } =
        En15316EnergyCarrier.NaturalGas;

    public En15316EnergyCarrier DefaultCoolingCarrier { get; init; } =
        En15316EnergyCarrier.Electricity;

    public En15316EnergyCarrier DefaultDhwCarrier { get; init; } =
        En15316EnergyCarrier.NaturalGas;
}
