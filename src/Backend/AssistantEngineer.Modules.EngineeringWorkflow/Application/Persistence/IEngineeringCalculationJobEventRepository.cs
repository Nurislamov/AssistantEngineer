using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence;

public interface IEngineeringCalculationJobEventRepository
{
    Task<EngineeringCalculationJobEventRecordDto> AppendAsync(
        EngineeringCalculationJobEventRecordDto jobEvent,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EngineeringCalculationJobEventRecordDto>> ListByJobIdAsync(
        string jobId,
        CancellationToken cancellationToken);
}
