using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Options;

public sealed class DomesticHotWaterOptionsValidator : IValidateOptions<DomesticHotWaterOptions>
{
    public ValidateOptionsResult Validate(string? name, DomesticHotWaterOptions options)
    {
        if (!Enum.IsDefined(options.DefaultUsageCategory))
        {
            return ValidateOptionsResult.Fail(
                "Calculations:DomesticHotWater:DefaultUsageCategory must be a defined Iso12831DomesticHotWaterUsageCategory value.");
        }

        if (!Enum.IsDefined(options.DefaultReferenceMode))
        {
            return ValidateOptionsResult.Fail(
                "Calculations:DomesticHotWater:DefaultReferenceMode must be a defined Iso12831DomesticHotWaterReferenceMode value.");
        }

        if (!Enum.IsDefined(options.DefaultDrawProfileKind))
        {
            return ValidateOptionsResult.Fail(
                "Calculations:DomesticHotWater:DefaultDrawProfileKind must be a defined Iso12831DomesticHotWaterDrawProfileKind value.");
        }

        return ValidateOptionsResult.Success;
    }
}
