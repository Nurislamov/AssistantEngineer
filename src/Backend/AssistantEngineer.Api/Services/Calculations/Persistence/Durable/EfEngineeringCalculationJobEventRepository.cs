using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Api.Services.Calculations.Persistence.Durable;

public sealed class EfEngineeringCalculationJobEventRepository : IEngineeringCalculationJobEventRepository
{
    private readonly EngineeringWorkflowPersistenceDbContext _context;

    public EfEngineeringCalculationJobEventRepository(EngineeringWorkflowPersistenceDbContext context)
    {
        _context = context;
    }

    public async Task<EngineeringCalculationJobEventRecordDto> AppendAsync(
        EngineeringCalculationJobEventRecordDto jobEvent,
        CancellationToken cancellationToken)
    {
        var entity = ToEntity(jobEvent);
        _context.JobEvents.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<IReadOnlyList<EngineeringCalculationJobEventRecordDto>> ListByJobIdAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        var entities = (await _context.JobEvents
            .AsNoTracking()
            .Where(item => item.JobId == jobId)
            .ToArrayAsync(cancellationToken))
            .OrderBy(item => item.CreatedAtUtc)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();

        return entities.Select(Map).ToArray();
    }

    private static EngineeringCalculationJobEventEntity ToEntity(EngineeringCalculationJobEventRecordDto source)
    {
        return new EngineeringCalculationJobEventEntity
        {
            Id = source.EventId,
            JobId = source.JobId,
            ScenarioId = source.ScenarioId,
            ProjectId = source.ProjectId,
            Status = source.Status.ToString(),
            EventKind = source.EventKind,
            Message = source.Message,
            DiagnosticsJson = source.DiagnosticsJson,
            ProgressPercent = source.ProgressPercent,
            CreatedAtUtc = source.CreatedAtUtc
        };
    }

    private static EngineeringCalculationJobEventRecordDto Map(EngineeringCalculationJobEventEntity entity)
    {
        var status = Enum.TryParse<EngineeringCalculationJobStatus>(entity.Status, true, out var parsedStatus)
            ? parsedStatus
            : EngineeringCalculationJobStatus.NotSupported;

        return new EngineeringCalculationJobEventRecordDto(
            EventId: entity.Id,
            JobId: entity.JobId,
            ScenarioId: entity.ScenarioId,
            ProjectId: entity.ProjectId,
            Status: status,
            EventKind: entity.EventKind,
            Message: entity.Message,
            DiagnosticsJson: entity.DiagnosticsJson,
            ProgressPercent: entity.ProgressPercent,
            CreatedAtUtc: entity.CreatedAtUtc);
    }
}
