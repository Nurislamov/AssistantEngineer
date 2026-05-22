using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Api.Services.Calculations.Persistence.Durable;

public sealed class EfEngineeringProjectRepository : IEngineeringProjectRepository
{
    private readonly EngineeringWorkflowPersistenceDbContext _context;

    public EfEngineeringProjectRepository(EngineeringWorkflowPersistenceDbContext context)
    {
        _context = context;
    }

    public async Task<EngineeringProjectRecordDto> UpsertAsync(
        EngineeringProjectRecordDto project,
        CancellationToken cancellationToken)
    {
        var entity = await _context.Projects
            .SingleOrDefaultAsync(item => item.Id == project.ProjectId, cancellationToken);

        if (entity is null)
        {
            entity = new EngineeringProjectEntity
            {
                Id = project.ProjectId
            };
            _context.Projects.Add(entity);
        }

        entity.Name = project.ProjectName;
        entity.Description = project.Description;
        entity.Status = project.Status.ToString();
        entity.MetadataJson = SerializeMetadata(project.MetadataJson);
        entity.CreatedAtUtc = project.CreatedAtUtc;
        entity.UpdatedAtUtc = project.UpdatedAtUtc;

        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<EngineeringProjectRecordDto?> GetByIdAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var entity = await _context.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == projectId, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<EngineeringProjectRecordDto>> ListAsync(
        CancellationToken cancellationToken)
    {
        var entities = await _context.Projects
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .ToArrayAsync(cancellationToken);

        return entities.Select(Map).ToArray();
    }

    public async Task<EngineeringProjectRecordDto?> UpdateMetadataAsync(
        int projectId,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken)
    {
        var entity = await _context.Projects
            .SingleOrDefaultAsync(item => item.Id == projectId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.MetadataJson = SerializeMetadata(metadata);
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    private static EngineeringProjectRecordDto Map(EngineeringProjectEntity entity)
    {
        return new EngineeringProjectRecordDto(
            ProjectId: entity.Id,
            ProjectName: entity.Name,
            Description: entity.Description,
            CreatedAtUtc: entity.CreatedAtUtc,
            UpdatedAtUtc: entity.UpdatedAtUtc,
            Status: Enum.TryParse<EngineeringProjectRecordStatus>(entity.Status, true, out var status)
                ? status
                : EngineeringProjectRecordStatus.Active,
            MetadataJson: DeserializeMetadata(entity.MetadataJson));
    }

    private static string? SerializeMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return null;
        }

        var normalized = metadata
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);

        return System.Text.Json.JsonSerializer.Serialize(normalized);
    }

    private static IReadOnlyDictionary<string, string>? DeserializeMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            var raw = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson);
            return raw?
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }
}
