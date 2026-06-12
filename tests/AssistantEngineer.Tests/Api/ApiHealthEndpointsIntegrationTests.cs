using System.Net;
using AssistantEngineer.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AssistantEngineer.Tests.Api;

public class ApiHealthEndpointsIntegrationTests
{
    [Fact]
    public async Task HealthEndpoint_Returns200_InTesting()
    {
        await using var factory = new HealthFactory(apiKeyEnabled: false);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReadyEndpoint_Returns200_InTesting()
    {
        await using var factory = new HealthFactory(apiKeyEnabled: false);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthAndReadyEndpoints_AreAnonymous_WhenApiKeyAuthEnabled()
    {
        await using var factory = new HealthFactory(apiKeyEnabled: true);
        var client = factory.CreateClient();

        var health = await client.GetAsync("/health");
        var ready = await client.GetAsync("/ready");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    [Fact]
    public async Task HealthAndReadyResponsesDoNotExposeOperationalSecretsOrInternalPaths()
    {
        await using var factory = new HealthFactory(apiKeyEnabled: false);
        var client = factory.CreateClient();

        var health = await (await client.GetAsync("/health")).Content.ReadAsStringAsync();
        var ready = await (await client.GetAsync("/ready")).Content.ReadAsStringAsync();
        var responses = health + ready;

        Assert.DoesNotContain("health_test_bot_token", responses, StringComparison.Ordinal);
        Assert.DoesNotContain("health_test_webhook_secret", responses, StringComparison.Ordinal);
        Assert.DoesNotContain("123456789", responses, StringComparison.Ordinal);
        Assert.DoesNotContain("artifacts/verification", responses, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("manual-codebook", responses, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class HealthFactory : WebApplicationFactory<Program>
    {
        private readonly bool _apiKeyEnabled;

        public HealthFactory(bool apiKeyEnabled)
        {
            _apiKeyEnabled = apiKeyEnabled;
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
                    ["Authentication:ApiKey:Enabled"] = _apiKeyEnabled ? "true" : "false",
                    ["Authentication:ApiKey:HeaderName"] = "X-AssistantEngineer-Api-Key",
                    ["Authentication:ApiKey:Key"] = "health-tests-api-key",
                    ["ApiHardening:RateLimiting:Enabled"] = "false",
                    ["AssistantEngineer:EquipmentDiagnostics:Telegram:BotToken"] = "health_test_bot_token",
                    ["AssistantEngineer:EquipmentDiagnostics:Telegram:WebhookSecret"] = "health_test_webhook_secret",
                    ["AssistantEngineer:EquipmentDiagnostics:Telegram:AllowedChatIds:0"] = "123456789"
                };

                configuration.AddInMemoryCollection(values);
            });
        }
    }
}
