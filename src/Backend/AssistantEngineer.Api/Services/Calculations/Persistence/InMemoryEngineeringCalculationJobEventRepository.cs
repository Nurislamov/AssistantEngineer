using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

public sealed class InMemoryEngineeringCalculationJobEventRepository : IEngineeringCalculationJobEventRepository
{
    private readonly EngineeringWorkflowMemoryStore _store;

    public InMemoryEngineeringCalculationJobEventRepository(EngineeringWorkflowMemoryStore store)
    {
        _store = store;
    }

    public Task<EngineeringCalculationJobEventRecordDto> AppendAsync(
        EngineeringCalculationJobEventRecordDto jobEvent,
        CancellationToken cancellationToken)
    {
        _store.JobEventsById.AddOrUpdate(jobEvent.EventId, jobEvent, (_, _) => jobEvent);
        return Task.FromResult(jobEvent);
    }

    public Task<IReadOnlyList<EngineeringCalculationJobEventRecordDto>> ListByJobIdAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        var events = _store.JobEventsById.Values
            .Where(item => item.JobId.Equals(jobId, StringComparison.Ordinal))
            .OrderBy(item => item.CreatedAtUtc)
            .ThenBy(item => item.EventId, StringComparer.Ordinal)
            .ToArray();

        return Task.FromResult<IReadOnlyList<EngineeringCalculationJobEventRecordDto>>(events);
    }
}
