using AssistantEngineer.Api.Contracts.Calculations;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Api.Services.Calculations.Persistence.Durable;

public sealed class EfEngineeringWorkflowStateRepository : IEngineeringWorkflowStateRepository
{
    private readonly EngineeringWorkflowPersistenceDbContext _context;

    public EfEngineeringWorkflowStateRepository(EngineeringWorkflowPersistenceDbContext context)
    {
        _context = context;
    }

    public async Task<EngineeringWorkflowStateRecordDto> SaveAsync(
        EngineeringWorkflowStateRecordDto state,
        CancellationToken cancellationToken)
    {
        var entity = await _context.WorkflowStates
            .SingleOrDefaultAsync(item => item.Id == state.WorkflowStateId, cancellationToken);

        if (entity is null)
        {
            entity = new EngineeringWorkflowStateEntity
            {
                Id = state.WorkflowStateId
            };
            _context.WorkflowStates.Add(entity);
        }

        entity.ProjectId = state.ProjectId;
        entity.BuildingId = state.BuildingId;
        entity.Version = state.Version;
        entity.CurrentStep = state.CurrentStep;
        entity.StateJson = state.WorkflowStateJson;
        entity.ValidationDiagnosticsJson = state.ValidationDiagnosticsJson;
        entity.CreatedAtUtc = state.CreatedAtUtc;
        entity.UpdatedAtUtc = state.UpdatedAtUtc;

        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<EngineeringWorkflowStateRecordDto?> GetLatestByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var entity = await _context.WorkflowStates
            .AsNoTracking()
            .Where(item => item.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        var latest = entity
            .OrderByDescending(item => item.Version)
            .ThenByDescending(item => item.UpdatedAtUtc)
            .FirstOrDefault();

        return latest is null ? null : Map(latest);
    }

    public async Task<EngineeringWorkflowStateRecordDto?> GetByIdAsync(
        string workflowStateId,
        CancellationToken cancellationToken)
    {
        var entity = await _context.WorkflowStates
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == workflowStateId, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<EngineeringWorkflowStateRecordDto>> ListVersionsByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var entities = (await _context.WorkflowStates
            .AsNoTracking()
            .Where(item => item.ProjectId == projectId)
            .ToArrayAsync(cancellationToken))
            .OrderByDescending(item => item.Version)
            .ThenByDescending(item => item.UpdatedAtUtc)
            .ToArray();

        return entities.Select(Map).ToArray();
    }

    private static EngineeringWorkflowStateRecordDto Map(EngineeringWorkflowStateEntity entity)
    {
        return new EngineeringWorkflowStateRecordDto(
            WorkflowStateId: entity.Id,
            ProjectId: entity.ProjectId,
            BuildingId: entity.BuildingId,
            Version: entity.Version,
            CurrentStep: entity.CurrentStep,
            WorkflowStateJson: entity.StateJson,
            ValidationDiagnosticsJson: entity.ValidationDiagnosticsJson,
            CreatedAtUtc: entity.CreatedAtUtc,
            UpdatedAtUtc: entity.UpdatedAtUtc);
    }
}
