using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using AssistantEngineer.Api;
using AssistantEngineer.Api.Controllers.Equipment;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;
using AssistantEngineer.Tests.EquipmentDiagnostics;
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

    [Theory]
    [InlineData(null, "H5")]
    [InlineData("Gree", null)]
    public async Task MissingRequiredIdentityReturnsValidationProblem(string? manufacturer, string? code)
    {
        await using var factory = new BotApiFactory();
        var response = await factory.CreateClient().PostAsJsonAsync(
            Endpoint,
            new EquipmentDiagnosticBotRequest(manufacturer, code, FreeText: "Free-text inference is not enabled."));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(await response.Content.ReadFromJsonAsync<ValidationProblemDetails>());
    }

    [Theory]
    [InlineData("manufacturer")]
    [InlineData("code")]
    [InlineData("freeText")]
    public async Task OverlongTextFieldsReturnValidationProblem(string field)
    {
        var request = field switch
        {
            "manufacturer" => new EquipmentDiagnosticBotRequest(new string('G', EquipmentDiagnosticBotRequestLimits.Manufacturer + 1), "H5"),
            "code" => new EquipmentDiagnosticBotRequest("Gree", new string('H', EquipmentDiagnosticBotRequestLimits.Code + 1)),
            _ => new EquipmentDiagnosticBotRequest("Gree", "H5", FreeText: new string('T', EquipmentDiagnosticBotRequestLimits.FreeText + 1))
        };
        await using var factory = new BotApiFactory();

        var response = await factory.CreateClient().PostAsJsonAsync(Endpoint, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TooManyMeasurementsReturnValidationProblem()
    {
        var measurements = Enumerable.Range(1, EquipmentDiagnosticBotRequestLimits.MeasurementCount + 1)
            .ToDictionary(index => $"m{index}", _ => "value");
        await using var factory = new BotApiFactory();

        var response = await factory.CreateClient().PostAsJsonAsync(
            Endpoint,
            new EquipmentDiagnosticBotRequest("Gree", "H5", OperatorProvidedMeasurements: measurements));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DisallowedControlCharactersReturnValidationProblem()
    {
        await using var factory = new BotApiFactory();

        var response = await factory.CreateClient().PostAsJsonAsync(
            Endpoint,
            new EquipmentDiagnosticBotRequest("Gree\u0000", "H5"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ValidMinimalTrimmedRequestReturnsAnswer()
    {
        await using var factory = new BotApiFactory();

        var result = await ReadSuccessAsync(await factory.CreateClient().PostAsJsonAsync(
            Endpoint,
            new EquipmentDiagnosticBotRequest(" Gree ", " H5 ", Series: " GMV ")));

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.Answer, result.Status);
        Assert.Equal("GREE", result.NormalizedManufacturer);
        Assert.Equal("H5", result.NormalizedCode);
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
            "short protection", "ignore protection", "DraftPreview",
            "artifacts/verification", "Knowledge/manual-codebook", "Knowledge/staging",
            "staging-candidate-preview", "D:\\", "C:\\", "/src/"
        };

        foreach (var response in responses)
        {
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            Assert.All(forbidden, fragment => Assert.DoesNotContain(fragment, json, StringComparison.OrdinalIgnoreCase));
            using var document = JsonDocument.Parse(json);
            Assert.DoesNotContain(EnumerateStrings(document.RootElement), value => value.Length > 1000);
        }
    }

    [Theory]
    [InlineData("gree-h5-answer")]
    [InlineData("gree-c5-clarification")]
    [InlineData("gree-a0-reference-only")]
    [InlineData("gree-unknown-not-found")]
    public async Task CoreFieldScenariosReturnHttp200WithExpectedSafeStatus(string scenarioId)
    {
        var scenario = BotScenarioTestCatalog.Get(scenarioId);
        await using var factory = new BotApiFactory();

        var response = await factory.CreateClient().PostAsJsonAsync(
            Endpoint,
            BotScenarioTestCatalog.ToRequest(scenario));
        var result = await ReadSuccessAsync(response);
        var json = JsonSerializer.Serialize(result);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(scenario.Expected.ResponseStatus, result.Status);
        Assert.All(BotScenarioTestCatalog.UnsafeFragments, fragment =>
            Assert.DoesNotContain(fragment, json, StringComparison.OrdinalIgnoreCase));
        Assert.All(BotScenarioTestCatalog.InternalArtifactFragments, fragment =>
            Assert.DoesNotContain(fragment, json, StringComparison.OrdinalIgnoreCase));
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

        var answer = ReadExample<EquipmentDiagnosticBotResponse>("bot-diagnostic-answer.example.json");
        var clarification = ReadExample<EquipmentDiagnosticBotResponse>("bot-diagnostic-clarification.example.json");
        var referenceOnly = ReadExample<EquipmentDiagnosticBotResponse>("bot-diagnostic-reference-only.example.json");
        var notFound = ReadExample<EquipmentDiagnosticBotResponse>("bot-diagnostic-not-found.example.json");

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.Answer, answer.Status);
        Assert.NotNull(answer.AnswerCard);
        Assert.NotNull(answer.SourceCard);
        Assert.NotNull(answer.SafetyCard);
        Assert.Equal(EquipmentDiagnosticBotResponseStatus.ClarificationRequired, clarification.Status);
        Assert.NotEmpty(clarification.ClarificationQuestion!.Options);
        Assert.Equal(EquipmentDiagnosticBotResponseStatus.ReferenceOnly, referenceOnly.Status);
        Assert.Null(referenceOnly.AnswerCard);
        Assert.Equal(EquipmentDiagnosticBotResponseStatus.NotFound, notFound.Status);
        Assert.Null(notFound.AnswerCard);
        Assert.NotEmpty(notFound.OperatorNextSteps);

        foreach (var response in new[] { answer, clarification, referenceOnly, notFound })
        {
            Assert.False(string.IsNullOrWhiteSpace(response.Title));
            Assert.False(string.IsNullOrWhiteSpace(response.Message));
            Assert.False(string.IsNullOrWhiteSpace(response.SafetyCard.Boundary));
            Assert.True(response.VerificationRequired);
        }
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

    private static IEnumerable<string> EnumerateStrings(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            yield return element.GetString() ?? string.Empty;
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                foreach (var value in EnumerateStrings(property.Value))
                    yield return value;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var value in EnumerateStrings(item))
                    yield return value;
            }
        }
    }

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
