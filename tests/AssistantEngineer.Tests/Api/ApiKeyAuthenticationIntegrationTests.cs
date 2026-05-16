using System.Net;
using AssistantEngineer.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AssistantEngineer.Tests.Api;

public class ApiKeyAuthenticationIntegrationTests
{
    private const string StatusUrl = "/api/v1/calculations/engineering-core/v1/status";
    private const string HeaderName = "X-AssistantEngineer-Api-Key";
    private const string ValidApiKey = "integration-test-api-key";

    [Fact]
    public async Task EnabledApiKeyAuthenticationRejectsMissingKey()
    {
        await using var factory = new ApiKeyAuthenticationFactory(enabled: true, apiKey: ValidApiKey);
        var client = factory.CreateClient();

        var response = await client.GetAsync(StatusUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EnabledApiKeyAuthenticationRejectsInvalidKey()
    {
        await using var factory = new ApiKeyAuthenticationFactory(enabled: true, apiKey: ValidApiKey);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, "wrong-api-key");

        var response = await client.GetAsync(StatusUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EnabledApiKeyAuthenticationAcceptsConfiguredKey()
    {
        await using var factory = new ApiKeyAuthenticationFactory(enabled: true, apiKey: ValidApiKey);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync(StatusUrl);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task DisabledApiKeyAuthenticationKeepsTestingEndpointsReachableWithoutHeader()
    {
        await using var factory = new ApiKeyAuthenticationFactory(enabled: false, apiKey: null);
        var client = factory.CreateClient();

        var response = await client.GetAsync(StatusUrl);

        response.EnsureSuccessStatusCode();
    }

    private sealed class ApiKeyAuthenticationFactory : WebApplicationFactory<Program>
    {
        private readonly bool _enabled;
        private readonly string? _apiKey;

        public ApiKeyAuthenticationFactory(bool enabled, string? apiKey)
        {
            _enabled = enabled;
            _apiKey = apiKey;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration(configuration =>
            {
                var values = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=AssistantEngineerTests;Username=postgres",
                    ["EngineeringWorkflowPersistence:Provider"] = "InMemory",
                    ["EnergyPlus:UseDocker"] = "false",
                    ["EnergyPlus:ExecutablePath"] = "energyplus",
                    ["Authentication:ApiKey:Enabled"] = _enabled ? "true" : "false",
                    ["Authentication:ApiKey:HeaderName"] = HeaderName,
                    ["Authentication:ApiKey:Key"] = _apiKey,
                    ["ApiAuthentication:Enabled"] = "true",
                    ["ApiAuthentication:AllowAnonymousInDevelopment"] = "false",
                    ["ApiAuthentication:ApiKeyHeaderName"] = HeaderName,
                    ["ApiAuthentication:EnableApiKeyAuthentication"] = "true",
                    ["ApiAuthentication:EnableJwtBearerAuthentication"] = "false"
                };

                configuration.AddInMemoryCollection(values);
            });
        }
    }
}
