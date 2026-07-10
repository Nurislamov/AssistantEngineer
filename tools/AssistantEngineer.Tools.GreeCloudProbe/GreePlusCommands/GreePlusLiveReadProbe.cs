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
            GreePlusLiveReadContractReport report = GreePlusLiveReadContractInspector.InspectKnownEvidence();
            string[] gaps = report.Gaps
                .Select(static gap => gap.Area + ": " + gap.MissingEvidence)
                .ToArray();

            return GreePlusLiveReadResult.NotReady(gaps);
        }

        if (string.IsNullOrWhiteSpace(options.ReadOnlyStatusJson))
        {
            return GreePlusLiveReadResult.MissingStatusPayload(["read-only status response payload"]);
        }

        GreePlusDeviceStatusSnapshot snapshot = GreePlusDeviceStatusParser.Parse(options.ReadOnlyStatusJson);

        return GreePlusLiveReadResult.Parsed(snapshot);
    }
}
