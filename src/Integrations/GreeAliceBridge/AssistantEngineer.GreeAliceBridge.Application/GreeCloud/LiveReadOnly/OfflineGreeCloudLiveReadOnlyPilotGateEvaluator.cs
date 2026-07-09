using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.LiveReadOnly.PilotGate;

namespace AssistantEngineer.GreeAliceBridge.Application.GreeCloud.LiveReadOnly;

public sealed class OfflineGreeCloudLiveReadOnlyPilotGateEvaluator : IGreeCloudLiveReadOnlyPilotGateEvaluator
{
    public GreeCloudLiveReadOnlyPilotGateDecision Evaluate(GreeCloudLiveReadOnlyPilotGateEvaluation? evaluation = null)
    {
        IReadOnlySet<string> satisfied = evaluation?.ManuallySatisfiedRequirements ?? new HashSet<string>(StringComparer.Ordinal);

        string[] unmet = GreeCloudLiveReadOnlyPilotGateBoundary.RequiredRequirements
            .Where(requirement => !satisfied.Contains(requirement))
            .ToArray();

        return new GreeCloudLiveReadOnlyPilotGateDecision(
            GreeCloudLiveReadOnlyPilotGateBoundary.PilotGateStatus,
            GreeCloudLiveReadOnlyPilotGateBoundary.PilotGateOpen,
            LiveReadOnlyPilotAllowed: false,
            GreeCloudLiveReadOnlyPilotGateBoundary.LiveReadOnlyAdapterEnabled,
            GreeCloudLiveReadOnlyPilotGateBoundary.LiveControlAllowed,
            GreeCloudLiveReadOnlyPilotGateBoundary.MqttAllowed,
            GreeCloudLiveReadOnlyPilotGateBoundary.ProductionWiringAllowed,
            unmet);
    }
}
