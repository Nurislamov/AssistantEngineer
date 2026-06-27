using System.Reflection;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramAdapterTests
{
    [Theory]
    [InlineData("/start")]
    [InlineData("/help")]
    public async Task HelpCommandsReturnHelpWithoutCallingFacade(string text)
    {
        var facade = new CountingFacade();
        var adapter = CreateAdapter(facade, EnabledOptions());

        var response = await adapter.HandleAsync(Update(text));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains("Диагностика оборудования", response.Text, StringComparison.Ordinal);
        Assert.Equal(0, facade.CallCount);
    }

    [Fact]
    public async Task H5CallsFacadeOnceAndFormatsSafetyProvenanceAndVerification()
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree H5"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.DoesNotContain("Черновик / непроверено", response.Text, StringComparison.Ordinal);
        Assert.Contains("Суть:", response.Text, StringComparison.Ordinal);
        Assert.Contains("Что проверить:", response.Text, StringComparison.Ordinal);
        Assert.Contains("Важно:", response.Text, StringComparison.Ordinal);
        Assert.Contains("Ограничения вывода:", response.Text, StringComparison.Ordinal);
        Assert.Contains("Дальше:", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Источник:", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Уверенность:", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Категория:", response.Text, StringComparison.Ordinal);
        Assert.Contains("защит", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("инверторного вентилятора", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ток", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("preliminary protection alarm", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("предварительный сигнал защиты", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AmbiguousCodeFormatsDeterministicClarificationOptions()
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var first = await adapter.HandleAsync(Update("Gree E1"));
        var second = await adapter.HandleAsync(Update("Gree E1"));

        Assert.Equal(first.Text, second.Text);
        Assert.Contains("тип оборудования", first.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Наружный блок", first.OutboundMessages.Single().ReplyMarkup!.Keyboard!.SelectMany(row => row).Select(button => button.Text));

        var outdoor = await adapter.HandleAsync(Update("Наружный блок"));

        Assert.Contains("Gree GMV E1", outdoor.Text, StringComparison.Ordinal);
        Assert.Contains("высокому давлению", outdoor.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Суть:", outdoor.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Категория:", outdoor.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReferenceOnlyCodeIsNotFormattedAsFaultDiagnosis()
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree GMV6 A0"));

        Assert.Contains("Gree GMV A0", response.Text, StringComparison.Ordinal);
        Assert.Contains("пусконаладк", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Суть:", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Категория:", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Внимание: ошибка", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Уверенность:", response.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Gree U0")]
    [InlineData("Gree GMV6 U0")]
    [InlineData("Gree debugging U0")]
    public async Task Gmv6DebuggingU0IsDiscoverable(string query)
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains("Gree GMV U0", response.Text, StringComparison.Ordinal);
        Assert.Contains("Суть:", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Категория:", response.Text, StringComparison.Ordinal);
        Assert.Contains("предварительного прогрева компрессора", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("не нашёл точную расшифровку", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Source:", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Confidence:", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Gmv6CommunicationTextUsesNaturalRussianWording()
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree GMV6 C0"));
        var last = await adapter.HandleAsync(Update("/last"));

        Assert.Contains("Gree GMV C0 — нарушение связи", response.Text, StringComparison.Ordinal);
        Assert.Contains("линию связи GMV", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("питание внутреннего блока, наружного блока и проводного пульта", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Что проверить:", response.Text, StringComparison.Ordinal);
        Assert.Contains("руководства применимой серии", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Категория:", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("классифицирован", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("связи связи", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Gree C0", last.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("связи связи", last.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Gree GMV Mini AJ", "Gree GMV Mini AJ")]
    [InlineData("GMV Mini AJ", "Gree GMV Mini AJ")]
    [InlineData("Gree GMV Mini C0", "Gree GMV Mini C0")]
    [InlineData("GMV Mini C0", "Gree GMV Mini C0")]
    public async Task GmvMiniMultiWordHintsResolveManualBackedCodes(string query, string expectedTitle)
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains(expectedTitle, response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("РЈРєР°Р¶РёС‚Рµ РєРѕРґ", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("РЅРµ РЅР°С€С‘Р»", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("C0")]
    [InlineData("Gree C0")]
    public async Task UnqualifiedC0UsesNeutralSameMeaningGroupOutput(string query)
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains("Gree GMV C0 — нарушение связи", response.Text, StringComparison.Ordinal);
        Assert.Contains("Применимо:", response.Text, StringComparison.Ordinal);
        Assert.Contains("Gree GMV6", response.Text, StringComparison.Ordinal);
        Assert.Contains("Gree GMV Mini", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV6 C0", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("руководства GMV6", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("руководства применимой серии", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("meaningGroupId", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gree-vrf-gmv-communication-c0", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("packageId", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("file_id", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExplicitGmvMiniC0KeepsGmvMiniSpecificTitleAndNextStep()
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree GMV Mini C0"));

        Assert.Contains("Gree GMV Mini C0 — ошибка связи", response.Text, StringComparison.Ordinal);
        Assert.Contains("по сервисному мануалу", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("руководства GMV6", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Применимо:", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LastWorksAfterGmv6DebuggingDiagnostic()
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        await adapter.HandleAsync(Update("Gree GMV6 U0"));
        var last = await adapter.HandleAsync(Update("/last"));

        Assert.Contains("Gree U0", last.Text, StringComparison.Ordinal);
        Assert.Contains("прогрева", last.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LastWorksAfterImprovedU3Diagnostic()
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        await adapter.HandleAsync(Update("Gree GMV6 U3"));
        var last = await adapter.HandleAsync(Update("/last"));

        Assert.Contains("Gree U3", last.Text, StringComparison.Ordinal);
        Assert.Contains("трёхфазн", last.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("фазиров", last.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("классифицирован", last.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Gree d1", "ошибка платы управления внутреннего блока")]
    [InlineData("Gree D1", "ошибка платы управления внутреннего блока")]
    [InlineData("Gree o1", "низкое напряжение шины внутреннего блока")]
    [InlineData("Gree O1", "низкое напряжение шины внутреннего блока")]
    public async Task MixedCaseIndoorCodesUseImprovedManualMeaning(string query, string expectedMeaning)
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains(expectedMeaning, response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Суть:", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Категория:", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Уверенность:", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Источник:", response.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Gree d1", "Gree GMV6 d1")]
    [InlineData("Gree D1", "Gree GMV6 d1")]
    [InlineData("Gree o1", "Gree GMV o1")]
    [InlineData("Gree O1", "Gree GMV o1")]
    [InlineData("Gree l1", "Gree GMV L1")]
    public async Task IndoorManualCodesDisplayCanonicalJsonCasing(string query, string expectedTitle)
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains(expectedTitle, response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LastUsesCanonicalCasingAfterLowercaseO1Lookup()
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        await adapter.HandleAsync(Update("Gree O1"));
        var last = await adapter.HandleAsync(Update("/last"));

        Assert.Contains("Gree o1", last.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree O1", last.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NumericZeroOneResolvesToMiniDebuggingCodeAndNotLowercaseO1()
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree 01"));
        var last = await adapter.HandleAsync(Update("/last"));

        Assert.DoesNotContain("Gree GMV o1", response.Text, StringComparison.Ordinal);
        Assert.Contains("Gree GMV Mini 01", response.Text, StringComparison.Ordinal);
        Assert.Contains("Set master unit", response.Text, StringComparison.Ordinal);
        Assert.Contains("Gree 01", last.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree o1", last.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Gree o1", "Gree GMV o1", "Код: o1 — буква O + цифра 1.")]
    [InlineData("Gree O1", "Gree GMV o1", "Код: o1 — буква O + цифра 1.")]
    [InlineData("Gree L1", "Gree GMV L1", "Код: L1 — буква L + цифра 1.")]
    public async Task ConfusableCanonicalCodesIncludeCompactClarification(
        string query,
        string expectedTitle,
        string expectedClarification)
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Contains(expectedTitle, response.Text, StringComparison.Ordinal);
        Assert.Contains(expectedClarification, response.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Gree Ho")]
    [InlineData("Gree HO")]
    public async Task HoVisualInputRoutesToCanonicalGmv6H0WithClarification(string query)
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains("Gree GMV6 H0", response.Text, StringComparison.Ordinal);
        Assert.Contains("HO/Ho часто означает H0", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree GMV6 Ho", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnqualifiedN2AsksForGreeSeriesRatherThanChoosingMini()
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree n2"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains("Для кода n2 есть несколько вариантов", response.Text, StringComparison.Ordinal);
        Assert.Contains("GMV6", response.Text, StringComparison.Ordinal);
        Assert.Contains("GMV Mini", response.Text, StringComparison.Ordinal);
        Assert.Contains("настройка предела коэффициента соответствия внутренних и наружных блоков", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, CountOccurrences(response.Text, "настройка предела коэффициента соответствия внутренних и наружных блоков"));
        Assert.DoesNotContain("Gree GMV Mini n2 — настройка предела", response.Text.Split('\n').First(), StringComparison.Ordinal);
        Assert.All(ForbiddenFragments(), fragment =>
            Assert.DoesNotContain(fragment, response.Text, StringComparison.OrdinalIgnoreCase));

        var selected = await adapter.HandleAsync(Update("GMV6"));

        Assert.Contains("Gree GMV6 n2", selected.Text, StringComparison.Ordinal);
        Assert.Contains("настройка предела коэффициента соответствия внутренних и наружных блоков", selected.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("не нашёл точную расшифровку", selected.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Gree GMV6 n2", "Gree GMV6 n2")]
    [InlineData("Gree GMV Mini n2", "Gree GMV Mini n2")]
    [InlineData("Gree Mini n2", "Gree GMV Mini n2")]
    public async Task ExplicitN2SeriesSelectsSeparateRuntimeAnswer(
        string query,
        string expectedTitle)
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains(expectedTitle, response.Text, StringComparison.Ordinal);
        Assert.Contains("настройка предела коэффициента соответствия внутренних и наружных блоков", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Уточните серию", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Gree FH")]
    [InlineData("Gree GMV6 FH")]
    public async Task ManualConfirmedFhResolvesToGmv6Answer(string query)
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains("Gree GMV6 FH", response.Text, StringComparison.Ordinal);
        Assert.Contains("датчика тока компрессора 1", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("не нашёл точную расшифровку", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Gree GMV Mini AJ", "сервисное напоминание", "не аварийная защита")]
    [InlineData("Gree GMV Mini n1", "параметрический статус", "не авария")]
    public async Task GmvMiniAnswerClassesDoNotLookLikeActiveFaults(
        string query,
        string expectedClass,
        string expectedNonFaultWording)
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Contains(expectedClass, response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(expectedNonFaultWording, response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Gree GMV Mini AJ")]
    [InlineData("Gree GMV Mini C0")]
    [InlineData("Gree GMV Mini n1")]
    [InlineData("Gree GMV6 C0")]
    [InlineData("Gree o1")]
    [InlineData("Gree L1")]
    [InlineData("Gree U3")]
    [InlineData("Gree U0")]
    [InlineData("Gree H5")]
    [InlineData("Gree d1")]
    public async Task SmokeRelevantOutputsAvoidGenericFillerAndUnsafeProtectionPhrases(string query)
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.DoesNotContain("классифицирован по таблице", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("диагностический вывод должен оставаться", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("если подробная процедура не добавлена", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("не обходить защиты", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("не отключать защиты", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnknownCodeFormatsSafeFallback()
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree ZZ99"));

        Assert.Contains("не нашёл точную расшифровку", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("🔎 Новый код", response.OutboundMessages.Single().ReplyMarkup!.Keyboard!.SelectMany(row => row).Select(button => button.Text));
    }

    [Fact]
    public async Task DisabledAdapterIsDeterministicallyIgnored()
    {
        var facade = new CountingFacade();
        var adapter = CreateAdapter(facade, new EquipmentDiagnosticTelegramOptions());

        var response = await adapter.HandleAsync(Update("Gree H5"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Ignored, response.ResponseKind);
        Assert.Empty(response.Text);
        Assert.Equal(0, facade.CallCount);
    }

    [Fact]
    public async Task UnknownChatIsAutoCreatedAsConsumer()
    {
        var facade = new StaticFacade();
        var adapter = CreateAdapter(facade, EnabledOptions() with { AllowedChatIds = [42] });

        var response = await adapter.HandleAsync(Update("Gree H5"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains("Что можно сделать безопасно", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AllowedChatPassesAccessPolicy()
    {
        var facade = new CountingFacade();
        var adapter = CreateAdapter(facade, EnabledOptions() with { AllowedChatIds = [7] });

        await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.HandleAsync(Update("Gree H5")));

        Assert.Equal(1, facade.CallCount);
    }

    [Fact]
    public async Task DeniedChatIsIgnoredAndDenyWinsOverAllow()
    {
        var facade = new CountingFacade();
        var adapter = CreateAdapter(
            facade,
            EnabledOptions() with { AllowedChatIds = [7], DeniedChatIds = [7] });

        var response = await adapter.HandleAsync(Update("Gree H5"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Ignored, response.ResponseKind);
        Assert.Equal(0, facade.CallCount);
    }

    [Fact]
    public async Task DeniedUsernameIsIgnoredAndDenyWinsOverAllow()
    {
        var facade = new StaticFacade();
        var adapter = CreateAdapter(
            facade,
            EnabledOptions() with { AllowedChatIds = [], AllowedUsernames = ["operator"], DeniedUsernames = ["OPERATOR"] });

        var response = await adapter.HandleAsync(Update("Gree H5"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Ignored, response.ResponseKind);
        Assert.Equal(0, facade.CallCount);
    }

    [Fact]
    public async Task EmptyAllowAndDenyListsAutoCreateConsumer()
    {
        var facade = new StaticFacade();
        var adapter = CreateAdapter(facade, EnabledOptions() with { AllowedChatIds = [] });

        var response = await adapter.HandleAsync(Update("Gree H5"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains("Что можно сделать безопасно", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AllowedUsernameDoesNotElevateUnknownUserAboveConsumer()
    {
        var facade = new StaticFacade();
        var adapter = CreateAdapter(
            facade,
            EnabledOptions() with { AllowedChatIds = [], AllowedUsernames = ["OPERATOR"] });

        var response = await adapter.HandleAsync(Update("Gree H5"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains("Что можно сделать безопасно", response.Text, StringComparison.Ordinal);
        Assert.Equal(1, facade.CallCount);
    }

    [Fact]
    public async Task IdentityDiscoveryWithEmptyAllowlistAllowsOnlyIdentityCommands()
    {
        var facade = new StaticFacade();
        var adapter = CreateAdapter(
            facade,
            EnabledOptions() with { AllowedChatIds = [], EnableChatIdDiscovery = true });

        var identity = await adapter.HandleAsync(Update("/id"));
        var diagnostic = await adapter.HandleAsync(Update("Gree H5"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, identity.ResponseKind);
        Assert.Contains("chatId: 7", identity.Text, StringComparison.Ordinal);
        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, diagnostic.ResponseKind);
        Assert.Contains("Что можно сделать безопасно", diagnostic.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/id")]
    [InlineData("/whoami")]
    public async Task IdentityDiscoveryDisabledDoesNotExposeIdentityOrCallFacade(string command)
    {
        var facade = new CountingFacade();
        var adapter = CreateAdapter(facade, EnabledOptions());

        var response = await adapter.HandleAsync(Update(command));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Unsupported, response.ResponseKind);
        Assert.DoesNotContain("chatId", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("userId", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, facade.CallCount);
    }

    [Theory]
    [InlineData("/id")]
    [InlineData("/whoami")]
    public async Task IdentityDiscoveryEnabledReturnsAccessIdentityWithoutCallingFacade(string command)
    {
        var facade = new CountingFacade();
        var adapter = CreateAdapter(facade, EnabledOptions() with { EnableChatIdDiscovery = true });

        var response = await adapter.HandleAsync(Update(command));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.Contains("chatId: 7", response.Text, StringComparison.Ordinal);
        Assert.Contains("userId: 11", response.Text, StringComparison.Ordinal);
        Assert.Contains("username: operator", response.Text, StringComparison.Ordinal);
        Assert.Contains("BootstrapOwnerChatId", response.Text, StringComparison.Ordinal);
        Assert.Equal(0, facade.CallCount);
        Assert.All(ForbiddenFragments(), fragment =>
            Assert.DoesNotContain(fragment, response.Text, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AdapterResponsesDoNotExposeUnsafeInternalOrSecretText()
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();
        var responses = new[]
        {
            await adapter.HandleAsync(Update("Gree H5")),
            await adapter.HandleAsync(Update("Gree E1")),
            await adapter.HandleAsync(Update("Gree A0")),
            await adapter.HandleAsync(Update("Gree ZZ99"))
        };
        var forbidden = ForbiddenFragments();

        foreach (var response in responses)
        {
            Assert.All(forbidden, fragment =>
                Assert.DoesNotContain(fragment, response.Text, StringComparison.OrdinalIgnoreCase));
            Assert.Null(response.ParseMode);
            Assert.True(response.DisableWebPagePreview);
            Assert.Null(response.InternalDecisionTrace);
        }
    }

    [Fact]
    public void AdapterDependsOnlyOnFacadeParserFormatterAndOptions()
    {
        var constructor = Assert.Single(typeof(EquipmentDiagnosticTelegramAdapter).GetConstructors());
        var dependencyTypes = constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();

        Assert.Equal(
            [
                typeof(IEquipmentDiagnosticBotFacade),
                typeof(EquipmentDiagnosticTelegramMessageParser),
                typeof(EquipmentDiagnosticTelegramResponseFormatter),
                typeof(EquipmentDiagnosticTelegramOptions),
                typeof(ITelegramUserAccessService),
                typeof(ITelegramUserStore),
                typeof(AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations.TelegramDiagnosticConversationService),
                typeof(AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History.TelegramDiagnosticHistoryService),
                typeof(AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests.TelegramServiceRequestService),
                typeof(AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests.TelegramServiceRequestQueueService),
                typeof(TelegramAdminUserManagementService),
                typeof(AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals.TelegramManualLibraryService)
            ],
            dependencyTypes);

        var moduleRoot = Path.Combine(
            TestPaths.RepoRoot,
            "src", "Backend", "AssistantEngineer.Modules.EquipmentDiagnostics", "Application", "Telegram");
        var source = string.Join(
            Environment.NewLine,
            Directory.GetFiles(moduleRoot, "*.cs").Select(File.ReadAllText));
        Assert.DoesNotContain("File" + ".", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Directory" + ".", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Bot" + "Token", source, StringComparison.Ordinal);
        Assert.DoesNotContain("web" + "hook", source, StringComparison.OrdinalIgnoreCase);
    }

    private static EquipmentDiagnosticTelegramAdapter CreateAdapter(
        IEquipmentDiagnosticBotFacade facade,
        EquipmentDiagnosticTelegramOptions options)
    {
        var store = new InMemoryTelegramUserStore();
        var access = new TelegramUserAccessService(store, options);
        return new(
            facade,
            new EquipmentDiagnosticTelegramMessageParser(),
            new EquipmentDiagnosticTelegramResponseFormatter(),
            options,
            access,
            store);
    }

    private static ServiceProvider CreateProvider(EquipmentDiagnosticTelegramOptions options)
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        services.AddSingleton(options);
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

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static IReadOnlyList<string> ForbiddenFragments() =>
    [
        "arti" + "facts/verification",
        "Knowledge/" + "staging",
        "Knowledge/" + "manual-codebook",
        "staging-candidate-" + "preview",
        "manual-" + "codebook",
        "by" + "pass",
        "disable " + "protection",
        "force " + "run",
        "short " + "protection",
        "ignore " + "protection",
        "Bot" + "Token",
        "secret",
        ".pdf"
    ];

    private sealed class CountingFacade : IEquipmentDiagnosticBotFacade
    {
        public int CallCount { get; private set; }

        public Task<EquipmentDiagnosticBotResponse> DiagnoseAsync(
            EquipmentDiagnosticBotRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            throw new InvalidOperationException("The facade should not be called by this test.");
        }
    }

    private sealed class StaticFacade : IEquipmentDiagnosticBotFacade
    {
        public int CallCount { get; private set; }

        public Task<EquipmentDiagnosticBotResponse> DiagnoseAsync(
            EquipmentDiagnosticBotRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new EquipmentDiagnosticBotResponse(
                EquipmentDiagnosticBotResponseStatus.Answer,
                "Gree H5",
                "Possible compressor overload indication.",
                request.Manufacturer ?? "Gree",
                request.Code ?? "H5",
                null,
                new EquipmentDiagnosticBotObservedCodeContext(request.Code ?? "H5", request.Code ?? "H5", request.FreeText),
                new EquipmentDiagnosticBotAnswerCard(
                    "Gree H5",
                    "The unit reports a protected operating condition.",
                    "Verification required.",
                    [],
                    [],
                    [],
                    [],
                    []),
                null,
                null,
                new EquipmentDiagnosticBotSafetyCard("Qualified technician required.", ["Do not bypass protections."]),
                VerificationRequired: true,
                Confidence: DiagnosticConfidence.Medium,
                IsManualVerified: false,
                IsSeedKnowledge: true,
                OperatorNextSteps: ["Share the code with service."],
                Warnings: [],
                InternalDecisionTrace: null));
        }
    }
}
