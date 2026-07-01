using System.Text.Json.Nodes;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeUMatchErvImport24Tests
{
    private static readonly string GreeRuntimeDirectory = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree");

    [Fact]
    public void RuntimeCountsIncludeUMatchAndErvWithoutChangingExistingGmvSeries()
    {
        Assert.Equal(262, Count("gmv6-hr"));
        Assert.Equal(263, Count("gmv6"));
        Assert.Equal(136, Count("gmv-mini"));
        Assert.Equal(263, Count("gmv-x"));
        Assert.Equal(260, Count("gmv9-flex"));
        Assert.Equal(107, Count("umatch-r32"));
        Assert.Equal(5, Count("erv-b-series"));
        Assert.Equal(1296, Directory.GetFiles(GreeRuntimeDirectory, "*.json", SearchOption.AllDirectories).Length);

        AssertPackageCount("gree-umatch-r32-error-codes.json", 107);
        AssertPackageCount("gree-erv-b-series-diagnostics.json", 5);
    }

    [Theory]
    [InlineData("h5", "H5", "Gree U-Match R32 Service Manual EN 3.5-16kW", "GC202209-I")]
    [InlineData("u7", "U7", "Gree U-Match R32 Service Manual EN 3.5-16kW", "GC202209-I")]
    public void UMatchCardsAreManualVerifiedAndSourceBound(string fileCode, string code, string sourceName, string documentCode)
    {
        var entry = ReadObject(Path.Combine(GreeRuntimeDirectory, "umatch-r32", "system", $"{fileCode}.json"));

        Assert.Equal("U-Match R32", RequiredString(entry, "series"));
        Assert.Equal(code, RequiredString(entry, "code"));
        Assert.Equal("ManualVerified", RequiredString(entry, "verificationStatus"));
        Assert.Equal(sourceName, RequiredString(entry, "sourceReferences", 0, "sourceName"));
        Assert.Equal(documentCode, RequiredString(entry, "sourceReferences", 0, "documentCode"));
        AssertVisibleTextDoesNotLeakInternalTerms(entry);
    }

    [Theory]
    [InlineData("e6", "E6")]
    [InlineData("l0", "L0")]
    [InlineData("l9", "L9")]
    [InlineData("df", "dF")]
    [InlineData("dh", "dH")]
    public void ErvCardsAreManualVerifiedAndSourceBound(string fileCode, string code)
    {
        var entry = ReadObject(Path.Combine(GreeRuntimeDirectory, "erv-b-series", "system", $"{fileCode}.json"));

        Assert.Equal("ERV B Series", RequiredString(entry, "series"));
        Assert.Equal(code, RequiredString(entry, "code"));
        Assert.Equal("ManualVerified", RequiredString(entry, "verificationStatus"));
        Assert.StartsWith("Gree ERV", RequiredString(entry, "sourceReferences", 0, "sourceName"), StringComparison.Ordinal);
        AssertVisibleTextDoesNotLeakInternalTerms(entry);
    }

    [Theory]
    [InlineData("Gree U-Match H5", "Gree U-Match R32 — H5")]
    [InlineData("Gree UMatch U7", "Gree U-Match R32 — U7")]
    [InlineData("Gree полупром H5", "Gree U-Match R32 — H5")]
    [InlineData("GUD125 H5", "Gree U-Match R32 — H5")]
    public async Task ExplicitUMatchQueriesResolveUMatchOnly(string query, string expectedTitle)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Contains("Gree U-Match R32", response.Text, StringComparison.Ordinal);
        Assert.Contains(expectedTitle.Contains("U7", StringComparison.Ordinal) ? "U7" : "H5", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV6 — H5", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV X — H5", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("GC202209-I", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Gree ERV E6", "Gree ERV B Series — E6")]
    [InlineData("Gree вентиляция L0", "Gree ERV B Series — L0")]
    [InlineData("FHBQG E6", "Gree ERV B Series — E6")]
    [InlineData("Gree ERV L9", "Gree ERV B Series — L9")]
    [InlineData("Gree ERV dF", "Gree ERV B Series — dF")]
    [InlineData("Gree ERV dH", "Gree ERV B Series — dH")]
    public async Task ExplicitErvQueriesResolveErvOnly(string query, string expectedTitle)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Contains("Gree ERV B Series", response.Text, StringComparison.Ordinal);
        Assert.Contains(expectedTitle, response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree U-Match", response.Text.Replace(expectedTitle, string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
        AssertVisibleTelegramTextDoesNotLeakEvidence(response.Text);
    }

    [Fact]
    public async Task UMatchE0ReturnsUsefulPublicCardWithoutEvidenceWording()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree U-Match E0"));

        Assert.Contains("Gree U-Match R32 — E0 — неисправность вентилятора", response.Text, StringComparison.Ordinal);
        Assert.Contains("питание", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("вентилятор", response.Text, StringComparison.OrdinalIgnoreCase);
        AssertVisibleTelegramTextDoesNotLeakEvidence(response.Text);
    }

    [Fact]
    public async Task ErvE6ReturnsControllerCommunicationCardWithoutEvidenceWording()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree ERV E6"));

        Assert.Contains("Gree ERV B Series — E6 — нарушение связи", response.Text, StringComparison.Ordinal);
        Assert.Contains("между проводным контроллером и блоком ERV", response.Text, StringComparison.OrdinalIgnoreCase);
        AssertVisibleTelegramTextDoesNotLeakEvidence(response.Text);
    }

    [Theory]
    [InlineData("Gree U-Match E0", "Gree U-Match R32", "E0")]
    [InlineData("Gree U-Match H5", "Gree U-Match R32", "H5")]
    [InlineData("Gree ERV E6", "Gree ERV B Series", "E6")]
    [InlineData("Gree ERV L9", "Gree ERV B Series", "L9")]
    public async Task TelegramTitleContainsDiagnosticCodeOnlyOnce(
        string query,
        string expectedSeries,
        string code)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Contains(expectedSeries, response.Text, StringComparison.Ordinal);
        Assert.Contains($"— {code} —", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain($"— {code} — {code}", response.Text, StringComparison.OrdinalIgnoreCase);
        AssertVisibleTelegramTextDoesNotLeakEvidence(response.Text);
    }

    [Theory]
    [InlineData("Gree H5", "U-Match R32")]
    [InlineData("Gree E6", "ERV B Series")]
    public async Task GeneralGreeAmbiguityIncludesOnlyActualRuntimeSeries(string query, string expectedSeries)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Contains("Уточните серию оборудования", response.Text, StringComparison.Ordinal);
        Assert.Contains(expectedSeries, response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ManualBindingRepairScriptsUseProductionColumnsAndExpectedFileNames()
    {
        var scripts = new Dictionary<string, string[]>
        {
            ["fix-gree-umatch-r32-service-manual-binding.sql"] =
                ["Gree U-Match R32 Service Manual EN 3.5-16kW.pdf"],
            ["fix-gree-umatch-r32-owner-manual-bindings.sql"] =
            [
                "Gree U-Match R32 Cassette Type Owner Manual EN 3.5-16kW.pdf",
                "Gree U-Match R32 Duct Type Owner Manual EN 3.5-16kW.pdf"
            ],
            ["fix-gree-erv-b-series-service-manual-binding.sql"] =
                ["Gree ERV B Series Service Manual EN FHBQG-D3.5B-D60B.pdf"],
            ["fix-gree-erv-b-series-owner-manual-bindings.sql"] =
                ["Gree ERV B Series Installation Startup Maintenance Manual EN FHBQG-D3.5B-D60B.pdf"]
        };

        foreach (var (fileName, expectedNames) in scripts)
        {
            var sql = File.ReadAllText(Path.Combine(
                TestPaths.RepoRoot,
                "scripts",
                "deployment",
                "manual-library",
                fileName));

            Assert.DoesNotContain("OriginalFileName", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("UpdatedAtUtc", sql, StringComparison.Ordinal);
            Assert.Contains("\"FileName\"", sql, StringComparison.Ordinal);
            Assert.Contains("\"UpdatedAt\"", sql, StringComparison.Ordinal);
            Assert.Contains("is distinct from", sql, StringComparison.OrdinalIgnoreCase);
            Assert.All(expectedNames, expected => Assert.Contains(expected, sql, StringComparison.Ordinal));
        }
    }

    private static int Count(string seriesDirectory) =>
        Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, seriesDirectory), "*.json", SearchOption.AllDirectories).Length;

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

    private static void AssertPackageCount(string packageFileName, int expected)
    {
        var package = ReadObject(Path.Combine(
            TestPaths.RepoRoot,
            "data",
            "equipment-diagnostics",
            "error-knowledge",
            "packages",
            packageFileName));

        Assert.Equal(expected, RequiredInt(package, "entryCountExpected"));
    }

    private static void AssertVisibleTextDoesNotLeakInternalTerms(JsonObject entry)
    {
        foreach (var text in RequiredArray(entry, "texts").OfType<JsonObject>())
        {
            var combined = string.Join(" ", VisibleValues(text));
            AssertVisibleTelegramTextDoesNotLeakEvidence(combined);
            foreach (var forbidden in new[] { "runtime", "staging", "raw", "sourceMeaning", "machine translated" })
            {
                Assert.DoesNotContain(forbidden, combined, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static void AssertVisibleTelegramTextDoesNotLeakEvidence(string text)
    {
        foreach (var forbidden in new[]
        {
            "подтвержден",
            "подтверждён",
            "таблиц",
            "раздел",
            "сервисном руководстве",
            "диагностическая карта",
            "GC202209-I",
            "source",
            "manual",
            "section",
            "table"
        })
        {
            Assert.DoesNotContain(forbidden, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static IEnumerable<string> VisibleValues(JsonObject text)
    {
        yield return RequiredString(text, "title");
        yield return RequiredString(text, "summary");
        yield return RequiredString(text, "safetyNote");
        yield return RequiredString(text, "recommendedAction");
        yield return RequiredString(text, "sourceNote");
        foreach (var property in new[] { "possibleCauses", "checkSteps", "doNotAdvise" })
        {
            foreach (var value in RequiredArray(text, property))
            {
                yield return Assert.IsAssignableFrom<JsonValue>(value).GetValue<string>();
            }
        }
    }

    private static JsonObject ReadObject(string path) =>
        Assert.IsType<JsonObject>(JsonNode.Parse(File.ReadAllText(path)));

    private static JsonArray RequiredArray(JsonObject node, string property) =>
        Assert.IsType<JsonArray>(node[property]);

    private static string RequiredString(JsonObject node, string property) =>
        Assert.IsAssignableFrom<JsonValue>(node[property]).GetValue<string>();

    private static string RequiredString(JsonObject node, string arrayProperty, int index, string property) =>
        RequiredString(Assert.IsType<JsonObject>(RequiredArray(node, arrayProperty)[index]), property);

    private static int RequiredInt(JsonObject node, string property) =>
        Assert.IsAssignableFrom<JsonValue>(node[property]).GetValue<int>();
}
