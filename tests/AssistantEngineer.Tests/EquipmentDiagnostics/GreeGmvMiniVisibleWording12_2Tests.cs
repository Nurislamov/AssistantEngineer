using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed partial class GreeGmvMiniVisibleWording12_2Tests
{
    private static readonly string MiniRuntimeDirectory = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree",
        "gmv-mini");

    private static readonly string[] ForbiddenFragments =
    [
        " - ",
        " Set master unit",
        "Poor indoor PCB",
        "Water overf",
        "режимl",
        "ненормальное состояниеly",
        "driven board for",
        "Cooling only",
        "Heating only",
        "Fan model",
        "Refrigerant-charging is invalid",
        "Power supply of wired controller is faulted",
        "Freeze prevention",
        "Mode shock",
        "No main indoor unit",
        "Allocate addresses",
        "Confirm the quantity",
        "Detect outdoor",
        "Detect indoor",
        "неисправность for",
        "неисправность of",
        "защита for",
        "Подтвердите код",
        "Сверьте модель",
        "по таблице",
        "основани",
        "руководств",
        "manual",
        "source",
        "packageId",
        "карточка неисправности"
    ];

    private static readonly string[] SmokeQueries =
    [
        "Gree GMV Mini 01",
        "Gree GMV Mini d3",
        "Gree GMV Mini L3",
        "Gree GMV Mini P1",
        "Gree GMV Mini nC",
        "Gree GMV Mini UE",
        "Gree GMV Mini b1",
        "Gree GMV Mini E0",
        "Gree GMV Mini P0",
        "Gree GMV Mini n2",
        "Gree GMV Mini C0",
        "Gree GMV Mini E1",
        "Gree GMV Mini E3",
        "Gree GMV Mini E4",
        "Gree GMV Mini qL",
        "Gree GMV Mini qF"
    ];

    [Fact]
    public void AllGmvMiniVisibleTextsUsePolishedRussianWording()
    {
        var files = Directory.GetFiles(MiniRuntimeDirectory, "*.json", SearchOption.AllDirectories);
        Assert.Equal(148, files.Length);

        foreach (var file in files)
        {
            var root = ReadObject(file);
            var texts = RequiredArray(root, "texts");
            Assert.Equal(3, texts.Count);

            foreach (var textNode in texts)
            {
                var text = Assert.IsType<JsonObject>(textNode);
                var title = RequiredString(text, "title");
                Assert.Contains(" — ", title, StringComparison.Ordinal);
                Assert.DoesNotContain(" - ", title, StringComparison.Ordinal);

                foreach (var visible in VisibleValues(text))
                {
                    Assert.All(ForbiddenFragments, fragment =>
                        Assert.DoesNotContain(fragment, visible, StringComparison.OrdinalIgnoreCase));
                    Assert.DoesNotMatch(ForbiddenEnglishTechnicalWordPattern(), visible);
                }
            }
        }
    }

    [Theory]
    [InlineData("Gree GMV Mini 01", "Set master unit", "назначение главного блока")]
    [InlineData("Gree GMV Mini d3", "of ambient", "датчика температуры воздуха")]
    [InlineData("Gree GMV Mini L3", "Water overf", "защита от переполнения водой")]
    [InlineData("Gree GMV Mini P1", "Driven board", "ненормальная работа платы привода компрессора")]
    [InlineData("Gree GMV Mini nC", "Cooling only", "модель только для охлаждения")]
    [InlineData("Gree GMV Mini UE", "Refrigerant-charging is invalid", "некорректный режим заправки хладагентом")]
    public async Task RepresentativeMiniAnswersUsePolishedRussianWording(
        string query,
        string forbidden,
        string expected)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains(expected, response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(forbidden, response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(ForbiddenEnglishTechnicalWordPattern(), StripAllowedBrandWords(response.Text));
    }

    [Fact]
    public async Task ExistingMiniRoutingAndN2AmbiguityRemainAvailable()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        foreach (var query in SmokeQueries)
        {
            var response = await adapter.HandleAsync(Update(query));

            Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
            Assert.Contains("Gree GMV Mini", response.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("не нашёл точную расшифровку", response.Text, StringComparison.OrdinalIgnoreCase);
        }

        var ambiguous = await adapter.HandleAsync(Update("Gree n2"));
        Assert.Contains("GMV6", ambiguous.Text, StringComparison.Ordinal);
        Assert.Contains("GMV Mini", ambiguous.Text, StringComparison.Ordinal);
        Assert.Contains("Выберите серию:", ambiguous.Text, StringComparison.Ordinal);
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

    private static string StripAllowedBrandWords(string value) =>
        value
            .Replace("Gree", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("GMV", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Mini", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("CO2", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("IPM", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("PFC", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("DC", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("AC", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("IPLV", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("EU AA", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("SE", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("K1", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("DIP", string.Empty, StringComparison.OrdinalIgnoreCase);

    private static JsonObject ReadObject(string path)
    {
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
        Assert.False(string.IsNullOrWhiteSpace(value), $"Property '{propertyName}' must not be empty.");
        return value;
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        services.AddSingleton(EnabledOptions());
        return services.BuildServiceProvider();
    }

    private static EquipmentDiagnosticTelegramOptions EnabledOptions() => new()
    {
        IsEnabled = true,
        DefaultManufacturer = "Gree",
        MaxMessageLength = 900,
        AllowedChatIds = [7]
    };

    private static EquipmentDiagnosticTelegramUpdate Update(string text) =>
        new(UpdateId: 1, ChatId: 7, Username: "operator", Text: text, UserId: 11);

    [GeneratedRegex(@"\b(of|for|fault|error|protection|sensor|compressor|outdoor|indoor|controller|communication|temperature|pressure|voltage|current|module|fan|motor|valve|discharge|suction|setting|inquiry|mode|unit|board|address|quantity|refrigerant|heating|cooling|emergency)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ForbiddenEnglishTechnicalWordPattern();
}
