using AssistantEngineer.Modules.Identity.Application.Contracts;
using AssistantEngineer.Modules.Identity.Application.Contracts.Audit;
using AssistantEngineer.Modules.Identity.Application.Services.Audit;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Identity;

public sealed class InMemoryAuditLogWriterTests
{
    [Fact]
    public async Task WriteAsync_StoresRecordWithIdAndTimestamp()
    {
        var writer = CreateWriter(enabled: true, fixedNow: new DateTimeOffset(2026, 5, 14, 10, 15, 0, TimeSpan.Zero));
        var request = CreateValidRequest();

        var result = await writer.WriteAsync(request);

        Assert.True(result.IsSuccess, result.Error);
        Assert.StartsWith("audit-", result.Value.AuditEventId, StringComparison.Ordinal);
        Assert.Equal(new DateTimeOffset(2026, 5, 14, 10, 15, 0, TimeSpan.Zero), result.Value.OccurredAtUtc);
        Assert.Equal(AuditEventTypes.AuthenticationSucceeded, result.Value.EventType);
    }

    [Fact]
    public async Task QueryByCorrelationIdAsync_ReturnsMatchingRecords()
    {
        var writer = CreateWriter(enabled: true);
        await writer.WriteAsync(CreateValidRequest(correlationId: "corr-1"));
        await writer.WriteAsync(CreateValidRequest(correlationId: "corr-2"));
        await writer.WriteAsync(CreateValidRequest(correlationId: "corr-1"));

        var query = await writer.QueryByCorrelationIdAsync("corr-1");

        Assert.True(query.IsSuccess, query.Error);
        Assert.Equal(2, query.Value.Count);
        Assert.All(query.Value, record => Assert.Equal("corr-1", record.CorrelationId));
    }

    [Fact]
    public async Task QueryByResourceAsync_ReturnsMatchingRecords()
    {
        var writer = CreateWriter(enabled: true);
        await writer.WriteAsync(CreateValidRequest(resourceType: "Workflow", resourceId: "wf-001"));
        await writer.WriteAsync(CreateValidRequest(resourceType: "Workflow", resourceId: "wf-002"));
        await writer.WriteAsync(CreateValidRequest(resourceType: "Workflow", resourceId: "wf-001"));

        var query = await writer.QueryByResourceAsync("Workflow", "wf-001");

        Assert.True(query.IsSuccess, query.Error);
        Assert.Equal(2, query.Value.Count);
    }

    [Fact]
    public async Task MissingEventType_ReturnsValidation()
    {
        var writer = CreateWriter(enabled: true);
        var invalidRequest = CreateValidRequest() with { EventType = string.Empty };

        var result = await writer.WriteAsync(invalidRequest);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task ForbiddenMetadata_IsSanitized()
    {
        var writer = CreateWriter(enabled: true);
        var request = CreateValidRequest(metadata: new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["apiKey"] = "super-secret",
            ["token"] = "token-value",
            ["safe"] = "ok"
        });

        var writeResult = await writer.WriteAsync(request);

        Assert.True(writeResult.IsSuccess, writeResult.Error);
        Assert.NotNull(writeResult.Value.Metadata);
        Assert.False(writeResult.Value.Metadata!.ContainsKey("apiKey"));
        Assert.False(writeResult.Value.Metadata.ContainsKey("token"));
        Assert.Equal("ok", writeResult.Value.Metadata["safe"]);
    }

    [Fact]
    public async Task ReturnedRecords_DoNotExposeMutableInternalMetadata()
    {
        var writer = CreateWriter(enabled: true);
        var request = CreateValidRequest(metadata: new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["safe"] = "original"
        });

        var writeResult = await writer.WriteAsync(request);
        Assert.True(writeResult.IsSuccess, writeResult.Error);

        var query = await writer.QueryByCorrelationIdAsync(writeResult.Value.CorrelationId!);
        Assert.True(query.IsSuccess, query.Error);
        var first = query.Value.Single();

        Assert.NotNull(first.Metadata);
        Assert.False(first.Metadata is Dictionary<string, string>);

        var secondQuery = await writer.QueryByCorrelationIdAsync(writeResult.Value.CorrelationId!);
        Assert.True(secondQuery.IsSuccess, secondQuery.Error);
        Assert.Equal("original", secondQuery.Value.Single().Metadata!["safe"]);
    }

    [Fact]
    public async Task DisabledAuditLog_ReturnsSkippedRecordWithoutPersistence()
    {
        var writer = CreateWriter(enabled: false);
        var request = CreateValidRequest(correlationId: "corr-disabled");

        var writeResult = await writer.WriteAsync(request);
        var query = await writer.QueryByCorrelationIdAsync("corr-disabled");

        Assert.True(writeResult.IsSuccess, writeResult.Error);
        Assert.Equal(AuditEventOutcome.Skipped, writeResult.Value.Outcome);
        Assert.True(query.IsSuccess, query.Error);
        Assert.Empty(query.Value);
    }

    private static InMemoryAuditLogWriter CreateWriter(
        bool enabled,
        DateTimeOffset? fixedNow = null)
    {
        var options = Options.Create(new AuditLogOptions
        {
            Enabled = enabled,
            MaxMetadataValueLength = 512,
            Provider = "InMemory",
            WriteArtifactEvents = true,
            WriteAuthorizationDeniedEvents = true
        });

        return new InMemoryAuditLogWriter(
            options,
            new AuditMetadataSanitizer(),
            new FixedTimeProvider(fixedNow ?? new DateTimeOffset(2026, 5, 14, 0, 0, 0, TimeSpan.Zero)));
    }

    private static AuditEventWriteRequest CreateValidRequest(
        string? correlationId = "corr-1",
        string? resourceType = "Workflow",
        string? resourceId = "wf-001",
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new AuditEventWriteRequest(
            EventType: AuditEventTypes.AuthenticationSucceeded,
            Category: AuditEventCategory.Authentication,
            Outcome: AuditEventOutcome.Succeeded,
            Principal: new PrincipalAccessContext(
                UserId: 100,
                OrganizationId: 200,
                ExternalSubjectId: "sub-100",
                Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Engineer" },
                Permissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ProjectsRead" },
                IsAuthenticated: true),
            CorrelationId: correlationId,
            RequestId: "req-1",
            ResourceType: resourceType,
            ResourceId: resourceId,
            ProjectId: "1",
            BuildingId: "2",
            WorkflowId: "wf-001",
            JobId: "job-001",
            ArtifactId: "art-001",
            Permission: "ProjectsRead",
            FailureReason: null,
            Metadata: metadata);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
