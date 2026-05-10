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
        lock (_store.SyncRoot)
        {
            _store.JobEventsById[jobEvent.EventId] = jobEvent;
            if (!_store.JobEventIdsByJobId.TryGetValue(jobEvent.JobId, out var ids))
            {
                ids = [];
                _store.JobEventIdsByJobId[jobEvent.JobId] = ids;
            }

            if (!ids.Contains(jobEvent.EventId, StringComparer.Ordinal))
            {
                ids.Add(jobEvent.EventId);
            }

            return Task.FromResult(jobEvent);
        }
    }

    public Task<IReadOnlyList<EngineeringCalculationJobEventRecordDto>> ListByJobIdAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        lock (_store.SyncRoot)
        {
            if (!_store.JobEventIdsByJobId.TryGetValue(jobId, out var ids))
            {
                return Task.FromResult<IReadOnlyList<EngineeringCalculationJobEventRecordDto>>([]);
            }

            var events = ids
                .Select(id => _store.JobEventsById[id])
                .OrderBy(item => item.CreatedAtUtc)
                .ThenBy(item => item.EventId, StringComparer.Ordinal)
                .ToArray();

            return Task.FromResult<IReadOnlyList<EngineeringCalculationJobEventRecordDto>>(events);
        }
    }
}
