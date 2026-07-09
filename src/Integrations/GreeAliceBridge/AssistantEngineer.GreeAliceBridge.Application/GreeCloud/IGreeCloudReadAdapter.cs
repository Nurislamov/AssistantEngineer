using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud;

namespace AssistantEngineer.GreeAliceBridge.Application.GreeCloud;

public interface IGreeCloudReadAdapter
{
    Task<IReadOnlyList<GreeCloudDeviceDescriptor>> DiscoverDevicesAsync(CancellationToken cancellationToken = default);

    Task<GreeCloudDeviceStateSnapshot> GetDeviceStateAsync(string deviceId, CancellationToken cancellationToken = default);
}
