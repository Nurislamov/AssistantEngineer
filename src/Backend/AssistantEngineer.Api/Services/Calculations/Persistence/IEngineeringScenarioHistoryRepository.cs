using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public interface IEngineeringScenarioHistoryRepository
{
    Task<EngineeringScenarioHistoryEntryDto> AppendAsync(
        EngineeringScenarioHistoryEntryDto historyEntry,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EngineeringScenarioHistoryEntryDto>> ListByScenarioIdAsync(
        string scenarioId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EngineeringScenarioHistoryEntryDto>> ListByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken);
}
