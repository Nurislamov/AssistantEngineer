using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.AccountLinking;

public interface IGreeAliceYandexAccountLinkingValidator
{
    GreeAliceYandexAccountLinkingValidationResult ValidateTemplate(GreeAliceYandexAccountLinkingTemplate? template);

    GreeAliceYandexAccountLinkingValidationResult ValidateSession(GreeAliceYandexAccountLinkingSession? session);

    GreeAliceYandexAccountLinkingValidationResult ValidateBinding(GreeAliceYandexUserBinding? binding);

    GreeAliceYandexAccountLinkingValidationResult ValidateScope(GreeAliceBridgeAccountScope? scope);
}
