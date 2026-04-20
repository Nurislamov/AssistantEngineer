using AssistantEngineer.Application.Contracts.Benchmarks;
using AssistantEngineer.Domain.Models;
using AssistantEngineer.Domain.Primitives;

namespace AssistantEngineer.Application.Abstractions;

public interface IEnergyPlusModelExporter
{
    Task<Result<EnergyPlusModelExportResult>> ExportAsync(
        Building building,
        string outputPath,
        CancellationToken cancellationToken = default);
}
