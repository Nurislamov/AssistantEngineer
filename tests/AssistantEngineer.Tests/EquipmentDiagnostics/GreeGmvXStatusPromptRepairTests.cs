using System.Text.Json.Nodes;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvXStatusPromptRepairTests
{
    private const string InternalSourceNote = "Служебная заметка не выводится пользователю.";

    private static readonly string GreeRoot = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree");

    private static readonly string GmvXRoot = Path.Combine(GreeRoot, "gmv-x");

    private static readonly string[] StatusPromptCodes =
    [
        "A0", "A2", "A3", "A4", "A6", "A7", "A8", "Ab", "AC", "Ad", "AE", "AF", "AH", "AJ", "AP", "AU",
        "C8", "C9", "CA",
        "db",
        "n0", "n2", "n4", "n6", "n7", "n8", "n9", "nA", "nC", "nE", "nF", "nH",
        "UC"
    ];

    private static readonly string[] ForbiddenVisibleFragments =
    [
        "Подтвердите код",
        "Сверьте модель",
        "Дальнейшие действия",
        "Точная причина зависит",
        "manual",
        "source",
        "packageId",
        "руководство",
        "основание",
        "по таблице",
        "классифицирован по таблице",
        "неисправность",
        "авария",
        "fault"
    ];

    [Fact]
    public void AllGmvXStatusPromptCodesExistAndAreRepaired()
    {
        var entries = ReadStatusPromptEntries();

        Assert.Equal(33, entries.Count);
        Assert.Equal(StatusPromptCodes.Order(StringComparer.Ordinal), entries.Keys.Order(StringComparer.Ordinal));

        foreach (var (code, entry) in entries)
        {
            var visible = VisibleBlob(entry);
            Assert.Contains($"Gree GMV X — {code} —", visible, StringComparison.Ordinal);
            Assert.Contains("статус", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("обслуживание оборудования", visible, StringComparison.OrdinalIgnoreCase);

            foreach (var forbidden in ForbiddenVisibleFragments)
            {
                Assert.DoesNotContain(forbidden, visible, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var text in RequiredArray(entry, "texts").OfType<JsonObject>())
            {
                Assert.True(text.TryGetPropertyValue("sourceNote", out var sourceNote));
                var note = sourceNote?.GetValue<string>();
                Assert.False(string.IsNullOrWhiteSpace(note), $"sourceNote is required for {code}.");
                Assert.Equal(InternalSourceNote, note);

                foreach (var forbidden in ForbiddenVisibleFragments)
                {
                    Assert.DoesNotContain(forbidden, note, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    [Fact]
    public async Task TelegramStatusPromptAnswersDoNotRenderSourceNote()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree GMV X AJ"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains("Gree GMV X — AJ", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(InternalSourceNote, response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Служебная заметка", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void KeyStatusPromptMeaningsStaySpecific()
    {
        AssertText("AJ", contains: ["фильтр", "очист", "сброс", "сервисный цикл"], excludes: ["обслуживание оборудования", "неисправность", "авария"]);
        AssertText("A0", contains: ["пусконалад", "запущен", "статус"], excludes: ["неисправность", "авария"]);
        AssertText("A2", contains: ["сбор", "хладагент", "статус"], excludes: ["неисправность", "авария"]);
        AssertText("A3", contains: ["оттайк", "5", "10", "вентилятор"], excludes: ["неисправность", "авария"]);
        AssertText("A4", contains: ["возврата масла", "статус"], excludes: ["неисправность", "авария"]);
        AssertText("db", contains: ["статус отладки"], excludes: ["неисправность", "авария"]);
        AssertText("UC", contains: ["настройка", "главного внутреннего блока", "успеш"], excludes: ["неисправность", "авария"]);
    }

    [Fact]
    public void RuntimeCountsRemainStableAfterGmvXStatusPromptRepair()
    {
        Assert.Equal(1308, Directory.GetFiles(GreeRoot, "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(263, Directory.GetFiles(GmvXRoot, "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(121, Directory.GetFiles(Path.Combine(GmvXRoot, "outdoor"), "*.json").Length);
        Assert.Equal(60, Directory.GetFiles(Path.Combine(GmvXRoot, "indoor"), "*.json").Length);
        Assert.Equal(44, Directory.GetFiles(Path.Combine(GmvXRoot, "status"), "*.json").Length);
        Assert.Equal(38, Directory.GetFiles(Path.Combine(GmvXRoot, "debugging"), "*.json").Length);
        Assert.Equal(263, Directory.GetFiles(Path.Combine(GreeRoot, "gmv6"), "*.json", SearchOption.AllDirectories).Length);
    }

    private static Dictionary<string, JsonObject> ReadStatusPromptEntries()
    {
        var codes = StatusPromptCodes.ToHashSet(StringComparer.Ordinal);
        return Directory.GetFiles(GmvXRoot, "*.json", SearchOption.AllDirectories)
            .Select(ReadObject)
            .Where(entry => codes.Contains(RequiredString(entry, "code")))
            .ToDictionary(entry => RequiredString(entry, "code"), StringComparer.Ordinal);
    }

    private static void AssertText(string code, string[] contains, string[] excludes)
    {
        var entries = ReadStatusPromptEntries();
        Assert.True(entries.TryGetValue(code, out var entry), $"Missing GMV X status/prompt code {code}.");
        var visible = VisibleBlob(entry);

        foreach (var fragment in contains)
        {
            Assert.Contains(fragment, visible, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var fragment in excludes)
        {
            Assert.DoesNotContain(fragment, visible, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string VisibleBlob(JsonObject entry)
    {
        var parts = new List<string>();
        foreach (var text in RequiredArray(entry, "texts").OfType<JsonObject>())
        {
            foreach (var propertyName in new[] { "title", "summary", "recommendedAction", "safetyNote" })
            {
                parts.Add(RequiredString(text, propertyName));
            }

            foreach (var propertyName in new[] { "possibleCauses", "checkSteps", "doNotAdvise" })
            {
                parts.AddRange(RequiredArray(text, propertyName)
                    .OfType<JsonValue>()
                    .Select(value => value.GetValue<string>()));
            }
        }

        return string.Join('\n', parts);
    }

    private static JsonObject ReadObject(string path)
    {
        Assert.True(File.Exists(path), $"JSON file does not exist: {path}");
        var node = JsonNode.Parse(File.ReadAllText(path));
        return Assert.IsType<JsonObject>(node);
    }

    private static string RequiredString(JsonObject entry, string propertyName)
    {
        Assert.True(entry.TryGetPropertyValue(propertyName, out var node), $"Missing property '{propertyName}'.");
        return Assert.IsAssignableFrom<JsonValue>(node).GetValue<string>();
    }

    private static JsonArray RequiredArray(JsonObject entry, string propertyName)
    {
        Assert.True(entry.TryGetPropertyValue(propertyName, out var node), $"Missing property '{propertyName}'.");
        return Assert.IsType<JsonArray>(node);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        services.AddSingleton(new EquipmentDiagnosticTelegramOptions
        {
            IsEnabled = true,
            DefaultManufacturer = "Gree",
            MaxMessageLength = 1200
        });
        return services.BuildServiceProvider();
    }

    private static EquipmentDiagnosticTelegramUpdate Update(string text) =>
        new(UpdateId: 1, ChatId: 7, Username: "operator", Text: text, UserId: 11);
}
