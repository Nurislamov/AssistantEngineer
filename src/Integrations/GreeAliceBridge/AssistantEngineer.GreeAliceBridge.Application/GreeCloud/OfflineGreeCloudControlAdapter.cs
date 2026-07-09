using AssistantEngineer.GreeAliceBridge.Application.Safety;
using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud;
using AssistantEngineer.GreeAliceBridge.Contracts.Safety;

namespace AssistantEngineer.GreeAliceBridge.Application.GreeCloud;

public sealed class OfflineGreeCloudControlAdapter : IGreeCloudControlAdapter
{
    private readonly IGreeAliceBridgeSafetyDecisionService safetyDecisionService;

    public OfflineGreeCloudControlAdapter()
        : this(new OfflineGreeAliceBridgeSafetyDecisionService())
    {
    }

    public OfflineGreeCloudControlAdapter(IGreeAliceBridgeSafetyDecisionService safetyDecisionService)
    {
        this.safetyDecisionService = safetyDecisionService;
    }

    public Task<GreeCloudControlResult> ExecuteControlAsync(
        GreeCloudControlRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);

        GreeAliceBridgeSafetyDecision decision = safetyDecisionService.Decide(new GreeAliceBridgeSafetyContext(
            GreeAliceBridgeSafetyAction.ControlAdapter,
            GreeCloudAdapterSafetyBoundary.AdapterMode,
            "offline-control-adapter"));

        return Task.FromResult(new GreeCloudControlResult(
            request.DeviceId,
            request.Capability,
            decision.IsFailClosed ? "dry-run-fail-closed" : "unexpected-control-allowed",
            SentToGreeCloud: false,
            SentToMqtt: false,
            SentToDevice: false,
            GreeCloudAdapterSafetyBoundary.AdapterMode));
    }
}
