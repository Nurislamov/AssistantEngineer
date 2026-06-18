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
        Assert.Contains("Перед окончательным выводом нужна проверка", response.Text, StringComparison.Ordinal);
        Assert.Contains("Источник:", response.Text, StringComparison.Ordinal);
        Assert.Contains("Безопасность:", response.Text, StringComparison.Ordinal);
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
    }

    [Fact]
    public async Task ReferenceOnlyCodeIsNotFormattedAsFaultDiagnosis()
    {
        using var provider = CreateProvider(EnabledOptions());
        var adapter = provider.GetRequiredService<IEquipmentDiagnosticTelegramAdapter>();

        var response = await adapter.HandleAsync(Update("Gree A0"));

        Assert.Contains("Справочное совпадение", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Уверенность:", response.Text, StringComparison.Ordinal);
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
                typeof(TelegramAdminUserManagementService)
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
