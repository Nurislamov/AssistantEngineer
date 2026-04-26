using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Contracts.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers.ReferenceData;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/standard-tables")]
public sealed class StandardTablesController : ControllerBase
{
    private readonly IStandardReferenceDataFacade _referenceData;

    public StandardTablesController(
        IStandardReferenceDataFacade referenceData)
    {
        _referenceData = referenceData;
    }

    [HttpGet("catalog")]
    public ActionResult<StandardTableCatalogResponse> GetCatalog() =>
        Ok(_referenceData.GetStandardTableCatalog());

    [HttpGet("internal-loads")]
    public ActionResult<InternalLoadStandardLookupResponse> GetInternalLoads(
        [FromQuery] RoomTypeDto roomType)
    {
        var result = _referenceData.GetInternalLoadStandard(roomType);
        return Ok(result);
    }

    [HttpGet("dhw")]
    public ActionResult<DomesticHotWaterStandardLookupResponse> GetDomesticHotWater(
        [FromQuery] RoomTypeDto roomType)
    {
        var result = _referenceData.GetDomesticHotWaterStandard(roomType);
        return Ok(result);
    }

    [HttpGet("tb14")]
    public ActionResult<Tb14VentilationStandardLookupResponse> GetTb14(
        [FromQuery] RoomTypeDto roomType)
    {
        var result = _referenceData.GetTb14VentilationStandard(roomType);
        return Ok(result);
    }
}
