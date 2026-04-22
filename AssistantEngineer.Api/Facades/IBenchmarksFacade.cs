using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Api.Facades;

public interface IBenchmarksFacade
{
    Task<Result<EnergyPlusBenchmarkResult>> RunEnergyPlusAsync(
        EnergyPlusBenchmarkRequest request,
        CancellationToken cancellationToken);

    Task<Result<EnergyPlusModelExportResult>> ExportEnergyPlusModelAsync(
        int buildingId,
        EnergyPlusModelExportRequest request,
        CancellationToken cancellationToken);

    Task<Result<VerificationReport>> VerifyCalculationAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto method,
        VerificationRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Iso52016ReferenceBenchmarkResult>> RunIso52016ReferenceCasesAsync(
        CancellationToken cancellationToken);
}
