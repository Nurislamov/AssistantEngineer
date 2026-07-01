using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed partial class GreeGmvXVisibleWording14_1Tests
{
    private static readonly string GreeRuntimeDirectory = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree");

    private static readonly string GmvXRuntimeDirectory = Path.Combine(GreeRuntimeDirectory, "gmv-x");

    private static readonly string[] InternalForbiddenFragments =
    [
        "support-каталог",
        "reference-only",
        "raw",
        "review",
        "staging",
        "runtime",
        "internal",
        "sourceMeaning",
        "machine translated",
        "imported",
        "pipeline"
    ];

    private static readonly string[] UnsafeConsumerFragments =
    [
        "measure voltage",
        "measure current",
        "open electrical cabinet",
        "open electrical panel",
        "bypass protection",
        "bypass protections",
        "short contacts",
        "replace board",
        "replace sensor",
        "replace compressor",
        "replace motor",
        "charge refrigerant",
        "release refrigerant",
        "force start"
    ];

    private static readonly string[] BrokenRussianCaseFragments =
    [
        "к наружного блока",
        "к внутреннего блока",
        "относится к наружного блока",
        "относится к внутреннего блока",
        "формулировка относится к наружного блока",
        "формулировка относится к внутреннего блока"
    ];

    [Fact]
    public void GmvXRuntimeCountsRemainUnchangedAfterVisibleWordingRepair()
    {
        Assert.Equal(263, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv6"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(136, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv-mini"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(263, Directory.GetFiles(GmvXRuntimeDirectory, "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(1293, Directory.GetFiles(GreeRuntimeDirectory, "*.json", SearchOption.AllDirectories).Length);
    }

    [Fact]
    public void AllGmvXVisibleTextFieldsAreReadableRussianAndNotQuestionMarkCorrupted()
    {
        var files = Directory.GetFiles(GmvXRuntimeDirectory, "*.json", SearchOption.AllDirectories);
        Assert.Equal(263, files.Length);

        foreach (var file in files)
        {
            var entry = ReadObject(file);
            var texts = RequiredArray(entry, "texts");
            Assert.Equal(3, texts.Count);

            foreach (var textNode in texts)
            {
                var text = Assert.IsType<JsonObject>(textNode);
                var title = RequiredString(text, "title");
                Assert.Contains(" — ", title, StringComparison.Ordinal);
                Assert.DoesNotContain("?", title, StringComparison.Ordinal);
                Assert.DoesNotContain(" ? ", title, StringComparison.Ordinal);

                foreach (var visible in VisibleValues(text))
                {
                    Assert.DoesNotContain("???", visible, StringComparison.Ordinal);
                    Assert.DoesNotContain("????", visible, StringComparison.Ordinal);
                    Assert.DoesNotContain("? ?", visible, StringComparison.Ordinal);
                    Assert.All(BrokenRussianCaseFragments, fragment =>
                        Assert.DoesNotContain(fragment, visible, StringComparison.OrdinalIgnoreCase));
                    Assert.True(
                        CountCyrillic(visible) >= 8,
                        $"Visible text must contain readable Cyrillic wording: {file}: {visible}");

                    Assert.All(InternalForbiddenFragments, fragment =>
                        Assert.DoesNotContain(fragment, visible, StringComparison.OrdinalIgnoreCase));

                    var stripped = StripAllowedTechnicalTerms(visible);
                    Assert.DoesNotMatch(ForbiddenEnglishTechnicalWordPattern(), stripped);
                }

                if (string.Equals(RequiredString(text, "audience"), "Consumer", StringComparison.OrdinalIgnoreCase))
                {
                    var consumerText = string.Join(" ", VisibleValues(text));
                    Assert.All(UnsafeConsumerFragments, fragment =>
                        Assert.DoesNotContain(fragment, consumerText, StringComparison.OrdinalIgnoreCase));
                }
            }
        }
    }

    [Theory]
    [InlineData("Gree GMV X E0", "неисправность наружного блока")]
    [InlineData("Gree X E0", "неисправность наружного блока")]
    [InlineData("Gree X series E0", "неисправность наружного блока")]
    [InlineData("Gree GMV X F5", "неисправность компрессора")]
    [InlineData("Gree GMV X d9", "неисправность управления")]
    [InlineData("Gree GMV X UE", "сообщение наладки холодильного контура")]
    [InlineData("Gree GMV X qP", "настройка региона экспорта")]
    [InlineData("Gree GMV X n2", "настройка предела коэффициента соответствия")]
    public async Task TelegramGmvXSmokeAnswersUseReadableRussianWithoutQuestionMarks(
        string query,
        string expectedFragment)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));
        var expectedCode = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)[^1];

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains($"Gree GMV X — {expectedCode}", response.Text, StringComparison.Ordinal);
        Assert.Equal("HTML", response.ParseMode);
        Assert.Contains("<b>Суть:</b>", response.Text, StringComparison.Ordinal);
        Assert.Contains(expectedFragment, response.Text, StringComparison.Ordinal);
        Assert.Contains("Gree GMV X", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("???", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("????", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("? ?", response.Text, StringComparison.Ordinal);
        Assert.All(BrokenRussianCaseFragments, fragment =>
            Assert.DoesNotContain(fragment, response.Text, StringComparison.OrdinalIgnoreCase));
        Assert.True(CountCyrillic(response.Text) >= 40, response.Text);
    }

    [Fact]
    public async Task ExplicitGmvXUnknownCodeDoesNotFallbackToGmv6OrMini()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree GMV X ZZ99"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.DoesNotContain("Gree GMV6", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV Mini", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnqualifiedN2AmbiguityIncludesGmvX()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree n2"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains("GMV6", response.Text, StringComparison.Ordinal);
        Assert.Contains("GMV Mini", response.Text, StringComparison.Ordinal);
        Assert.Contains("GMV X", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("???", response.Text, StringComparison.Ordinal);
        Assert.Contains("- <b>GMV X</b>", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> VisibleValues(JsonObject text)
    {
        yield return RequiredString(text, "title");
        yield return RequiredString(text, "summary");
        yield return RequiredString(text, "safetyNote");
        yield return RequiredString(text, "recommendedAction");
        yield return RequiredString(text, "sourceNote");

        foreach (var propertyName in new[] { "possibleCauses", "checkSteps", "doNotAdvise" })
        {
            foreach (var node in RequiredArray(text, propertyName))
            {
                yield return Assert.IsAssignableFrom<JsonValue>(node).GetValue<string>();
            }
        }
    }

    private static string StripAllowedTechnicalTerms(string value) =>
        value
            .Replace("Gree", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("GMV", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("VRF", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("PV", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("AC", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("DC", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("IPM", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("PFC", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("CO2", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("K1", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("SE", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("IPLV", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("DIP", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("GC202209-I", string.Empty, StringComparison.OrdinalIgnoreCase);

    private static int CountCyrillic(string value) =>
        value.Count(character => character >= '\u0400' && character <= '\u04ff');

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        services.AddSingleton(new EquipmentDiagnosticTelegramOptions
        {
            IsEnabled = true,
            DefaultManufacturer = "Gree",
            MaxMessageLength = 900,
            AllowedChatIds = [7]
        });

        return services.BuildServiceProvider();
    }

    private static EquipmentDiagnosticTelegramUpdate Update(string text) =>
        new(UpdateId: 1, ChatId: 7, Username: "operator", Text: text, UserId: 11);

    private static JsonObject ReadObject(string path)
    {
        Assert.True(File.Exists(path), $"JSON file does not exist: {path}");
        var node = JsonNode.Parse(File.ReadAllText(path));
        Assert.NotNull(node);
        return Assert.IsType<JsonObject>(node);
    }

    private static JsonArray RequiredArray(JsonObject obj, string propertyName)
    {
        var node = obj[propertyName];
        Assert.NotNull(node);
        return Assert.IsType<JsonArray>(node);
    }

    private static string RequiredString(JsonObject obj, string propertyName)
    {
        var node = obj[propertyName];
        Assert.NotNull(node);
        var value = node.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(value), $"Property {propertyName} must not be blank.");
        return value;
    }

    [GeneratedRegex(@"\b(of|for|fault|error|protection|sensor|compressor|outdoor|indoor|controller|communication|temperature|pressure|voltage|current|module|fan|motor|valve|discharge|suction|setting|inquiry|mode|unit|board|address|quantity|refrigerant|heating|cooling|emergency|runtime|staging|review|imported|pipeline|raw|internal)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ForbiddenEnglishTechnicalWordPattern();
}
