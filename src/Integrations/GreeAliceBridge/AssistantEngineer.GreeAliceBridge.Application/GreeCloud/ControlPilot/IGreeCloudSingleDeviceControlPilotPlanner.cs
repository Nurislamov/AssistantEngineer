using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.ControlPilot;

namespace AssistantEngineer.GreeAliceBridge.Application.GreeCloud.ControlPilot;

public interface IGreeCloudSingleDeviceControlPilotPlanner
{
    GreeCloudSingleDeviceControlPilotDecision Plan();
}
