using AssistantEngineer.GreeAliceBridge.Contracts.Safety;

namespace AssistantEngineer.GreeAliceBridge.Application.Safety;

public sealed class OfflineGreeAliceBridgeSafetyDecisionService : IGreeAliceBridgeSafetyDecisionService
{
    private readonly GreeAliceBridgeSafetyPolicy policy;
    private readonly GreeAliceBridgeKillSwitchState killSwitches;

    public OfflineGreeAliceBridgeSafetyDecisionService()
        : this(GreeAliceBridgeSafetyPolicy.OfflineDefault, GreeAliceBridgeKillSwitchState.OfflineDefault)
    {
    }

    public OfflineGreeAliceBridgeSafetyDecisionService(
        GreeAliceBridgeSafetyPolicy policy,
        GreeAliceBridgeKillSwitchState killSwitches)
    {
        this.policy = policy;
        this.killSwitches = killSwitches;
    }

    public GreeAliceBridgeSafetyDecision Decide(GreeAliceBridgeSafetyContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (killSwitches.GlobalBridgeDisabled)
        {
            return Block(context.Action, GreeAliceBridgeSafetyDecisionReason.KillSwitchBlocked);
        }

        return context.Action switch
        {
            GreeAliceBridgeSafetyAction.Health => Allow(context.Action),
            GreeAliceBridgeSafetyAction.DiscoverDevices when policy.AllowOfflineDeviceDiscovery && !killSwitches.DiscoveryDisabled => Allow(context.Action),
            GreeAliceBridgeSafetyAction.QueryDevices when policy.AllowReadOnlyFixtureQueries && !killSwitches.QueryDisabled => Allow(context.Action),
            GreeAliceBridgeSafetyAction.Unlink when policy.AllowOfflineUnlink && !killSwitches.UnlinkDisabled => Allow(context.Action),
            GreeAliceBridgeSafetyAction.ReadAdapter when !killSwitches.ReadAdapterDisabled => Allow(context.Action),
            GreeAliceBridgeSafetyAction.ExecuteAction when policy.AllowActionDryRun && !killSwitches.ActionDisabled => DryRun(context.Action),
            GreeAliceBridgeSafetyAction.ControlAdapter => Block(context.Action, GreeAliceBridgeSafetyDecisionReason.DryRunFailClosed),
            GreeAliceBridgeSafetyAction.LiveHttpNetwork => Block(context.Action, GreeAliceBridgeSafetyDecisionReason.LiveOperationBlocked),
            GreeAliceBridgeSafetyAction.MqttConnect => Block(context.Action, GreeAliceBridgeSafetyDecisionReason.LiveOperationBlocked),
            GreeAliceBridgeSafetyAction.MqttSubscribe => Block(context.Action, GreeAliceBridgeSafetyDecisionReason.LiveOperationBlocked),
            GreeAliceBridgeSafetyAction.MqttPublish => Block(context.Action, GreeAliceBridgeSafetyDecisionReason.LiveOperationBlocked),
            GreeAliceBridgeSafetyAction.DeviceControl => Block(context.Action, GreeAliceBridgeSafetyDecisionReason.LiveOperationBlocked),
            GreeAliceBridgeSafetyAction.ProductionRuntimeWiring => Block(context.Action, GreeAliceBridgeSafetyDecisionReason.ProductionWiringBlocked),
            _ => Block(context.Action, GreeAliceBridgeSafetyDecisionReason.KillSwitchBlocked)
        };
    }

    private GreeAliceBridgeSafetyDecision Allow(string action)
    {
        return new GreeAliceBridgeSafetyDecision(
            action,
            IsAllowed: true,
            IsDryRunOnly: false,
            IsFailClosed: false,
            GreeAliceBridgeSafetyDecisionReason.OfflineAllowed,
            policy.RuntimeMode);
    }

    private GreeAliceBridgeSafetyDecision DryRun(string action)
    {
        return new GreeAliceBridgeSafetyDecision(
            action,
            IsAllowed: true,
            IsDryRunOnly: true,
            IsFailClosed: true,
            GreeAliceBridgeSafetyDecisionReason.DryRunFailClosed,
            policy.RuntimeMode);
    }

    private GreeAliceBridgeSafetyDecision Block(string action, string reason)
    {
        return new GreeAliceBridgeSafetyDecision(
            action,
            IsAllowed: false,
            IsDryRunOnly: true,
            IsFailClosed: true,
            reason,
            policy.RuntimeMode);
    }
}
