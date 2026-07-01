using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeSeriesRefinementUx7Tests
{
    [Theory]
    [InlineData("n2", "GMV6 HR", "GMV6", "GMV Mini", "GMV X")]
    [InlineData("E0", "GMV6 HR", "GMV6", "GMV Mini", "GMV X", "GMV9 Flex", "U-Match R32")]
    [InlineData("U4", "GMV6 HR", "GMV6", "GMV Mini", "GMV X", "GMV9 Flex")]
    [InlineData("C2", "GMV6 HR", "GMV6", "GMV Mini", "GMV X", "GMV9 Flex", "U-Match R32")]
    public void RuntimeLocalizationContainsExpectedSeries(
        string code,
        params string[] expectedSeries)
    {
        using var provider = CreateProvider();
        var source = provider.GetRequiredService<IErrorKnowledgeLocalizationSource>();

        var series = source.GetEntries()
            .Where(entry => entry.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.Series)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(SeriesSortKey)
            .ThenBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedSeries, series);
    }

    [Theory]
    [InlineData("n2", "GMV6 HR", "GMV6", "GMV Mini", "GMV X")]
    [InlineData("E0", "GMV6 HR", "GMV6", "GMV Mini", "GMV X", "GMV9 Flex", "U-Match R32")]
    [InlineData("U4", "GMV6 HR", "GMV6", "GMV Mini", "GMV X", "GMV9 Flex")]
    [InlineData("C2", "GMV6 HR", "GMV6", "GMV Mini", "GMV X", "GMV9 Flex", "U-Match R32")]
    [InlineData("H5", "GMV6 HR", "GMV6", "GMV X", "GMV9 Flex", "U-Match R32")]
    [InlineData("o1", "GMV6 HR", "GMV6", "GMV X", "GMV9 Flex")]
    [InlineData("FH", "GMV6", "GMV X")]
    public async Task GenericCodeRefinementUsesAllRuntimeSeriesInStableOrder(
        string code,
        params string[] expectedSeries)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await ReachSeriesRefinementAsync(adapter, code);
        var rows = response.OutboundMessages.Single().ReplyMarkup!.Keyboard!;
        var seriesButtons = rows
            .SelectMany(row => row)
            .Select(button => button.Text)
            .Where(IsSeriesButton)
            .ToArray();

        Assert.Equal(expectedSeries, seriesButtons);
        Assert.Contains(
            rows.SelectMany(row => row),
            button => button.Text == TelegramDiagnosticConversationService.UnknownButton);
        Assert.All(
            rows,
            row => Assert.True(
                row.Count(button => button.Text.StartsWith("GMV", StringComparison.Ordinal)) <= 2));
    }

    [Theory]
    [InlineData("Gree GMV6 HR n2", "Gree GMV6 HR — n2", "GMV6 HR")]
    [InlineData("Gree GMV6 n2", "Gree GMV6 — n2", "GMV6")]
    [InlineData("Gree GMV Mini n2", "Gree GMV Mini — n2", "GMV Mini")]
    [InlineData("Gree GMV X n2", "Gree GMV X — n2", "GMV X")]
    public async Task ExplicitSeriesRoutesDirectlyWithoutCrossSeriesRefinement(
        string query,
        string expectedTitle,
        string expectedSeries)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Contains(expectedTitle, response.Text, StringComparison.Ordinal);
        Assert.Contains($"<b>Серия:</b> {expectedSeries}", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(
            response.OutboundMessages.Single().ReplyMarkup!.Keyboard!.SelectMany(row => row),
            button => button.Text.StartsWith("GMV", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SingleSeriesCodeDoesNotShowSeriesRefinement()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree 01"));

        Assert.Contains("Gree GMV Mini — 01", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(
            response.OutboundMessages.Single().ReplyMarkup!.Keyboard!.SelectMany(row => row),
            button => button.Text.StartsWith("GMV", StringComparison.Ordinal));
    }

    private static async Task<EquipmentDiagnosticTelegramResponse> ReachSeriesRefinementAsync(
        IEquipmentDiagnosticTelegramAdapter adapter,
        string code)
    {
        var response = await adapter.HandleAsync(Update($"Gree {code}"));
        var trace = new List<string>();
        for (var step = 0; step < 4; step++)
        {
            var buttons = response.OutboundMessages
                .Single()
                .ReplyMarkup?
                .Keyboard?
                .SelectMany(row => row)
                .Select(button => button.Text)
                .ToArray() ?? [];
            trace.Add($"{response.Text.ReplaceLineEndings(" ")} [{string.Join(", ", buttons)}]");
            if (buttons.Count(IsSeriesButton) >= 2)
            {
                return response;
            }

            response = await adapter.HandleAsync(Update(TelegramDiagnosticConversationService.UnknownButton));
        }

        throw new Xunit.Sdk.XunitException(
            $"Series refinement was not reached for code {code}. Flow: {string.Join(" -> ", trace)}");
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

    private static int SeriesSortKey(string? series) =>
        series switch
        {
            "GMV6 HR" => 0,
            "GMV6" => 1,
            "GMV Mini" => 2,
            "GMV X" => 3,
            "GMV9 Flex" => 4,
            "U-Match R32" => 5,
            "ERV B Series" => 6,
            _ => 100
        };

    private static bool IsSeriesButton(string text) =>
        text.StartsWith("GMV", StringComparison.Ordinal) ||
        text.StartsWith("U-Match", StringComparison.Ordinal) ||
        text.StartsWith("ERV", StringComparison.Ordinal);
}
