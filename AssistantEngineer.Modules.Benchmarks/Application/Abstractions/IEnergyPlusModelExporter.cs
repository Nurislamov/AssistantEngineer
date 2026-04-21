using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Benchmarks.Application.Abstractions;

public interface IEnergyPlusModelExporter
{
    Task<Result<EnergyPlusModelExportResult>> ExportAsync(
        Building building,
        string outputPath,
        CancellationToken cancellationToken = default);
}
