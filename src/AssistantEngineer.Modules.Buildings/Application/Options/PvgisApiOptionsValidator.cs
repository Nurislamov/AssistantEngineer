using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Buildings.Application.Options;

public sealed class PvgisApiOptionsValidator : IValidateOptions<PvgisApiOptions>
{
    public ValidateOptionsResult Validate(string? name, PvgisApiOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            failures.Add("Buildings:Pvgis:BaseUrl is required.");
        }
        else if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri))
        {
            failures.Add("Buildings:Pvgis:BaseUrl must be an absolute URI.");
        }
        else
        {
            if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("Buildings:Pvgis:BaseUrl must use http or https.");
            }

            if (!string.IsNullOrWhiteSpace(uri.UserInfo))
                failures.Add("Buildings:Pvgis:BaseUrl must not embed credentials.");
        }

        RequireRange(options.TimeoutSeconds, 1, 300, "Buildings:Pvgis:TimeoutSeconds", failures);
        RequireRange(options.MaxRetryAttempts, 0, 5, "Buildings:Pvgis:MaxRetryAttempts", failures);
        RequireRange(options.InitialRetryDelayMilliseconds, 100, 60_000, "Buildings:Pvgis:InitialRetryDelayMilliseconds", failures);
        RequireRange(options.CircuitBreakerFailureThreshold, 1, 20, "Buildings:Pvgis:CircuitBreakerFailureThreshold", failures);
        RequireRange(options.CircuitBreakerBreakDurationSeconds, 1, 3_600, "Buildings:Pvgis:CircuitBreakerBreakDurationSeconds", failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void RequireRange(int value, int min, int max, string path, List<string> failures)
    {
        if (value < min || value > max)
            failures.Add($"{path} must be between {min} and {max}. Actual value: {value}.");
    }
}
