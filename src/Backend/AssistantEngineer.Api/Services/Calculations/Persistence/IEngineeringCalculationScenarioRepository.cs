using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public interface IEngineeringCalculationScenarioRepository
{
    Task<EngineeringCalculationScenarioRecordDto> CreateAsync(
        EngineeringCalculationScenarioRecordDto scenario,
        CancellationToken cancellationToken);

    Task<EngineeringCalculationScenarioRecordDto> UpdateAsync(
        EngineeringCalculationScenarioRecordDto scenario,
        CancellationToken cancellationToken);

    Task<EngineeringCalculationScenarioRecordDto?> GetByIdAsync(
        string scenarioId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EngineeringCalculationScenarioRecordDto>> ListByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken);

    Task<EngineeringCalculationScenarioRecordDto?> GetLatestByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken);
}
