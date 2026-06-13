using System.Net.Http.Json;
using AssistantEngineer.Api;
using AssistantEngineer.Api.Services.OperationalDiagnostics;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AssistantEngineer.Tests.Api;

public sealed class ApiCorrelationIdIntegrationTests
{
    private const string HeaderName = "X-Correlation-ID";

    [Fact]
    public async Task MissingCorrelationIdGeneratesSafeResponseHeader()
    {
        await using var factory = new CorrelationFactory();

        var response = await factory.CreateClient().GetAsync("/health");
        var value = Assert.Single(response.Headers.GetValues(HeaderName));

        Assert.True(OperationalCorrelationIdPolicy.IsValid(value, 128));
    }

    [Fact]
    public async Task ValidCorrelationIdIsEchoedAndInvalidValuesAreReplaced()
    {
        await using var factory = new CorrelationFactory();
        var client = factory.CreateClient();
        using var validRequest = new HttpRequestMessage(HttpMethod.Get, "/ready");
        validRequest.Headers.Add(HeaderName, "field-check_2026.06");
        using var invalidRequest = new HttpRequestMessage(HttpMethod.Get, "/health");
        invalidRequest.Headers.Add(HeaderName, new string('x', 129));

        var validResponse = await client.SendAsync(validRequest);
        var invalidResponse = await client.SendAsync(invalidRequest);

        Assert.Equal("field-check_2026.06", Assert.Single(validResponse.Headers.GetValues(HeaderName)));
        Assert.NotEqual(new string('x', 129), Assert.Single(invalidResponse.Headers.GetValues(HeaderName)));
    }

    [Fact]
    public async Task BotAndDisabledTelegramWebhookReturnCorrelationHeader()
    {
        await using var factory = new CorrelationFactory();
        var client = factory.CreateClient();

        var bot = await client.PostAsJsonAsync(
            "/api/v1/equipment-diagnostics/bot/diagnose",
            new EquipmentDiagnosticBotRequest("Gree", "H5", Series: "GMV"));
        var webhook = await client.PostAsJsonAsync(
            "/api/v1/equipment-diagnostics/telegram/webhook",
            new { update_id = 1 });

        Assert.True(bot.Headers.Contains(HeaderName));
        Assert.True(webhook.Headers.Contains(HeaderName));
    }

    [Fact]
    public void SafeRequestPathExcludesQueryString()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/equipment-diagnostics/error-codes";
        context.Request.QueryString = new QueryString("?manufacturer=secret-like-value");

        var safePath = OperationalCorrelationIdPolicy.GetSafeRequestPath(context.Request);

        Assert.Equal("/api/v1/equipment-diagnostics/error-codes", safePath);
        Assert.DoesNotContain("manufacturer", safePath, StringComparison.Ordinal);
    }

    private sealed class CorrelationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration(configuration =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=AssistantEngineerTests",
                    ["EngineeringWorkflowPersistence:Provider"] = "InMemory",
                    ["EnergyPlus:UseDocker"] = "false",
                    ["Authentication:ApiKey:Enabled"] = "false",
                    ["ApiHardening:RateLimiting:Enabled"] = "false"
                });
            });
        }
    }
}
