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

        if (options.GroundTemperatureAmplitudeC < 0)
            errors.Add("Calculations:Iso13370GroundHeatTransfer:GroundTemperatureAmplitudeC must be non-negative.");

        if (options.VirtualGroundSeasonalAmplitudeC < 0)
            errors.Add("Calculations:Iso13370GroundHeatTransfer:VirtualGroundSeasonalAmplitudeC must be non-negative.");

        if (options.VirtualGroundEquivalentGroundThicknessM < 0)
            errors.Add("Calculations:Iso13370GroundHeatTransfer:VirtualGroundEquivalentGroundThicknessM must be non-negative.");

        if (options.VirtualGroundThermalBridgeLinearTransmittanceWPerMK < 0)
            errors.Add("Calculations:Iso13370GroundHeatTransfer:VirtualGroundThermalBridgeLinearTransmittanceWPerMK must be non-negative.");

        if (options.VirtualGroundSeasonalAttenuationFactor < 0 || options.VirtualGroundSeasonalAttenuationFactor > 1)
            errors.Add("Calculations:Iso13370GroundHeatTransfer:VirtualGroundSeasonalAttenuationFactor must be within [0,1].");

        if (options.VirtualGroundMonthlyHeatTransferVariationFactor < 0 || options.VirtualGroundMonthlyHeatTransferVariationFactor > 1)
            errors.Add("Calculations:Iso13370GroundHeatTransfer:VirtualGroundMonthlyHeatTransferVariationFactor must be within [0,1].");

        if (options.VirtualGroundMinimumEquivalentGroundThicknessM <= 0)
            errors.Add("Calculations:Iso13370GroundHeatTransfer:VirtualGroundMinimumEquivalentGroundThicknessM must be positive.");

        if (options.VirtualGroundMaximumEquivalentGroundThicknessM <= 0)
            errors.Add("Calculations:Iso13370GroundHeatTransfer:VirtualGroundMaximumEquivalentGroundThicknessM must be positive.");

        if (options.VirtualGroundMaximumEquivalentGroundThicknessM < options.VirtualGroundMinimumEquivalentGroundThicknessM)
            errors.Add("Calculations:Iso13370GroundHeatTransfer:VirtualGroundMaximumEquivalentGroundThicknessM must be >= VirtualGroundMinimumEquivalentGroundThicknessM.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
