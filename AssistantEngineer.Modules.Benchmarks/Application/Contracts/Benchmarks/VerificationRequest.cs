namespace AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;

public class VerificationRequest
{
    public string WeatherArtifactId { get; set; } = string.Empty;
    public string? RunName { get; set; }
    public IReadOnlyList<string> AdditionalArguments { get; set; } = [];
}
