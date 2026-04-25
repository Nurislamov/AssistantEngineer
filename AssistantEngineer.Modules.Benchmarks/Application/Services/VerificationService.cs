using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Services.Buildings;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Benchmarks.Application.Services;

internal sealed class VerificationService
{
    private readonly IBuildingRepository _buildings;
    private readonly BuildingCoolingLoadService _coolingLoadService;
    private readonly IEnergyPlusModelExporter _energyPlusModelExporter;
    private readonly IEnergyPlusBenchmarkRunner _energyPlusBenchmarkRunner;
    private readonly IEnergyPlusResultParser _energyPlusResultParser;
    private readonly IVerificationComparator _verificationComparator;
    private readonly IEnergyPlusArtifactStore _artifacts;
    private readonly ILogger<VerificationService> _logger;

    public VerificationService(
        IBuildingRepository buildings,
        BuildingCoolingLoadService coolingLoadService,
        IEnergyPlusModelExporter energyPlusModelExporter,
        IEnergyPlusBenchmarkRunner energyPlusBenchmarkRunner,
        IEnergyPlusResultParser energyPlusResultParser,
        IVerificationComparator verificationComparator,
        IEnergyPlusArtifactStore artifacts,
        ILogger<VerificationService>? logger = null)
    {
        _buildings = buildings;
        _coolingLoadService = coolingLoadService;
        _energyPlusModelExporter = energyPlusModelExporter;
        _energyPlusBenchmarkRunner = energyPlusBenchmarkRunner;
        _energyPlusResultParser = energyPlusResultParser;
        _verificationComparator = verificationComparator;
        _artifacts = artifacts;
        _logger = logger ?? NullLogger<VerificationService>.Instance;
    }

    public async Task<Result<VerificationReport>> VerifyBuildingAsync(
        int buildingId,
        CoolingLoadCalculationMethod method,
        VerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var building = await _buildings.GetForCalculationAsync(buildingId, cancellationToken);
        if (building is null)
            return Result<VerificationReport>.NotFound($"Building with id {buildingId} not found.");

        var ourResult = await _coolingLoadService.CalculateAsync(buildingId, method, cancellationToken);
        if (ourResult.IsFailure)
            return Result<VerificationReport>.Failure(ourResult);

        return await VerifyAgainstEnergyPlusAsync(
            building,
            ourResult.Value,
            request,
            cancellationToken);
    }

    private async Task<Result<VerificationReport>> VerifyAgainstEnergyPlusAsync(
        Building building,
        BuildingCalculationResult ourResult,
        VerificationRequest request,
        CancellationToken cancellationToken)
    {
        var exportResult = await _energyPlusModelExporter.ExportAsync(building, request.RunName, cancellationToken);
        if (exportResult.IsFailure)
            return Result<VerificationReport>.Failure(exportResult);

        var runResult = await _energyPlusBenchmarkRunner.RunAsync(
            new EnergyPlusBenchmarkRequest
            {
                ModelArtifactId = exportResult.Value.ModelArtifactId,
                WeatherArtifactId = request.WeatherArtifactId,
                RunName = request.RunName,
                AdditionalArguments = request.AdditionalArguments
            },
            cancellationToken);
        if (runResult.IsFailure)
            return Result<VerificationReport>.Failure(runResult);

        try
        {
            var workspace = _artifacts.GetRunWorkspace(runResult.Value.RunArtifactId);
            if (workspace.IsFailure)
                return Result<VerificationReport>.Failure(workspace);

            var epSummary = _energyPlusResultParser.Parse(workspace.Value.OutputDirectory);
            var report = _verificationComparator.Compare(ourResult, epSummary);
            return Result<VerificationReport>.Success(report);
        }
        finally
        {
            DeleteRunWorkspaceQuietly(runResult.Value.RunArtifactId);
        }
    }

    private void DeleteRunWorkspaceQuietly(string runArtifactId)
    {
        try
        {
            _artifacts.DeleteRunWorkspace(runArtifactId);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to delete EnergyPlus verification run artifact {RunArtifactId}.",
                runArtifactId);
        }
    }
}
