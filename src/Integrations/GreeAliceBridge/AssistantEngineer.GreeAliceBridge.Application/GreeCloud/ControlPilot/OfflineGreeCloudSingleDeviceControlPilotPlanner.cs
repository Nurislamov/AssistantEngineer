using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.ControlPilot;

namespace AssistantEngineer.GreeAliceBridge.Application.GreeCloud.ControlPilot;

public sealed class OfflineGreeCloudSingleDeviceControlPilotPlanner : IGreeCloudSingleDeviceControlPilotPlanner
{
    public GreeCloudSingleDeviceControlPilotDecision Plan()
    {
        GreeCloudSingleDeviceControlPilotScope scope = GreeCloudSingleDeviceControlPilotBoundary.DummyScope;
        var plan = new GreeCloudSingleDeviceControlPilotCommandPlan(
            scope,
            GreeCloudSingleDeviceControlPilotBoundary.CandidateCommandPlan);
        var result = new GreeCloudSingleDeviceControlPilotDryRunResult(
            GreeCloudSingleDeviceControlPilotBoundary.PilotStatus,
            GreeCloudSingleDeviceControlPilotBoundary.DryRunOnly,
            GreeCloudSingleDeviceControlPilotBoundary.FailClosed,
            WillSendToGreeCloud: false,
            WillSendToMqtt: false,
            WillSendDeviceCommand: false);

        return new GreeCloudSingleDeviceControlPilotDecision(
            GreeCloudSingleDeviceControlPilotBoundary.PilotStatus,
            scope,
            plan,
            result,
            GreeCloudSingleDeviceControlPilotBoundary.LiveControlEnabled,
            GreeCloudSingleDeviceControlPilotBoundary.MqttAllowed,
            GreeCloudSingleDeviceControlPilotBoundary.ProductionWiringAllowed);
    }
}
