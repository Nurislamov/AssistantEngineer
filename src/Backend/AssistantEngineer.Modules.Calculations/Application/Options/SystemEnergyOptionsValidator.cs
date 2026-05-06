using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Options;

public sealed class SystemEnergyOptionsValidator : IValidateOptions<SystemEnergyOptions>
{
    public ValidateOptionsResult Validate(string? name, SystemEnergyOptions options)
    {
        if (!Enum.IsDefined(options.DefaultHeatingTechnology))
        {
            return ValidateOptionsResult.Fail(
                "Calculations:SystemEnergy:DefaultHeatingTechnology must be a defined En15316GenerationTechnology value.");
        }

        if (!Enum.IsDefined(options.DefaultCoolingTechnology))
        {
            return ValidateOptionsResult.Fail(
                "Calculations:SystemEnergy:DefaultCoolingTechnology must be a defined En15316GenerationTechnology value.");
        }

        if (!Enum.IsDefined(options.DefaultDhwTechnology))
        {
            return ValidateOptionsResult.Fail(
                "Calculations:SystemEnergy:DefaultDhwTechnology must be a defined En15316GenerationTechnology value.");
        }

        if (!Enum.IsDefined(options.DefaultHeatingCarrier))
        {
            return ValidateOptionsResult.Fail(
                "Calculations:SystemEnergy:DefaultHeatingCarrier must be a defined En15316EnergyCarrier value.");
        }

        if (!Enum.IsDefined(options.DefaultCoolingCarrier))
        {
            return ValidateOptionsResult.Fail(
                "Calculations:SystemEnergy:DefaultCoolingCarrier must be a defined En15316EnergyCarrier value.");
        }

        if (!Enum.IsDefined(options.DefaultDhwCarrier))
        {
            return ValidateOptionsResult.Fail(
                "Calculations:SystemEnergy:DefaultDhwCarrier must be a defined En15316EnergyCarrier value.");
        }

        return ValidateOptionsResult.Success;
    }
}
