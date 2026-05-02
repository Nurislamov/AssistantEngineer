using AssistantEngineer.Api.Extensions.Results;
using AssistantEngineer.Modules.Calculations.Application.Contracts.CoreStatus;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.Calculations;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/calculations/engineering-core")]
public class EngineeringCoreStatusController : ControllerBase
{
    private readonly IEngineeringCoreStatusFacade _engineeringCoreStatus;

    public EngineeringCoreStatusController(
        IEngineeringCoreStatusFacade engineeringCoreStatus)
    {
        _engineeringCoreStatus = engineeringCoreStatus;
    }

    [HttpGet("v1/status")]
    public ActionResult<EngineeringCoreV1StatusResponse> GetEngineeringCoreV1Status()
    {
        var result = _engineeringCoreStatus.GetEngineeringCoreV1Status();

        return result.ToActionResult(this);
    }
    [HttpGet("v1/diagnostics-catalog")]
    public ActionResult<EngineeringCoreV1DiagnosticsCatalogResponse> GetEngineeringCoreV1DiagnosticsCatalog()
    {
        var result = _engineeringCoreStatus.GetEngineeringCoreV1DiagnosticsCatalog();

        return result.ToActionResult(this);
    }
}
