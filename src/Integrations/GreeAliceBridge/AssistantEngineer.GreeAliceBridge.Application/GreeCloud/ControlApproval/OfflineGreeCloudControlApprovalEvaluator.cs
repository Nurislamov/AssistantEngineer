using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.ControlApproval;

namespace AssistantEngineer.GreeAliceBridge.Application.GreeCloud.ControlApproval;

public sealed class OfflineGreeCloudControlApprovalEvaluator : IGreeCloudControlApprovalEvaluator
{
    public GreeCloudControlPilotDecision Evaluate(GreeCloudControlApprovalEvaluation? evaluation = null)
    {
        IReadOnlySet<string> satisfied = evaluation?.ManuallySatisfiedRequirements ?? new HashSet<string>(StringComparer.Ordinal);

        string[] unmet = GreeCloudControlApprovalBoundary.RequiredRequirements
            .Where(requirement => !satisfied.Contains(requirement))
            .ToArray();

        return new GreeCloudControlPilotDecision(
            GreeCloudControlApprovalBoundary.ControlApprovalStatus,
            GreeCloudControlApprovalBoundary.LiveControlApproved,
            GreeCloudControlApprovalBoundary.LiveControlEnabled,
            GreeCloudControlApprovalBoundary.ControlAdapterEnabled,
            GreeCloudControlApprovalBoundary.ControlAdapterFailClosed,
            GreeCloudControlApprovalBoundary.MqttAllowed,
            GreeCloudControlApprovalBoundary.ProductionWiringAllowed,
            GreeCloudControlApprovalBoundary.SingleDevicePilotApproved,
            unmet);
    }
}
