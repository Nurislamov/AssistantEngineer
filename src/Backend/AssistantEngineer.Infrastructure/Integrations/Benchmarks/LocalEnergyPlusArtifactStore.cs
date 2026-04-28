using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Benchmarks.Application.Models;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Infrastructure.Integrations.Benchmarks;

public sealed class LocalEnergyPlusArtifactStore : IEnergyPlusArtifactStore
{
    private readonly EnergyPlusBenchmarkOptions _options;

    public LocalEnergyPlusArtifactStore(IOptions<EnergyPlusBenchmarkOptions> options)
    {
        _options = options.Value;
    }

    public Result<EnergyPlusArtifactFile> CreateModelArtifact(
        int buildingId,
        string? runName = null)
    {
        var artifactsDirectory = GetArtifactsDirectory();
        Directory.CreateDirectory(artifactsDirectory);

        var artifactId = $"{CreatePrefix(runName, "model")}-{buildingId}-{Guid.NewGuid():N}.idf";
        var fileSystemPath = Path.Combine(artifactsDirectory, artifactId);
        return Result<EnergyPlusArtifactFile>.Success(new EnergyPlusArtifactFile(artifactId, fileSystemPath));
    }

    public Result<EnergyPlusArtifactFile> GetModelArtifact(string artifactId) =>
        GetFileArtifact(artifactId, ".idf", "EnergyPlus model artifact");

    public Result<EnergyPlusArtifactFile> GetWeatherArtifact(string artifactId) =>
        GetFileArtifact(artifactId, ".epw", "EnergyPlus weather artifact");

    public Result<EnergyPlusRunWorkspace> CreateRunWorkspace(string? runName = null)
    {
        var runsDirectory = GetRunsDirectory();
        Directory.CreateDirectory(runsDirectory);

        var runArtifactId = $"{CreatePrefix(runName, "run")}-{Guid.NewGuid():N}";
        var workingDirectory = Path.Combine(runsDirectory, runArtifactId);
        var outputDirectory = Path.Combine(workingDirectory, "output");
        Directory.CreateDirectory(outputDirectory);

        return Result<EnergyPlusRunWorkspace>.Success(new EnergyPlusRunWorkspace(
            runArtifactId,
            workingDirectory,
            outputDirectory));
    }

    public Result<EnergyPlusRunWorkspace> GetRunWorkspace(string runArtifactId)
    {
        var validation = ValidateArtifactId(runArtifactId, "EnergyPlus run artifact id");
        if (validation.IsFailure)
            return Result<EnergyPlusRunWorkspace>.Failure(validation);

        var runsDirectory = GetRunsDirectory();
        var workingDirectory = Path.GetFullPath(Path.Combine(runsDirectory, runArtifactId));
        if (!IsInsideDirectory(workingDirectory, runsDirectory) || !Directory.Exists(workingDirectory))
            return Result<EnergyPlusRunWorkspace>.NotFound($"EnergyPlus run artifact '{runArtifactId}' was not found.");

        return Result<EnergyPlusRunWorkspace>.Success(new EnergyPlusRunWorkspace(
            runArtifactId,
            workingDirectory,
            Path.Combine(workingDirectory, "output")));
    }

    public void DeleteRunWorkspace(string runArtifactId)
    {
        var workspace = GetRunWorkspace(runArtifactId);
        if (workspace.IsFailure)
            return;

        Directory.Delete(workspace.Value.WorkingDirectory, recursive: true);
    }

    private Result<EnergyPlusArtifactFile> GetFileArtifact(
        string artifactId,
        string expectedExtension,
        string description)
    {
        var validation = ValidateArtifactId(artifactId, $"{description} id");
        if (validation.IsFailure)
            return Result<EnergyPlusArtifactFile>.Failure(validation);

        if (!Path.GetExtension(artifactId).Equals(expectedExtension, StringComparison.OrdinalIgnoreCase))
            return Result<EnergyPlusArtifactFile>.Validation($"{description} id must reference a {expectedExtension} artifact.");

        var artifactsDirectory = GetArtifactsDirectory();
        var fileSystemPath = Path.GetFullPath(Path.Combine(artifactsDirectory, artifactId));
        if (!IsInsideDirectory(fileSystemPath, artifactsDirectory) || !File.Exists(fileSystemPath))
            return Result<EnergyPlusArtifactFile>.NotFound($"{description} '{artifactId}' was not found.");

        return Result<EnergyPlusArtifactFile>.Success(new EnergyPlusArtifactFile(artifactId, fileSystemPath));
    }

    private string GetArtifactsDirectory() =>
        Path.Combine(GetRootDirectory(), "artifacts");

    private string GetRunsDirectory() =>
        Path.Combine(GetRootDirectory(), "runs");

    private string GetRootDirectory() =>
        Path.GetFullPath(string.IsNullOrWhiteSpace(_options.ArtifactRootDirectory)
            ? Path.Combine(Path.GetTempPath(), "assistant-engineer-energyplus")
            : _options.ArtifactRootDirectory);

    private static Result ValidateArtifactId(string artifactId, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(artifactId))
            return Result.Validation($"{fieldName} is required.");

        if (artifactId.Length > 160)
            return Result.Validation($"{fieldName} is too long.");

        foreach (var c in artifactId)
        {
            if (char.IsLetterOrDigit(c) || c is '-' or '_' or '.')
                continue;

            return Result.Validation($"{fieldName} contains unsupported characters.");
        }

        if (artifactId.Contains("..", StringComparison.Ordinal) ||
            artifactId.Contains(Path.DirectorySeparatorChar) ||
            artifactId.Contains(Path.AltDirectorySeparatorChar))
        {
            return Result.Validation($"{fieldName} is invalid.");
        }

        return Result.Success();
    }

    private static bool IsInsideDirectory(string candidate, string directory)
    {
        var normalizedCandidate = Path.GetFullPath(candidate);
        var normalizedDirectory = Path.GetFullPath(directory);
        if (!normalizedDirectory.EndsWith(Path.DirectorySeparatorChar))
            normalizedDirectory += Path.DirectorySeparatorChar;

        return normalizedCandidate.StartsWith(normalizedDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static string CreatePrefix(string? runName, string fallback)
    {
        if (string.IsNullOrWhiteSpace(runName))
            return fallback;

        var chars = runName
            .Trim()
            .Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-')
            .ToArray();
        var sanitized = new string(chars).Trim('-');
        return string.IsNullOrWhiteSpace(sanitized)
            ? fallback
            : sanitized[..Math.Min(sanitized.Length, 40)];
    }
}
