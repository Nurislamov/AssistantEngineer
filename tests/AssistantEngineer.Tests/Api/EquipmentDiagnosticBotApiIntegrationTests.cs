using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using AssistantEngineer.Api;
using AssistantEngineer.Api.Controllers.Equipment;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AssistantEngineer.Tests.Api;

public sealed class EquipmentDiagnosticBotApiIntegrationTests
{
    private const string Endpoint = "/api/v1/equipment-diagnostics/bot/diagnose";

    [Fact]
    public async Task ExactRuntimeMatchReturnsSeedAnswerWithSafetyAndVerification()
    {
        await using var factory = new BotApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(Endpoint, new EquipmentDiagnosticBotRequest("Gree", "H5", Series: "GMV"));
        var result = await ReadSuccessAsync(response);

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.Answer, result.Status);
        Assert.NotNull(result.AnswerCard);
        Assert.NotNull(result.SourceCard);
        Assert.False(string.IsNullOrWhiteSpace(result.Title));
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
        Assert.False(string.IsNullOrWhiteSpace(result.SafetyCard.Boundary));
        Assert.True(result.VerificationRequired);
        Assert.True(result.IsSeedKnowledge);
        Assert.False(result.IsManualVerified);
    }

    [Fact]
    public async Task AmbiguousRuntimeCodeReturnsDeterministicClarificationOptions()
    {
        await using var factory = new BotApiFactory();
        var client = factory.CreateClient();
        var request = new EquipmentDiagnosticBotRequest("Gree", "E1");

        var first = await ReadSuccessAsync(await client.PostAsJsonAsync(Endpoint, request));
        var second = await ReadSuccessAsync(await client.PostAsJsonAsync(Endpoint, request));

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.ClarificationRequired, first.Status);
        Assert.NotNull(first.ClarificationQuestion);
        Assert.Null(first.AnswerCard);
        Assert.True(first.ClarificationQuestion.Options.Count >= 3);
        Assert.Equal(
            JsonSerializer.Serialize(first.ClarificationQuestion.Options),
            JsonSerializer.Serialize(second.ClarificationQuestion!.Options));
    }

    [Theory]
    [InlineData("A0")]
    [InlineData("n6")]
    [InlineData("qA")]
    [InlineData("db")]
    public async Task ReferencePatternsNeverReturnAnswer(string code)
    {
        await using var factory = new BotApiFactory();
        var client = factory.CreateClient();

        var result = await ReadSuccessAsync(
            await client.PostAsJsonAsync(Endpoint, new EquipmentDiagnosticBotRequest("Gree", code)));

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.ReferenceOnly, result.Status);
        Assert.Null(result.AnswerCard);
        Assert.Null(result.SourceCard);
    }

    [Fact]
    public async Task UnknownCodeReturnsSafeRuntimeNotFound()
    {
        await using var factory = new BotApiFactory();
        var client = factory.CreateClient();

        var result = await ReadSuccessAsync(
            await client.PostAsJsonAsync(Endpoint, new EquipmentDiagnosticBotRequest("Gree", "ZZ99")));

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.NotFound, result.Status);
        Assert.Null(result.AnswerCard);
        Assert.Null(result.SourceCard);
        Assert.Contains("No runtime diagnostic case found", result.Message, StringComparison.Ordinal);
        Assert.Contains(result.Warnings, warning => warning.Contains("not used as a final diagnosis", StringComparison.Ordinal));
    }

    [Fact]
    public async Task NullBodyReturnsValidationProblem()
    {
        await using var factory = new BotApiFactory();
        var client = factory.CreateClient();
        using var content = new StringContent("null", Encoding.UTF8, "application/json");

        var response = await client.PostAsync(Endpoint, content);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Validation failed", problem.Title);
        Assert.Contains(problem.Errors.Keys, key => string.Equals(key, "request", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EndpointResponsesDoNotExposeUnsafeOrNonRuntimeFinalDiagnosis()
    {
        await using var factory = new BotApiFactory();
        var client = factory.CreateClient();
        var responses = new[]
        {
            await client.PostAsJsonAsync(Endpoint, new EquipmentDiagnosticBotRequest("Gree", "H5", Series: "GMV")),
            await client.PostAsJsonAsync(Endpoint, new EquipmentDiagnosticBotRequest("Gree", "E1")),
            await client.PostAsJsonAsync(Endpoint, new EquipmentDiagnosticBotRequest("Gree", "ZZ99"))
        };
        var forbidden = new[]
        {
            "bypass", "disable protection", "disable protections", "force run",
            "short protection", "ignore protection", "DraftPreview"
        };

        foreach (var response in responses)
        {
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            Assert.All(forbidden, fragment => Assert.DoesNotContain(fragment, json, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void ControllerBotActionDependsOnFacadeAndContainsNoKnowledgeAccess()
    {
        var constructorParameters = Assert.Single(typeof(EquipmentDiagnosticsController).GetConstructors()).GetParameters();
        Assert.Contains(constructorParameters, parameter => parameter.ParameterType == typeof(IEquipmentDiagnosticBotFacade));

        var action = typeof(EquipmentDiagnosticsController).GetMethod(nameof(EquipmentDiagnosticsController.DiagnoseBotRequest));
        Assert.NotNull(action);
        Assert.Equal("bot/diagnose", Assert.Single(action.GetCustomAttributes<HttpPostAttribute>()).Template);

        var path = Path.Combine(
            TestPaths.RepoRoot,
            "src", "Backend", "AssistantEngineer.Api", "Controllers", "Equipment", "EquipmentDiagnosticsController.cs");
        var source = File.ReadAllText(path);
        Assert.DoesNotContain("Knowledge", source, StringComparison.Ordinal);
        Assert.DoesNotContain("manual-codebook", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("staging-candidate", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BotContractExamplesDeserializeToExpectedContracts()
    {
        var request = ReadExample<EquipmentDiagnosticBotRequest>("bot-diagnostic-request.example.json");
        Assert.Equal("Gree", request.Manufacturer);
        Assert.Equal("H5", request.Code);

        Assert.Equal(
            EquipmentDiagnosticBotResponseStatus.Answer,
            ReadExample<EquipmentDiagnosticBotResponse>("bot-diagnostic-answer.example.json").Status);
        Assert.Equal(
            EquipmentDiagnosticBotResponseStatus.ClarificationRequired,
            ReadExample<EquipmentDiagnosticBotResponse>("bot-diagnostic-clarification.example.json").Status);
        Assert.Equal(
            EquipmentDiagnosticBotResponseStatus.ReferenceOnly,
            ReadExample<EquipmentDiagnosticBotResponse>("bot-diagnostic-reference-only.example.json").Status);
        Assert.Equal(
            EquipmentDiagnosticBotResponseStatus.NotFound,
            ReadExample<EquipmentDiagnosticBotResponse>("bot-diagnostic-not-found.example.json").Status);
    }

    private static async Task<EquipmentDiagnosticBotResponse> ReadSuccessAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return Assert.IsType<EquipmentDiagnosticBotResponse>(
            await response.Content.ReadFromJsonAsync<EquipmentDiagnosticBotResponse>());
    }

    private static T ReadExample<T>(string fileName) =>
        Assert.IsType<T>(JsonSerializer.Deserialize<T>(
            File.ReadAllText(Path.Combine(
                TestPaths.RepoRoot,
                "docs", "equipment-diagnostics", "examples", fileName)),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }));

    private sealed class BotApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration(configuration =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=AssistantEngineerTests;Username=postgres",
                    ["EngineeringWorkflowPersistence:Provider"] = "InMemory",
                    ["EnergyPlus:UseDocker"] = "false",
                    ["EnergyPlus:ExecutablePath"] = "energyplus",
                    ["Authentication:ApiKey:Enabled"] = "false",
                    ["ApiHardening:RateLimiting:Enabled"] = "false"
                });
            });
        }
    }
}
