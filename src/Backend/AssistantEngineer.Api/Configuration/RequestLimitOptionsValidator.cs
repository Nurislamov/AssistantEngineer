using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Configuration;

internal sealed class RequestLimitOptionsValidator : IValidateOptions<RequestLimitOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        RequestLimitOptions options)
    {
        var failures = ValidateOptions(options);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    public static IReadOnlyList<string> ValidateOptions(
        RequestLimitOptions options)
    {
        var failures = new List<string>();

        if (options.MaxRequestBodyBytes <= 0)
        {
            failures.Add(
                $"{RequestLimitOptions.SectionName}:{nameof(RequestLimitOptions.MaxRequestBodyBytes)} must be greater than zero.");
        }

        if (options.DefaultTimeoutSeconds <= 0)
        {
            failures.Add(
                $"{RequestLimitOptions.SectionName}:{nameof(RequestLimitOptions.DefaultTimeoutSeconds)} must be greater than zero.");
        }

        if (options.LongRunningTimeoutSeconds <= 0)
        {
            failures.Add(
                $"{RequestLimitOptions.SectionName}:{nameof(RequestLimitOptions.LongRunningTimeoutSeconds)} must be greater than zero.");
        }

        if (options.LongRunningTimeoutSeconds < options.DefaultTimeoutSeconds)
        {
            failures.Add(
                $"{RequestLimitOptions.SectionName}:{nameof(RequestLimitOptions.LongRunningTimeoutSeconds)} must be greater than or equal to {nameof(RequestLimitOptions.DefaultTimeoutSeconds)}.");
        }

        return failures;
    }
}