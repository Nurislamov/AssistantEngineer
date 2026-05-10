using AssistantEngineer.Api.Contracts.Calculations;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Api.Services.Calculations.Persistence.Durable;

public sealed class EfEngineeringScenarioHistoryRepository : IEngineeringScenarioHistoryRepository
{
    private readonly EngineeringWorkflowPersistenceDbContext _context;

    public EfEngineeringScenarioHistoryRepository(EngineeringWorkflowPersistenceDbContext context)
    {
        _context = context;
    }

    public async Task<EngineeringScenarioHistoryEntryDto> AppendAsync(
        EngineeringScenarioHistoryEntryDto historyEntry,
        CancellationToken cancellationToken)
    {
        var entity = await _context.HistoryEntries
            .SingleOrDefaultAsync(item => item.Id == historyEntry.EventId, cancellationToken);

        if (entity is null)
        {
            entity = new EngineeringScenarioHistoryEntryEntity
            {
                Id = historyEntry.EventId
            };
            _context.HistoryEntries.Add(entity);
        }

        entity.ScenarioId = historyEntry.ScenarioId;
        entity.ProjectId = historyEntry.ProjectId;
        entity.EventKind = historyEntry.EventKind.ToString();
        entity.Message = historyEntry.Message;
        entity.DiagnosticsJson = historyEntry.DiagnosticsJson;
        entity.CreatedAtUtc = historyEntry.CreatedAtUtc;

        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<IReadOnlyList<EngineeringScenarioHistoryEntryDto>> ListByScenarioIdAsync(
        string scenarioId,
        CancellationToken cancellationToken)
    {
        var entities = (await _context.HistoryEntries
            .AsNoTracking()
            .Where(item => item.ScenarioId == scenarioId)
            .ToArrayAsync(cancellationToken))
            .OrderBy(item => item.CreatedAtUtc)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();

        return entities.Select(Map).ToArray();
    }

    public async Task<IReadOnlyList<EngineeringScenarioHistoryEntryDto>> ListByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var entities = (await _context.HistoryEntries
            .AsNoTracking()
            .Where(item => item.ProjectId == projectId)
            .ToArrayAsync(cancellationToken))
            .OrderBy(item => item.CreatedAtUtc)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();

        return entities.Select(Map).ToArray();
    }

    private static EngineeringScenarioHistoryEntryDto Map(EngineeringScenarioHistoryEntryEntity entity)
    {
        var eventKind = Enum.TryParse<EngineeringScenarioHistoryEventKind>(entity.EventKind, true, out var parsed)
            ? parsed
            : EngineeringScenarioHistoryEventKind.Created;

        return new EngineeringScenarioHistoryEntryDto(
            EventId: entity.Id,
            ScenarioId: entity.ScenarioId,
            ProjectId: entity.ProjectId,
            EventKind: eventKind,
            Message: entity.Message,
            DiagnosticsJson: entity.DiagnosticsJson,
            CreatedAtUtc: entity.CreatedAtUtc);
    }
}
