using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations.Application.Options;

public sealed class Iso52016ConstructionOptionsValidator : IValidateOptions<Iso52016ConstructionOptions>
{
    public ValidateOptionsResult Validate(string? name, Iso52016ConstructionOptions options)
    {
        var failures = new List<string>();

        RequirePositive(
            options.DefaultInternalSurfaceResistanceM2KPerW,
            "Calculations:Iso52016Construction:DefaultInternalSurfaceResistanceM2KPerW",
            failures);
        RequireNonNegative(
            options.DefaultExternalSurfaceResistanceM2KPerW,
            "Calculations:Iso52016Construction:DefaultExternalSurfaceResistanceM2KPerW",
            failures);

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void RequirePositive(double value, string path, List<string> failures) =>
        RequireRange(value, double.Epsilon, double.MaxValue, path, failures);

    private static void RequireNonNegative(double value, string path, List<string> failures) =>
        RequireRange(value, 0, double.MaxValue, path, failures);

    private static void RequireRange(double value, double min, double max, string path, List<string> failures)
    {
        if (!double.IsFinite(value) || value < min || value > max)
            failures.Add($"{path} must be between {min} and {max}. Actual value: {value}.");
    }
}
