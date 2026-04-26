using Asp.Versioning;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Configuration;

internal sealed class ApiVersioningOptionsSetup : IConfigureOptions<ApiVersioningOptions>
{
    public void Configure(
        ApiVersioningOptions options)
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = false;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    }
}