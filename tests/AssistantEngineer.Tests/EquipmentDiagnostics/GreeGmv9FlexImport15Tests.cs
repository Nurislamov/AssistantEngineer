using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed partial class GreeGmv9FlexImport15Tests
{
    private const string ManualId = "gree-gmv9-flex-service-manual-2025-12";
    private const string DocumentCode = "GC202512-I";
    private const string ModelCode = "GMV-450WML/A-X(D)";

    private static readonly string ReportDirectory = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "reference",
        "gree-official-support-error-catalog",
        "staging",
        "gmv9-flex-import-15");

    private static readonly string GreeRuntimeDirectory = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree");

    private static readonly IReadOnlyDictionary<string, int> CategoryCounts =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["indoor"] = 60,
            ["outdoor"] = 120,
            ["debugging"] = 37,
            ["status"] = 43
        };

    [Fact]
    public void ImportReportArtifactsExist()
    {
        foreach (var fileName in new[]
        {
            "README.md",
            "gmv9-flex-manual-inventory-15.csv",
            "gmv9-flex-manual-inventory-15.json",
            "imported-codes-15.csv",
            "imported-codes-15.json",
            "skipped-codes-15.csv",
            "manual-review-15.csv",
            "runtime-overlap-15.csv",
            "runtime-package-summary-15.csv"
        })
        {
            Assert.True(File.Exists(Path.Combine(ReportDirectory, fileName)), $"Missing report artifact: {fileName}");
        }
    }

    [Fact]
    public void ManualInventoryReferencesGc202512IAndImportsAllCodeRows()
    {
        var inventory = ReadArray(Path.Combine(ReportDirectory, "gmv9-flex-manual-inventory-15.json"))
            .Select(Assert.IsType<JsonObject>)
            .ToArray();
        var imported = ReadArray(Path.Combine(ReportDirectory, "imported-codes-15.json"))
            .Select(Assert.IsType<JsonObject>)
            .ToArray();

        Assert.Equal(260, inventory.Length);
        Assert.Equal(260, imported.Length);
        Assert.Equal(260, inventory.Count(row => RequiredString(row, "actionRecommendation") == "ready-for-import"));
        Assert.All(inventory, row =>
        {
            Assert.Equal(ManualId, RequiredString(row, "sourceManualId"));
            Assert.Equal(DocumentCode, RequiredString(row, "sourceDocument"));
            Assert.Equal("GMV9 Flex", RequiredString(row, "seriesCandidate"));
        });

        foreach (var (category, expected) in CategoryCounts)
            Assert.Equal(expected, inventory.Count(row => RequiredString(row, "categoryCandidate") == category));
    }

    [Fact]
    public void PlaceholderRowsAreSkippedAndManualReviewIsEmpty()
    {
        var skipped = ReadCsv(Path.Combine(ReportDirectory, "skipped-codes-15.csv"));
        var manualReview = ReadCsv(Path.Combine(ReportDirectory, "manual-review-15.csv"));

        Assert.Equal(2, skipped.Count);
        Assert.Empty(manualReview);
        Assert.All(skipped, row => Assert.Equal("placeholder-or-non-code-table-cell", row["reason"]));
    }

    [Fact]
    public void RuntimeAndPackageCountsMatchGmv9FlexImport()
    {
        Assert.Equal(263, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv6"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(136, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv-mini"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(263, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv-x"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(260, Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv9-flex"), "*.json", SearchOption.AllDirectories).Length);
        Assert.Equal(1296, Directory.GetFiles(GreeRuntimeDirectory, "*.json", SearchOption.AllDirectories).Length);

        AssertPackageCount("gree-gmv9-flex-indoor-fault-codes.json", 60);
        AssertPackageCount("gree-gmv9-flex-outdoor-fault-protection-codes.json", 120);
        AssertPackageCount("gree-gmv9-flex-debugging-codes.json", 37);
        AssertPackageCount("gree-gmv9-flex-status-codes.json", 43);

        Assert.False(Directory.Exists(Path.Combine(GreeRuntimeDirectory, "9-series-flex")));
        Assert.False(Directory.Exists(Path.Combine(GreeRuntimeDirectory, "flex")));
    }

    [Theory]
    [InlineData("E0", "outdoor")]
    [InlineData("H5", "outdoor")]
    [InlineData("C0", "debugging")]
    [InlineData("A0", "status")]
    [InlineData("L1", "indoor")]
    public void ImportedCardsAreManualVerifiedAndSourceBound(string code, string category)
    {
        var entry = ReadObject(Path.Combine(GreeRuntimeDirectory, "gmv9-flex", category, $"{code.ToLowerInvariant()}.json"));

        Assert.Equal($"gree-gmv9-flex-{category}-{code.ToLowerInvariant()}", RequiredString(entry, "id"));
        Assert.Equal("GMV9 Flex", RequiredString(entry, "series"));
        Assert.Equal(code, RequiredString(entry, "code"));
        Assert.Equal("ManualVerified", RequiredString(entry, "verificationStatus"));
        Assert.Equal("High", RequiredString(entry, "confidence"));
        Assert.Equal(ManualId, RequiredString(entry, "sourceReferences", 0, "manualId"));
        Assert.Equal(DocumentCode, RequiredString(entry, "sourceReferences", 0, "documentCode"));
        Assert.Contains(ModelCode, RequiredArray(entry, "models").Select(node => node!.GetValue<string>()));
    }

    [Fact]
    public void CardsDistinguishTroubleshootingSectionsFromErrorIndicationOnlyEvidence()
    {
        var entries = Directory
            .GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv9-flex"), "*.json", SearchOption.AllDirectories)
            .Select(ReadObject)
            .ToArray();

        Assert.Equal(173, entries.Count(entry =>
            RequiredString(entry, "sourceReference").Contains(
                "Chapter 3 Faults / Troubleshooting /",
                StringComparison.Ordinal)));
        Assert.Equal(87, entries.Count(entry =>
            RequiredString(entry, "sourceReference").Contains(
                "Chapter 3 Faults / Error Indication /",
                StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("A0", "status", "2.1 A0", "PDF page 71", "manual page 69")]
    [InlineData("A2", "status", "2.2 A2", "PDF page 71", "manual page 69")]
    [InlineData("bJ", "outdoor", "2.28 bJ", "PDF page 85", "manual page 83")]
    [InlineData("bn", "outdoor", "2.29 bn", "PDF page 86", "manual page 84")]
    [InlineData("C0", "debugging", "2.30 C0", "PDF page 86-87", "manual page 84-85")]
    public void SelectedCardsReferenceTheirTroubleshootingSections(
        string code,
        string category,
        string section,
        string pdfPage,
        string manualPage)
    {
        var entry = ReadObject(Path.Combine(
            GreeRuntimeDirectory,
            "gmv9-flex",
            category,
            $"{code.ToLowerInvariant()}.json"));
        var reference = RequiredString(entry, "sourceReference");

        Assert.Contains("Chapter 3 Faults / Troubleshooting /", reference, StringComparison.Ordinal);
        Assert.Contains(section, reference, StringComparison.Ordinal);
        Assert.Contains(pdfPage, reference, StringComparison.Ordinal);
        Assert.Contains(manualPage, reference, StringComparison.Ordinal);
        Assert.Equal(reference, RequiredString(entry, "sourceReferences", 0, "sourceReference"));
    }

    [Fact]
    public void ManualRegistryIncludesGmv9FlexServiceManual()
    {
        var registry = ReadObject(Path.Combine(
            TestPaths.RepoRoot,
            "data",
            "equipment-diagnostics",
            "manual-library",
            "manuals.json"));
        var manuals = RequiredArray(registry, "manuals").OfType<JsonObject>().ToArray();
        var manual = Assert.Single(manuals, item => RequiredString(item, "manualId") == ManualId);

        Assert.Equal("GMV9 Flex", RequiredString(manual, "series"));
        Assert.Equal(DocumentCode, RequiredString(manual, "documentCode"));
        Assert.Equal(260, RequiredInt(manual, "entriesImported"));
        Assert.Equal("Imported", RequiredString(manual, "importStatus"));
    }

    [Theory]
    [InlineData("Gree GMV9 Flex E0", "Gree GMV9 Flex — E0")]
    [InlineData("Gree GMV9 H5", "Gree GMV9 Flex — H5")]
    [InlineData("Gree 9 series Flex C0", "Gree GMV9 Flex — C0")]
    [InlineData("Gree 9-Flex A0", "Gree GMV9 Flex — A0")]
    [InlineData("Gree GMV9 Flex A2", "Gree GMV9 Flex — A2")]
    [InlineData("Gree GMV9 Flex bJ", "Gree GMV9 Flex — bJ")]
    [InlineData("Gree GMV9 Flex BJ", "Gree GMV9 Flex — bJ")]
    [InlineData("Gree GMV9 Flex bj", "Gree GMV9 Flex — bJ")]
    [InlineData("Gree GMV9 Flex bn", "Gree GMV9 Flex — bn")]
    [InlineData("Gree GMV9 Flex BN", "Gree GMV9 Flex — bn")]
    [InlineData("Gree GMV9 Flex GMV-450WML/A-X(D) C0", "Gree GMV9 Flex — C0")]
    public async Task ExplicitGmv9FlexQueriesResolveGmv9FlexCards(string query, string expectedTitle)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains(expectedTitle, response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV6", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV6 HR", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV Mini", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV X", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExactGmv9FlexModelMatchesRuntimeCardModelFilter()
    {
        using var provider = CreateProvider();
        var bot = provider.GetRequiredService<IEquipmentDiagnosticBotService>();

        var response = await bot.DiagnoseAsync(new EquipmentDiagnosticBotRequest(
            Manufacturer: "Gree",
            Code: "C0",
            Series: "GMV9 Flex",
            ModelCode: ModelCode));
        var unmatched = await bot.DiagnoseAsync(new EquipmentDiagnosticBotRequest(
            Manufacturer: "Gree",
            Code: "C0",
            Series: "GMV9 Flex",
            ModelCode: "GMV-UNKNOWN"));

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.ReferenceOnly, response.Status);
        Assert.Equal("C0", response.ObservedCode.Code);
        Assert.NotNull(response.EquipmentContext);
        Assert.NotEqual(EquipmentDiagnosticBotResponseStatus.ReferenceOnly, unmatched.Status);
    }

    [Theory]
    [InlineData("Gree GMV9 Flex A0", "информационное состояние")]
    [InlineData("Gree GMV9 Flex A2", "информационное состояние")]
    [InlineData("Gree GMV9 Flex bJ", "4,9-5,1 В")]
    [InlineData("Gree GMV9 Flex bn", "клемму датчика")]
    [InlineData("Gree GMV9 Flex C0", "адреса проводных пультов")]
    public async Task SelectedTelegramAnswersArePracticalAndHideInternalEvidence(
        string query,
        string expectedText)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains(expectedText, response.Text, StringComparison.OrdinalIgnoreCase);
        foreach (var forbidden in ForbiddenVisiblePhrases)
            Assert.DoesNotContain(forbidden, response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LastPreservesCanonicalGmv9FlexMixedCaseCode()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        await adapter.HandleAsync(Update("Gree GMV9 Flex BJ"));
        var last = await adapter.HandleAsync(Update("/last"));

        Assert.Contains("Gree bJ", last.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExplicitGmv9FlexUnknownCodeDoesNotFallbackToOtherGmvSeries()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree GMV9 Flex n2"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.DoesNotContain("Gree GMV6 n2", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Gree GMV6 HR n2", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Gree GMV Mini n2", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Gree GMV X n2", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Gree GMV6 A9", "Gree GMV6 — A9")]
    [InlineData("Gree GMV Mini n2", "Gree GMV Mini — n2")]
    public async Task ExplicitGmv6AndMiniQueriesRemainStable(string query, string expectedTitle)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains(expectedTitle, response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void VisibleTextAvoidsInternalEnglishAndUnsafeConsumerAdvice()
    {
        var files = Directory.GetFiles(Path.Combine(GreeRuntimeDirectory, "gmv9-flex"), "*.json", SearchOption.AllDirectories);
        Assert.Equal(260, files.Length);

        foreach (var file in files)
        {
            var entry = ReadObject(file);
            foreach (var text in RequiredArray(entry, "texts").OfType<JsonObject>())
            {
                foreach (var visible in VisibleValues(text))
                {
                    Assert.DoesNotContain("???", visible, StringComparison.Ordinal);
                    Assert.DoesNotContain("sourceMeaning", visible, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("runtime", visible, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("staging", visible, StringComparison.OrdinalIgnoreCase);
                    foreach (var forbidden in ForbiddenVisiblePhrases)
                        Assert.DoesNotContain(forbidden, visible, StringComparison.OrdinalIgnoreCase);
                    Assert.True(
                        CountCyrillic(visible) >= 8,
                        $"Visible text must contain readable Cyrillic wording: {file}: {visible}");

                    var stripped = StripAllowedTechnicalTerms(visible);
                    Assert.DoesNotMatch(ForbiddenEnglishTechnicalWordPattern(), stripped);
                }

                if (string.Equals(RequiredString(text, "audience"), "Consumer", StringComparison.OrdinalIgnoreCase))
                {
                    var consumerText = string.Join(" ", VisibleValues(text));
                    Assert.DoesNotContain("принудительный запуск", consumerText, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("измерьте напряжение", consumerText, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("замените плату", consumerText, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("замените датчик", consumerText, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("замените компрессор", consumerText, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    [Fact]
    public void EquipmentCatalogMarksGmv9FlexImportedAndRegistersModelAlias()
    {
        var registry = ReadObject(Path.Combine(
            TestPaths.RepoRoot,
            "data",
            "equipment-diagnostics",
            "equipment-catalog",
            "gree-vrf-equipment-map.json"));
        var series = RequiredArray(registry, "series").OfType<JsonObject>().ToArray();
        var gmv9Flex = Assert.Single(series, item => RequiredString(item, "id") == "gmv9_flex");
        var backlog = RequiredArray(registry, "manualSearchBacklog").OfType<JsonObject>().ToArray();

        Assert.Equal("Imported", RequiredString(gmv9Flex, "coverageStatus"));
        Assert.Contains(ModelCode, RequiredArray(gmv9Flex, "aliases").Select(node => node!.GetValue<string>()));
        Assert.DoesNotContain(backlog, item => RequiredString(item, "id") == "gmv9_flex_service_manual");
    }

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
                yield return Assert.IsAssignableFrom<JsonValue>(node).GetValue<string>();
        }
    }

    private static readonly string[] ForbiddenVisiblePhrases =
    [
        "Подтвердите код",
        "Сверьте модель",
        "по таблице",
        "основание",
        "руководство",
        "manual",
        "source",
        "packageId",
        "карточка неисправности"
    ];

    private static string StripAllowedTechnicalTerms(string value) =>
        value
            .Replace("Gree", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("GMV9", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("GMV", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Flex", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("VRF", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("PV", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("AC", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("DC", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("IPM", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("PFC", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("CO2", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("K1", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("SE", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("DIP", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("HBS", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("EC", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("PG", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("IFD", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(DocumentCode, string.Empty, StringComparison.OrdinalIgnoreCase);

    private static int CountCyrillic(string value) =>
        value.Count(character => character >= '\u0400' && character <= '\u04ff');

    private static IReadOnlyList<Dictionary<string, string>> ReadCsv(string path)
    {
        Assert.True(File.Exists(path), $"CSV file does not exist: {path}");
        var lines = File.ReadAllLines(path);
        if (lines.Length <= 1)
        {
            return [];
        }

        var header = ParseCsvLine(lines[0]);
        return lines
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line =>
            {
                var values = ParseCsvLine(line);
                return header
                    .Select((name, index) => new { name, value = index < values.Count ? values[index] : string.Empty })
                    .ToDictionary(item => item.name, item => item.value, StringComparer.Ordinal);
            })
            .ToArray();
    }

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new List<char>();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var ch = line[index];
            if (ch == '"' && quoted && index + 1 < line.Length && line[index + 1] == '"')
            {
                current.Add('"');
                index++;
                continue;
            }

            if (ch == '"')
            {
                quoted = !quoted;
                continue;
            }

            if (ch == ',' && !quoted)
            {
                values.Add(new string(current.ToArray()));
                current.Clear();
                continue;
            }

            current.Add(ch);
        }

        values.Add(new string(current.ToArray()));
        return values;
    }

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

    private static JsonArray ReadArray(string path)
    {
        Assert.True(File.Exists(path), $"JSON file does not exist: {path}");
        var node = JsonNode.Parse(File.ReadAllText(path));
        Assert.NotNull(node);
        return Assert.IsType<JsonArray>(node);
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

    private static string RequiredString(JsonObject obj, string arrayName, int index, string propertyName)
    {
        var array = RequiredArray(obj, arrayName);
        var child = Assert.IsType<JsonObject>(array[index]);
        return RequiredString(child, propertyName);
    }

    private static int RequiredInt(JsonObject obj, string propertyName)
    {
        var node = obj[propertyName];
        Assert.NotNull(node);
        return node.GetValue<int>();
    }

    [GeneratedRegex(@"\b(of|for|fault|error|protection|sensor|compressor|outdoor|indoor|controller|communication|temperature|pressure|voltage|current|module|fan|motor|valve|discharge|suction|setting|inquiry|mode|unit|board|address|quantity|refrigerant|heating|cooling|emergency|runtime|staging|review|imported|pipeline|raw|internal)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ForbiddenEnglishTechnicalWordPattern();
}
