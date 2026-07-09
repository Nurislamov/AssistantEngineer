using AssistantEngineer.GreeAliceBridge.Contracts.Pilot;

namespace AssistantEngineer.GreeAliceBridge.Application.Pilot;

public sealed class OfflineGreeAliceMinimalProductionPilotReadinessEvaluator : IGreeAliceMinimalProductionPilotReadinessEvaluator
{
    public GreeAliceMinimalProductionPilotDecision Evaluate(GreeAliceMinimalProductionPilotEvaluation? evaluation = null)
    {
        IReadOnlySet<string> satisfied = evaluation?.ManuallySatisfiedRequirements ?? new HashSet<string>(StringComparer.Ordinal);

        string[] unmet = GreeAliceMinimalProductionPilotBoundary.RequiredRequirements
            .Where(requirement => !satisfied.Contains(requirement))
            .ToArray();

        return new GreeAliceMinimalProductionPilotDecision(
            GreeAliceMinimalProductionPilotBoundary.ProductionPilotStatus,
            GreeAliceMinimalProductionPilotBoundary.DefaultMode,
            GreeAliceMinimalProductionPilotBoundary.DefaultScope,
            GreeAliceMinimalProductionPilotBoundary.ProductionPilotApproved,
            GreeAliceMinimalProductionPilotBoundary.ProductionPilotEnabled,
            GreeAliceMinimalProductionPilotBoundary.ProductionDeploymentWiringEnabled,
            GreeAliceMinimalProductionPilotBoundary.LiveReadOnlyPilotEnabled,
            GreeAliceMinimalProductionPilotBoundary.LiveControlEnabled,
            GreeAliceMinimalProductionPilotBoundary.MqttAllowed,
            unmet);
    }
}
