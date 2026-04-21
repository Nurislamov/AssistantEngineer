namespace AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;

public class VerificationRequest
{
    public string WeatherFilePath { get; set; } = string.Empty;
    public IReadOnlyList<string> AdditionalArguments { get; set; } = [];
}