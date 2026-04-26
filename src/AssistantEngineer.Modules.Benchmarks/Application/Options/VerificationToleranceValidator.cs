using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Benchmarks.Application.Options;

internal sealed class VerificationToleranceValidator : IValidateOptions<VerificationTolerance>
{
    public ValidateOptionsResult Validate(string? name, VerificationTolerance options)
    {
        var failures = new List<string>();

        RequirePositive(options.RmseTolerance, "Verification:RmseTolerance", failures);
        RequirePositive(options.MaxAbsoluteErrorTolerance, "Verification:MaxAbsoluteErrorTolerance", failures);
        RequireRange(options.PeakLoadTolerancePercent, 0, 100, "Verification:PeakLoadTolerancePercent", failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void RequirePositive(double value, string path, List<string> failures)
    {
        if (!double.IsFinite(value) || value <= 0)
            failures.Add($"{path} must be positive. Actual value: {value}.");
    }

    private static void RequireRange(double value, double min, double max, string path, List<string> failures)
    {
        if (!double.IsFinite(value) || value < min || value > max)
            failures.Add($"{path} must be between {min} and {max}. Actual value: {value}.");
    }
}
