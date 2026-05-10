using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public interface IEngineeringCalculationArtifactRepository
{
    Task<EngineeringCalculationArtifactRecordDto> SaveAsync(
        EngineeringCalculationArtifactRecordDto artifact,
        CancellationToken cancellationToken);

    Task<EngineeringCalculationArtifactRecordDto?> GetByScenarioAndKindAsync(
        string scenarioId,
        EngineeringCalculationArtifactKind artifactKind,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EngineeringCalculationArtifactRecordDto>> ListByScenarioIdAsync(
        string scenarioId,
        CancellationToken cancellationToken);
}
