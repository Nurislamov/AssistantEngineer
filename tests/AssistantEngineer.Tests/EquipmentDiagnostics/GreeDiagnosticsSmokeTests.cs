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
        Assert.Contains("Для кода n2 есть несколько вариантов", response.Text, StringComparison.Ordinal);
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
        Assert.Contains("Gree GMV X n2", response.Text, StringComparison.Ordinal);
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
        Assert.Contains("не нашёл точную расшифровку", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Gree GMV6 n2", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Gree GMV Mini n2", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Gree GMV X n2", response.Text, StringComparison.OrdinalIgnoreCase);
        AssertSafeVisibleText(response.Text);
    }

    [Theory]
    [InlineData("Gree GMV9 Flex E0", "Gree GMV9 Flex E0")]
    [InlineData("Gree GMV9 H5", "Gree GMV9 Flex H5")]
    [InlineData("Gree 9 series Flex C0", "Gree GMV9 Flex C0")]
    [InlineData("Gree 9-Flex A0", "Gree GMV9 Flex A0")]
    [InlineData("Gree GMV6 A9", "Gree GMV6 A9")]
    [InlineData("Gree GMV6 Uy", "Gree GMV6 Uy")]
    public async Task KnownManualBackedCodesResolveWithSafeVisibleText(
        string query,
        string expectedTitle)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains(expectedTitle, response.Text, StringComparison.Ordinal);
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
