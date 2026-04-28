using AssistantEngineer.Modules.Benchmarks.Application.Models;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Benchmarks.Application.Abstractions;

public interface IEnergyPlusArtifactStore
{
    Result<EnergyPlusArtifactFile> CreateModelArtifact(
        int buildingId,
        string? runName = null);

    Result<EnergyPlusArtifactFile> GetModelArtifact(string artifactId);

    Result<EnergyPlusArtifactFile> GetWeatherArtifact(string artifactId);

    Result<EnergyPlusRunWorkspace> CreateRunWorkspace(string? runName = null);

    Result<EnergyPlusRunWorkspace> GetRunWorkspace(string runArtifactId);

    void DeleteRunWorkspace(string runArtifactId);
}
