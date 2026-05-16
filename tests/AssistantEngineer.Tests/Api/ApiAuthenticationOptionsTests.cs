using AssistantEngineer.Api.Options.Security;

namespace AssistantEngineer.Tests.Api;

public sealed class ApiAuthenticationOptionsTests
{
    [Fact]
    public void Defaults_PreserveCompatibilityAndExpectedHeaderName()
    {
        var options = new ApiAuthenticationOptions();

        Assert.False(options.Enabled);
        Assert.True(options.AllowAnonymousInDevelopment);
        Assert.Equal("X-AssistantEngineer-Api-Key", options.ApiKeyHeaderName);
        Assert.True(options.EnableApiKeyAuthentication);
        Assert.False(options.EnableJwtBearerAuthentication);
    }
}
