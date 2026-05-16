using System.Net;
using AssistantEngineer.Api;
using AssistantEngineer.Modules.Identity.Application.Abstractions;
using AssistantEngineer.Modules.Identity.Application.Contracts.Audit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.Api;

public sealed class ApiAuthenticationAuditIntegrationTests
{
    private const string StatusUrl = "/api/v1/calculations/engineering-core/v1/status";
    private const string HeaderName = "X-AssistantEngineer-Api-Key";
    private const string ValidApiKey = "integration-test-api-key";

    [Fact]
    public async Task ValidApiKey_WritesAuthenticationSucceededAuditEvent()
    {
        await using var factory = new ApiAuthenticationAuditFactory(enabled: true, apiKey: ValidApiKey);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "corr-audit-valid");

        var response = await client.GetAsync(StatusUrl);

        response.EnsureSuccessStatusCode();
        var events = await QueryAuditEventsAsync(factory, "corr-audit-valid");
        Assert.Contains(events, entry => entry.EventType == AuditEventTypes.AuthenticationSucceeded);
    }

    [Fact]
    public async Task InvalidApiKey_WritesAuthenticationFailedAuditEvent()
    {
        await using var factory = new ApiAuthenticationAuditFactory(enabled: true, apiKey: ValidApiKey);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, "wrong-key");
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "corr-audit-invalid");

        var response = await client.GetAsync(StatusUrl);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var events = await QueryAuditEventsAsync(factory, "corr-audit-invalid");
        Assert.Contains(events, entry => entry.EventType == AuditEventTypes.AuthenticationFailed);
        Assert.DoesNotContain(events, entry => entry.Metadata?.ContainsKey("apiKey") == true);
    }

    private static async Task<IReadOnlyList<AuditEventRecord>> QueryAuditEventsAsync(
        WebApplicationFactory<Program> factory,
        string correlationId)
    {
        using var scope = factory.Services.CreateScope();
        var writer = scope.ServiceProvider.GetRequiredService<IAuditLogWriter>();
        var query = await writer.QueryByCorrelationIdAsync(correlationId);
        Assert.True(query.IsSuccess, query.Error);
        return query.Value;
    }

    private sealed class ApiAuthenticationAuditFactory : WebApplicationFactory<Program>
    {
        private readonly bool _enabled;
        private readonly string? _apiKey;

        public ApiAuthenticationAuditFactory(bool enabled, string? apiKey)
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
                    ["ApiAuthentication:EnableJwtBearerAuthentication"] = "false",
                    ["Identity:AuditLog:Enabled"] = "true",
                    ["Identity:AuditLog:Provider"] = "InMemory",
                    ["Identity:AuditLog:MaxMetadataValueLength"] = "512"
                };

                configuration.AddInMemoryCollection(values);
            });
        }
    }
}
