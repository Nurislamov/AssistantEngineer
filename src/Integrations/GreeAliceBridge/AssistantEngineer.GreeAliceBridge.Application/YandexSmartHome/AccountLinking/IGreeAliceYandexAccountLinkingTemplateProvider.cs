using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.AccountLinking;

public interface IGreeAliceYandexAccountLinkingTemplateProvider
{
    GreeAliceYandexAccountLinkingTemplate GetTemplate();
}
