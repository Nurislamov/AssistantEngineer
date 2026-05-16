using System.Net;
using AssistantEngineer.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AssistantEngineer.Tests.Api;

public sealed class ApiAuthenticationBoundaryIntegrationTests
{
    private const string StatusUrl = "/api/v1/calculations/engineering-core/v1/status";
    private const string HeaderName = "X-AssistantEngineer-Api-Key";
    private const string ValidApiKey = "boundary-test-api-key";

    [Fact]
    public async Task ApiAuthenticationDisabled_AllowsRequestWithoutApiKey()
    {
        await using var factory = new ApiAuthenticationBoundaryFactory(
            environmentName: "Testing",
            apiAuthenticationEnabled: false,
            allowAnonymousInDevelopment: false,
            apiKeyEnabled: true,
            apiKey: ValidApiKey);

        var client = factory.CreateClient();
        var response = await client.GetAsync(StatusUrl);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ApiAuthenticationEnabled_InvalidApiKey_ReturnsUnauthorized()
    {
        await using var factory = new ApiAuthenticationBoundaryFactory(
            environmentName: "Testing",
            apiAuthenticationEnabled: true,
            allowAnonymousInDevelopment: false,
            apiKeyEnabled: true,
            apiKey: ValidApiKey);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, "wrong-key");

        var response = await client.GetAsync(StatusUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApiAuthenticationEnabled_ValidApiKey_AllowsRequest()
    {
        await using var factory = new ApiAuthenticationBoundaryFactory(
            environmentName: "Testing",
            apiAuthenticationEnabled: true,
            allowAnonymousInDevelopment: false,
            apiKeyEnabled: true,
            apiKey: ValidApiKey);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync(StatusUrl);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task DevelopmentCompatibility_AllowsRequestWithoutApiKeyWhenConfigured()
    {
        await using var factory = new ApiAuthenticationBoundaryFactory(
            environmentName: "Development",
            apiAuthenticationEnabled: true,
            allowAnonymousInDevelopment: true,
            apiKeyEnabled: true,
            apiKey: ValidApiKey);

        var client = factory.CreateClient();
        var response = await client.GetAsync(StatusUrl);

        response.EnsureSuccessStatusCode();
    }

    private sealed class ApiAuthenticationBoundaryFactory : WebApplicationFactory<Program>
    {
        private readonly string _environmentName;
        private readonly bool _apiAuthenticationEnabled;
        private readonly bool _allowAnonymousInDevelopment;
        private readonly bool _apiKeyEnabled;
        private readonly string? _apiKey;

        public ApiAuthenticationBoundaryFactory(
            string environmentName,
            bool apiAuthenticationEnabled,
            bool allowAnonymousInDevelopment,
            bool apiKeyEnabled,
            string? apiKey)
        {
            _environmentName = environmentName;
            _apiAuthenticationEnabled = apiAuthenticationEnabled;
            _allowAnonymousInDevelopment = allowAnonymousInDevelopment;
            _apiKeyEnabled = apiKeyEnabled;
            _apiKey = apiKey;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(_environmentName);
            builder.ConfigureAppConfiguration(configuration =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=AssistantEngineerTests;Username=postgres",
                    ["EngineeringWorkflowPersistence:Provider"] = "InMemory",
                    ["EnergyPlus:UseDocker"] = "false",
                    ["EnergyPlus:ExecutablePath"] = "energyplus",
                    ["Authentication:ApiKey:Enabled"] = _apiKeyEnabled ? "true" : "false",
                    ["Authentication:ApiKey:HeaderName"] = HeaderName,
                    ["Authentication:ApiKey:Key"] = _apiKey,
                    ["ApiAuthentication:Enabled"] = _apiAuthenticationEnabled ? "true" : "false",
                    ["ApiAuthentication:AllowAnonymousInDevelopment"] = _allowAnonymousInDevelopment ? "true" : "false",
                    ["ApiAuthentication:ApiKeyHeaderName"] = HeaderName,
                    ["ApiAuthentication:EnableApiKeyAuthentication"] = "true",
                    ["ApiAuthentication:EnableJwtBearerAuthentication"] = "false"
                });
            });
        }
    }
}
