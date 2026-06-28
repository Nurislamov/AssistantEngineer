using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeDiagnosticsQualityBaselineTests
{
    private static readonly string[] ForbiddenVisibleFragments =
    [
        "GC202512-I",
        "GC202209-I",
        "GC202203-IV",
        "PDF page",
        "manual page",
        "Chapter 3 Faults",
        "Error Indication",
        "???",
        "Рє",
        "РЅ",
        "Р°",
        "СЃ",
        "Р±",
        "Р»Р",
        "runtime",
        "staging",
        "sourceMeaning",
        "raw",
        "machine translated",
        "к наружного блока",
        "к внутреннего блока",
        "к наладки системы"
    ];

    [Fact]
    public void RuntimeCountsRemainAtApprovedGreeBaseline()
    {
        var entries = new JsonErrorKnowledgeLocalizationSource().GetEntries();
        var countsBySeries = entries
            .GroupBy(
                entry => entry.Series
                    ?? throw new InvalidOperationException($"Gree runtime entry {entry.Id} has no series."),
                StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        Assert.Equal(922, entries.Count);
        Assert.Equal(4, countsBySeries.Count);
        Assert.Equal(263, countsBySeries["GMV6"]);
        Assert.Equal(136, countsBySeries["GMV Mini"]);
        Assert.Equal(263, countsBySeries["GMV X"]);
        Assert.Equal(260, countsBySeries["GMV9 Flex"]);
    }

    [Fact]
    public void RuntimeVisibleTextsRemainFreeOfLeaksCorruptionAndKnownBadGrammar()
    {
        var entries = new JsonErrorKnowledgeLocalizationSource().GetEntries();

        foreach (var entry in entries)
        {
            foreach (var text in entry.Texts)
            {
                foreach (var visibleValue in VisibleValues(text))
                {
                    Assert.All(
                        ForbiddenVisibleFragments,
                        fragment => Assert.DoesNotContain(
                            fragment,
                            visibleValue,
                            StringComparison.OrdinalIgnoreCase));
                }
            }
        }
    }

    [Fact]
    public async Task GeneralN2OffersOnlySeriesThatContainN2()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree n2"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains("Для кода n2 есть несколько вариантов", response.Text, StringComparison.Ordinal);
        Assert.Contains("GMV Mini", response.Text, StringComparison.Ordinal);
        Assert.Contains("GMV6", response.Text, StringComparison.Ordinal);
        Assert.Contains("GMV X", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("GMV9 Flex", response.Text, StringComparison.OrdinalIgnoreCase);

        var seriesButtons = response.OutboundMessages
            .Single()
            .ReplyMarkup!
            .Keyboard!
            .SelectMany(row => row)
            .Select(button => button.Text)
            .Where(text => text.StartsWith("GMV", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(
            ["GMV Mini", "GMV X", "GMV6"],
            seriesButtons.Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task ExplicitGmvXN2ResolvesOnlyGmvXN2()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree GMV X n2"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains("Gree GMV X n2", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV6 n2", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Gree GMV Mini n2", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Gree GMV9 Flex n2", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Gree GMV Mini H5", "Gree GMV6 H5", "Gree GMV X H5", "Gree GMV9 Flex H5")]
    [InlineData("Gree GMV9 Flex n2", "Gree GMV6 n2", "Gree GMV Mini n2", "Gree GMV X n2")]
    public async Task ExplicitSeriesMissDoesNotFallbackToAnotherSeries(
        string query,
        string firstForbiddenTitle,
        string secondForbiddenTitle,
        string thirdForbiddenTitle)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains("не нашёл точную расшифровку", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(firstForbiddenTitle, response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(secondForbiddenTitle, response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(thirdForbiddenTitle, response.Text, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> VisibleValues(ErrorKnowledgeTextV2 text)
    {
        yield return text.Title;
        yield return text.Summary;
        yield return text.SafetyNote;
        yield return text.RecommendedAction;
        yield return text.SourceNote;

        foreach (var value in text.PossibleCauses)
            yield return value;

        foreach (var value in text.CheckSteps)
            yield return value;

        foreach (var value in text.DoNotAdvise)
            yield return value;
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
}
