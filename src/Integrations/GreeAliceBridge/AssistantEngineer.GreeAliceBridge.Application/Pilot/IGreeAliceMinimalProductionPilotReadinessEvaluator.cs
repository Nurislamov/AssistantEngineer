using AssistantEngineer.GreeAliceBridge.Contracts.Pilot;

namespace AssistantEngineer.GreeAliceBridge.Application.Pilot;

public interface IGreeAliceMinimalProductionPilotReadinessEvaluator
{
    GreeAliceMinimalProductionPilotDecision Evaluate(GreeAliceMinimalProductionPilotEvaluation? evaluation = null);
}
