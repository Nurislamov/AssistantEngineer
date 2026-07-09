using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.AccountLinking;

public sealed class OfflineGreeAliceYandexScopedRegistryResolver : IGreeAliceYandexScopedRegistryResolver
{
    private readonly GreeAliceYandexAccountLinkingTemplate template;

    public OfflineGreeAliceYandexScopedRegistryResolver()
        : this(new OfflineGreeAliceYandexAccountLinkingTemplateProvider())
    {
    }

    public OfflineGreeAliceYandexScopedRegistryResolver(IGreeAliceYandexAccountLinkingTemplateProvider templateProvider)
    {
        template = templateProvider.GetTemplate();
    }

    public GreeAliceRegistryScopeBinding Resolve(string yandexUserReference)
    {
        if (string.Equals(yandexUserReference, template.ActiveBinding.YandexUserReference, StringComparison.Ordinal))
        {
            return Resolve(template.ActiveBinding);
        }

        return Empty(yandexUserReference);
    }

    public GreeAliceRegistryScopeBinding Resolve(GreeAliceYandexUserBinding? binding)
    {
        if (binding is null || !binding.IsActive || binding.UnlinkedAtUtc is not null)
        {
            return Empty(binding?.YandexUserReference ?? "unknown-yandex-user");
        }

        if (!string.Equals(binding.YandexUserReference, template.ActiveBinding.YandexUserReference, StringComparison.Ordinal)
            || !string.Equals(binding.RegistryScopeReference, template.RegistryScope.RegistryScopeReference, StringComparison.Ordinal))
        {
            return Empty(binding.YandexUserReference);
        }

        string[] allowedDeviceIds =
        [
            .. template.RegistryScope.AllowedDeviceIds,
            .. template.RegistryScope.AllowedVrfChildUnitIds
        ];

        return new GreeAliceRegistryScopeBinding(
            binding.YandexUserReference,
            binding.BridgeAccountReference,
            binding.RegistryScopeReference,
            allowedDeviceIds,
            GreeAliceYandexAccountLinkingStatus.ActiveTemplate);
    }

    private static GreeAliceRegistryScopeBinding Empty(string yandexUserReference)
    {
        return new GreeAliceRegistryScopeBinding(
            yandexUserReference,
            BridgeAccountReference: string.Empty,
            RegistryScopeReference: string.Empty,
            AllowedDeviceIds: [],
            GreeAliceYandexAccountLinkingStatus.FailClosed);
    }
}
