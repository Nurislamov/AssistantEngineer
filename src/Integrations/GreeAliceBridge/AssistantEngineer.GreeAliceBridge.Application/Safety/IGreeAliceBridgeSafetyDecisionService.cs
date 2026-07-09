using AssistantEngineer.GreeAliceBridge.Contracts.Safety;

namespace AssistantEngineer.GreeAliceBridge.Application.Safety;

public interface IGreeAliceBridgeSafetyDecisionService
{
    GreeAliceBridgeSafetyDecision Decide(GreeAliceBridgeSafetyContext context);
}
