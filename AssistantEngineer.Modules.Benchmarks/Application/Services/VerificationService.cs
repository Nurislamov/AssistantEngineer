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

public sealed class VerificationService
{
    private readonly IBuildingRepository _buildings;
    private readonly BuildingCoolingLoadService _coolingLoadService;
    private readonly IEnergyPlusModelExporter _energyPlusModelExporter;
    private readonly IEnergyPlusBenchmarkRunner _energyPlusBenchmarkRunner;
    private readonly IEnergyPlusResultParser _energyPlusResultParser;
    private readonly IVerificationComparator _verificationComparator;
    private readonly ILogger<VerificationService> _logger;

    public VerificationService(
        IBuildingRepository buildings,
        BuildingCoolingLoadService coolingLoadService,
        IEnergyPlusModelExporter energyPlusModelExporter,
        IEnergyPlusBenchmarkRunner energyPlusBenchmarkRunner,
        IEnergyPlusResultParser energyPlusResultParser,
        IVerificationComparator verificationComparator,
        ILogger<VerificationService>? logger = null)
    {
        _buildings = buildings;
        _coolingLoadService = coolingLoadService;
        _energyPlusModelExporter = energyPlusModelExporter;
        _energyPlusBenchmarkRunner = energyPlusBenchmarkRunner;
        _energyPlusResultParser = energyPlusResultParser;
        _verificationComparator = verificationComparator;
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

        var tempDir = Path.Combine(Path.GetTempPath(), $"ep_verify_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            return await VerifyAgainstEnergyPlusAsync(
                building,
                ourResult.Value,
                request,
                tempDir,
                cancellationToken);
        }
        finally
        {
            DeleteDirectoryQuietly(tempDir);
        }
    }

    private async Task<Result<VerificationReport>> VerifyAgainstEnergyPlusAsync(
        Building building,
        BuildingCalculationResult ourResult,
        VerificationRequest request,
        string tempDir,
        CancellationToken cancellationToken)
    {
        var idfPath = Path.Combine(tempDir, "model.idf");
        var exportResult = await _energyPlusModelExporter.ExportAsync(building, idfPath, cancellationToken);
        if (exportResult.IsFailure)
            return Result<VerificationReport>.Failure(exportResult);

        var runResult = await _energyPlusBenchmarkRunner.RunAsync(
            new EnergyPlusBenchmarkRequest
            {
                ModelPath = idfPath,
                WeatherFilePath = request.WeatherFilePath,
                OutputDirectory = tempDir,
                AdditionalArguments = request.AdditionalArguments
            },
            cancellationToken);
        if (runResult.IsFailure)
            return Result<VerificationReport>.Failure(runResult);

        var epSummary = _energyPlusResultParser.Parse(tempDir);
        var report = _verificationComparator.Compare(ourResult, epSummary);
        return Result<VerificationReport>.Success(report);
    }

    private void DeleteDirectoryQuietly(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to delete EnergyPlus verification temporary directory {TempDirectory}.",
                path);
        }
    }
}
