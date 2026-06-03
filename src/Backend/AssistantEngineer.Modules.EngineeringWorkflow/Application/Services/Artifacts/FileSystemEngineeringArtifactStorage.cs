using System.Security.Cryptography;
using System.Text.Json;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Abstractions.Artifacts;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.Artifacts;
using AssistantEngineer.SharedKernel.Diagnostics;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Services.Artifacts;

public sealed class FileSystemEngineeringArtifactStorage : IEngineeringArtifactStorage
{
    private const string ContentFileName = "content.bin";
    private const string DescriptorFileName = "descriptor.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly EngineeringArtifactStorageOptions _options;
    private readonly string _rootPath;
    private readonly ILogger<FileSystemEngineeringArtifactStorage> _logger;

    public FileSystemEngineeringArtifactStorage(
        IOptions<EngineeringArtifactStorageOptions> options,
        ILogger<FileSystemEngineeringArtifactStorage>? logger = null)
    {
        _options = options.Value;
        _rootPath = ResolveRootPath(_options.RootPath);
        _logger = logger ?? NullLogger<FileSystemEngineeringArtifactStorage>.Instance;
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<Result<EngineeringArtifactDescriptor>> WriteAsync(
        EngineeringArtifactWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validation = ValidateWriteRequest(request);
        if (validation.IsFailure)
        {
            LogValidationFailure(request, validation.Error);
            return Result<EngineeringArtifactDescriptor>.Failure(validation);
        }

        var artifactId = $"engart-{Guid.NewGuid():N}";
        _logger.LogInformation(
            "{EventCode} Artifact write started. Provider={StorageProvider} ArtifactId={ArtifactId} ArtifactKind={ArtifactKind} Scope={Scope} SubjectType={SubjectType} SubjectId={SubjectId} SizeBytes={SizeBytes} CorrelationId={CorrelationId}",
            ObservabilityEventCodes.ArtifactWriteStarted,
            EngineeringArtifactStorageProviders.FileSystem,
            artifactId,
            request.ArtifactKind,
            request.Scope,
            request.SubjectType,
            request.SubjectId,
            request.Content.LongLength,
            "n/a");
        var artifactDirectory = GetArtifactDirectoryPath(artifactId);
        Directory.CreateDirectory(artifactDirectory);

        var contentCopy = request.Content.ToArray();
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
            StorageProvider: EngineeringArtifactStorageProviders.FileSystem,
            StorageKey: $"{artifactId}/{ContentFileName}",
            CreatedAtUtc: DateTimeOffset.UtcNow,
            Metadata: metadata);

        var contentPath = Path.Combine(artifactDirectory, ContentFileName);
        var descriptorPath = Path.Combine(artifactDirectory, DescriptorFileName);

        await File.WriteAllBytesAsync(contentPath, contentCopy, cancellationToken);
        var persistable = PersistedDescriptor.FromDescriptor(descriptor);
        await File.WriteAllTextAsync(
            descriptorPath,
            JsonSerializer.Serialize(persistable, JsonOptions),
            cancellationToken);

        _logger.LogInformation(
            "{EventCode} Artifact write completed. Provider={StorageProvider} ArtifactId={ArtifactId} ArtifactKind={ArtifactKind} Scope={Scope} SizeBytes={SizeBytes} Sha256={Sha256} CorrelationId={CorrelationId}",
            ObservabilityEventCodes.ArtifactWriteCompleted,
            EngineeringArtifactStorageProviders.FileSystem,
            descriptor.ArtifactId,
            descriptor.ArtifactKind,
            descriptor.Scope,
            descriptor.SizeBytes,
            descriptor.Sha256,
            "n/a");

        return Result<EngineeringArtifactDescriptor>.Success(CloneDescriptor(descriptor));
    }

    public async Task<Result<EngineeringArtifactReadResult>> ReadAsync(
        string artifactId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var descriptorResult = await LoadDescriptorAsync(artifactId, cancellationToken);
        if (descriptorResult.IsFailure)
        {
            return Result<EngineeringArtifactReadResult>.Failure(descriptorResult);
        }

        var descriptor = descriptorResult.Value;
        var artifactDirectory = GetArtifactDirectoryPath(descriptor.ArtifactId);
        var contentPath = Path.Combine(artifactDirectory, ContentFileName);
        if (!File.Exists(contentPath))
        {
            return Result<EngineeringArtifactReadResult>.NotFound(
                $"Engineering artifact '{descriptor.ArtifactId}' was not found.");
        }

        var bytes = await File.ReadAllBytesAsync(contentPath, cancellationToken);

        if (_options.EnableSha256Verification)
        {
            var actual = ComputeSha256Hex(bytes);
            if (!string.Equals(actual, descriptor.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "{EventCode} Artifact integrity check failed. Provider={StorageProvider} ArtifactId={ArtifactId} ExpectedSha256={ExpectedSha256} ActualSha256={ActualSha256} CorrelationId={CorrelationId}",
                    ObservabilityEventCodes.ArtifactIntegrityCheckFailed,
                    EngineeringArtifactStorageProviders.FileSystem,
                    descriptor.ArtifactId,
                    descriptor.Sha256,
                    actual,
                    "n/a");
                return Result<EngineeringArtifactReadResult>.Failure(
                    $"Engineering artifact '{descriptor.ArtifactId}' failed SHA256 verification.");
            }
        }

        _logger.LogInformation(
            "{EventCode} Artifact read completed. Provider={StorageProvider} ArtifactId={ArtifactId} SizeBytes={SizeBytes} CorrelationId={CorrelationId}",
            ObservabilityEventCodes.ArtifactReadCompleted,
            EngineeringArtifactStorageProviders.FileSystem,
            descriptor.ArtifactId,
            descriptor.SizeBytes,
            "n/a");

        return Result<EngineeringArtifactReadResult>.Success(
            new EngineeringArtifactReadResult(CloneDescriptor(descriptor), bytes.ToArray()));
    }

    public async Task<Result<EngineeringArtifactDescriptor>> GetDescriptorAsync(
        string artifactId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var descriptorResult = await LoadDescriptorAsync(artifactId, cancellationToken);
        if (descriptorResult.IsFailure)
        {
            return descriptorResult;
        }

        return Result<EngineeringArtifactDescriptor>.Success(CloneDescriptor(descriptorResult.Value));
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
        var artifactDirectory = GetArtifactDirectoryPath(normalizedArtifactId);
        if (!Directory.Exists(artifactDirectory))
        {
            return Task.FromResult(Result.NotFound($"Engineering artifact '{normalizedArtifactId}' was not found."));
        }

        Directory.Delete(artifactDirectory, recursive: true);
        return Task.FromResult(Result.Success());
    }

    private async Task<Result<EngineeringArtifactDescriptor>> LoadDescriptorAsync(
        string artifactId,
        CancellationToken cancellationToken)
    {
        var artifactIdValidation = ValidateArtifactId(artifactId);
        if (artifactIdValidation.IsFailure)
        {
            return Result<EngineeringArtifactDescriptor>.Failure(artifactIdValidation);
        }

        var normalizedArtifactId = artifactId.Trim();
        var artifactDirectory = GetArtifactDirectoryPath(normalizedArtifactId);
        var descriptorPath = Path.Combine(artifactDirectory, DescriptorFileName);
        if (!File.Exists(descriptorPath))
        {
            return Result<EngineeringArtifactDescriptor>.NotFound(
                $"Engineering artifact '{normalizedArtifactId}' was not found.");
        }

        try
        {
            var json = await File.ReadAllTextAsync(descriptorPath, cancellationToken);
            var persisted = JsonSerializer.Deserialize<PersistedDescriptor>(json, JsonOptions);
            if (persisted is null)
            {
                return Result<EngineeringArtifactDescriptor>.Failure(
                    $"Engineering artifact descriptor '{normalizedArtifactId}' could not be parsed.");
            }

            var descriptor = persisted.ToDescriptor();
            return Result<EngineeringArtifactDescriptor>.Success(descriptor);
        }
        catch (JsonException ex)
        {
            return Result<EngineeringArtifactDescriptor>.Failure(
                $"Engineering artifact descriptor '{normalizedArtifactId}' is invalid JSON: {ex.Message}");
        }
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
            EngineeringArtifactStorageProviders.FileSystem,
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

        if (artifactId.Contains("..", StringComparison.Ordinal) ||
            artifactId.Contains(Path.DirectorySeparatorChar) ||
            artifactId.Contains(Path.AltDirectorySeparatorChar))
        {
            return Result.Validation("Engineering artifact id is invalid.");
        }

        return Result.Success();
    }

    private static string ResolveRootPath(string? configuredRootPath)
    {
        if (string.IsNullOrWhiteSpace(configuredRootPath))
        {
            return Path.GetFullPath(Path.Combine(Path.GetTempPath(), "assistant-engineer-artifacts"));
        }

        return Path.GetFullPath(configuredRootPath);
    }

    private string GetArtifactDirectoryPath(string artifactId)
    {
        var combinedPath = Path.GetFullPath(Path.Combine(_rootPath, artifactId));
        if (!IsInsideDirectory(combinedPath, _rootPath))
        {
            throw new InvalidOperationException("Resolved artifact directory escaped configured root path.");
        }

        return combinedPath;
    }

    private static bool IsInsideDirectory(string candidatePath, string directoryPath)
    {
        var normalizedCandidate = Path.GetFullPath(candidatePath);
        var normalizedDirectory = Path.GetFullPath(directoryPath);
        if (!normalizedDirectory.EndsWith(Path.DirectorySeparatorChar))
        {
            normalizedDirectory += Path.DirectorySeparatorChar;
        }

        return normalizedCandidate.StartsWith(normalizedDirectory, StringComparison.OrdinalIgnoreCase);
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

    private sealed record PersistedDescriptor(
        string ArtifactId,
        string ArtifactKind,
        string Scope,
        string? SubjectType,
        string? SubjectId,
        string ContentType,
        long SizeBytes,
        string Sha256,
        string StorageProvider,
        string StorageKey,
        DateTimeOffset CreatedAtUtc,
        Dictionary<string, string>? Metadata)
    {
        public EngineeringArtifactDescriptor ToDescriptor()
        {
            var metadata = Metadata is null || Metadata.Count == 0
                ? null
                : Metadata
                    .OrderBy(item => item.Key, StringComparer.Ordinal)
                    .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);

            return new EngineeringArtifactDescriptor(
                ArtifactId: ArtifactId,
                ArtifactKind: ArtifactKind,
                Scope: Scope,
                SubjectType: SubjectType,
                SubjectId: SubjectId,
                ContentType: ContentType,
                SizeBytes: SizeBytes,
                Sha256: Sha256,
                StorageProvider: StorageProvider,
                StorageKey: StorageKey,
                CreatedAtUtc: CreatedAtUtc,
                Metadata: metadata);
        }

        public static PersistedDescriptor FromDescriptor(EngineeringArtifactDescriptor descriptor)
        {
            var metadata = descriptor.Metadata is null || descriptor.Metadata.Count == 0
                ? null
                : descriptor.Metadata.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);

            return new PersistedDescriptor(
                ArtifactId: descriptor.ArtifactId,
                ArtifactKind: descriptor.ArtifactKind,
                Scope: descriptor.Scope,
                SubjectType: descriptor.SubjectType,
                SubjectId: descriptor.SubjectId,
                ContentType: descriptor.ContentType,
                SizeBytes: descriptor.SizeBytes,
                Sha256: descriptor.Sha256,
                StorageProvider: descriptor.StorageProvider,
                StorageKey: descriptor.StorageKey,
                CreatedAtUtc: descriptor.CreatedAtUtc,
                Metadata: metadata);
        }
    }
}
