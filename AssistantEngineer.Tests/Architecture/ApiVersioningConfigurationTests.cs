using AssistantEngineer.Api.Configuration;
using Asp.Versioning;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Architecture;

public class ApiVersioningConfigurationTests
{
    [Fact]
    public void ApiVersioningOptionsSetupConfiguresDefaultApiVersion()
    {
        var options = new ApiVersioningOptions();
        var setup = new ApiVersioningOptionsSetup();

        setup.Configure(options);

        Assert.Equal(new ApiVersion(1, 0), options.DefaultApiVersion);
        Assert.False(options.AssumeDefaultVersionWhenUnspecified);
        Assert.True(options.ReportApiVersions);
        Assert.IsType<UrlSegmentApiVersionReader>(options.ApiVersionReader);
    }

    [Fact]
    public void ApiVersioningOptionsSetupImplementsOptionsConfiguration()
    {
        Assert.True(
            typeof(IConfigureOptions<ApiVersioningOptions>).IsAssignableFrom(typeof(ApiVersioningOptionsSetup)));
    }
}