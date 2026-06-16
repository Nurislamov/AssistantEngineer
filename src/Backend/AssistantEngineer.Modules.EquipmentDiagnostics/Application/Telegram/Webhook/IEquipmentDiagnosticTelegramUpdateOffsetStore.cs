namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

public interface IEquipmentDiagnosticTelegramUpdateOffsetStore
{
    Task<long?> GetLastProcessedUpdateIdAsync(CancellationToken cancellationToken = default);

    Task SaveLastProcessedUpdateIdAsync(
        long updateId,
        CancellationToken cancellationToken = default);
}
