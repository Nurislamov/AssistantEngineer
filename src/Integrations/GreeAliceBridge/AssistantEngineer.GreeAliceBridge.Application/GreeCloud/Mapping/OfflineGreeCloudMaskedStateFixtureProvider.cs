using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.Mapping;

namespace AssistantEngineer.GreeAliceBridge.Application.GreeCloud.Mapping;

public sealed class OfflineGreeCloudMaskedStateFixtureProvider : IGreeCloudMaskedStateFixtureProvider
{
    public GreeCloudMaskedRawStateSnapshot GetSnapshot(string deviceId)
    {
        return deviceId switch
        {
            "dummy-gree-ac-001" => CreateKnownSnapshot(deviceId, currentTemperatureC: "23"),
            "dummy-vrf-child-001" => CreateKnownSnapshot(deviceId, currentTemperatureC: "22"),
            "dummy-vrf-child-living-001" => CreateKnownSnapshot(deviceId, currentTemperatureC: "22"),
            "dummy-vrf-child-bedroom-001" => CreateKnownSnapshot(deviceId, currentTemperatureC: "21"),
            _ => new GreeCloudMaskedRawStateSnapshot(
                deviceId,
                IsKnownDevice: false,
                [],
                "offline-masked-fixture",
                GreeCloudStateMappingSafetyBoundary.MappingMode)
        };
    }

    private static GreeCloudMaskedRawStateSnapshot CreateKnownSnapshot(string deviceId, string currentTemperatureC)
    {
        return new GreeCloudMaskedRawStateSnapshot(
            deviceId,
            IsKnownDevice: true,
            [
                new GreeCloudMaskedRawStateField("Pow", "1", IsMasked: true),
                new GreeCloudMaskedRawStateField("Mod", "cool", IsMasked: true),
                new GreeCloudMaskedRawStateField("SetTem", "24", IsMasked: true),
                new GreeCloudMaskedRawStateField("TemSen", currentTemperatureC, IsMasked: true),
                new GreeCloudMaskedRawStateField("WdSpd", "auto", IsMasked: true),
                new GreeCloudMaskedRawStateField("SwUpDn", "fixed", IsMasked: true),
                new GreeCloudMaskedRawStateField("SwLfRig", "off", IsMasked: true),
                new GreeCloudMaskedRawStateField("Online", "1", IsMasked: true)
            ],
            "offline-masked-fixture",
            GreeCloudStateMappingSafetyBoundary.MappingMode);
    }
}
