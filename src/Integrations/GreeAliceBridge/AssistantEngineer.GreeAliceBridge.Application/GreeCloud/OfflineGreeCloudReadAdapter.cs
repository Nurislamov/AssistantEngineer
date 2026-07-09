using AssistantEngineer.GreeAliceBridge.Application.Registry;
using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud;
using AssistantEngineer.GreeAliceBridge.Contracts.Registry;

namespace AssistantEngineer.GreeAliceBridge.Application.GreeCloud;

public sealed class OfflineGreeCloudReadAdapter : IGreeCloudReadAdapter
{
    private readonly IGreeAliceOfflineRegistryProvider registryProvider;

    public OfflineGreeCloudReadAdapter(IGreeAliceOfflineRegistryProvider registryProvider)
    {
        this.registryProvider = registryProvider;
    }

    public Task<IReadOnlyList<GreeCloudDeviceDescriptor>> DiscoverDevicesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<GreeCloudDeviceDescriptor> devices = registryProvider
            .GetSnapshot()
            .Devices
            .Select(MapDevice)
            .ToArray();

        return Task.FromResult(devices);
    }

    public Task<GreeCloudDeviceStateSnapshot> GetDeviceStateAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GreeAliceRegisteredDevice? device = registryProvider
            .GetSnapshot()
            .Devices
            .FirstOrDefault(candidate => string.Equals(candidate.Id, deviceId, StringComparison.Ordinal));

        if (device is null)
        {
            return Task.FromResult(new GreeCloudDeviceStateSnapshot(
                deviceId,
                "offline-unknown",
                Online: false,
                On: null,
                "unknown",
                TargetTemperatureC: null,
                "unknown",
                "offline-fake",
                GreeCloudAdapterSafetyBoundary.AdapterMode));
        }

        return Task.FromResult(new GreeCloudDeviceStateSnapshot(
            device.Id,
            "offline-fixture",
            Online: true,
            On: device.Capabilities.OnOff ? true : null,
            device.Capabilities.Mode ? "cool" : "not-supported",
            device.Capabilities.Temperature ? 24 : null,
            device.Capabilities.FanSpeed ? "auto" : "not-supported",
            "offline-fake",
            GreeCloudAdapterSafetyBoundary.AdapterMode));
    }

    private static GreeCloudDeviceDescriptor MapDevice(GreeAliceRegisteredDevice device)
    {
        return new GreeCloudDeviceDescriptor(
            device.Id,
            device.DisplayName,
            device.Kind,
            device.RoomRef,
            device.ParentGatewayRef,
            device.YandexExposed,
            device.Capabilities,
            "offline-fake",
            GreeCloudAdapterSafetyBoundary.AdapterMode);
    }
}
