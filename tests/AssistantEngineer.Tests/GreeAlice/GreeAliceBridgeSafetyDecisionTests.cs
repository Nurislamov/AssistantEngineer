using AssistantEngineer.GreeAliceBridge.Application.GreeCloud;
using AssistantEngineer.GreeAliceBridge.Application.Safety;
using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud;
using AssistantEngineer.GreeAliceBridge.Contracts.Safety;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceBridgeSafetyDecisionTests
{
    [Fact]
    public void DefaultSafetyPolicyAllowsOnlyOfflineOperations()
    {
        GreeAliceBridgeSafetyPolicy policy = GreeAliceBridgeSafetyPolicy.OfflineDefault;

        Assert.Equal("offline-fixture", policy.RuntimeMode);
        Assert.True(policy.AllowReadOnlyFixtureQueries);
        Assert.True(policy.AllowOfflineDeviceDiscovery);
        Assert.True(policy.AllowOfflineUnlink);
        Assert.True(policy.AllowActionDryRun);
        Assert.False(policy.AllowLiveGreeCloud);
        Assert.False(policy.AllowLiveHttpNetwork);
        Assert.False(policy.AllowMqttConnect);
        Assert.False(policy.AllowMqttSubscribe);
        Assert.False(policy.AllowMqttPublish);
        Assert.False(policy.AllowDeviceControl);
        Assert.False(policy.AllowRuntimeControl);
        Assert.False(policy.AllowProductionRuntimeWiring);
    }

    [Fact]
    public void DefaultKillSwitchesKeepDangerousControlDisabled()
    {
        GreeAliceBridgeKillSwitchState killSwitches = GreeAliceBridgeKillSwitchState.OfflineDefault;

        Assert.False(killSwitches.GlobalBridgeDisabled);
        Assert.False(killSwitches.DiscoveryDisabled);
        Assert.False(killSwitches.QueryDisabled);
        Assert.False(killSwitches.ActionDisabled);
        Assert.False(killSwitches.UnlinkDisabled);
        Assert.False(killSwitches.ReadAdapterDisabled);
        Assert.True(killSwitches.ControlAdapterDisabled);
    }

    [Theory]
    [InlineData(GreeAliceBridgeSafetyAction.Health)]
    [InlineData(GreeAliceBridgeSafetyAction.DiscoverDevices)]
    [InlineData(GreeAliceBridgeSafetyAction.QueryDevices)]
    [InlineData(GreeAliceBridgeSafetyAction.Unlink)]
    [InlineData(GreeAliceBridgeSafetyAction.ReadAdapter)]
    public void SafetyDecisionAllowsOfflineReadOnlyActions(string action)
    {
        GreeAliceBridgeSafetyDecision decision = Decide(action);

        Assert.True(decision.IsAllowed);
        Assert.False(decision.IsDryRunOnly);
        Assert.False(decision.IsFailClosed);
        Assert.Equal(GreeAliceBridgeSafetyDecisionReason.OfflineAllowed, decision.Reason);
        Assert.Equal("offline-fixture", decision.RuntimeMode);
    }

    [Fact]
    public void SafetyDecisionAllowsExecuteActionOnlyAsDryRunFailClosed()
    {
        GreeAliceBridgeSafetyDecision decision = Decide(GreeAliceBridgeSafetyAction.ExecuteAction);

        Assert.True(decision.IsAllowed);
        Assert.True(decision.IsDryRunOnly);
        Assert.True(decision.IsFailClosed);
        Assert.Equal(GreeAliceBridgeSafetyDecisionReason.DryRunFailClosed, decision.Reason);
    }

    [Theory]
    [InlineData(GreeAliceBridgeSafetyAction.ControlAdapter)]
    [InlineData(GreeAliceBridgeSafetyAction.LiveHttpNetwork)]
    [InlineData(GreeAliceBridgeSafetyAction.MqttConnect)]
    [InlineData(GreeAliceBridgeSafetyAction.MqttSubscribe)]
    [InlineData(GreeAliceBridgeSafetyAction.MqttPublish)]
    [InlineData(GreeAliceBridgeSafetyAction.DeviceControl)]
    [InlineData(GreeAliceBridgeSafetyAction.ProductionRuntimeWiring)]
    public void SafetyDecisionBlocksDangerousOperations(string action)
    {
        GreeAliceBridgeSafetyDecision decision = Decide(action);

        Assert.False(decision.IsAllowed);
        Assert.True(decision.IsDryRunOnly);
        Assert.True(decision.IsFailClosed);
        Assert.Equal("offline-fixture", decision.RuntimeMode);
    }

    [Fact]
    public void SafetyMiddlewareBoundaryAppliesOnlyToIsolatedBridgeAndBlocksLiveControl()
    {
        Assert.Equal("offline-safety-filter", GreeAliceBridgeSafetyMiddlewareBoundary.BoundaryMode);
        Assert.True(GreeAliceBridgeSafetyMiddlewareBoundary.AppliesToIsolatedBridgeApiOnly);
        Assert.False(GreeAliceBridgeSafetyMiddlewareBoundary.AllowsProductionRuntimeWiring);
        Assert.False(GreeAliceBridgeSafetyMiddlewareBoundary.AllowsLiveControl);
    }

    [Fact]
    public async Task OfflineControlAdapterMatchesSafetyDecisionAndFailsClosed()
    {
        IGreeCloudControlAdapter adapter = new OfflineGreeCloudControlAdapter(new OfflineGreeAliceBridgeSafetyDecisionService());

        GreeCloudControlResult result = await adapter.ExecuteControlAsync(new GreeCloudControlRequest(
            "dummy-gree-ac-001",
            "devices.capabilities.on_off",
            "set",
            "true"));

        Assert.Equal("dry-run-fail-closed", result.Status);
        Assert.False(result.SentToGreeCloud);
        Assert.False(result.SentToMqtt);
        Assert.False(result.SentToDevice);
        Assert.Equal("offline-fake", result.AdapterMode);
    }

    private static GreeAliceBridgeSafetyDecision Decide(string action)
    {
        return new OfflineGreeAliceBridgeSafetyDecisionService().Decide(new GreeAliceBridgeSafetyContext(
            action,
            "offline-fixture",
            "test"));
    }
}
