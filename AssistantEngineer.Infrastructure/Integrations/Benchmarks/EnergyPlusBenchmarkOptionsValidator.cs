using Microsoft.Extensions.Options;

namespace AssistantEngineer.Infrastructure.Integrations.Benchmarks;

public sealed class EnergyPlusBenchmarkOptionsValidator : IValidateOptions<EnergyPlusBenchmarkOptions>
{
    public ValidateOptionsResult Validate(string? name, EnergyPlusBenchmarkOptions options)
    {
        var failures = new List<string>();

        if (options.UseDocker)
        {
            if (string.IsNullOrWhiteSpace(options.DockerImage))
                failures.Add("EnergyPlus:DockerImage is required when EnergyPlus:UseDocker is true.");

            if (!string.IsNullOrWhiteSpace(options.DockerUri) &&
                (!Uri.TryCreate(options.DockerUri, UriKind.Absolute, out var dockerUri) ||
                 !IsSupportedDockerScheme(dockerUri.Scheme)))
            {
                failures.Add("EnergyPlus:DockerUri must be an absolute URI with tcp, http, https, unix or npipe scheme.");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(options.ExecutablePath))
            {
                failures.Add("EnergyPlus:ExecutablePath is required when EnergyPlus:UseDocker is false.");
            }
            else if (Path.IsPathRooted(options.ExecutablePath) && !File.Exists(options.ExecutablePath))
            {
                failures.Add($"EnergyPlus:ExecutablePath '{options.ExecutablePath}' does not exist.");
            }
        }

        RequireRange(options.MaxCapturedLogCharacters, 1, 1_048_576, "EnergyPlus:MaxCapturedLogCharacters", failures);
        RequireRange(options.ExecutionTimeoutSeconds, 1, 86_400, "EnergyPlus:ExecutionTimeoutSeconds", failures);
        RequireRange(options.MaxRetryAttempts, 0, 5, "EnergyPlus:MaxRetryAttempts", failures);
        RequireRange(options.InitialRetryDelayMilliseconds, 100, 60_000, "EnergyPlus:InitialRetryDelayMilliseconds", failures);
        RequireRange(options.CircuitBreakerFailureThreshold, 1, 20, "EnergyPlus:CircuitBreakerFailureThreshold", failures);
        RequireRange(options.CircuitBreakerBreakDurationSeconds, 1, 3_600, "EnergyPlus:CircuitBreakerBreakDurationSeconds", failures);

        if (!string.IsNullOrWhiteSpace(options.ArtifactRootDirectory))
        {
            try
            {
                _ = Path.GetFullPath(options.ArtifactRootDirectory);
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                failures.Add($"EnergyPlus:ArtifactRootDirectory is invalid: {exception.Message}");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsSupportedDockerScheme(string scheme) =>
        scheme.Equals("tcp", StringComparison.OrdinalIgnoreCase) ||
        scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
        scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
        scheme.Equals("unix", StringComparison.OrdinalIgnoreCase) ||
        scheme.Equals("npipe", StringComparison.OrdinalIgnoreCase);

    private static void RequireRange(int value, int min, int max, string path, List<string> failures)
    {
        if (value < min || value > max)
            failures.Add($"{path} must be between {min} and {max}. Actual value: {value}.");
    }
}
