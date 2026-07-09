using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.AccountLinking;

public interface IGreeAliceYandexScopedRegistryResolver
{
    GreeAliceRegistryScopeBinding Resolve(string yandexUserReference);

    GreeAliceRegistryScopeBinding Resolve(GreeAliceYandexUserBinding? binding);
}
