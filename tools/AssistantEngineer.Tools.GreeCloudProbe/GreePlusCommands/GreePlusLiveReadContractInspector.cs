namespace AssistantEngineer.Tools.GreeCloudProbe.GreePlusCommands;

public static class GreePlusLiveReadContractInspector
{
    public static GreePlusLiveReadContractReport InspectKnownEvidence()
    {
        return new GreePlusLiveReadContractReport(
            GreePlusLiveReadContractStatus.EvidencePartial,
            KnownEvidence:
            [
                "Gree Plus package, app version, plugin number, region hosts, and server settings are observed.",
                "Homes, message, and statistics path candidates are observed.",
                "Status field names for plugin 10001 are observed and parsed offline.",
                "Bridge functions for system info, user info, selected home, status reads, and device data transfer are observed."
            ],
            Gaps:
            [
                new GreePlusLiveReadContractGap("region selection", "region-to-host resolution contract is not confirmed"),
                new GreePlusLiveReadContractGap("session", "authenticated session request and refresh flow are not confirmed"),
                new GreePlusLiveReadContractGap("home discovery", "request shape and required identifiers are not confirmed"),
                new GreePlusLiveReadContractGap("device discovery", "response shape and selected-device binding are not confirmed"),
                new GreePlusLiveReadContractGap("status read", "read-only status endpoint, method, headers, and response envelope are not confirmed")
            ]);
    }
}
