using AssistantEngineer.Api.Contracts.Calculations;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Api.Services.Calculations.Persistence.Durable;

public sealed class EfEngineeringCalculationArtifactRepository : IEngineeringCalculationArtifactRepository
{
    private readonly EngineeringWorkflowPersistenceDbContext _context;

    public EfEngineeringCalculationArtifactRepository(EngineeringWorkflowPersistenceDbContext context)
    {
        _context = context;
    }

    public async Task<EngineeringCalculationArtifactRecordDto> SaveAsync(
        EngineeringCalculationArtifactRecordDto artifact,
        CancellationToken cancellationToken)
    {
        var entity = await _context.Artifacts
            .SingleOrDefaultAsync(item => item.Id == artifact.ArtifactId, cancellationToken);

        if (entity is null)
        {
            entity = new EngineeringCalculationArtifactEntity
            {
                Id = artifact.ArtifactId
            };
            _context.Artifacts.Add(entity);
        }

        entity.ScenarioId = artifact.ScenarioId;
        entity.ArtifactKind = artifact.ArtifactKind.ToString();
        entity.ContentType = artifact.ContentType;
        entity.Content = artifact.Content;
        entity.SizeBytes = artifact.SizeBytes;
        entity.Checksum = artifact.ChecksumSha256;
        entity.CreatedAtUtc = artifact.CreatedAtUtc;

        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<EngineeringCalculationArtifactRecordDto?> GetByScenarioAndKindAsync(
        string scenarioId,
        EngineeringCalculationArtifactKind artifactKind,
        CancellationToken cancellationToken)
    {
        var entity = (await _context.Artifacts
            .AsNoTracking()
            .Where(item => item.ScenarioId == scenarioId && item.ArtifactKind == artifactKind.ToString())
            .ToArrayAsync(cancellationToken))
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .FirstOrDefault();

        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<EngineeringCalculationArtifactRecordDto>> ListByScenarioIdAsync(
        string scenarioId,
        CancellationToken cancellationToken)
    {
        var entities = (await _context.Artifacts
            .AsNoTracking()
            .Where(item => item.ScenarioId == scenarioId)
            .ToArrayAsync(cancellationToken))
            .OrderBy(item => item.ArtifactKind, StringComparer.Ordinal)
            .ThenByDescending(item => item.CreatedAtUtc)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .ToArray();

        return entities.Select(Map).ToArray();
    }

    private static EngineeringCalculationArtifactRecordDto Map(EngineeringCalculationArtifactEntity entity)
    {
        var artifactKind = Enum.TryParse<EngineeringCalculationArtifactKind>(entity.ArtifactKind, true, out var parsed)
            ? parsed
            : EngineeringCalculationArtifactKind.ScenarioResultJson;

        return new EngineeringCalculationArtifactRecordDto(
            ArtifactId: entity.Id,
            ScenarioId: entity.ScenarioId,
            ArtifactKind: artifactKind,
            ContentType: entity.ContentType,
            Content: entity.Content,
            CreatedAtUtc: entity.CreatedAtUtc,
            SizeBytes: entity.SizeBytes,
            ChecksumSha256: entity.Checksum);
    }
}
