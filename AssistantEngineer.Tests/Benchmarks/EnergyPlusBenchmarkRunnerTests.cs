using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.Resilience;
using AssistantEngineer.Infrastructure.Integrations.Benchmarks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests;

public class EnergyPlusBenchmarkRunnerTests
{
    [Fact]
    public async Task RunAsyncReturnsValidationWhenRequiredArtifactIdsAreMissing()
    {
        var runner = CreateRunner(new EnergyPlusBenchmarkOptions());

        var result = await runner.RunAsync(new EnergyPlusBenchmarkRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Contains("model artifact", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsyncReturnsNotFoundWhenModelFileDoesNotExist()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            SeedArtifact(tempDirectory, "weather.epw", "weather");
            var runner = CreateRunner(new EnergyPlusBenchmarkOptions { ArtifactRootDirectory = tempDirectory });

            var result = await runner.RunAsync(new EnergyPlusBenchmarkRequest
            {
                ModelArtifactId = "missing.idf",
                WeatherArtifactId = "weather.epw"
            });

            Assert.True(result.IsFailure);
            Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
            Assert.Contains("model artifact", result.Error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsyncReturnsValidationWhenExecutablePathIsNotConfigured()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var runner = CreateRunner(new EnergyPlusBenchmarkOptions
            {
                UseDocker = false,
                ExecutablePath = "",
                ArtifactRootDirectory = tempDirectory
            });

            var result = await runner.RunAsync(new EnergyPlusBenchmarkRequest
            {
                ModelArtifactId = "model.idf",
                WeatherArtifactId = "weather.epw"
            });

            Assert.True(result.IsFailure);
            Assert.Equal(ResultErrorType.Validation, result.ErrorType);
            Assert.Contains("executable path", result.Error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsyncReturnsValidationWhenLogCaptureLimitIsInvalid()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var runner = CreateRunner(new EnergyPlusBenchmarkOptions
            {
                MaxCapturedLogCharacters = 0,
                ArtifactRootDirectory = tempDirectory
            });

            var result = await runner.RunAsync(new EnergyPlusBenchmarkRequest
            {
                ModelArtifactId = "model.idf",
                WeatherArtifactId = "weather.epw"
            });

            Assert.True(result.IsFailure);
            Assert.Equal(ResultErrorType.Validation, result.ErrorType);
            Assert.Contains("log", result.Error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"assistant-engineer-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static EnergyPlusBenchmarkRunner CreateRunner(EnergyPlusBenchmarkOptions options)
    {
        var artifacts = new LocalEnergyPlusArtifactStore(Options.Create(options));
        return new(
            Options.Create(options),
            NullLogger<EnergyPlusBenchmarkRunner>.Instance,
            artifacts,
            new ResilientOperationExecutor());
    }

    private static void SeedArtifact(string rootDirectory, string artifactId, string content)
    {
        var artifactsDirectory = Path.Combine(rootDirectory, "artifacts");
        Directory.CreateDirectory(artifactsDirectory);
        File.WriteAllText(Path.Combine(artifactsDirectory, artifactId), content);
    }
}
