using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.ProviderReadiness;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.ProviderReadiness;

public interface IGreeAliceYandexProviderReadinessEvaluator
{
    GreeAliceYandexProviderReadinessReview Evaluate();
}
