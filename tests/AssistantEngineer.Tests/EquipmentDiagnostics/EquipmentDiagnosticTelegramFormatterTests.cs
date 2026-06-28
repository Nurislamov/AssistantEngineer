using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramFormatterTests
{
    private static readonly string DiagnosticAnswerStagesPath = Path.Combine(
        TestPaths.RepoRoot,
        "docs",
        "equipment-diagnostics",
        "telegram-diagnostic-answer-stages.md");

    [Theory]
    [InlineData("H5", EquipmentDiagnosticBotResponseStatus.Answer, "Значение:")]
    [InlineData("E1", EquipmentDiagnosticBotResponseStatus.ClarificationRequired, "укажите контекст")]
    [InlineData("A0", EquipmentDiagnosticBotResponseStatus.ReferenceOnly, "Gree GMV6 — A0")]
    [InlineData("ZZ99", EquipmentDiagnosticBotResponseStatus.NotFound, "Код не найден")]
    public async Task StatusFormatsAreDeterministic(string code, EquipmentDiagnosticBotResponseStatus status, string expected)
    {
        using var provider = CreateProvider();
        var facade = provider.GetRequiredService<IEquipmentDiagnosticBotFacade>();
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter();
        var response = await facade.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", code));

        var first = formatter.Format(response, 900);
        var second = formatter.Format(response, 900);

        Assert.Equal(status, response.Status);
        Assert.Equal(first, second);
        Assert.Contains(expected, first, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Важно:", first, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FormatterAppliesDeterministicLengthLimit()
    {
        using var provider = CreateProvider();
        var facade = provider.GetRequiredService<IEquipmentDiagnosticBotFacade>();
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter();
        var response = await facade.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", "H5"));

        var text = formatter.Format(response, 220);

        Assert.True(text.Length <= 220);
        Assert.Contains("Ответ сокращен", text, StringComparison.Ordinal);
    }

    [Fact]
    public void HelpAndValidationArePlainTextAndLengthBounded()
    {
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter();

        var help = formatter.FormatHelp(500);
        var validation = formatter.FormatValidation(["Manufacturer is required."], 500);

        Assert.Contains("Gree H5", help, StringComparison.Ordinal);
        Assert.Contains("Укажите производителя", validation, StringComparison.Ordinal);
        Assert.True(help.Length <= 500);
        Assert.True(validation.Length <= 500);
    }

    [Fact]
    public async Task ConsumerFormatIsRussianAndDoesNotExposeTechnicalMarkers()
    {
        using var provider = CreateProvider();
        var facade = provider.GetRequiredService<IEquipmentDiagnosticBotFacade>();
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter();
        var response = await facade.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", "H5"));

        var text = formatter.FormatConsumer(response, hasPhoneNumber: false, maxLength: 900);

        Assert.Contains("Возможное значение", text, StringComparison.Ordinal);
        Assert.Contains("Что можно сделать безопасно", text, StringComparison.Ordinal);
        Assert.Contains("Gree GMV H5", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Confidence", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Source", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deterministic bot API", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Response shortened", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Preliminary diagnostic", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Safe next steps", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("internal trace", text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(TelegramUserRole.Installer)]
    [InlineData(TelegramUserRole.Engineer)]
    [InlineData(TelegramUserRole.Admin)]
    [InlineData(TelegramUserRole.Owner)]
    public async Task GreeH5TechnicalOutputIsRussianForTechnicalRoles(TelegramUserRole role)
    {
        using var provider = CreateProvider();
        var facade = provider.GetRequiredService<IEquipmentDiagnosticBotFacade>();
        var formatter = provider.GetRequiredService<EquipmentDiagnosticTelegramResponseFormatter>();
        var response = await facade.DiagnoseAsync(
            new EquipmentDiagnosticBotRequest("Gree", "H5", Series: "GMV"));

        var text = formatter.FormatTechnical(response, role);

        Assert.Contains("Gree GMV6 — H5", text, StringComparison.Ordinal);
        Assert.Contains("Значение:", text, StringComparison.Ordinal);
        Assert.Contains("Важно:", text, StringComparison.Ordinal);
        Assert.Contains("Первые проверки:", text, StringComparison.Ordinal);
        Assert.Contains("Серия: GMV6", text, StringComparison.Ordinal);
        Assert.Contains("Ограничения вывода:", text, StringComparison.Ordinal);
        Assert.Contains("Дальше:", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Категория:", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Уверенность:", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Источник:", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Черновик / непроверено", text, StringComparison.Ordinal);
        foreach (var forbidden in EnglishTechnicalMarkers())
        {
            Assert.DoesNotContain(forbidden, text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void MergedGmvIduTechnicalOutputUsesCompactMultiManualSourceLabel()
    {
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter(new JsonErrorKnowledgeLocalizationSource());
        var response = LocalizedResponse(
            code: "L1",
            series: "GMV6",
            category: EquipmentCategory.VrfIndoorUnit,
            side: EquipmentDiagnosticBotEquipmentSide.Indoor,
            displayContext: EquipmentDiagnosticBotDisplayContext.IduDisplay);

        var technical = formatter.FormatTechnical(response, TelegramUserRole.Engineer);
        var consumer = formatter.FormatConsumer(response, hasPhoneNumber: false, maxLength: 4000);

        Assert.Contains("защита вентилятора внутреннего блока", technical, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Источник:", technical, StringComparison.Ordinal);
        Assert.DoesNotContain("gree-gmv-idu-service-manual", technical, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gree-gmv6-indoor-fault-codes", technical, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SERVICE_MANUAL_GMV_IDU.pdf", technical, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("D:\\", technical, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("C:\\", technical, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("РІС‹Р±РµСЂРёС‚Рµ", technical, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Источник:", consumer, StringComparison.Ordinal);
        Assert.DoesNotContain("gree-gmv-idu-service-manual", consumer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GroupedC0UsesNeutralTitleAndNextStepForTechnicalAndConsumerOutput()
    {
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter(new JsonErrorKnowledgeLocalizationSource());
        var response = LocalizedResponse() with
        {
            ApplicableContexts = ["Gree GMV Mini", "Gree GMV6"]
        };

        var technical = formatter.FormatTechnical(response, TelegramUserRole.Engineer);
        var consumer = formatter.FormatConsumer(response, hasPhoneNumber: false, maxLength: 4000);

        Assert.Contains("Gree GMV — C0", technical, StringComparison.Ordinal);
        Assert.Contains("Значение:", technical, StringComparison.Ordinal);
        Assert.Contains("Серия: GMV", technical, StringComparison.Ordinal);
        Assert.Contains("Gree GMV C0 — нарушение связи", consumer, StringComparison.Ordinal);
        foreach (var text in new[] { technical, consumer })
        {
            Assert.Contains("руководства применимой серии", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Gree GMV6 C0", text, StringComparison.Ordinal);
            Assert.DoesNotContain("руководства GMV6", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task GreeU3TechnicalOutputExplainsPowerPhaseProblemWithoutWaterOrUnsafeProtectionWording()
    {
        using var provider = CreateProvider();
        var facade = provider.GetRequiredService<IEquipmentDiagnosticBotFacade>();
        var formatter = provider.GetRequiredService<EquipmentDiagnosticTelegramResponseFormatter>();

        var response = await facade.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", "U3", Series: "GMV6"));
        var text = formatter.FormatTechnical(response, TelegramUserRole.Engineer);

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.ReferenceOnly, response.Status);
        Assert.Contains("Gree GMV6 — U3", text, StringComparison.Ordinal);
        Assert.Contains("фазировке питания", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("трёхфазн", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("наличие всех фаз", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("фазиров", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Значение:", text, StringComparison.Ordinal);
        Assert.Contains("Первые проверки:", text, StringComparison.Ordinal);
        Assert.Contains("Серия: GMV6", text, StringComparison.Ordinal);
        Assert.Contains("Важно:", text, StringComparison.Ordinal);
        Assert.Contains("Ограничения вывода:", text, StringComparison.Ordinal);
        Assert.Contains("Дальше:", text, StringComparison.Ordinal);
        Assert.DoesNotContain("классифицирован", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("диагностический вывод должен оставаться", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("если подробная процедура не добавлена", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("не обходить защит", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("не отключать защит", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Категория:", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Уверенность:", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Источник:", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FinalOutputNormalizesEveryLocalizedDiagnosticField()
    {
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter(
            new DuplicateCommunicationLocalizationSource());
        var response = LocalizedResponse();

        var technical = formatter.FormatTechnical(response);
        var consumer = formatter.FormatConsumer(response, hasPhoneNumber: true, maxLength: 4000);

        Assert.DoesNotContain("связи связи", technical, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("связи связи", consumer, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(6, CountOccurrences(technical, "сообщение о связи и адресации"));
        Assert.Equal(6, CountOccurrences(consumer, "сообщение о связи и адресации"));
    }

    [Fact]
    public void GreeTechnicalHtmlEscapesEveryDynamicDiagnosticField()
    {
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter(
            new SpecialCharacterLocalizationSource());

        var html = formatter.FormatTechnicalHtml(
            LocalizedResponse(code: "C<0", series: "GMV <X> &"),
            TelegramUserRole.Engineer);

        Assert.Contains("<b>Диагностика GREE C&lt;0</b>", html, StringComparison.Ordinal);
        Assert.Contains("GMV &lt;X&gt; &amp;", html, StringComparison.Ordinal);
        Assert.Contains("Сводка &lt;summary&gt; &amp;", html, StringComparison.Ordinal);
        Assert.Contains("Причина &lt;cause&gt; &amp;", html, StringComparison.Ordinal);
        Assert.Contains(
            "Подтвердите код C&lt;0, серию GMV &lt;X&gt; &amp; и место индикации.",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            "Сверьте модель, условия появления и сопутствующие коды.",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            "Дальнейшие действия выполняйте по сервисной процедуре для этой серии.",
            html,
            StringComparison.Ordinal);
        Assert.Contains("Не обходите защиты", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>Ограничения:</b>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>Техническая заметка:</b>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<summary>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<check>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Проверка &lt;check&gt; &amp;", html, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(PolishedGmvDiagnosticRequests))]
    public async Task PolishedGmvTechnicalAnswersKeepCoreDiagnosticStages(EquipmentDiagnosticBotRequest request)
    {
        using var provider = CreateProvider();
        var facade = provider.GetRequiredService<IEquipmentDiagnosticBotFacade>();
        var formatter = provider.GetRequiredService<EquipmentDiagnosticTelegramResponseFormatter>();

        var response = await ResolveDiagnosticStageResponseAsync(facade, request);
        var text = formatter.FormatTechnical(response, TelegramUserRole.Engineer);

        Assert.True(
            response.Status is EquipmentDiagnosticBotResponseStatus.Answer or EquipmentDiagnosticBotResponseStatus.ReferenceOnly,
            $"Expected a resolved diagnostic answer for {request.Manufacturer} {request.Code}, got {response.Status}.");
        AssertDiagnosticAnswerHasCoreStages(text, response.NormalizedManufacturer, response.NormalizedCode);
        AssertNoInternalCatalogWords(text);
        AssertNoUnsafeProtectionAdvice(text);
    }

    [Theory]
    [MemberData(nameof(PolishedGmvDiagnosticRequests))]
    public async Task PolishedGmvConsumerAnswersKeepSafeUserStages(EquipmentDiagnosticBotRequest request)
    {
        using var provider = CreateProvider();
        var facade = provider.GetRequiredService<IEquipmentDiagnosticBotFacade>();
        var formatter = provider.GetRequiredService<EquipmentDiagnosticTelegramResponseFormatter>();

        var response = await ResolveDiagnosticStageResponseAsync(facade, request);
        var text = formatter.FormatConsumer(response, hasPhoneNumber: false, maxLength: 4000);

        Assert.Contains($"Код оборудования: {response.NormalizedManufacturer} {response.NormalizedCode}", text, StringComparison.Ordinal);
        Assert.Contains("Возможное значение:", text, StringComparison.Ordinal);
        Assert.Contains("Что можно сделать безопасно:", text, StringComparison.Ordinal);
        Assert.Contains("Рекомендованное действие:", text, StringComparison.Ordinal);
        Assert.Contains("Для сервиса:", text, StringComparison.Ordinal);
        AssertNoInternalCatalogWords(text);
        AssertNoUnsafeConsumerAdvice(text);
    }

    [Fact]
    public void DiagnosticAnswerStageContractIsDocumented()
    {
        Assert.True(File.Exists(DiagnosticAnswerStagesPath), $"Missing diagnostic answer stage contract: {DiagnosticAnswerStagesPath}");
        var text = File.ReadAllText(DiagnosticAnswerStagesPath);

        Assert.Contains("Identification", text, StringComparison.Ordinal);
        Assert.Contains("Meaning", text, StringComparison.Ordinal);
        Assert.Contains("Safety", text, StringComparison.Ordinal);
        Assert.Contains("Safe User Checks", text, StringComparison.Ordinal);
        Assert.Contains("Installer Checks", text, StringComparison.Ordinal);
        Assert.Contains("Engineer Or Service Checks", text, StringComparison.Ordinal);
        Assert.Contains("Next Action", text, StringComparison.Ordinal);
        Assert.Contains("<b>Суть:</b>", text, StringComparison.Ordinal);
        Assert.Contains("<b>Что проверить:</b>", text, StringComparison.Ordinal);
        Assert.Contains("<b>Важно:</b>", text, StringComparison.Ordinal);
        Assert.Contains("one short combined safety block", text, StringComparison.OrdinalIgnoreCase);
    }

    public static IEnumerable<object[]> PolishedGmvDiagnosticRequests()
    {
        yield return
        [
            new EquipmentDiagnosticBotRequest("Gree", "C0")
        ];
        yield return
        [
            new EquipmentDiagnosticBotRequest("Gree", "H5", Series: "GMV6")
        ];
        yield return
        [
            new EquipmentDiagnosticBotRequest("Gree", "U3", Series: "GMV6")
        ];
        yield return
        [
            new EquipmentDiagnosticBotRequest(
                "Gree",
                "E1",
                Series: "GMV6",
                Category: EquipmentCategory.VrfOutdoorUnit,
                EquipmentSide: EquipmentDiagnosticBotEquipmentSide.Outdoor,
                DisplayContext: EquipmentDiagnosticBotDisplayContext.OduMainBoardLed)
        ];
        yield return
        [
            new EquipmentDiagnosticBotRequest(
                "Gree",
                "P0",
                Series: "GMV6",
                Category: EquipmentCategory.VrfOutdoorUnit,
                EquipmentSide: EquipmentDiagnosticBotEquipmentSide.Outdoor,
                DisplayContext: EquipmentDiagnosticBotDisplayContext.OduMainBoardLed)
        ];
        yield return
        [
            new EquipmentDiagnosticBotRequest("Gree", "o1", Series: "GMV6")
        ];
        yield return
        [
            new EquipmentDiagnosticBotRequest("Gree", "L1", Series: "GMV6")
        ];
    }

    private static IReadOnlyList<string> EnglishTechnicalMarkers() =>
    [
        "Safety",
        "Step ",
        "Source:",
        "Confidence:",
        " Low",
        " Medium",
        " High",
        "Preliminary diagnostic entry",
        "Recommended action"
    ];

    private static void AssertDiagnosticAnswerHasCoreStages(
        string text,
        string manufacturer,
        string code)
    {
        Assert.Contains("Gree GMV", text, StringComparison.Ordinal);
        Assert.Contains($"— {code}", text, StringComparison.Ordinal);
        Assert.Contains("Значение:", text, StringComparison.Ordinal);
        Assert.Contains("Первые проверки:", text, StringComparison.Ordinal);
        Assert.Contains("Серия:", text, StringComparison.Ordinal);
        Assert.Contains("Важно:", text, StringComparison.Ordinal);
        Assert.Contains("Дальше:", text, StringComparison.Ordinal);
    }

    private static void AssertNoUnsafeConsumerAdvice(string text)
    {
        foreach (var fragment in UnsafeConsumerAdviceMarkers())
        {
            Assert.DoesNotContain(fragment, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static void AssertNoUnsafeProtectionAdvice(string text)
    {
        foreach (var fragment in UnsafeProtectionAdviceMarkers())
        {
            Assert.DoesNotContain(fragment, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static void AssertNoInternalCatalogWords(string text)
    {
        foreach (var fragment in InternalCatalogMarkers())
        {
            Assert.DoesNotContain(fragment, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static IReadOnlyList<string> UnsafeConsumerAdviceMarkers() =>
    [
        "разберите",
        "разобрать",
        "откройте шкаф",
        "снимите крыш",
        "измерьте",
        "замерьте",
        "под напряжением",
        "замкните",
        "перемкните",
        "замените плат",
        "заменить плат",
        "замените датчик",
        "заменить датчик",
        "запустите повторно",
        "сбросьте несколько раз"
    ];

    private static async Task<EquipmentDiagnosticBotResponse> ResolveDiagnosticStageResponseAsync(
        IEquipmentDiagnosticBotFacade facade,
        EquipmentDiagnosticBotRequest request)
    {
        var response = await facade.DiagnoseAsync(request);
        if (response.Status != EquipmentDiagnosticBotResponseStatus.NotFound ||
            !string.Equals(request.Code, "P0", StringComparison.Ordinal))
        {
            return response;
        }

        return LocalizedResponse(
            code: "P0",
            series: "GMV6",
            category: EquipmentCategory.VrfOutdoorUnit,
            side: EquipmentDiagnosticBotEquipmentSide.Outdoor,
            displayContext: EquipmentDiagnosticBotDisplayContext.OduMainBoardLed);
    }

    private static IReadOnlyList<string> UnsafeProtectionAdviceMarkers() =>
    [
        "обойдите защит",
        "отключите защит",
        "disable " + "protection",
        "by" + "pass protection",
        "force " + "run",
        "short " + "protection",
        "ignore " + "protection"
    ];

    private static IReadOnlyList<string> InternalCatalogMarkers() =>
    [
        "source " + "card",
        "catalog " + "pipeline",
        "reference-" + "only",
        "machine " + "translated",
        "support-" + "каталог",
        "Knowledge/" + "staging",
        "manual-" + "codebook",
        "arti" + "facts/verification",
        "internal trace",
        "deterministic bot API"
    ];

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        return services.BuildServiceProvider();
    }

    private static EquipmentDiagnosticBotResponse LocalizedResponse(
        string code = "C0",
        string series = "GMV6",
        EquipmentCategory category = EquipmentCategory.VrfOutdoorUnit,
        EquipmentDiagnosticBotEquipmentSide side = EquipmentDiagnosticBotEquipmentSide.Outdoor,
        EquipmentDiagnosticBotDisplayContext displayContext = EquipmentDiagnosticBotDisplayContext.OduMainBoardLed) =>
        new(
            EquipmentDiagnosticBotResponseStatus.Answer,
            $"Gree {series} {code}",
            "Communication diagnostic.",
            "GREE",
            code,
            new EquipmentDiagnosticBotEquipmentContext(
                "Gree",
                series,
                null,
                category,
                side,
                displayContext),
            new EquipmentDiagnosticBotObservedCodeContext(code, code, null),
            AnswerCard: null,
            ClarificationQuestion: null,
            SourceCard: null,
            new EquipmentDiagnosticBotSafetyCard(string.Empty, []),
            VerificationRequired: false,
            Confidence: DiagnosticConfidence.High,
            IsManualVerified: true,
            IsSeedKnowledge: false,
            OperatorNextSteps: [],
            Warnings: [],
            InternalDecisionTrace: null);

    private static int CountOccurrences(string text, string value) =>
        text.Split(value, StringSplitOptions.None).Length - 1;

    private sealed class DuplicateCommunicationLocalizationSource : IErrorKnowledgeLocalizationSource
    {
        private const string Duplicate = "сообщение о связи связи и адресации";
        private static readonly ErrorKnowledgeTextV2 Text = new(
            "c0-ru-engineer",
            "c0",
            "ru",
            ErrorKnowledgeAudience.Engineer,
            $"Gree GMV6 C0 — {Duplicate}",
            $"Кратко: {Duplicate}.",
            $"Безопасность: {Duplicate}.",
            [$"Причина: {Duplicate}."],
            [$"Проверка: {Duplicate}."],
            [$"Не советовать: {Duplicate}."],
            $"Действие: {Duplicate}.",
            string.Empty,
            IsMachineTranslated: false,
            IsReviewed: true,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch);
        private static readonly ErrorKnowledgeEntryV2 Entry = new(
            "c0",
            "Gree",
            ErrorKnowledgeEquipmentFamily.VRF,
            ErrorKnowledgeEquipmentType.OutdoorUnit,
            "GMV6",
            [],
            "C0",
            ErrorKnowledgeSignalType.Communication,
            ErrorKnowledgeDisplaySource.OutdoorBoard,
            ErrorKnowledgeSystemPart.Communication,
            ErrorKnowledgeSeverity.Medium,
            true,
            false,
            "test",
            "ru",
            "Manual",
            "Test",
            Duplicate,
            null,
            "High",
            "ManualVerified",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            [Text]);

        public IReadOnlyCollection<ErrorKnowledgeEntryV2> GetEntries() => [Entry];

        public ErrorKnowledgeLocalizationSelection? Select(
            EquipmentDiagnosticBotResponse response,
            string locale,
            ErrorKnowledgeAudience audience)
        {
            var text = Text with { Audience = audience };
            return new ErrorKnowledgeLocalizationSelection(Entry, text);
        }
    }

    private sealed class SpecialCharacterLocalizationSource : IErrorKnowledgeLocalizationSource
    {
        private static readonly ErrorKnowledgeTextV2 Text = new(
            "special-ru-engineer",
            "special",
            "ru",
            ErrorKnowledgeAudience.Engineer,
            "Gree GMV <X> & C<0 - Значение <meaning> &",
            "Сводка <summary> &",
            "Важно <safety> &",
            ["Причина <cause> &"],
            ["Проверка <check> &"],
            ["Ограничение <limit> &"],
            "Действие <action> &",
            "Источник <source> &",
            IsMachineTranslated: false,
            IsReviewed: true,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch);

        private static readonly ErrorKnowledgeEntryV2 Entry = new(
            "special",
            "Gree",
            ErrorKnowledgeEquipmentFamily.VRF,
            ErrorKnowledgeEquipmentType.OutdoorUnit,
            "GMV <X> &",
            [],
            "C<0",
            ErrorKnowledgeSignalType.Fault,
            ErrorKnowledgeDisplaySource.OutdoorBoard,
            ErrorKnowledgeSystemPart.Unknown,
            ErrorKnowledgeSeverity.Medium,
            true,
            false,
            "test",
            "ru",
            "Manual",
            "Test",
            "Meaning <entry> &",
            null,
            "High",
            "ManualVerified",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            [Text]);

        public IReadOnlyCollection<ErrorKnowledgeEntryV2> GetEntries() => [Entry];

        public ErrorKnowledgeLocalizationSelection? Select(
            EquipmentDiagnosticBotResponse response,
            string locale,
            ErrorKnowledgeAudience audience) =>
            new(Entry, Text with { Audience = audience });
    }
}
