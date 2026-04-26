using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Options;

public sealed class Iso13370GroundHeatTransferOptionsValidator : IValidateOptions<Iso13370GroundHeatTransferOptions>
{
    public ValidateOptionsResult Validate(string? name, Iso13370GroundHeatTransferOptions options)
    {
        var errors = new List<string>();

        if (options.GroundConductivityWPerMK <= 0)
            errors.Add("Calculations:Iso13370GroundHeatTransfer:GroundConductivityWPerMK must be positive.");

        if (options.BaseCharacteristicDepthM <= 0)
            errors.Add("Calculations:Iso13370GroundHeatTransfer:BaseCharacteristicDepthM must be positive.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}