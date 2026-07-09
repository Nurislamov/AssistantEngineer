using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.ControlApproval;

namespace AssistantEngineer.GreeAliceBridge.Application.GreeCloud.ControlApproval;

public interface IGreeCloudControlApprovalEvaluator
{
    GreeCloudControlPilotDecision Evaluate(GreeCloudControlApprovalEvaluation? evaluation = null);
}
