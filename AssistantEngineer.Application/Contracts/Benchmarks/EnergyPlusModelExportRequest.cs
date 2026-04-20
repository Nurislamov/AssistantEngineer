namespace AssistantEngineer.Application.Contracts.Benchmarks;

public sealed class EnergyPlusModelExportRequest
{
    public string OutputPath { get; init; } = string.Empty;
}
