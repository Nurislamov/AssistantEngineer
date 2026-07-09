using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.LiveReadOnly.PilotGate;

namespace AssistantEngineer.GreeAliceBridge.Application.GreeCloud.LiveReadOnly;

public interface IGreeCloudLiveReadOnlyPilotGateEvaluator
{
    GreeCloudLiveReadOnlyPilotGateDecision Evaluate(GreeCloudLiveReadOnlyPilotGateEvaluation? evaluation = null);
}
