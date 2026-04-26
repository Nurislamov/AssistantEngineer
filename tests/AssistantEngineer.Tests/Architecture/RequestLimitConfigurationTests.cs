using AssistantEngineer.Api.Configuration;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Architecture;

public class RequestLimitConfigurationTests
{
    [Fact]
    public void RequestLimitOptionsHaveSafeDefaults()
    {
        var options = new RequestLimitOptions();

        Assert.Equal(1_048_576, options.MaxRequestBodyBytes);
        Assert.Equal(30, options.DefaultTimeoutSeconds);
        Assert.Equal(600, options.LongRunningTimeoutSeconds);
    }

    [Fact]
    public void RequestLimitOptionsValidatorAcceptsDefaultOptions()
    {
        var options = new RequestLimitOptions();

        var failures = RequestLimitOptionsValidator.ValidateOptions(
            options);

        Assert.Empty(failures);
    }

    [Fact]
    public void RequestLimitOptionsValidatorRejectsNonPositiveValues()
    {
        var options = new RequestLimitOptions
        {
            MaxRequestBodyBytes = 0,
            DefaultTimeoutSeconds = 0,
            LongRunningTimeoutSeconds = 0
        };

        var failures = RequestLimitOptionsValidator.ValidateOptions(
            options);

        Assert.Contains(
            failures,
            failure => failure.Contains(nameof(RequestLimitOptions.MaxRequestBodyBytes), StringComparison.Ordinal));

        Assert.Contains(
            failures,
            failure => failure.Contains(nameof(RequestLimitOptions.DefaultTimeoutSeconds), StringComparison.Ordinal));

        Assert.Contains(
            failures,
            failure => failure.Contains(nameof(RequestLimitOptions.LongRunningTimeoutSeconds), StringComparison.Ordinal));
    }

    [Fact]
    public void RequestLimitOptionsValidatorRejectsLongRunningTimeoutLowerThanDefaultTimeout()
    {
        var options = new RequestLimitOptions
        {
            MaxRequestBodyBytes = 1_048_576,
            DefaultTimeoutSeconds = 60,
            LongRunningTimeoutSeconds = 30
        };

        var failures = RequestLimitOptionsValidator.ValidateOptions(
            options);

        Assert.Contains(
            failures,
            failure => failure.Contains(
                $"{RequestLimitOptions.SectionName}:{nameof(RequestLimitOptions.LongRunningTimeoutSeconds)}",
                StringComparison.Ordinal));
    }

    [Fact]
    public void RequestLimitOptionsValidatorImplementsOptionsValidation()
    {
        var validator = new RequestLimitOptionsValidator();

        var result = validator.Validate(
            Options.DefaultName,
            new RequestLimitOptions());

        Assert.False(result.Failed);
    }
}