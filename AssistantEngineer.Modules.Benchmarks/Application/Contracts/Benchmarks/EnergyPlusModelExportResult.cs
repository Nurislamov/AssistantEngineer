namespace AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;

public sealed class EnergyPlusModelExportResult
{
    public int BuildingId { get; init; }
    public string BuildingName { get; init; } = string.Empty;
    public string ModelArtifactId { get; init; } = string.Empty;
}
