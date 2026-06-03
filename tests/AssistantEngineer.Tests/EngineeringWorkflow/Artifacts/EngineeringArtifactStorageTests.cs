using System.Security.Cryptography;
using System.Text;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Abstractions.Artifacts;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.Artifacts;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Services.Artifacts;
using AssistantEngineer.SharedKernel.Diagnostics;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.EngineeringWorkflow.Artifacts;

public sealed class EngineeringArtifactStorageTests
{
    [Fact]
    public async Task InMemoryWriteAndReadRoundTripPreservesBytesAndDescriptor()
    {
        var storage = CreateInMemoryStorage(maxArtifactBytes: 1024 * 1024);
        var payload = Encoding.UTF8.GetBytes("{\"trace\":\"room-heating\"}");

        var writeResult = await storage.WriteAsync(new EngineeringArtifactWriteRequest(
            ArtifactKind: "CalculationTrace",
            Scope: "RoomHeating",
            SubjectType: "Room",
            SubjectId: "42",
            ContentType: "application/json",
            Content: payload,
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["scenarioId"] = "scenario-001"
            }));

        Assert.True(writeResult.IsSuccess, writeResult.Error);

        var readResult = await storage.ReadAsync(writeResult.Value.ArtifactId);
        Assert.True(readResult.IsSuccess, readResult.Error);
        Assert.Equal(payload, readResult.Value.Content);
        Assert.Equal("CalculationTrace", readResult.Value.Descriptor.ArtifactKind);
        Assert.Equal("application/json", readResult.Value.Descriptor.ContentType);
        Assert.Equal(payload.LongLength, readResult.Value.Descriptor.SizeBytes);
    }

    [Fact]
    public async Task InMemoryGetDescriptorAndDeleteBehaveAsExpected()
    {
        var storage = CreateInMemoryStorage(maxArtifactBytes: 1024 * 1024);
        var payload = Encoding.UTF8.GetBytes("diagnostics");

        var write = await storage.WriteAsync(new EngineeringArtifactWriteRequest(
            ArtifactKind: "DiagnosticPayload",
            Scope: "Workflow",
            SubjectType: "Scenario",
            SubjectId: "scenario-11",
            ContentType: "text/plain",
            Content: payload,
            Metadata: null));

        Assert.True(write.IsSuccess, write.Error);

        var descriptor = await storage.GetDescriptorAsync(write.Value.ArtifactId);
        Assert.True(descriptor.IsSuccess, descriptor.Error);
        Assert.Equal(write.Value.ArtifactId, descriptor.Value.ArtifactId);

        var deleted = await storage.DeleteAsync(write.Value.ArtifactId);
        Assert.True(deleted.IsSuccess, deleted.Error);

        var missingRead = await storage.ReadAsync(write.Value.ArtifactId);
        Assert.True(missingRead.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, missingRead.ErrorType);
    }

    [Fact]
    public async Task InMemoryWriteRejectsOversizedPayload()
    {
        var storage = CreateInMemoryStorage(maxArtifactBytes: 16);
        var payload = new byte[64];

        var result = await storage.WriteAsync(new EngineeringArtifactWriteRequest(
            ArtifactKind: "EngineeringReport",
            Scope: "Building",
            SubjectType: "Building",
            SubjectId: "10",
            ContentType: "application/json",
            Content: payload,
            Metadata: null));

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task InMemoryDescriptorSha256MatchesPersistedContent()
    {
        var storage = CreateInMemoryStorage(maxArtifactBytes: 1024 * 1024);
        var payload = Encoding.UTF8.GetBytes("checksum-test");

        var write = await storage.WriteAsync(new EngineeringArtifactWriteRequest(
            ArtifactKind: "ValidationComparison",
            Scope: "Tier1Manual",
            SubjectType: "Fixture",
            SubjectId: "MAN-ENG-HEAT-001",
            ContentType: "text/plain",
            Content: payload,
            Metadata: null));

        Assert.True(write.IsSuccess, write.Error);

        var expectedSha = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
        Assert.Equal(expectedSha, write.Value.Sha256);
    }

    [Fact]
    public async Task InMemoryReturnsDefensiveCopiesForByteArrays()
    {
        var storage = CreateInMemoryStorage(maxArtifactBytes: 1024 * 1024);
        var payload = Encoding.UTF8.GetBytes("defensive-copy");

        var write = await storage.WriteAsync(new EngineeringArtifactWriteRequest(
            ArtifactKind: "WorkflowScenarioResult",
            Scope: "ScenarioRun",
            SubjectType: "Scenario",
            SubjectId: "scenario-copy",
            ContentType: "application/octet-stream",
            Content: payload,
            Metadata: null));

        Assert.True(write.IsSuccess, write.Error);

        var firstRead = await storage.ReadAsync(write.Value.ArtifactId);
        var secondRead = await storage.ReadAsync(write.Value.ArtifactId);
        Assert.True(firstRead.IsSuccess && secondRead.IsSuccess);

        firstRead.Value.Content[0] = (byte)'X';
        Assert.NotEqual(firstRead.Value.Content[0], secondRead.Value.Content[0]);
    }

    [Fact]
    public async Task FileSystemWriteReadAndDeleteRoundTrip()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"assistant-engineer-artifacts-{Guid.NewGuid():N}");
        try
        {
            var storage = CreateFileSystemStorage(tempRoot, enableSha256Verification: true, maxArtifactBytes: 1024 * 1024);
            var payload = Encoding.UTF8.GetBytes("{\"report\":\"ok\"}");

            var write = await storage.WriteAsync(new EngineeringArtifactWriteRequest(
                ArtifactKind: "EngineeringReport",
                Scope: "BuildingReport",
                SubjectType: "Building",
                SubjectId: "20",
                ContentType: "application/json",
                Content: payload,
                Metadata: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["format"] = "json"
                }));

            Assert.True(write.IsSuccess, write.Error);

            var artifactDir = Path.Combine(tempRoot, write.Value.ArtifactId);
            Assert.True(File.Exists(Path.Combine(artifactDir, "content.bin")));
            Assert.True(File.Exists(Path.Combine(artifactDir, "descriptor.json")));

            var read = await storage.ReadAsync(write.Value.ArtifactId);
            Assert.True(read.IsSuccess, read.Error);
            Assert.Equal(payload, read.Value.Content);

            var deleted = await storage.DeleteAsync(write.Value.ArtifactId);
            Assert.True(deleted.IsSuccess, deleted.Error);
            Assert.False(Directory.Exists(artifactDir));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task FileSystemReadMissingArtifactReturnsNotFound()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"assistant-engineer-artifacts-missing-{Guid.NewGuid():N}");
        try
        {
            var storage = CreateFileSystemStorage(tempRoot, enableSha256Verification: true, maxArtifactBytes: 1024 * 1024);

            var read = await storage.ReadAsync("engart-missing");
            Assert.True(read.IsFailure);
            Assert.Equal(ResultErrorType.NotFound, read.ErrorType);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task FileSystemRejectsInvalidArtifactIdTraversalPattern()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"assistant-engineer-artifacts-invalid-{Guid.NewGuid():N}");
        try
        {
            var storage = CreateFileSystemStorage(tempRoot, enableSha256Verification: true, maxArtifactBytes: 1024 * 1024);

            var result = await storage.GetDescriptorAsync("../outside");
            Assert.True(result.IsFailure);
            Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task InMemoryStorage_LogsArtifactWriteStartAndCompletionEventCodes()
    {
        var logger = new CapturingLogger<InMemoryEngineeringArtifactStorage>();
        var storage = CreateInMemoryStorage(maxArtifactBytes: 1024 * 1024, logger);
        var payload = Encoding.UTF8.GetBytes("trace-content-should-not-be-logged");

        var result = await storage.WriteAsync(new EngineeringArtifactWriteRequest(
            ArtifactKind: "CalculationTrace",
            Scope: "RoomHeating",
            SubjectType: "Room",
            SubjectId: "42",
            ContentType: "application/json",
            Content: payload,
            Metadata: null));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains(logger.Entries, entry => entry.EventCode == ObservabilityEventCodes.ArtifactWriteStarted);
        Assert.Contains(logger.Entries, entry => entry.EventCode == ObservabilityEventCodes.ArtifactWriteCompleted);
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains("trace-content-should-not-be-logged", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InMemoryStorage_LogsSizeLimitExceededEventCode()
    {
        var logger = new CapturingLogger<InMemoryEngineeringArtifactStorage>();
        var storage = CreateInMemoryStorage(maxArtifactBytes: 8, logger);
        var payload = Encoding.UTF8.GetBytes("payload-exceeds-limit");

        var result = await storage.WriteAsync(new EngineeringArtifactWriteRequest(
            ArtifactKind: "EngineeringReport",
            Scope: "Building",
            SubjectType: "Building",
            SubjectId: "10",
            ContentType: "application/json",
            Content: payload,
            Metadata: null));

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Contains(logger.Entries, entry => entry.EventCode == ObservabilityEventCodes.ArtifactSizeLimitExceeded);
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains("payload-exceeds-limit", StringComparison.Ordinal));
    }

    private static IEngineeringArtifactStorage CreateInMemoryStorage(
        long maxArtifactBytes,
        ILogger<InMemoryEngineeringArtifactStorage>? logger = null)
    {
        var options = Options.Create(new EngineeringArtifactStorageOptions
        {
            Provider = EngineeringArtifactStorageProviders.InMemory,
            MaxArtifactBytes = maxArtifactBytes,
            EnableSha256Verification = true
        });

        return new InMemoryEngineeringArtifactStorage(options, logger);
    }

    private static IEngineeringArtifactStorage CreateFileSystemStorage(
        string rootPath,
        bool enableSha256Verification,
        long maxArtifactBytes)
    {
        var options = Options.Create(new EngineeringArtifactStorageOptions
        {
            Provider = EngineeringArtifactStorageProviders.FileSystem,
            RootPath = rootPath,
            MaxArtifactBytes = maxArtifactBytes,
            EnableSha256Verification = enableSha256Verification
        });

        return new FileSystemEngineeringArtifactStorage(options);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            var eventCode = string.Empty;

            if (state is IEnumerable<KeyValuePair<string, object?>> properties)
            {
                foreach (var property in properties)
                {
                    if (string.Equals(property.Key, "EventCode", StringComparison.Ordinal))
                    {
                        eventCode = property.Value?.ToString() ?? string.Empty;
                        break;
                    }
                }
            }

            Entries.Add(new LogEntry(logLevel, eventCode, message));
        }
    }

    private sealed record LogEntry(
        LogLevel LogLevel,
        string EventCode,
        string Message);
}
