namespace AssistantEngineer.Tools.GreeCloudProbe.GreePlusCommands;

public static class GreePlusLiveReadSafetyGate
{
    public static GreePlusLiveReadSafetyGateResult Evaluate(GreePlusLiveReadOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> missing = [];

        if (!string.Equals(options.LiveReadSwitchValue, "true", StringComparison.OrdinalIgnoreCase))
        {
            missing.Add("GREE_ALICE_ENABLE_LIVE_READ=true");
        }

        if (!options.ApproveReadOnly)
        {
            missing.Add("explicit read-only approval flag");
        }

        if (string.IsNullOrWhiteSpace(options.DeviceAlias))
        {
            missing.Add("allowlisted device alias");
        }

        if (options.ConfigSource is not (GreePlusLiveReadConfigSource.OperatorFile or GreePlusLiveReadConfigSource.Environment))
        {
            missing.Add("local-only or environment-only config source");
        }

        return missing.Count == 0
            ? GreePlusLiveReadSafetyGateResult.Allowed
            : GreePlusLiveReadSafetyGateResult.Blocked(missing);
    }
}
