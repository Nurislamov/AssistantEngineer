using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed record BotScenarioDefinition(
    string ScenarioId,
    string Title,
    string Description,
    BotScenarioRequest Request,
    BotScenarioExpected Expected);

public sealed record BotScenarioRequest(
    string? Manufacturer,
    string? Code,
    string? Series,
    string? ModelCode,
    EquipmentDiagnosticBotEquipmentSide? EquipmentSide,
    EquipmentDiagnosticBotDisplayContext? DisplayContext,
    string? FreeText);

public sealed record BotScenarioExpected(
    EquipmentDiagnosticBotResponseStatus ResponseStatus,
    bool? VerificationRequired,
    bool RequiresSafetyBoundary,
    bool RequiresSourceOrProvenance,
    IReadOnlyList<string> MustContainText,
    IReadOnlyList<string> MustNotContainText,
    bool MustNotExposeInternalArtifacts,
    bool MustNotExposeStagingOrCodebook,
    bool MustNotUseUnsafeWording,
    int? ClarificationOptionsMinimum,
    BotScenarioUiExpectation UiExpectation);

public sealed record BotScenarioUiExpectation(
    bool ShowAnswerCard,
    bool ShowClarificationOptions,
    bool ShowReferenceOnlyNotice,
    bool ShowNotFoundFallback,
    bool ShowVerificationBanner,
    bool ShowSafetyCard);

public static class BotScenarioTestCatalog
{
    public static readonly string[] UnsafeFragments =
    [
        "bypass", "disable protection", "disable protections", "force run",
        "short protection", "ignore protection"
    ];

    public static readonly string[] InternalArtifactFragments =
    [
        "artifacts/verification", "Knowledge/staging", "Knowledge/manual-codebook",
        "staging-candidate-preview", "manual-codebook", "C:\\", "D:\\", "/src/", ".pdf"
    ];

    public static IReadOnlyList<BotScenarioDefinition> LoadAndValidate()
    {
        var files = Directory.GetFiles(ScenarioDirectory, "*.scenario.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(8, files.Length);

        var scenarios = files.Select(Load).ToArray();
        Assert.Equal(scenarios.Length, scenarios.Select(item => item.ScenarioId).Distinct(StringComparer.Ordinal).Count());
        return scenarios;
    }

    public static BotScenarioDefinition Get(string scenarioId) =>
        Assert.Single(LoadAndValidate(), scenario => scenario.ScenarioId == scenarioId);

    public static EquipmentDiagnosticBotRequest ToRequest(BotScenarioDefinition scenario) =>
        new(
            scenario.Request.Manufacturer,
            scenario.Request.Code,
            scenario.Request.FreeText,
            scenario.Request.Series,
            scenario.Request.ModelCode,
            Category: null,
            scenario.Request.EquipmentSide,
            scenario.Request.DisplayContext);

    private static BotScenarioDefinition Load(string path)
    {
        var raw = File.ReadAllText(path);
        using var document = JsonDocument.Parse(raw);
        var scenario = Assert.IsType<BotScenarioDefinition>(JsonSerializer.Deserialize<BotScenarioDefinition>(raw, Options));

        Assert.False(string.IsNullOrWhiteSpace(scenario.ScenarioId));
        Assert.False(string.IsNullOrWhiteSpace(scenario.Title));
        Assert.False(string.IsNullOrWhiteSpace(scenario.Description));
        Assert.False(string.IsNullOrWhiteSpace(scenario.Request.Manufacturer));
        Assert.False(string.IsNullOrWhiteSpace(scenario.Request.Code));
        Assert.True(Enum.IsDefined(scenario.Expected.ResponseStatus));
        Assert.NotNull(scenario.Expected.MustContainText);
        Assert.NotNull(scenario.Expected.MustNotContainText);
        Assert.All(UnsafeFragments, fragment => Assert.DoesNotContain(fragment, raw, StringComparison.OrdinalIgnoreCase));
        Assert.All(InternalArtifactFragments, fragment => Assert.DoesNotContain(fragment, raw, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(EnumerateStrings(document.RootElement), value => value.Length > 500);

        return scenario;
    }

    private static IEnumerable<string> EnumerateStrings(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
            yield return element.GetString() ?? string.Empty;
        else if (element.ValueKind == JsonValueKind.Object)
            foreach (var property in element.EnumerateObject())
                foreach (var value in EnumerateStrings(property.Value))
                    yield return value;
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray())
                foreach (var value in EnumerateStrings(item))
                    yield return value;
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static string ScenarioDirectory =>
        Path.Combine(TestPaths.RepoRoot, "docs", "equipment-diagnostics", "bot-scenarios");
}
