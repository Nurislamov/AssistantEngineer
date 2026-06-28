using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class MultiSourceDiagnosticReferenceTests
{
    [Fact]
    public async Task SameCodeSameEquipmentMeaningWithMultipleSourcesReturnsOneAnswer()
    {
        using var provider = CreateProvider();
        var service = provider.GetRequiredService<IEquipmentDiagnosticBotService>();
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter(new MultiSourceLocalizationSource());

        var response = await service.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", "H5", Series: "GMV"));
        var text = formatter.FormatTechnical(response, TelegramUserRole.Engineer);

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.Answer, response.Status);
        Assert.Null(response.ClarificationQuestion);
        Assert.Contains("Значение:", text, StringComparison.Ordinal);
        Assert.Contains("Первые проверки:", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Источник:", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Уверенность:", text, StringComparison.Ordinal);
        Assert.DoesNotContain("выберите", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("manual", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gree-gmv6-outdoor-fault-protection-codes", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SERVICE_MANUAL_GMV_IDU.pdf", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("D:\\", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("C:\\", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LastCommandSummaryStillWorksWithMultiSourceReferences()
    {
        var store = new InMemoryTelegramDiagnosticCaseStore();
        var user = EngineerUser();
        await store.CreateAsync(new TelegramDiagnosticCaseCreate(
            user.Id,
            TelegramConversationSessionId: null,
            TelegramDiagnosticCaseStatus.Completed,
            TelegramUserRole.Engineer,
            TelegramDiagnosticCaseResponseMode.Technical,
            "H5",
            "Gree",
            EquipmentType: null,
            DisplayContext: null,
            ResultSummary: "Stored summary should be replaced by localized source.",
            NormalizedRequestJson: null,
            CandidateCount: 1,
            PhoneWasSaved: false,
            PhoneNumberSource: null,
            CreatedAt: new DateTimeOffset(2026, 6, 21, 10, 0, 0, TimeSpan.Zero)));
        var history = new TelegramDiagnosticHistoryService(
            store,
            new TelegramDisplayTimeFormatter(
                new EquipmentDiagnosticTelegramOptions
                {
                    IsEnabled = true,
                    DisplayTimeZone = "Asia/Tashkent"
                }),
            new MultiSourceLocalizationSource());

        var text = await history.FormatLastAsync(user);

        Assert.Contains("Последняя диагностика", text, StringComparison.Ordinal);
        Assert.Contains("защита инверторного вентилятора по току", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gree-gmv6-outdoor-fault-protection-codes", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SERVICE_MANUAL_GMV_IDU.pdf", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("D:\\", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("C:\\", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DifferentEquipmentMeaningAsksEquipmentClarificationNotManualSelection()
    {
        var service = new EquipmentDiagnosticBotService(
            new FakeDiagnosticsService([
                Summary("E1", "GMV6", EquipmentCategory.VrfIndoorUnit, "Indoor E1"),
                Summary("E1", "GMV6", EquipmentCategory.VrfOutdoorUnit, "Outdoor E1")
            ]),
            new MultiSourceLocalizationSource());
        var formatter = new EquipmentDiagnosticTelegramResponseFormatter(new MultiSourceLocalizationSource());

        var response = await service.DiagnoseAsync(new EquipmentDiagnosticBotRequest("Gree", "E1"));
        var text = formatter.FormatTechnical(response, TelegramUserRole.Engineer);

        Assert.Equal(EquipmentDiagnosticBotResponseStatus.ClarificationRequired, response.Status);
        Assert.NotNull(response.ClarificationQuestion);
        Assert.Contains(response.ClarificationQuestion.Options, option => option.EquipmentSide == EquipmentDiagnosticBotEquipmentSide.Indoor);
        Assert.Contains(response.ClarificationQuestion.Options, option => option.EquipmentSide == EquipmentDiagnosticBotEquipmentSide.Outdoor);
        Assert.Contains("внутренний блок", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("наружный блок", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("руководств", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("source", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("package", text, StringComparison.OrdinalIgnoreCase);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        return services.BuildServiceProvider();
    }

    private static TelegramUserSnapshot EngineerUser() =>
        new(
            Id: 1,
            TelegramChatId: 100,
            TelegramUserId: 200,
            Username: "engineer",
            FirstName: "Engineer",
            LastName: null,
            TelegramUserRole.Engineer,
            IsEnabled: true,
            IsBlocked: false,
            PhoneNumberVerified: false,
            HasPhoneNumber: false,
            PhoneNumberSource: null,
            CreatedAt: DateTimeOffset.UnixEpoch,
            LastSeenAt: null,
            LastAccessDeniedAt: null);

    private static EquipmentErrorCodeSummaryDto Summary(
        string code,
        string series,
        EquipmentCategory category,
        string meaning = "Synthetic meaning") =>
        new("Gree", series, null, code, $"{code} title", meaning, "Service review", category, DiagnosticConfidence.Low, null);

    private sealed class MultiSourceLocalizationSource : IErrorKnowledgeLocalizationSource
    {
        private static readonly ErrorKnowledgeTextV2 EngineerText = new(
            "h5-ru-engineer",
            "h5",
            "ru",
            ErrorKnowledgeAudience.Engineer,
            "Gree GMV6 H5 - защита инверторного вентилятора по току",
            "Защита инверторного вентилятора по току.",
            "Проверки выполняет квалифицированный специалист.",
            ["Перегрузка или неисправность цепи вентилятора."],
            ["Сверить модель и место отображения кода."],
            ["Не выбирать руководство как диагностический вариант."],
            "Сверить установленную модель и выполнить безопасную проверку.",
            "Источник: руководства производителя.",
            IsMachineTranslated: false,
            IsReviewed: true,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch);

        private static readonly ErrorKnowledgeEntryV2 Entry = new(
            "h5",
            "Gree",
            ErrorKnowledgeEquipmentFamily.VRF,
            ErrorKnowledgeEquipmentType.OutdoorUnit,
            "GMV6",
            [],
            "H5",
            ErrorKnowledgeSignalType.Protection,
            ErrorKnowledgeDisplaySource.OutdoorBoard,
            ErrorKnowledgeSystemPart.Fan,
            ErrorKnowledgeSeverity.High,
            true,
            false,
            "gree-gmv6-outdoor-fault-protection-codes",
            "en",
            "Manual",
            "Gree GMV6 service manual",
            "Over-current protection of inverter fan",
            "Manual page 1 / PDF page 2",
            "High",
            "ManualVerified",
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            [EngineerText])
        {
            SourceReferences =
            [
                new(
                    "Gree GMV6 service manual",
                    "GC202001-I",
                    "Manual page 1 / PDF page 2",
                    "Manual",
                    "en",
                    "ManualVerified",
                    "High",
                    "gree-gmv6-service-manual-2020-09",
                    "gree-gmv6-outdoor-fault-protection-codes",
                    "Primary source."),
                new(
                    "Gree GMV IDU service manual",
                    "GC202004-X",
                    "Manual page 173 / PDF page 178",
                    "Manual",
                    "en",
                    "ManualVerified",
                    "High",
                    "gree-gmv-idu-service-manual",
                    "gree-gmv6-indoor-fault-codes",
                    "Local path D:\\Project\\AssistantEngineer\\artifacts\\manual-intake\\sources\\gree\\SERVICE_MANUAL_GMV_IDU.pdf is intentionally not displayed.")
            ]
        };

        public IReadOnlyCollection<ErrorKnowledgeEntryV2> GetEntries() => [Entry];

        public ErrorKnowledgeLocalizationSelection? Select(
            EquipmentDiagnosticBotResponse response,
            string locale,
            ErrorKnowledgeAudience audience)
        {
            if (!response.NormalizedManufacturer.Equals("GREE", StringComparison.OrdinalIgnoreCase) &&
                !response.NormalizedManufacturer.Equals("Gree", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!response.NormalizedCode.Equals("H5", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return new ErrorKnowledgeLocalizationSelection(Entry, EngineerText with { Audience = audience });
        }
    }

    private sealed class FakeDiagnosticsService(
        IReadOnlyList<EquipmentErrorCodeSummaryDto> summaries) : IEquipmentDiagnosticsService
    {
        public Task<IReadOnlyList<EquipmentErrorCodeSummaryDto>> SearchErrorCodesAsync(
            SearchEquipmentErrorCodesQuery query,
            CancellationToken cancellationToken) =>
            Task.FromResult(summaries);

        public Task<EquipmentDiagnosticCaseDto?> GetDiagnosticCaseAsync(
            string manufacturer,
            string errorCode,
            string? series,
            string? modelCode,
            CancellationToken cancellationToken) =>
            Task.FromResult<EquipmentDiagnosticCaseDto?>(null);

        public Task<EquipmentDiagnosticsCatalogIndexDto> GetCatalogIndexAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
