using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.Modules.Benchmarks.Application.Services;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Mappers;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Benchmarks.Application.Facades;

public sealed class BenchmarksFacade : IBenchmarksFacade
{
    private readonly IEnergyPlusBenchmarkRunner _energyPlusBenchmarkRunner;
    private readonly EnergyPlusModelExportService _energyPlusModelExportService;
    private readonly VerificationService _verificationService;
    private readonly Iso52016ReferenceBenchmarkService _iso52016ReferenceBenchmarkService;

    internal BenchmarksFacade(
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

    public Task<Result<EnergyPlusBenchmarkResult>> RunEnergyPlusAsync(
        EnergyPlusBenchmarkRequest request,
        CancellationToken cancellationToken) =>
        _energyPlusBenchmarkRunner.RunAsync(request, cancellationToken);

    public Task<Result<EnergyPlusModelExportResult>> ExportEnergyPlusModelAsync(
        int buildingId,
        EnergyPlusModelExportRequest request,
        CancellationToken cancellationToken) =>
        _energyPlusModelExportService.ExportBuildingModelAsync(
            buildingId,
            request,
            cancellationToken);

    public Task<Result<VerificationReport>> VerifyCalculationAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto method,
        VerificationRequest request,
        CancellationToken cancellationToken) =>
        _verificationService.VerifyBuildingAsync(
            buildingId,
            method.ToDomain(),
            request,
            cancellationToken);

    public Task<Result<IReadOnlyList<Iso52016ReferenceBenchmarkResult>>> RunIso52016ReferenceCasesAsync(
        CancellationToken cancellationToken) =>
        _iso52016ReferenceBenchmarkService.RunAsync(cancellationToken);
}
