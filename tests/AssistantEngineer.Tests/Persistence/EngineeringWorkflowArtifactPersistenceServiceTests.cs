using System.Security.Cryptography;
using System.Text;
using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;

namespace AssistantEngineer.Tests.Persistence;

public class EngineeringWorkflowArtifactPersistenceServiceTests
{
    [Fact]
    public async Task SaveScenarioArtifact_UnderLimit_PersistsUnchangedContentAndMetadata()
    {
        var store = new EngineeringWorkflowMemoryStore();
        var repository = new InMemoryEngineeringCalculationArtifactRepository(store);
        var limits = CreateLimits(maxBytes: 1024);
        var payloadService = new EngineeringWorkflowPersistencePayloadService(limits);
        var artifactService = new EngineeringWorkflowArtifactPersistenceService(repository, payloadService, limits);
        var content = "{\"result\":\"ok\"}";
        var createdAt = DateTimeOffset.Parse("2026-05-11T00:00:00Z");

        await artifactService.SaveScenarioArtifactAsync(
            "scenario-artifact-under-limit",
            EngineeringCalculationArtifactKind.ReportJson,
            "application/json",
            content,
            createdAt,
            CancellationToken.None);

        var persisted = await repository.GetByScenarioAndKindAsync(
            "scenario-artifact-under-limit",
            EngineeringCalculationArtifactKind.ReportJson,
            CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal(content, persisted.Content);
        Assert.Equal("application/json", persisted.ContentType);
        Assert.Equal(Encoding.UTF8.GetByteCount(content), persisted.SizeBytes);
        Assert.Equal(
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant(),
            persisted.ChecksumSha256);
    }

    [Fact]
    public async Task SaveScenarioArtifact_OverLimit_AppliesDeterministicTruncationPolicy()
    {
        var store = new EngineeringWorkflowMemoryStore();
        var repository = new InMemoryEngineeringCalculationArtifactRepository(store);
        var limits = CreateLimits(maxBytes: 256);
        var payloadService = new EngineeringWorkflowPersistencePayloadService(limits);
        var artifactService = new EngineeringWorkflowArtifactPersistenceService(repository, payloadService, limits);
        var content = "{\"report\":\"" + new string('r', 8_000) + "\"}";

        await artifactService.SaveScenarioArtifactAsync(
            "scenario-artifact-over-limit",
            EngineeringCalculationArtifactKind.ReportJson,
            "application/json",
            content,
            DateTimeOffset.UtcNow,
            CancellationToken.None);

        var persisted = await repository.GetByScenarioAndKindAsync(
            "scenario-artifact-over-limit",
            EngineeringCalculationArtifactKind.ReportJson,
            CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.True(persisted.SizeBytes <= 256);
        Assert.Contains("\"truncated\":true", persisted.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[TRUNCATED_BY_ASSISTANT_ENGINEER_PAYLOAD_LIMIT]", persisted.Content, StringComparison.Ordinal);
    }

    private static EngineeringWorkflowPayloadLimitsOptions CreateLimits(int maxBytes)
    {
        return new EngineeringWorkflowPayloadLimitsOptions
        {
            Enabled = true,
            RequestJsonMaxBytes = maxBytes,
            StateJsonMaxBytes = maxBytes,
            ResultSummaryJsonMaxBytes = maxBytes,
            DiagnosticsJsonMaxBytes = maxBytes,
            ArtifactContentMaxBytes = maxBytes,
            TruncationMarker = "[TRUNCATED_BY_ASSISTANT_ENGINEER_PAYLOAD_LIMIT]"
        };
    }
}
