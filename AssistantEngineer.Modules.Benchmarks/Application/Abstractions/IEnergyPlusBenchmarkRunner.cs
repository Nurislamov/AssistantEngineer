using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Benchmarks.Application.Abstractions;

public interface IEnergyPlusBenchmarkRunner
{
    Task<Result<EnergyPlusBenchmarkResult>> RunAsync(
        EnergyPlusBenchmarkRequest request,
        CancellationToken cancellationToken = default);
}
