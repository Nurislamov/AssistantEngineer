namespace AssistantEngineer.Tools.GreeCloudProbe.Models;

internal static class GreeCloudDeviceClassifications
{
    public const string CloudAcCandidate = "cloud-ac-candidate";
    public const string SplitCandidate = "split-candidate";
    public const string VrfGatewayCandidate = "vrf-gateway-candidate";
    public const string VrfChildUnitCandidate = "vrf-child-unit-candidate";
    public const string Unknown = "unknown";

    public static bool IsKnown(string? value)
    {
        return value is
            CloudAcCandidate or
            SplitCandidate or
            VrfGatewayCandidate or
            VrfChildUnitCandidate or
            Unknown;
    }
}
