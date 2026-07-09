using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.AccountLinking;

public sealed class OfflineGreeAliceYandexAccountLinkingTemplateProvider : IGreeAliceYandexAccountLinkingTemplateProvider
{
    public const string DummySessionId = "dummy-link-session-001";
    public const string DummyYandexUserReference = "masked-yandex-user-001";
    public const string DummyBridgeAccountReference = "dummy-bridge-account-001";
    public const string DummyRegistryScopeReference = "dummy-registry-scope-001";

    public GreeAliceYandexAccountLinkingTemplate GetTemplate()
    {
        GreeAliceYandexAccountLinkingSession session = new(
            DummySessionId,
            GreeAliceYandexAccountLinkingMode.OfflineTemplate,
            GreeAliceYandexAccountLinkingStatus.NotApproved,
            RequestedAtUtc: null,
            DummyYandexUserReference,
            DummyBridgeAccountReference,
            DummyRegistryScopeReference,
            IsMasked: true,
            IsDummyOrTemplate: true);

        GreeAliceYandexUserBinding binding = new(
            DummyYandexUserReference,
            DummyBridgeAccountReference,
            DummyRegistryScopeReference,
            LinkedAtUtc: null,
            UnlinkedAtUtc: null,
            IsActive: true,
            IsMasked: true,
            IsDummyOrTemplate: true);

        GreeAliceBridgeAccountScope scope = new(
            DummyBridgeAccountReference,
            DummyRegistryScopeReference,
            ["dummy-home-001"],
            ["dummy-gree-ac-001"],
            ["dummy-vrf-gateway-001"],
            ["dummy-vrf-child-living-001", "dummy-vrf-child-bedroom-001"],
            IsMasked: true,
            IsDummyOrTemplate: true);

        GreeAliceYandexAccountUnlinkRequest request = new(
            DummyYandexUserReference,
            DummyBridgeAccountReference,
            DummyRegistryScopeReference,
            IsDummyOrTemplate: true);

        GreeAliceYandexAccountUnlinkResult result = new(
            DummyYandexUserReference,
            DummyBridgeAccountReference,
            DummyRegistryScopeReference,
            WasLinked: true,
            IsNowUnlinked: true,
            RevokedAccessToRegistryScope: true,
            DeletedSecrets: false,
            DeletedTokens: false,
            RealTokenStorageImplemented: false,
            "offline-template-unlink",
            IsDummyOrTemplate: true);

        return new GreeAliceYandexAccountLinkingTemplate(session, binding, scope, request, result);
    }
}
