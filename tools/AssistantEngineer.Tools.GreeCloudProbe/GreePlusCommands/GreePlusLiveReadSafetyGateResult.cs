namespace AssistantEngineer.Tools.GreeCloudProbe.GreePlusCommands;

public sealed record GreePlusLiveReadSafetyGateResult(
    bool IsAllowed,
    IReadOnlyList<string> MissingRequirements)
{
    public static GreePlusLiveReadSafetyGateResult Allowed { get; } = new(true, []);

    public static GreePlusLiveReadSafetyGateResult Blocked(IReadOnlyList<string> missingRequirements)
    {
        return new GreePlusLiveReadSafetyGateResult(false, missingRequirements);
    }
}
