using AssistantEngineer.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AssistantEngineer.Tests.Api;

public class ApiCorsIntegrationTests
{
    private const string AllowedOrigin = "http://localhost:5173";
    private const string DisallowedOrigin = "https://example.org";

    [Fact]
    public async Task CorsPreflight_ForAllowedOrigin_ReturnsAccessControlAllowOriginHeader()
    {
        await using var factory = new CorsFactory();
        var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/engineering-workflow/validate");
        request.Headers.TryAddWithoutValidation("Origin", AllowedOrigin);
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "POST");
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", "Content-Type");

        var response = await client.SendAsync(request);

        Assert.True(
            response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values),
            "Expected Access-Control-Allow-Origin header for allowed origin.");
        Assert.Contains(AllowedOrigin, values);
    }

    [Fact]
    public async Task CorsPreflight_ForDisallowedOrigin_DoesNotReturnPermissiveOriginHeader()
    {
        await using var factory = new CorsFactory();
        var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/engineering-workflow/validate");
        request.Headers.TryAddWithoutValidation("Origin", DisallowedOrigin);
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "POST");
        request.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", "Content-Type");

        var response = await client.SendAsync(request);

        Assert.False(response.Headers.TryGetValues("Access-Control-Allow-Origin", out _));
    }

    private sealed class CorsFactory : WebApplicationFactory<Program>
    {
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
                    ["Authentication:ApiKey:Enabled"] = "false",
                    ["ApiHardening:Cors:Enabled"] = "true",
                    ["ApiHardening:Cors:PolicyName"] = "ApiCors",
                    ["ApiHardening:Cors:AllowedOrigins:0"] = AllowedOrigin,
                    ["ApiHardening:Cors:AllowedMethods:0"] = "GET",
                    ["ApiHardening:Cors:AllowedMethods:1"] = "POST",
                    ["ApiHardening:Cors:AllowedMethods:2"] = "OPTIONS",
                    ["ApiHardening:Cors:AllowedHeaders:0"] = "Content-Type",
                    ["ApiHardening:Cors:AllowedHeaders:1"] = "Authorization",
                    ["ApiHardening:Cors:AllowedHeaders:2"] = "X-AssistantEngineer-Api-Key",
                    ["ApiHardening:RateLimiting:Enabled"] = "false"
                };

                configuration.AddInMemoryCollection(values);
            });
        }
    }
}
