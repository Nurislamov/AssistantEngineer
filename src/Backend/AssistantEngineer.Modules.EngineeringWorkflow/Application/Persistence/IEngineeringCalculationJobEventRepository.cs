using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public interface IEngineeringCalculationJobEventRepository
{
    Task<EngineeringCalculationJobEventRecordDto> AppendAsync(
        EngineeringCalculationJobEventRecordDto jobEvent,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EngineeringCalculationJobEventRecordDto>> ListByJobIdAsync(
        string jobId,
        CancellationToken cancellationToken);
}
