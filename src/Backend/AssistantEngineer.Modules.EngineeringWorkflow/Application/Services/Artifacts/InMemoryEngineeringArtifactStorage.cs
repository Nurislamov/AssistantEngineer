using System.Collections.Concurrent;
using System.Security.Cryptography;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Abstractions.Artifacts;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.Artifacts;
using AssistantEngineer.SharedKernel.Diagnostics;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Services.Artifacts;

public sealed class InMemoryEngineeringArtifactStorage : IEngineeringArtifactStorage
{
    private readonly ConcurrentDictionary<string, StoredArtifact> _store = new(StringComparer.Ordinal);
    private readonly EngineeringArtifactStorageOptions _options;
    private readonly ILogger<InMemoryEngineeringArtifactStorage> _logger;

    public InMemoryEngineeringArtifactStorage(
        IOptions<EngineeringArtifactStorageOptions> options,
        ILogger<InMemoryEngineeringArtifactStorage>? logger = null)
    {
        _options = options.Value;
        _logger = logger ?? NullLogger<InMemoryEngineeringArtifactStorage>.Instance;
    }

    public Task<Result<EngineeringArtifactDescriptor>> WriteAsync(
        EngineeringArtifactWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validation = ValidateWriteRequest(request);
        if (validation.IsFailure)
        {
            LogValidationFailure(request, validation.Error);
            return Task.FromResult(Result<EngineeringArtifactDescriptor>.Failure(validation));
        }

        var artifactId = $"engart-{Guid.NewGuid():N}";
        _logger.LogInformation(
            "{EventCode} Artifact write started. Provider={StorageProvider} ArtifactId={ArtifactId} ArtifactKind={ArtifactKind} Scope={Scope} SubjectType={SubjectType} SubjectId={SubjectId} SizeBytes={SizeBytes} CorrelationId={CorrelationId}",
            ObservabilityEventCodes.ArtifactWriteStarted,
            EngineeringArtifactStorageProviders.InMemory,
            artifactId,
            request.ArtifactKind,
            request.Scope,
            request.SubjectType,
            request.SubjectId,
            request.Content.LongLength,
            "n/a");

        var contentCopy = request.Content.ToArray();
        var createdAtUtc = DateTimeOffset.UtcNow;
        var checksum = ComputeSha256Hex(contentCopy);
        var metadata = CloneMetadata(request.Metadata);

        var descriptor = new EngineeringArtifactDescriptor(
            ArtifactId: artifactId,
            ArtifactKind: request.ArtifactKind.Trim(),
            Scope: request.Scope.Trim(),
            SubjectType: NormalizeOptional(request.SubjectType),
            SubjectId: NormalizeOptional(request.SubjectId),
            ContentType: request.ContentType.Trim(),
            SizeBytes: contentCopy.LongLength,
            Sha256: checksum,
            StorageProvider: EngineeringArtifactStorageProviders.InMemory,
            StorageKey: artifactId,
            CreatedAtUtc: createdAtUtc,
            Metadata: metadata);

        _store[artifactId] = new StoredArtifact(descriptor, contentCopy);
        _logger.LogInformation(
            "{EventCode} Artifact write completed. Provider={StorageProvider} ArtifactId={ArtifactId} ArtifactKind={ArtifactKind} Scope={Scope} SizeBytes={SizeBytes} Sha256={Sha256} CorrelationId={CorrelationId}",
            ObservabilityEventCodes.ArtifactWriteCompleted,
            EngineeringArtifactStorageProviders.InMemory,
            descriptor.ArtifactId,
            descriptor.ArtifactKind,
            descriptor.Scope,
            descriptor.SizeBytes,
            descriptor.Sha256,
            "n/a");

        return Task.FromResult(Result<EngineeringArtifactDescriptor>.Success(CloneDescriptor(descriptor)));
    }

    public Task<Result<EngineeringArtifactReadResult>> ReadAsync(
        string artifactId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var artifactIdValidation = ValidateArtifactId(artifactId);
        if (artifactIdValidation.IsFailure)
        {
            return Task.FromResult(Result<EngineeringArtifactReadResult>.Failure(artifactIdValidation));
        }

        var normalizedArtifactId = artifactId.Trim();
        if (!_store.TryGetValue(normalizedArtifactId, out var stored))
        {
            return Task.FromResult(Result<EngineeringArtifactReadResult>.NotFound(
                $"Engineering artifact '{normalizedArtifactId}' was not found."));
        }

        var descriptor = CloneDescriptor(stored.Descriptor);
        var content = stored.Content.ToArray();
        _logger.LogInformation(
            "{EventCode} Artifact read completed. Provider={StorageProvider} ArtifactId={ArtifactId} SizeBytes={SizeBytes} CorrelationId={CorrelationId}",
            ObservabilityEventCodes.ArtifactReadCompleted,
            EngineeringArtifactStorageProviders.InMemory,
            descriptor.ArtifactId,
            descriptor.SizeBytes,
            "n/a");
        return Task.FromResult(Result<EngineeringArtifactReadResult>.Success(
            new EngineeringArtifactReadResult(descriptor, content)));
    }

    public Task<Result<EngineeringArtifactDescriptor>> GetDescriptorAsync(
        string artifactId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var artifactIdValidation = ValidateArtifactId(artifactId);
        if (artifactIdValidation.IsFailure)
        {
            return Task.FromResult(Result<EngineeringArtifactDescriptor>.Failure(artifactIdValidation));
        }

        var normalizedArtifactId = artifactId.Trim();
        if (!_store.TryGetValue(normalizedArtifactId, out var stored))
        {
            return Task.FromResult(Result<EngineeringArtifactDescriptor>.NotFound(
                $"Engineering artifact '{normalizedArtifactId}' was not found."));
        }

        return Task.FromResult(Result<EngineeringArtifactDescriptor>.Success(CloneDescriptor(stored.Descriptor)));
    }

    public Task<Result> DeleteAsync(
        string artifactId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var artifactIdValidation = ValidateArtifactId(artifactId);
        if (artifactIdValidation.IsFailure)
        {
            return Task.FromResult(Result.Failure(artifactIdValidation.Error, artifactIdValidation.ErrorType));
        }

        var normalizedArtifactId = artifactId.Trim();
        if (!_store.TryRemove(normalizedArtifactId, out _))
        {
            return Task.FromResult(Result.NotFound($"Engineering artifact '{normalizedArtifactId}' was not found."));
        }

        return Task.FromResult(Result.Success());
    }

    private Result ValidateWriteRequest(EngineeringArtifactWriteRequest request)
    {
        if (request is null)
        {
            return Result.Validation("Engineering artifact write request is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ArtifactKind))
        {
            return Result.Validation("Engineering artifact kind is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Scope))
        {
            return Result.Validation("Engineering artifact scope is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ContentType))
        {
            return Result.Validation("Engineering artifact content type is required.");
        }

        if (request.Content is null || request.Content.Length == 0)
        {
            return Result.Validation("Engineering artifact content is required.");
        }

        if (request.Content.LongLength > _options.MaxArtifactBytes)
        {
            return Result.Validation(
                $"Engineering artifact content exceeds max size limit of {_options.MaxArtifactBytes} bytes.");
        }

        return Result.Success();
    }

    private void LogValidationFailure(EngineeringArtifactWriteRequest request, string error)
    {
        var sizeBytes = request?.Content?.LongLength ?? 0;
        var eventCode = error.Contains("max size limit", StringComparison.OrdinalIgnoreCase)
            ? ObservabilityEventCodes.ArtifactSizeLimitExceeded
            : ObservabilityEventCodes.ArtifactWriteStarted;
        var level = eventCode == ObservabilityEventCodes.ArtifactSizeLimitExceeded ? LogLevel.Warning : LogLevel.Error;

        _logger.Log(
            level,
            "{EventCode} Artifact write rejected. Provider={StorageProvider} ArtifactKind={ArtifactKind} Scope={Scope} SizeBytes={SizeBytes} Error={Error} CorrelationId={CorrelationId}",
            eventCode,
            EngineeringArtifactStorageProviders.InMemory,
            request?.ArtifactKind,
            request?.Scope,
            sizeBytes,
            error,
            "n/a");
    }

    private static Result ValidateArtifactId(string artifactId)
    {
        if (string.IsNullOrWhiteSpace(artifactId))
        {
            return Result.Validation("Engineering artifact id is required.");
        }

        if (artifactId.Length > 160)
        {
            return Result.Validation("Engineering artifact id is too long.");
        }

        foreach (var character in artifactId)
        {
            if (char.IsLetterOrDigit(character) || character is '-' or '_')
            {
                continue;
            }

            return Result.Validation("Engineering artifact id contains unsupported characters.");
        }

        return Result.Success();
    }

    private static string ComputeSha256Hex(byte[] content)
    {
        return Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
    }

    private static IReadOnlyDictionary<string, string>? CloneMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return null;
        }

        return metadata
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
    }

    private static EngineeringArtifactDescriptor CloneDescriptor(EngineeringArtifactDescriptor descriptor)
    {
        return descriptor with
        {
            Metadata = CloneMetadata(descriptor.Metadata)
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed record StoredArtifact(
        EngineeringArtifactDescriptor Descriptor,
        byte[] Content);
}
