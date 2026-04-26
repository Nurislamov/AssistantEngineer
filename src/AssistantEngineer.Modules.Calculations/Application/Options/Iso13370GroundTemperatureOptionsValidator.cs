using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Options;

public sealed class Iso13370GroundTemperatureOptionsValidator : IValidateOptions<Iso13370GroundTemperatureOptions>
{
    public ValidateOptionsResult Validate(string? name, Iso13370GroundTemperatureOptions options)
    {
        var errors = new List<string>();

        if (options.AmplitudeAttenuationFactor < 0 || options.AmplitudeAttenuationFactor > 1)
            errors.Add("Calculations:Iso13370Ground:AmplitudeAttenuationFactor must be between 0 and 1.");

        if (options.PhaseShiftDays < 0 || options.PhaseShiftDays > 180)
            errors.Add("Calculations:Iso13370Ground:PhaseShiftDays must be between 0 and 180.");

        if (options.MinimumGroundTemperatureC > options.MaximumGroundTemperatureC)
            errors.Add("Calculations:Iso13370Ground:MinimumGroundTemperatureC must be less than or equal to MaximumGroundTemperatureC.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}