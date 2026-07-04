using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Broadcasts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.OperatorInbox;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;

public sealed class EquipmentDiagnosticTelegramAdapter : IEquipmentDiagnosticTelegramAdapter
{
    public const string BlockedUserMessage = "Доступ к боту ограничен.";

    private readonly TelegramUpdateHandlerPipeline _pipeline;

    public EquipmentDiagnosticTelegramAdapter(
        IEquipmentDiagnosticBotFacade botFacade,
        EquipmentDiagnosticTelegramMessageParser parser,
        EquipmentDiagnosticTelegramResponseFormatter formatter,
        EquipmentDiagnosticTelegramOptions options,
        ITelegramUserAccessService accessService,
        ITelegramUserStore userStore,
        TelegramDiagnosticConversationService? conversationService = null,
        TelegramDiagnosticHistoryService? historyService = null,
        TelegramServiceRequestService? serviceRequestService = null,
        TelegramServiceRequestQueueService? serviceRequestQueueService = null,
        TelegramServiceRequestDialogService? serviceRequestDialogService = null,
        TelegramAdminUserManagementService? adminUserManagementService = null,
        TelegramUserOverviewService? userOverviewService = null,
        TelegramBroadcastService? broadcastService = null,
        TelegramManualLibraryService? manualLibraryService = null,
        ITelegramOperatorInboxService? operatorInboxService = null)
        : this(
            new TelegramUpdateHandlerPipeline(
            [
                new TelegramUpdateGuardHandler(options, userStore),
                new TelegramCallbackUpdateHandler(
                    accessService,
                    serviceRequestQueueService,
                    serviceRequestDialogService,
                    adminUserManagementService,
                    userOverviewService,
                    broadcastService,
                    manualLibraryService),
                new TelegramMessageUpdateHandler(
                    botFacade,
                    parser,
                    formatter,
                    options,
                    accessService,
                    userStore,
                    conversationService,
                    historyService,
                    serviceRequestService,
                    serviceRequestQueueService,
                    serviceRequestDialogService,
                    adminUserManagementService,
                    broadcastService,
                    manualLibraryService,
                    operatorInboxService)
            ]))
    {
    }

    internal EquipmentDiagnosticTelegramAdapter(TelegramUpdateHandlerPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public Task<EquipmentDiagnosticTelegramResponse> HandleAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken = default) =>
        _pipeline.HandleAsync(update, cancellationToken);
}
