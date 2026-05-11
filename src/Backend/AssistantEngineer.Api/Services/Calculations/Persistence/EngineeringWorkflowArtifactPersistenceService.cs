using System.Security.Cryptography;
using System.Text;
using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

internal sealed class EngineeringWorkflowArtifactPersistenceService
{
    private readonly IEngineeringCalculationArtifactRepository _artifactRepository;
    private readonly EngineeringWorkflowPersistencePayloadService _payloadService;
    private readonly EngineeringWorkflowPayloadLimitsOptions _payloadLimits;

    public EngineeringWorkflowArtifactPersistenceService(
        IEngineeringCalculationArtifactRepository artifactRepository,
        EngineeringWorkflowPersistencePayloadService payloadService,
        EngineeringWorkflowPayloadLimitsOptions payloadLimits)
    {
        _artifactRepository = artifactRepository;
        _payloadService = payloadService;
        _payloadLimits = payloadLimits;
    }

    public async Task SaveScenarioArtifactAsync(
        string scenarioId,
        EngineeringCalculationArtifactKind artifactKind,
        string contentType,
        string content,
        DateTimeOffset timestampUtc,
        CancellationToken cancellationToken)
    {
        var limitedContent = _payloadService.ApplyPayloadLimit(
            $"scenario-artifact-{artifactKind}",
            content,
            _payloadLimits.ArtifactContentMaxBytes,
            contentType);
        var bytes = Encoding.UTF8.GetBytes(limitedContent.Content);
        var artifact = new EngineeringCalculationArtifactRecordDto(
            ArtifactId: $"{scenarioId}:{artifactKind}",
            ScenarioId: scenarioId,
            ArtifactKind: artifactKind,
            ContentType: contentType,
            Content: limitedContent.Content,
            CreatedAtUtc: timestampUtc,
            SizeBytes: bytes.Length,
            ChecksumSha256: Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());

        await _artifactRepository.SaveAsync(artifact, cancellationToken);
    }
}
