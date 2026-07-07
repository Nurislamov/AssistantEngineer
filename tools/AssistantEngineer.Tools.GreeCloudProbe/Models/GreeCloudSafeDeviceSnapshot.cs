namespace AssistantEngineer.Tools.GreeCloudProbe.Models;

internal sealed record GreeCloudSafeDeviceSnapshot(
    string? HomeId,
    string? HomeName,
    string? RoomId,
    string? RoomName,
    string? DeviceId,
    string? DeviceName,
    string? DeviceType,
    string? DeviceModel,
    string? ProductModel,
    string? Brand,
    string? Vendor,
    string? Mid,
    string? Hid,
    string? Version,
    bool? Online,
    string Classification,
    bool LocalKeyProvided,
    string? LocalKeyMasked,
    string? MacMasked,
    string? ParentMacMasked,
    string? ChildMacMasked,
    IReadOnlyList<string> RawFieldNames,
    IReadOnlyDictionary<string, string?> SafeRawProperties)
{
    public bool IsCloudControllable =>
        Classification is
            GreeCloudDeviceClassifications.CloudAcCandidate or
            GreeCloudDeviceClassifications.SplitCandidate or
            GreeCloudDeviceClassifications.VrfGatewayCandidate or
            GreeCloudDeviceClassifications.VrfChildUnitCandidate;

    public bool HasVrfRelationshipHints =>
        Classification is
            GreeCloudDeviceClassifications.VrfGatewayCandidate or
            GreeCloudDeviceClassifications.VrfChildUnitCandidate;
}
