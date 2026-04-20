using AssistantEngineer.Application.Contracts.Benchmarks;
using AssistantEngineer.Domain.Primitives;

namespace AssistantEngineer.Application.Abstractions;

public interface IEnergyPlusBenchmarkRunner
{
    Task<Result<EnergyPlusBenchmarkResult>> RunAsync(
        EnergyPlusBenchmarkRequest request,
        CancellationToken cancellationToken = default);
}
