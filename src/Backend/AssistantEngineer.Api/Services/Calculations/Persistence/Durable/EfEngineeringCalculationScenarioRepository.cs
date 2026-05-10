using AssistantEngineer.Api.Contracts.Calculations;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Api.Services.Calculations.Persistence.Durable;

public sealed class EfEngineeringCalculationScenarioRepository : IEngineeringCalculationScenarioRepository
{
    private readonly EngineeringWorkflowPersistenceDbContext _context;

    public EfEngineeringCalculationScenarioRepository(EngineeringWorkflowPersistenceDbContext context)
    {
        _context = context;
    }

    public async Task<EngineeringCalculationScenarioRecordDto> CreateAsync(
        EngineeringCalculationScenarioRecordDto scenario,
        CancellationToken cancellationToken)
    {
        var entity = ToEntity(scenario);
        _context.Scenarios.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<EngineeringCalculationScenarioRecordDto> UpdateAsync(
        EngineeringCalculationScenarioRecordDto scenario,
        CancellationToken cancellationToken)
    {
        var entity = await _context.Scenarios
            .SingleOrDefaultAsync(item => item.Id == scenario.ScenarioId, cancellationToken);

        if (entity is null)
        {
            entity = ToEntity(scenario);
            _context.Scenarios.Add(entity);
        }
        else
        {
            entity.ProjectId = scenario.ProjectId;
            entity.BuildingId = scenario.BuildingId;
            entity.ScenarioKind = scenario.ScenarioKind.ToString();
            entity.ExecutionMode = scenario.ExecutionMode.ToString();
            entity.Status = scenario.Status.ToString();
            entity.RequestJson = scenario.RequestJson;
            entity.ResultSummaryJson = scenario.ResultSummaryJson;
            entity.DiagnosticsJson = scenario.DiagnosticsJson;
            entity.CreatedAtUtc = scenario.CreatedAtUtc;
            entity.StartedAtUtc = scenario.StartedAtUtc;
            entity.CompletedAtUtc = scenario.CompletedAtUtc;
            entity.DurationMs = scenario.DurationMilliseconds;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<EngineeringCalculationScenarioRecordDto?> GetByIdAsync(
        string scenarioId,
        CancellationToken cancellationToken)
    {
        var entity = await _context.Scenarios
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == scenarioId, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<EngineeringCalculationScenarioRecordDto>> ListByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var entities = (await _context.Scenarios
            .AsNoTracking()
            .Where(item => item.ProjectId == projectId)
            .ToArrayAsync(cancellationToken))
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();

        return entities.Select(Map).ToArray();
    }

    public async Task<EngineeringCalculationScenarioRecordDto?> GetLatestByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var entity = (await _context.Scenarios
            .AsNoTracking()
            .Where(item => item.ProjectId == projectId)
            .ToArrayAsync(cancellationToken))
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .FirstOrDefault();

        return entity is null ? null : Map(entity);
    }

    private static EngineeringCalculationScenarioEntity ToEntity(EngineeringCalculationScenarioRecordDto source)
    {
        return new EngineeringCalculationScenarioEntity
        {
            Id = source.ScenarioId,
            ProjectId = source.ProjectId,
            BuildingId = source.BuildingId,
            ScenarioKind = source.ScenarioKind.ToString(),
            ExecutionMode = source.ExecutionMode.ToString(),
            Status = source.Status.ToString(),
            RequestJson = source.RequestJson,
            ResultSummaryJson = source.ResultSummaryJson,
            DiagnosticsJson = source.DiagnosticsJson,
            CreatedAtUtc = source.CreatedAtUtc,
            StartedAtUtc = source.StartedAtUtc,
            CompletedAtUtc = source.CompletedAtUtc,
            DurationMs = source.DurationMilliseconds
        };
    }

    private static EngineeringCalculationScenarioRecordDto Map(EngineeringCalculationScenarioEntity entity)
    {
        var scenarioKind = Enum.TryParse<EngineeringCalculationScenarioKind>(entity.ScenarioKind, true, out var parsedKind)
            ? parsedKind
            : EngineeringCalculationScenarioKind.FullEngineeringCore;
        var executionMode = Enum.TryParse<EngineeringCalculationExecutionMode>(entity.ExecutionMode, true, out var parsedMode)
            ? parsedMode
            : EngineeringCalculationExecutionMode.ExecuteAvailableModules;
        var status = Enum.TryParse<EngineeringCalculationExecutionStatus>(entity.Status, true, out var parsedStatus)
            ? parsedStatus
            : EngineeringCalculationExecutionStatus.NotSupported;

        return new EngineeringCalculationScenarioRecordDto(
            ScenarioId: entity.Id,
            ProjectId: entity.ProjectId,
            BuildingId: entity.BuildingId,
            ScenarioKind: scenarioKind,
            ExecutionMode: executionMode,
            Status: status,
            RequestJson: entity.RequestJson,
            ResultSummaryJson: entity.ResultSummaryJson,
            CreatedAtUtc: entity.CreatedAtUtc,
            StartedAtUtc: entity.StartedAtUtc,
            CompletedAtUtc: entity.CompletedAtUtc,
            DurationMilliseconds: entity.DurationMs,
            DiagnosticsJson: entity.DiagnosticsJson);
    }
}
