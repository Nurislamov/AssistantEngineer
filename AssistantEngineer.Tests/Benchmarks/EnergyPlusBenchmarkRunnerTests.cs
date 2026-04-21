using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.Infrastructure.Services.Benchmarks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests;

public class EnergyPlusBenchmarkRunnerTests
{
    [Fact]
    public async Task RunAsyncReturnsValidationWhenRequiredPathsAreMissing()
    {
        var runner = CreateRunner(new EnergyPlusBenchmarkOptions());

        var result = await runner.RunAsync(new EnergyPlusBenchmarkRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Contains("model path", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsyncReturnsNotFoundWhenModelFileDoesNotExist()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var weatherPath = Path.Combine(tempDirectory, "weather.epw");
            await File.WriteAllTextAsync(weatherPath, "weather");
            var runner = CreateRunner(new EnergyPlusBenchmarkOptions());

            var result = await runner.RunAsync(new EnergyPlusBenchmarkRequest
            {
                ModelPath = Path.Combine(tempDirectory, "missing.idf"),
                WeatherFilePath = weatherPath,
                OutputDirectory = Path.Combine(tempDirectory, "output")
            });

            Assert.True(result.IsFailure);
            Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
            Assert.Contains("model file", result.Error, StringComparison.OrdinalIgnoreCase);
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
            var modelPath = Path.Combine(tempDirectory, "model.idf");
            var weatherPath = Path.Combine(tempDirectory, "weather.epw");
            await File.WriteAllTextAsync(modelPath, "model");
            await File.WriteAllTextAsync(weatherPath, "weather");
            var runner = CreateRunner(new EnergyPlusBenchmarkOptions
            {
                UseDocker = false,
                ExecutablePath = ""
            });

            var result = await runner.RunAsync(new EnergyPlusBenchmarkRequest
            {
                ModelPath = modelPath,
                WeatherFilePath = weatherPath,
                OutputDirectory = Path.Combine(tempDirectory, "output")
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
            var modelPath = Path.Combine(tempDirectory, "model.idf");
            var weatherPath = Path.Combine(tempDirectory, "weather.epw");
            await File.WriteAllTextAsync(modelPath, "model");
            await File.WriteAllTextAsync(weatherPath, "weather");
            var runner = CreateRunner(new EnergyPlusBenchmarkOptions
            {
                MaxCapturedLogCharacters = 0
            });

            var result = await runner.RunAsync(new EnergyPlusBenchmarkRequest
            {
                ModelPath = modelPath,
                WeatherFilePath = weatherPath,
                OutputDirectory = Path.Combine(tempDirectory, "output")
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

    private static EnergyPlusBenchmarkRunner CreateRunner(EnergyPlusBenchmarkOptions options) =>
        new(Options.Create(options), NullLogger<EnergyPlusBenchmarkRunner>.Instance);
}
