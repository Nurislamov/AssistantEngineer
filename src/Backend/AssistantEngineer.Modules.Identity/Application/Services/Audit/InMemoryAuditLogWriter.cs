using System.Collections.Concurrent;
using AssistantEngineer.Modules.Identity.Application.Abstractions;
using AssistantEngineer.Modules.Identity.Application.Contracts.Audit;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Identity.Application.Services.Audit;

public sealed class InMemoryAuditLogWriter : IAuditLogWriter
{
    private readonly ConcurrentQueue<AuditEventRecord> _records = new();
    private readonly AuditLogOptions _options;
    private readonly AuditMetadataSanitizer _metadataSanitizer;
    private readonly TimeProvider _timeProvider;

    public InMemoryAuditLogWriter(
        IOptions<AuditLogOptions> options,
        AuditMetadataSanitizer metadataSanitizer,
        TimeProvider timeProvider)
    {
        _options = options.Value;
        _metadataSanitizer = metadataSanitizer;
        _timeProvider = timeProvider;
    }

    public Task<Result<AuditEventRecord>> WriteAsync(
        AuditEventWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validation = Validate(request);
        if (validation.IsFailure)
        {
            return Task.FromResult(Result<AuditEventRecord>.Failure(validation));
        }

        if (!_options.Enabled)
        {
            return Task.FromResult(Result<AuditEventRecord>.Success(CreateSkippedRecord(request)));
        }

        var record = CreateRecord(request);
        _records.Enqueue(record);
        return Task.FromResult(Result<AuditEventRecord>.Success(record));
    }

    public Task<Result<IReadOnlyList<AuditEventRecord>>> QueryByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return Task.FromResult(Result<IReadOnlyList<AuditEventRecord>>.Validation("Correlation id is required."));
        }

        var normalized = correlationId.Trim();
        var records = _records
            .Where(record => string.Equals(record.CorrelationId, normalized, StringComparison.Ordinal))
            .Select(CloneRecord)
            .ToArray();

        return Task.FromResult(Result<IReadOnlyList<AuditEventRecord>>.Success(records));
    }

    public Task<Result<IReadOnlyList<AuditEventRecord>>> QueryByResourceAsync(
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(resourceType))
        {
            return Task.FromResult(Result<IReadOnlyList<AuditEventRecord>>.Validation("Resource type is required."));
        }

        if (string.IsNullOrWhiteSpace(resourceId))
        {
            return Task.FromResult(Result<IReadOnlyList<AuditEventRecord>>.Validation("Resource id is required."));
        }

        var normalizedType = resourceType.Trim();
        var normalizedId = resourceId.Trim();
        var records = _records
            .Where(record =>
                string.Equals(record.ResourceType, normalizedType, StringComparison.Ordinal) &&
                string.Equals(record.ResourceId, normalizedId, StringComparison.Ordinal))
            .Select(CloneRecord)
            .ToArray();

        return Task.FromResult(Result<IReadOnlyList<AuditEventRecord>>.Success(records));
    }

    private Result Validate(AuditEventWriteRequest? request)
    {
        if (request is null)
        {
            return Result.Validation("Audit event request is required.");
        }

        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            return Result.Validation("Audit event type is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            return Result.Validation("Audit category is required.");
        }

        return Result.Success();
    }

    private AuditEventRecord CreateRecord(AuditEventWriteRequest request)
    {
        var principal = request.Principal;
        var metadata = _metadataSanitizer.Sanitize(request.Metadata, _options.MaxMetadataValueLength);

        return new AuditEventRecord(
            AuditEventId: $"audit-{Guid.NewGuid():N}",
            OccurredAtUtc: _timeProvider.GetUtcNow(),
            EventType: request.EventType.Trim(),
            Category: request.Category.Trim(),
            Outcome: request.Outcome,
            UserId: principal?.UserId,
            OrganizationId: principal?.OrganizationId,
            ExternalSubjectId: NormalizeOptional(principal?.ExternalSubjectId),
            CorrelationId: NormalizeOptional(request.CorrelationId),
            RequestId: NormalizeOptional(request.RequestId),
            ProjectId: NormalizeOptional(request.ProjectId),
            BuildingId: NormalizeOptional(request.BuildingId),
            WorkflowId: NormalizeOptional(request.WorkflowId),
            JobId: NormalizeOptional(request.JobId),
            ArtifactId: NormalizeOptional(request.ArtifactId),
            ResourceType: NormalizeOptional(request.ResourceType),
            ResourceId: NormalizeOptional(request.ResourceId),
            Permission: NormalizeOptional(request.Permission),
            FailureReason: NormalizeOptional(request.FailureReason),
            Metadata: metadata);
    }

    private AuditEventRecord CreateSkippedRecord(AuditEventWriteRequest request)
    {
        var metadata = _metadataSanitizer.Sanitize(request.Metadata, _options.MaxMetadataValueLength);
        return new AuditEventRecord(
            AuditEventId: $"audit-disabled-{Guid.NewGuid():N}",
            OccurredAtUtc: _timeProvider.GetUtcNow(),
            EventType: string.IsNullOrWhiteSpace(request.EventType) ? "AUD-SYS-000" : request.EventType.Trim(),
            Category: string.IsNullOrWhiteSpace(request.Category) ? AuditEventCategory.System : request.Category.Trim(),
            Outcome: AuditEventOutcome.Skipped,
            UserId: request.Principal?.UserId,
            OrganizationId: request.Principal?.OrganizationId,
            ExternalSubjectId: NormalizeOptional(request.Principal?.ExternalSubjectId),
            CorrelationId: NormalizeOptional(request.CorrelationId),
            RequestId: NormalizeOptional(request.RequestId),
            ProjectId: NormalizeOptional(request.ProjectId),
            BuildingId: NormalizeOptional(request.BuildingId),
            WorkflowId: NormalizeOptional(request.WorkflowId),
            JobId: NormalizeOptional(request.JobId),
            ArtifactId: NormalizeOptional(request.ArtifactId),
            ResourceType: NormalizeOptional(request.ResourceType),
            ResourceId: NormalizeOptional(request.ResourceId),
            Permission: NormalizeOptional(request.Permission),
            FailureReason: "AuditLogDisabled",
            Metadata: metadata);
    }

    private AuditEventRecord CloneRecord(AuditEventRecord record)
    {
        return record with
        {
            Metadata = _metadataSanitizer.Sanitize(record.Metadata, _options.MaxMetadataValueLength)
        };
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
