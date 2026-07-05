using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class GreeGmvMiniRouting12_1Tests
{
    [Theory]
    [InlineData("Gree GMV Mini d1", "Gree GMV Mini — d1", "Poor indoor PCB")]
    [InlineData("Gree Mini d1", "Gree GMV Mini — d1", "Gree GMV6 — d1")]
    [InlineData("Gree GMV Mini b1", "Gree GMV Mini — b1", "Gree GMV6 — b1")]
    [InlineData("Gree Mini b1", "Gree GMV Mini — b1", "Gree GMV6 — b1")]
    [InlineData("Gree GMV Mini E0", "Gree GMV Mini — E0", "Gree GMV6 — E0")]
    [InlineData("Gree Mini E0", "Gree GMV Mini — E0", "Gree GMV6 — E0")]
    [InlineData("Gree GMV Mini P0", "Gree GMV Mini — P0", "Gree GMV6 — P0")]
    [InlineData("Gree Mini P0", "Gree GMV Mini — P0", "Gree GMV6 — P0")]
    [InlineData("Gree GMV Mini 01", "Gree GMV Mini — 01", "Gree GMV6 — o1")]
    [InlineData("Gree Mini 01", "Gree GMV Mini — 01", "Gree GMV6 — o1")]
    [InlineData("Gree GMV Mini n2", "Gree GMV Mini — n2", "Gree GMV6 — n2")]
    [InlineData("Gree Mini n2", "Gree GMV Mini — n2", "Gree GMV6 — n2")]
    public async Task ExplicitMiniQueriesResolveGmvMiniCards(
        string query,
        string expectedTitle,
        string forbiddenTitle)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains(expectedTitle, response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(forbiddenTitle, response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("не нашёл точную расшифровку", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Gree GMV Mini C0", "Gree GMV Mini — C0")]
    [InlineData("Gree GMV Mini E1", "Gree GMV Mini — E1")]
    [InlineData("Gree GMV Mini E3", "Gree GMV Mini — E3")]
    [InlineData("Gree GMV Mini E4", "Gree GMV Mini — E4")]
    [InlineData("Gree GMV Mini F1", "Gree GMV Mini — F1")]
    [InlineData("Gree GMV Mini F3", "Gree GMV Mini — F3")]
    [InlineData("Gree GMV Mini qL", "Gree GMV Mini — qL")]
    [InlineData("Gree GMV Mini QL", "Gree GMV Mini — qL")]
    [InlineData("Gree GMV Mini qF", "Gree GMV Mini — qF")]
    [InlineData("Gree GMV Mini QF", "Gree GMV Mini — qF")]
    [InlineData("Gree GMV Mini GMV-141WL/C-T qL", "Gree GMV Mini — qL")]
    [InlineData("Gree GMV Mini GMV-180WL/C-X(D) E1", "Gree GMV Mini — E1")]
    [InlineData("Gree GMV Mini GMV-280WL/C1-X qL", "Gree GMV Mini — qL")]
    [InlineData("Gree GMV Mini GMV-335WL/C1-X(S) C0", "Gree GMV Mini — C0")]
    public async Task AuditedMiniQueriesUseExpectedSeriesAndCodeCasing(
        string query,
        string expected)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains(expected, response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV6", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV X", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV9 Flex", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Gmv141FunctionCodeHonorsRuntimeModelFilter()
    {
        using var provider = CreateProvider();
        var bot = provider.GetRequiredService<IEquipmentDiagnosticBotService>();

        var matched = await bot.DiagnoseAsync(new EquipmentDiagnosticBotRequest(
            Manufacturer: "Gree",
            Code: "qL",
            Series: "GMV Mini",
            ModelCode: "GMV-141WL/C-T"));
        var unmatched = await bot.DiagnoseAsync(new EquipmentDiagnosticBotRequest(
            Manufacturer: "Gree",
            Code: "qL",
            Series: "GMV Mini",
            ModelCode: "GMV-280WL/C1-X"));

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.ReferenceOnly, matched.Status);
        Assert.NotEqual(EquipmentDiagnosticBotResponseStatus.ReferenceOnly, unmatched.Status);
    }

    [Theory]
    [InlineData("Gree GMV Mini C0", "кабел")]
    [InlineData("Gree GMV Mini E1", "манометр")]
    [InlineData("Gree GMV Mini E3", "герметич")]
    [InlineData("Gree GMV Mini E4", "сопротивление датчика")]
    [InlineData("Gree GMV Mini F1", "постоянного напряжения")]
    [InlineData("Gree GMV Mini P8", "крепление модулей IPM")]
    [InlineData("Gree GMV Mini PH", "185-264 В")]
    [InlineData("Gree GMV Mini C2", "процессорной платы")]
    public async Task AuditedFaultAnswersContainFlowchartBasedChecks(string query, string expected)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains(expected, response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.All(ForbiddenVisiblePhrases, fragment =>
            Assert.DoesNotContain(fragment, response.Text, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExplicitMiniQueryDoesNotFallbackToGmv6WhenMiniCodeIsMissing()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree GMV Mini H5"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains("Код H5 не найден для Gree GMV Mini.", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV6 H5", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("инверторного вентилятора", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnqualifiedN2AsksForEveryRuntimeSeriesThatContainsIt()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree n2"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains("GMV6", response.Text, StringComparison.Ordinal);
        Assert.Contains("GMV6 HR", response.Text, StringComparison.Ordinal);
        Assert.Contains("GMV Mini", response.Text, StringComparison.Ordinal);
        Assert.Contains("GMV X", response.Text, StringComparison.Ordinal);
        Assert.Contains("Выберите серию:", response.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Gree GMV6 C0", "Gree GMV6 — C0")]
    [InlineData("Gree GMV6 E1", "Gree GMV6 — E1")]
    [InlineData("Gree GMV6 P0", "Gree GMV6 — P0")]
    public async Task ExplicitGmv6QueriesRemainDirect(
        string query,
        string expectedTitle)
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains(expectedTitle, response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RepresentativeMiniVisibleWordingAvoidsMixedRussianEnglishFragments()
    {
        using var provider = CreateProvider();
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        foreach (var query in new[] { "Gree GMV Mini d1", "Gree GMV Mini b1", "Gree GMV Mini E0", "Gree GMV Mini P0" })
        {
            var response = await adapter.HandleAsync(Update(query));

            Assert.DoesNotContain("Poor indoor PCB", response.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("неисправность for", response.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("неисправность of", response.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("driven board for", response.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("support-каталог", response.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("reference-only", response.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("sourceMeaning", response.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("runtime", response.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("pipeline", response.Text, StringComparison.OrdinalIgnoreCase);
        }
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

    private static readonly string[] ForbiddenVisiblePhrases =
    [
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
}
