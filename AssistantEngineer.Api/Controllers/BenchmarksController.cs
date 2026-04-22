using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.Modules.Benchmarks.Application.Facades;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/benchmarks")]
public class BenchmarksController : ControllerBase
{
    private readonly IBenchmarksFacade _benchmarks;

    public BenchmarksController(IBenchmarksFacade benchmarks)
    {
        _benchmarks = benchmarks;
    }

    [HttpPost("energyplus")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<EnergyPlusBenchmarkResult>> RunEnergyPlus(
        [FromBody] EnergyPlusBenchmarkRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _benchmarks.RunEnergyPlusAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("energyplus/buildings/{buildingId:int}/model")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<EnergyPlusModelExportResult>> ExportEnergyPlusModel(
        int buildingId,
        [FromBody] EnergyPlusModelExportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _benchmarks.ExportEnergyPlusModelAsync(
            buildingId,
            request,
            cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("buildings/{buildingId:int}/verify")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<VerificationReport>> VerifyCalculation(
        int buildingId,
        [FromQuery] CoolingLoadCalculationMethodDto method,
        [FromBody] VerificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _benchmarks.VerifyCalculationAsync(
            buildingId,
            method,
            request,
            cancellationToken);
        return result.ToOkResult();
    }

    [HttpGet("iso52016/reference-cases")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<IReadOnlyList<Iso52016ReferenceBenchmarkResult>>> RunIso52016ReferenceCases(
        CancellationToken cancellationToken)
    {
        var result = await _benchmarks.RunIso52016ReferenceCasesAsync(cancellationToken);
        return result.ToOkResult();
    }
}
