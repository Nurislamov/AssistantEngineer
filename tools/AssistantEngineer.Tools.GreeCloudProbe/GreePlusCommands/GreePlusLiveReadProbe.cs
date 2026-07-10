namespace AssistantEngineer.Tools.GreeCloudProbe.GreePlusCommands;

public sealed class GreePlusLiveReadProbe
{
    public GreePlusLiveReadResult Run(GreePlusLiveReadOptions options)
    {
        GreePlusLiveReadSafetyGateResult gate = GreePlusLiveReadSafetyGate.Evaluate(options);
        if (!gate.IsAllowed)
        {
            return GreePlusLiveReadResult.Blocked(gate.MissingRequirements);
        }

        if (!options.ExactReadContractKnown)
        {
            return GreePlusLiveReadResult.NotReady(["exact authenticated read-only status endpoint/request contract"]);
        }

        if (string.IsNullOrWhiteSpace(options.ReadOnlyStatusJson))
        {
            return GreePlusLiveReadResult.NotReady(["read-only status response payload"]);
        }

        GreePlusDeviceStatusSnapshot snapshot = GreePlusDeviceStatusParser.Parse(options.ReadOnlyStatusJson);

        return GreePlusLiveReadResult.Parsed(snapshot);
    }
}
