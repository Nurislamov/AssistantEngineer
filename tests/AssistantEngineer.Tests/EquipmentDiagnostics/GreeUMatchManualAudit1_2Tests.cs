using System.Text.Json.Nodes;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeUMatchManualAudit1_2Tests
{
    private static readonly string GreeRuntimeDirectory = Path.Combine(
        TestPaths.RepoRoot,
        "data",
        "equipment-diagnostics",
        "error-knowledge",
        "gree");

    private static readonly string UMatchRuntimeDirectory = Path.Combine(
        GreeRuntimeDirectory,
        "umatch-r32",
        "system");

    private static readonly string[] ExpectedCodes =
    [
        "A1", "A6", "A8", "A9", "AA", "Ab", "Ac", "Ad", "AE", "AF", "AH", "AJ", "AL", "An", "AP", "Ar", "AU",
        "C0", "C1", "C2", "C3", "C4", "C6", "C7", "C8", "C9", "CE", "CJ", "CL", "CP",
        "d1", "d2", "d3", "dc", "dH", "dJ",
        "E0", "E1", "E2", "E3", "E4", "E6", "E7", "E9", "EE", "EL", "F3", "Fo",
        "H1", "H4", "H5", "H7", "HC", "HE",
        "L3", "L4", "L5", "L6", "L7", "LA", "Lc", "LE", "LF", "LP", "oE",
        "P0", "P5", "P6", "P7", "P8", "P9", "PA", "Pd", "PE", "PF", "PH", "PL", "PP", "PU",
        "q0", "q1", "q2", "q3", "q4", "q5", "q6", "q7", "q8", "q9", "qA", "qb", "qC", "qd", "qE", "qF", "qH", "qL", "qo", "qp",
        "U1", "U2", "U3", "U5", "U7", "U8", "UL", "Uo"
    ];

    private static readonly string[] ExpectedModels =
    [
        "GUD35W1/NhB-S", "GUD50W1/NhB-S", "GUD71W1/NhB-S", "GUD85W1/NhB-S",
        "GUD100W1/NhB-S", "GUD125W1/NhB-S", "GUD140W1/NhB-S", "GUD160W1/NhB-S",
        "GUD125W1/NhB-X", "GUD140W1/NhB-X", "GUD160W1/NhB-X",
        "GUD35T1/B-S", "GUD50T1/B-S", "GUD71T1/B-S", "GUD85T1/B-S",
        "GUD100T1/B-S", "GUD125T1/B-S", "GUD140T1/B-S", "GUD160T1/B-S",
        "GUD35P1/B-S", "GUD35PS1/B-S", "GUD50P1/B-S", "GUD50PS1/B-S",
        "GUD71PH1/B-S", "GUD71PHS1/B-S", "GUD85PH1/B-S", "GUD85PHS1/B-S",
        "GUD100PH1/B-S", "GUD100PHS1/B-S", "GUD125PH1/B-S", "GUD125PHS1/B-S",
        "GUD140PH1/B-S", "GUD140PHS1/B-S", "GUD160PH1/B-S", "GUD160PHS1/B-S",
        "GUD35ZD1/B-S", "GUD50ZD1/B-S", "GUD71ZD1/B-S", "GUD85ZD1/B-S",
        "GUD100ZD1/B-S", "GUD125ZD1/B-S", "GUD140ZD1/B-S", "GUD160ZD1/B-S"
    ];

    private static readonly string[] ForbiddenVisibleFragments =
    [
        "manual",
        "source",
        "packageId",
        "карточка неисправности",
        "по таблице",
        "основание",
        "руководство",
        "Подтвердите код",
        "Сверьте модель"
    ];

    [Fact]
    public void RuntimeInventoryExactlyMatchesThe107CodeManualTable()
    {
        var cards = ReadCards();

        Assert.Equal(107, cards.Count);
        Assert.Equal(
            ExpectedCodes.Order(StringComparer.Ordinal),
            cards.Select(card => RequiredString(card, "code")).Order(StringComparer.Ordinal));

        Assert.All(cards, card =>
        {
            Assert.Equal("U-Match R32", RequiredString(card, "series"));
            Assert.Equal("ManualVerified", RequiredString(card, "verificationStatus"));
            Assert.Equal("High", RequiredString(card, "confidence"));
            Assert.Equal(
                ExpectedModels.Order(StringComparer.Ordinal),
                RequiredArray(card, "models")
                    .Select(value => Assert.IsAssignableFrom<JsonValue>(value).GetValue<string>())
                    .Order(StringComparer.Ordinal));
        });
    }

    [Fact]
    public void RuntimeCountsRemainStableAcrossGreeSeries()
    {
        Assert.Equal(262, Count("gmv6-hr"));
        Assert.Equal(263, Count("gmv6"));
        Assert.Equal(148, Count("gmv-mini"));
        Assert.Equal(263, Count("gmv-x"));
        Assert.Equal(260, Count("gmv9-flex"));
        Assert.Equal(107, Count("umatch-r32"));
        Assert.Equal(5, Count("erv-b-series"));
        Assert.Equal(1308, Directory.GetFiles(GreeRuntimeDirectory, "*.json", SearchOption.AllDirectories).Length);
    }

    [Theory]
    [InlineData("E0", "крыльчатк")]
    [InlineData("E1", "реле высокого давления")]
    [InlineData("E2", "испарител")]
    [InlineData("E3", "реле низкого давления")]
    [InlineData("E4", "расширительного клапана")]
    [InlineData("E6", "линии связи")]
    [InlineData("E9", "поплавкового выключателя")]
    [InlineData("C6", "сопротивление датчика")]
    [InlineData("F3", "наружной температуры")]
    [InlineData("CE", "проводного контроллера")]
    [InlineData("CJ", "перемычк")]
    [InlineData("H4", "теплообменник")]
    [InlineData("H5", "IPM")]
    [InlineData("HC", "PFC")]
    [InlineData("Lc", "U/V/W")]
    [InlineData("U7", "четырёхходового клапана")]
    [InlineData("qC", "3,3 В")]
    [InlineData("PA", "входной ток")]
    [InlineData("PL", "под нагрузкой")]
    [InlineData("PH", "стабильность")]
    [InlineData("C8", "разъёме JUMP")]
    [InlineData("EL", "пожарн")]
    public void DetailedTroubleshootingCardsContainPracticalManualBasedChecks(
        string code,
        string expectedFragment)
    {
        var card = ReadCard(code);

        Assert.Contains("section 3.4", RequiredString(card, "sourceReference"), StringComparison.OrdinalIgnoreCase);
        Assert.All(RequiredArray(card, "texts").OfType<JsonObject>(), text =>
        {
            Assert.True(RequiredArray(text, "possibleCauses").Count >= 2);
            Assert.True(RequiredArray(text, "checkSteps").Count >= 3);
            Assert.Contains(
                expectedFragment,
                string.Join(" ", VisibleValues(text)),
                StringComparison.OrdinalIgnoreCase);
        });
    }

    [Theory]
    [InlineData("CL")]
    [InlineData("Fo")]
    [InlineData("H1")]
    [InlineData("d1")]
    [InlineData("d2")]
    [InlineData("d3")]
    public void FunctionAndModeCodesRemainNonAlarmingStatuses(string code)
    {
        var card = ReadCard(code);

        Assert.Equal("Status", RequiredString(card, "signalType"));
        Assert.Equal("Info", RequiredString(card, "severity"));
        Assert.All(RequiredArray(card, "texts").OfType<JsonObject>(), text =>
        {
            var visible = string.Join(" ", VisibleValues(text));
            Assert.Contains("не отказ компонента", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("авария", visible, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("поломка", visible, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task DetailedCodesRouteToUMatchWithoutGmvFallback()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        foreach (var code in new[]
                 {
                     "E0", "E1", "E2", "E3", "E4", "E6", "E9", "C6", "F3", "CE", "CJ",
                     "H4", "H5", "HC", "Lc", "U7", "qC", "PA", "PL", "PH", "C8", "EL"
                 })
        {
            var response = await adapter.HandleAsync(Update($"Gree U-Match {code}"));

            Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
            Assert.Contains("Gree U-Match R32", response.Text, StringComparison.Ordinal);
            Assert.Contains($"Диагностика GREE {code}", response.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("Gree GMV6", response.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("Gree GMV X", response.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("Gree GMV Mini", response.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("Gree GMV9 Flex", response.Text, StringComparison.Ordinal);
            AssertVisibleTextIsSafe(response.Text);
        }
    }

    [Fact]
    public async Task OutdoorAndIndoorModelAliasesRouteToUMatch()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        foreach (var (query, code) in new[]
                 {
                     ("Gree U-Match GUD125W1/NhB-S E1", "E1"),
                     ("Gree U-Match GUD160PHS1/B-S E0", "E0"),
                     ("Gree U-Match GUD71PH1/B-S E9", "E9")
                 })
        {
            var response = await adapter.HandleAsync(Update(query));

            Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
            Assert.Contains("Gree U-Match R32", response.Text, StringComparison.Ordinal);
            Assert.Contains($"Диагностика GREE {code}", response.Text, StringComparison.Ordinal);
            AssertVisibleTextIsSafe(response.Text);
        }
    }

    [Fact]
    public void EveryVisibleCardTextAvoidsInternalWording()
    {
        foreach (var card in ReadCards())
        {
            Assert.All(RequiredArray(card, "texts").OfType<JsonObject>(), text =>
                AssertVisibleTextIsSafe(string.Join(" ", VisibleValues(text))));
        }
    }

    [Fact]
    public void ManualRegistryRecordsTheAuditedUMatchServiceManual()
    {
        var registry = ReadObject(Path.Combine(
            TestPaths.RepoRoot,
            "data",
            "equipment-diagnostics",
            "manual-library",
            "manuals.json"));

        var record = RequiredArray(registry, "manuals")
            .OfType<JsonObject>()
            .Single(item => RequiredString(item, "manualId") == "gree-umatch-r32-service-manual");

        Assert.Equal("U-MATCH DC INVERTER UNIT", RequiredString(record, "documentTitle"));
        Assert.Equal("GC202209-I", RequiredString(record, "documentCode"));
        Assert.Equal(107, RequiredInt(record, "entriesImported"));
        Assert.Equal("DiagnosticScopeImported", RequiredString(record, "coverageStatus"));
    }

    private static IReadOnlyList<JsonObject> ReadCards() =>
        Directory.GetFiles(UMatchRuntimeDirectory, "*.json")
            .Select(ReadObject)
            .ToArray();

    private static JsonObject ReadCard(string code) =>
        ReadObject(Path.Combine(UMatchRuntimeDirectory, $"{code.ToLowerInvariant()}.json"));

    private static int Count(string seriesDirectory) =>
        Directory.GetFiles(
            Path.Combine(GreeRuntimeDirectory, seriesDirectory),
            "*.json",
            SearchOption.AllDirectories).Length;

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        services.AddSingleton(new EquipmentDiagnosticTelegramOptions
        {
            IsEnabled = true,
            DefaultManufacturer = "Gree",
            MaxMessageLength = 1400,
            AllowedChatIds = [7]
        });
        return services.BuildServiceProvider();
    }

    private static EquipmentDiagnosticTelegramUpdate Update(string text) =>
        new(UpdateId: 1, ChatId: 7, Username: "operator", Text: text, UserId: 11);

    private static void AssertVisibleTextIsSafe(string text)
    {
        Assert.All(ForbiddenVisibleFragments, fragment =>
            Assert.DoesNotContain(fragment, text, StringComparison.OrdinalIgnoreCase));
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

    private static int RequiredInt(JsonObject node, string property) =>
        Assert.IsAssignableFrom<JsonValue>(node[property]).GetValue<int>();
}
