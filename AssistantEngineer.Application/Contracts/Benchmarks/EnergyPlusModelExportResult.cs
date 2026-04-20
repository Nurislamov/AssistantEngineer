namespace AssistantEngineer.Application.Contracts.Benchmarks;

public sealed class EnergyPlusModelExportResult
{
    public int BuildingId { get; init; }
    public string BuildingName { get; init; } = string.Empty;
    public string ModelPath { get; init; } = string.Empty;
}
