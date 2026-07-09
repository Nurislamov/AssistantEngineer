using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud;

namespace AssistantEngineer.GreeAliceBridge.Application.GreeCloud;

public sealed class OfflineGreeCloudControlAdapter : IGreeCloudControlAdapter
{
    public Task<GreeCloudControlResult> ExecuteControlAsync(
        GreeCloudControlRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);

        return Task.FromResult(new GreeCloudControlResult(
            request.DeviceId,
            request.Capability,
            "dry-run-fail-closed",
            SentToGreeCloud: false,
            SentToMqtt: false,
            SentToDevice: false,
            GreeCloudAdapterSafetyBoundary.AdapterMode));
    }
}
