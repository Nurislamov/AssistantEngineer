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
        Assert.Equal("HTML", response.ParseMode);
        Assert.Contains("<b>Суть:</b>", response.Text, StringComparison.Ordinal);
        Assert.Contains("<b>Что проверить:</b>", response.Text, StringComparison.Ordinal);
        Assert.Contains("<b>Важно:</b>", response.Text, StringComparison.Ordinal);
        Assert.Contains("только по одному коду", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Не обходите защиты", response.Text, StringComparison.Ordinal);
        Assert.Contains("квалифицированные специалисты", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<b>Ограничения:</b>", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>Техническая заметка:</b>", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Ограничения вывода:", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Дальше:", response.Text, StringComparison.Ordinal);
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

        Assert.Contains("Gree GMV6 — E1", outdoor.Text, StringComparison.Ordinal);
        Assert.Contains("высокому давлению", outdoor.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<b>Суть:</b>", outdoor.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Категория:", outdoor.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReferenceOnlyCodeIsNotFormattedAsFaultDiagnosis()
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree GMV6 A0"));

        Assert.Contains("Gree GMV6 — A0", response.Text, StringComparison.Ordinal);
        Assert.Contains("пусконаладк", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<b>Суть:</b>", response.Text, StringComparison.Ordinal);
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
        Assert.Contains("Gree GMV6 — U0", response.Text, StringComparison.Ordinal);
        Assert.Contains("<b>Суть:</b>", response.Text, StringComparison.Ordinal);
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

        Assert.Contains("Gree GMV6 — C0", response.Text, StringComparison.Ordinal);
        Assert.Contains("Нарушение связи", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("линию связи GMV", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<b>Что проверить:</b>", response.Text, StringComparison.Ordinal);
        Assert.Contains("Подтвердите код C0, серию GMV6 и место индикации.", response.Text, StringComparison.Ordinal);
        Assert.Contains("Сверьте модель, условия появления и сопутствующие коды.", response.Text, StringComparison.Ordinal);
        Assert.Contains("сервисной процедуре для этой серии", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Категория:", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("классифицирован", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("связи связи", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Gree C0", last.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("связи связи", last.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Gree GMV Mini AJ", "Gree GMV Mini — AJ")]
    [InlineData("GMV Mini AJ", "Gree GMV Mini — AJ")]
    [InlineData("Gree GMV Mini C0", "Gree GMV Mini — C0")]
    [InlineData("GMV Mini C0", "Gree GMV Mini — C0")]
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
        Assert.Contains("Gree GMV — C0", response.Text, StringComparison.Ordinal);
        Assert.Contains("Нарушение связи", response.Text, StringComparison.OrdinalIgnoreCase);
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

        Assert.Contains("Gree GMV Mini — C0", response.Text, StringComparison.Ordinal);
        Assert.Contains("Нарушение связи", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("точная причина зависит", response.Text, StringComparison.OrdinalIgnoreCase);
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
        Assert.Contains("<b>Суть:</b>", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Категория:", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Уверенность:", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Источник:", response.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Gree d1", "Gree GMV6 — d1")]
    [InlineData("Gree D1", "Gree GMV6 — d1")]
    [InlineData("Gree o1", "Gree GMV6 — o1")]
    [InlineData("Gree O1", "Gree GMV6 — o1")]
    [InlineData("Gree l1", "Gree GMV6 — L1")]
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
        Assert.Contains("Gree GMV Mini — 01", response.Text, StringComparison.Ordinal);
        Assert.Contains("назначение главного блока", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Gree 01", last.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Gree o1", last.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Gree o1", "Gree GMV6 — o1", "Код: o1 — буква O + цифра 1.")]
    [InlineData("Gree O1", "Gree GMV6 — o1", "Код: o1 — буква O + цифра 1.")]
    [InlineData("Gree L1", "Gree GMV6 — L1", "Код: L1 — буква L + цифра 1.")]
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
        Assert.Contains("Gree GMV6 — H0", response.Text, StringComparison.Ordinal);
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
        Assert.Contains("Код n2 найден в нескольких сериях Gree.", response.Text, StringComparison.Ordinal);
        Assert.Contains("Выберите серию:", response.Text, StringComparison.Ordinal);
        Assert.Contains("GMV6", response.Text, StringComparison.Ordinal);
        Assert.Contains("GMV Mini", response.Text, StringComparison.Ordinal);
        Assert.Contains("GMV X", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("GMV6 HR", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("GMV9 Flex", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.All(ForbiddenFragments(), fragment =>
            Assert.DoesNotContain(fragment, response.Text, StringComparison.OrdinalIgnoreCase));
        var buttons = response.OutboundMessages.Single().ReplyMarkup!.Keyboard!.SelectMany(row => row).Select(button => button.Text);
        Assert.Contains("GMV Mini", buttons);
        Assert.Contains("GMV6", buttons);
        Assert.Contains("GMV X", buttons);
        Assert.Contains("Не знаю", buttons);
        Assert.Contains("🔎 Новый код", buttons);

        var selected = await adapter.HandleAsync(Update("GMV6"));

        Assert.Contains("Gree GMV6 — n2", selected.Text, StringComparison.Ordinal);
        Assert.Contains("настройка предела коэффициента соответствия внутренних и наружных блоков", selected.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("не нашёл точную расшифровку", selected.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SelectingGmvXAfterUnqualifiedN2ReturnsGmvXN2()
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        _ = await adapter.HandleAsync(Update("Gree n2"));
        var selected = await adapter.HandleAsync(Update("GMV X"));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, selected.ResponseKind);
        Assert.Contains("Gree GMV X — n2", selected.Text, StringComparison.Ordinal);
        Assert.Contains("настройка предела коэффициента соответствия внутренних и наружных блоков", selected.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Gree GMV6 n2", selected.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Gree GMV6 HR n2", selected.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Gree GMV Mini n2", selected.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Gree GMV6 n2", "Gree GMV6 — n2")]
    [InlineData("Gree GMV Mini n2", "Gree GMV Mini — n2")]
    [InlineData("Gree Mini n2", "Gree GMV Mini — n2")]
    [InlineData("Gree GMV X n2", "Gree GMV X — n2")]
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
    [InlineData("Gree GMV9 Flex E0", "GC202512-I")]
    [InlineData("Gree GMV9 H5", "GC202512-I")]
    [InlineData("Gree 9 series Flex C0", "GC202512-I")]
    [InlineData("Gree 9-Flex A0", "GC202512-I")]
    [InlineData("Gree GMV X E0", "GC202209-I")]
    [InlineData("Gree GMV X n2", "GC202209-I")]
    [InlineData("Gree GMV6 A9", "GC202203-IV")]
    [InlineData("Gree GMV6 Uy", "GC202203-IV")]
    public async Task ManualBackedTelegramAnswersHideTechnicalManualReferences(string query, string documentCode)
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update(query));

        Assert.Equal(EquipmentDiagnosticTelegramResponseKind.Reply, response.ResponseKind);
        Assert.DoesNotContain(documentCode, response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PDF page", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("manual page", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Chapter 3 Faults", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Error Indication", response.Text, StringComparison.OrdinalIgnoreCase);
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
        Assert.Contains("Gree GMV6 — FH", response.Text, StringComparison.Ordinal);
        Assert.Contains("датчика тока компрессора 1", response.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("не нашёл точную расшифровку", response.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Gree GMV Mini AJ", "предупреждение о необходимости очистки фильтра", "не самостоятельный признак отказа компонента")]
    [InlineData("Gree GMV Mini n1", "настройка периода оттайки K1", "не самостоятельный признак отказа компонента")]
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
            Assert.True(
                response.ParseMode is null or "HTML",
                $"Unexpected parse mode: {response.ParseMode}");
            if (response.ParseMode == "HTML")
            {
                Assert.Equal("HTML", response.OutboundMessages.Single().ParseMode);
            }
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
                typeof(AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals.TelegramManualLibraryService),
                typeof(AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.OperatorInbox.ITelegramOperatorInboxService)
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
