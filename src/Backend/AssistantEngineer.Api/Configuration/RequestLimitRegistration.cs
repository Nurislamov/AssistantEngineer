using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Configuration;

internal static class RequestLimitRegistration
{
    public static WebApplicationBuilder ConfigureRequestLimits(
        this WebApplicationBuilder builder)
    {
        var options = BindAndValidateRequestLimitOptions(
            builder.Configuration);

        builder.Services.AddSingleton<IValidateOptions<RequestLimitOptions>, RequestLimitOptionsValidator>();

        builder.Services
            .AddOptions<RequestLimitOptions>()
            .Bind(builder.Configuration.GetSection(RequestLimitOptions.SectionName))
            .ValidateOnStart();

        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            kestrel.Limits.MaxRequestBodySize = options.MaxRequestBodyBytes;
        });

        builder.Services.AddRequestTimeouts(timeouts =>
        {
            timeouts.DefaultPolicy = new RequestTimeoutPolicy
            {
                Timeout = TimeSpan.FromSeconds(options.DefaultTimeoutSeconds),
                TimeoutStatusCode = StatusCodes.Status503ServiceUnavailable
            };

            timeouts.AddPolicy(
                RequestPolicies.LongRunning,
                new RequestTimeoutPolicy
                {
                    Timeout = TimeSpan.FromSeconds(options.LongRunningTimeoutSeconds),
                    TimeoutStatusCode = StatusCodes.Status503ServiceUnavailable
                });
        });

        return builder;
    }

    private static RequestLimitOptions BindAndValidateRequestLimitOptions(
        IConfiguration configuration)
    {
        var options = new RequestLimitOptions();

        configuration
            .GetSection(RequestLimitOptions.SectionName)
            .Bind(options);

        var failures = RequestLimitOptionsValidator.ValidateOptions(
            options);

        if (failures.Count > 0)
        {
            throw new OptionsValidationException(
                Options.DefaultName,
                typeof(RequestLimitOptions),
                failures);
        }

        return options;
    }
}