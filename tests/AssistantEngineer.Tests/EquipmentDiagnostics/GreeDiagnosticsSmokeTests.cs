using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeDiagnosticsSmokeTests
{
    private static readonly string[] ForbiddenVisibleFragments =
    [
        "GC202512-I",
        "GC202209-I",
        "GC202203-IV",
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
        "machine translated"
    ];

    [Fact]
    public async Task GeneralN2OffersOnlySeriesWithN2()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree n2"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Equal("HTML", response.ParseMode);
        Assert.Equal("HTML", response.OutboundMessages.Single().ParseMode);
        Assert.Contains("<b>Код n2 найден в нескольких сериях Gree.</b>", response.Text, StringComparison.Ordinal);
        Assert.Contains("<b>Выберите серию:</b>", response.Text, StringComparison.Ordinal);
        Assert.Contains("Код n2 найден в нескольких сериях Gree.", response.Text, StringComparison.Ordinal);
        Assert.Contains("Выберите серию:", response.Text, StringComparison.Ordinal);
        AssertSafeVisibleText(response.Text);

        var seriesButtons = response.OutboundMessages
            .Single()
            .ReplyMarkup!
            .Keyboard!
            .SelectMany(row => row)
            .Select(button => button.Text)
            .Where(text => text.StartsWith("GMV", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["GMV Mini", "GMV X", "GMV6"], seriesButtons);
        Assert.DoesNotContain("GMV9 Flex", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExplicitGmvXN2ResolvesWithoutCrossSeriesFallback()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree GMV X n2"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Equal("HTML", response.ParseMode);
        Assert.Equal("HTML", response.OutboundMessages.Single().ParseMode);
        Assert.Contains("Gree GMV X — n2", response.Text, StringComparison.Ordinal);
        Assert.Contains("<b>Диагностика GREE n2</b>", response.Text, StringComparison.Ordinal);
        Assert.Contains("<b>Суть:</b>", response.Text, StringComparison.Ordinal);
        Assert.Contains("<b>Серия:</b> GMV X", response.Text, StringComparison.Ordinal);
        Assert.Contains("<b>Важно:</b>", response.Text, StringComparison.Ordinal);
        Assert.Contains("только по одному коду", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Не обходите защиты", response.Text, StringComparison.Ordinal);
        Assert.Contains("квалифицированные специалисты", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<b>Ограничения:</b>", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>Техническая заметка:</b>", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Ограничения вывода:", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Дальше:", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV6 n2", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Gree GMV Mini n2", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Gree GMV9 Flex n2", response.Text, StringComparison.OrdinalIgnoreCase);
        AssertSafeVisibleText(response.Text);
    }

    [Fact]
    public async Task ExplicitGmv9FlexN2RemainsUnresolvedWithoutFallback()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree GMV9 Flex n2"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Equal("HTML", response.ParseMode);
        Assert.Equal("HTML", response.OutboundMessages.Single().ParseMode);
        Assert.Contains("<b>Код n2 не найден для Gree GMV9 Flex.</b>", response.Text, StringComparison.Ordinal);
        Assert.Contains("не подставляю значения из других серий", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<b>Проверьте:</b>", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("укажите бренд", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Gree GMV6 n2", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Gree GMV Mini n2", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Gree GMV X n2", response.Text, StringComparison.OrdinalIgnoreCase);
        AssertSafeVisibleText(response.Text);
    }

    [Theory]
    [InlineData("Gree GMV9 Flex E0", "Gree GMV9 Flex — E0", "GMV9 Flex")]
    [InlineData("Gree GMV9 H5", "Gree GMV9 Flex — H5", "GMV9 Flex")]
    [InlineData("Gree 9 series Flex C0", "Gree GMV9 Flex — C0", "GMV9 Flex")]
    [InlineData("Gree 9-Flex A0", "Gree GMV9 Flex — A0", "GMV9 Flex")]
    [InlineData("Gree GMV6 A9", "Gree GMV6 — A9", "GMV6")]
    [InlineData("Gree GMV6 Uy", "Gree GMV6 — Uy", "GMV6")]
    public async Task KnownManualBackedCodesResolveWithSafeVisibleText(
        string query,
        string expectedTitle,
        string expectedSeries)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Equal("HTML", response.ParseMode);
        Assert.Equal("HTML", response.OutboundMessages.Single().ParseMode);
        Assert.Contains(expectedTitle, response.Text, StringComparison.Ordinal);
        Assert.Contains($"<b>Диагностика GREE {query.Split(' ')[^1]}</b>", response.Text, StringComparison.Ordinal);
        Assert.Contains("<b>Суть:</b>", response.Text, StringComparison.Ordinal);
        Assert.Contains("<b>Что проверить:</b>", response.Text, StringComparison.Ordinal);
        Assert.Contains($"<b>Серия:</b> {expectedSeries}", response.Text, StringComparison.Ordinal);
        Assert.Contains("<b>Важно:</b>", response.Text, StringComparison.Ordinal);
        Assert.Contains("только по одному коду", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Не обходите защиты", response.Text, StringComparison.Ordinal);
        Assert.Contains("квалифицированные специалисты", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<b>Ограничения:</b>", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>Техническая заметка:</b>", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Ограничения вывода:", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Дальше:", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("не нашёл точную расшифровку", response.Text, StringComparison.OrdinalIgnoreCase);
        AssertSafeVisibleText(response.Text);
    }

    private static void AssertSafeVisibleText(string text)
    {
        Assert.All(
            ForbiddenVisibleFragments,
            fragment => Assert.DoesNotContain(fragment, text, StringComparison.OrdinalIgnoreCase));
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
        new(UpdateId: 1, ChatId: 7, Username: "local-smoke", Text: text, UserId: 11);
}
