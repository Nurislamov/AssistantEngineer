namespace AssistantEngineer.Tools.GreeCloudProbe.GreePlusCommands;

public sealed record GreePlusLiveReadContractReport(
    GreePlusLiveReadContractStatus Status,
    IReadOnlyList<string> KnownEvidence,
    IReadOnlyList<GreePlusLiveReadContractGap> Gaps)
{
    public bool IsReadOnlyContractConfirmed => Status == GreePlusLiveReadContractStatus.ConfirmedReadOnly;
}
