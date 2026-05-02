namespace AssistantEngineer.Modules.Benchmarks.Application.Models;

public sealed record EnergyPlusRunWorkspace(
    string RunArtifactId,
    string WorkingDirectory,
    string OutputDirectory);
