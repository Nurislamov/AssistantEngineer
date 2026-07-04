namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;

internal sealed class TelegramUpdateHandlerPipeline(IEnumerable<ITelegramUpdateHandler> handlers)
{
    private readonly IReadOnlyList<ITelegramUpdateHandler> _handlers = handlers.ToArray();

    public async Task<EquipmentDiagnosticTelegramResponse> HandleAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken)
    {
        foreach (var handler in _handlers)
        {
            var response = await handler.TryHandleAsync(update, cancellationToken);
            if (response is not null)
            {
                return response;
            }
        }

        throw new InvalidOperationException("Telegram update handler pipeline did not produce a response.");
    }
}
