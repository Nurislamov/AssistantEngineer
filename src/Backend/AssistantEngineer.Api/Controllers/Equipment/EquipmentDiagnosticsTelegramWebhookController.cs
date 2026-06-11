using AssistantEngineer.Api.Extensions.Http;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Equipment;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/equipment-diagnostics/telegram")]
public sealed class EquipmentDiagnosticsTelegramWebhookController : ControllerBase
{
    public const string SecretHeaderName = "X-Telegram-Bot-Api-Secret-Token";

    private readonly IEquipmentDiagnosticTelegramWebhookHandler _handler;

    public EquipmentDiagnosticsTelegramWebhookController(IEquipmentDiagnosticTelegramWebhookHandler handler)
    {
        _handler = handler;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Receive(
        [FromBody] TelegramWebhookUpdateDto? update,
        CancellationToken cancellationToken)
    {
        if (update is null)
        {
            return ApiProblemDetailsFactory.CreateValidationResult(
                this,
                "Request body is required.",
                "update",
                "Request body is required.");
        }

        var secret = Request.Headers[SecretHeaderName].FirstOrDefault();
        var result = await _handler.HandleAsync(update, secret, cancellationToken);

        return result.Status switch
        {
            EquipmentDiagnosticTelegramWebhookStatus.Disabled => NotFound(),
            EquipmentDiagnosticTelegramWebhookStatus.Unauthorized => StatusCode(StatusCodes.Status403Forbidden),
            EquipmentDiagnosticTelegramWebhookStatus.Rejected => StatusCode(StatusCodes.Status503ServiceUnavailable),
            EquipmentDiagnosticTelegramWebhookStatus.InvalidUpdate => BadRequest(),
            EquipmentDiagnosticTelegramWebhookStatus.OutboundFailed => StatusCode(StatusCodes.Status502BadGateway),
            _ => Ok()
        };
    }
}
