using AssistantEngineer.Api.Extensions;
using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.Modules.Benchmarks.Application.Services;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Mappers;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Controllers;

[ApiController]
[Route("api/v1/benchmarks")]
[Route("api/benchmarks")]
public class BenchmarksController : ControllerBase
{
    private readonly IEnergyPlusBenchmarkRunner _energyPlusBenchmarkRunner;
    private readonly EnergyPlusModelExportService _energyPlusModelExportService;
    private readonly VerificationService _verificationService;
    private readonly Iso52016ReferenceBenchmarkService _iso52016ReferenceBenchmarkService;

    public BenchmarksController(
        IEnergyPlusBenchmarkRunner energyPlusBenchmarkRunner,
        EnergyPlusModelExportService energyPlusModelExportService,
        VerificationService verificationService,
        Iso52016ReferenceBenchmarkService iso52016ReferenceBenchmarkService)
    {
        _energyPlusBenchmarkRunner = energyPlusBenchmarkRunner;
        _energyPlusModelExportService = energyPlusModelExportService;
        _verificationService = verificationService;
        _iso52016ReferenceBenchmarkService = iso52016ReferenceBenchmarkService;
    }

    [HttpPost("energyplus")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<EnergyPlusBenchmarkResult>> RunEnergyPlus(
        [FromBody] EnergyPlusBenchmarkRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _energyPlusBenchmarkRunner.RunAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("energyplus/buildings/{buildingId:int}/model")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<EnergyPlusModelExportResult>> ExportEnergyPlusModel(
        int buildingId,
        [FromBody] EnergyPlusModelExportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _energyPlusModelExportService.ExportBuildingModelAsync(
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
        var result = await _verificationService.VerifyBuildingAsync(
            buildingId,
            method.ToDomain(),
            request,
            cancellationToken);
        return result.ToOkResult();
    }

    [HttpGet("iso52016/reference-cases")]
    [RequestTimeout(RequestPolicies.LongRunning)]
    public async Task<ActionResult<IReadOnlyList<Iso52016ReferenceBenchmarkResult>>> RunIso52016ReferenceCases(
        CancellationToken cancellationToken)
    {
        var result = await _iso52016ReferenceBenchmarkService.RunAsync(cancellationToken);
        return Ok(result);
    }
}
