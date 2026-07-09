using AssistantEngineer.GreeAliceBridge.Application.Registry;
using AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.AccountLinking;
using AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.ProviderReadiness;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.ProviderReadiness;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.Smoke;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.Smoke;

public sealed class OfflineGreeAliceYandexProviderSmokeHarness : IGreeAliceYandexProviderSmokeHarness
{
    private const string SplitDeviceId = "dummy-gree-ac-001";
    private const string VrfLivingYandexDeviceId = "yandex-dummy-vrf-child-living-001";
    private const string VrfBedroomYandexDeviceId = "yandex-dummy-vrf-child-bedroom-001";
    private const string VrfLivingInternalDeviceId = "dummy-vrf-child-living-001";
    private const string VrfBedroomInternalDeviceId = "dummy-vrf-child-bedroom-001";
    private const string VrfGatewayId = "dummy-vrf-gateway-001";
    private const string DummyLinkedUser = "masked-yandex-user-001";
    private const string UnknownUser = "masked-yandex-user-unknown";
    private const string UnknownDeviceId = "dummy-unknown-device-001";

    private readonly IYandexSmartHomeOfflineService yandexService;
    private readonly IGreeAliceYandexScopedRegistryResolver scopedRegistryResolver;
    private readonly IGreeAliceYandexAccountLinkingTemplateProvider accountLinkingTemplateProvider;
    private readonly IGreeAliceYandexProviderReadinessEvaluator readinessEvaluator;

    public OfflineGreeAliceYandexProviderSmokeHarness()
        : this(
            new YandexSmartHomeOfflineService(
                new OfflineGreeAliceBridgeService(),
                new OfflineGreeAliceRegistryProvider()),
            new OfflineGreeAliceYandexScopedRegistryResolver(),
            new OfflineGreeAliceYandexAccountLinkingTemplateProvider(),
            new OfflineGreeAliceYandexProviderReadinessEvaluator())
    {
    }

    public OfflineGreeAliceYandexProviderSmokeHarness(
        IYandexSmartHomeOfflineService yandexService,
        IGreeAliceYandexScopedRegistryResolver scopedRegistryResolver,
        IGreeAliceYandexAccountLinkingTemplateProvider accountLinkingTemplateProvider,
        IGreeAliceYandexProviderReadinessEvaluator readinessEvaluator)
    {
        this.yandexService = yandexService;
        this.scopedRegistryResolver = scopedRegistryResolver;
        this.accountLinkingTemplateProvider = accountLinkingTemplateProvider;
        this.readinessEvaluator = readinessEvaluator;
    }

    public IReadOnlyList<GreeAliceYandexProviderSmokeScenario> GetScenarios()
    {
        return
        [
            Scenario("linked-user-devices", "Linked user devices", "Linked dummy user resolves scoped devices and offline /devices output.", "offline-pass"),
            Scenario("linked-user-query", "Linked user query", "Known split AC and VRF child units return offline fixture state.", "offline-pass"),
            Scenario("linked-user-action-fail-closed", "Linked user action fail-closed", "Known split AC and VRF child unit actions remain dry-run fail-closed.", "dry-run-fail-closed"),
            Scenario("linked-user-unlink", "Linked user unlink", "Unlink remains offline/template and touches no production state.", "offline-pass"),
            Scenario("unknown-user-devices-fail-closed", "Unknown user devices fail-closed", "Unknown Yandex user receives empty scoped registry access.", "fail-closed"),
            Scenario("unknown-device-query-fail-closed", "Unknown device query fail-closed", "Unknown device query returns controlled offline unknown state.", "offline-unknown"),
            Scenario("unknown-device-action-fail-closed", "Unknown device action fail-closed", "Unknown device action returns controlled fail-closed result.", "dry-run-fail-closed"),
            Scenario("vrf-child-unit-exposure", "VRF child unit exposure", "VRF child units are exposed as Yandex devices while gateway remains internal.", "offline-pass"),
            Scenario("gateway-not-exposed", "Gateway not exposed", "VRF gateway is not exposed by default.", "offline-pass"),
            Scenario("account-linking-template", "Account linking template", "Account linking template remains masked dummy/template data.", "offline-pass"),
            Scenario("registry-scope-template", "Registry scope template", "Linked dummy user scope remains explicit and least-scope.", "offline-pass"),
            Scenario("provider-readiness-not-ready", "Provider readiness not-ready", "Provider readiness remains not-ready and not approved.", "not-ready")
        ];
    }

    public GreeAliceYandexProviderSmokeResult Run()
    {
        List<GreeAliceYandexProviderSmokeScenarioResult> results = [];

        foreach (GreeAliceYandexProviderSmokeScenario scenario in GetScenarios())
        {
            results.Add(RunScenario(scenario));
        }

        bool allPassed = results.All(result => result.Passed)
            && !GreeAliceYandexProviderSmokeSafetyBoundary.RunsAgainstRealYandex
            && !GreeAliceYandexProviderSmokeSafetyBoundary.RunsAgainstRealOAuth
            && !GreeAliceYandexProviderSmokeSafetyBoundary.RunsAgainstProductionEndpoint
            && !GreeAliceYandexProviderSmokeSafetyBoundary.RunsAgainstLiveGreeCloud
            && !GreeAliceYandexProviderSmokeSafetyBoundary.RunsAgainstMqtt
            && !GreeAliceYandexProviderSmokeSafetyBoundary.AllowsDeviceControl
            && !GreeAliceYandexProviderSmokeSafetyBoundary.AllowsCommandExecution;

        return new GreeAliceYandexProviderSmokeResult(
            GreeAliceYandexProviderSmokeHarnessBoundary.SmokeHarnessMode,
            allPassed ? "offline-pass" : "offline-failed",
            StartedAtUtc: null,
            CompletedAtUtc: null,
            allPassed,
            results,
            "offline-local/no-real-yandex/no-oauth/no-live-gree/no-mqtt/no-device-control/no-command-execution",
            allPassed
                ? "Offline local Yandex provider smoke passed."
                : "Offline local Yandex provider smoke failed in controlled mode.");
    }

    private GreeAliceYandexProviderSmokeScenarioResult RunScenario(GreeAliceYandexProviderSmokeScenario scenario)
    {
        try
        {
            IReadOnlyList<GreeAliceYandexProviderSmokeStepResult> steps = scenario.ScenarioId switch
            {
                "linked-user-devices" => LinkedUserDevices(),
                "linked-user-query" => LinkedUserQuery(),
                "linked-user-action-fail-closed" => LinkedUserActionFailClosed(),
                "linked-user-unlink" => LinkedUserUnlink(),
                "unknown-user-devices-fail-closed" => UnknownUserDevicesFailClosed(),
                "unknown-device-query-fail-closed" => UnknownDeviceQueryFailClosed(),
                "unknown-device-action-fail-closed" => UnknownDeviceActionFailClosed(),
                "vrf-child-unit-exposure" => VrfChildUnitExposure(),
                "gateway-not-exposed" => GatewayNotExposed(),
                "account-linking-template" => AccountLinkingTemplate(),
                "registry-scope-template" => RegistryScopeTemplate(),
                "provider-readiness-not-ready" => ProviderReadinessNotReady(),
                _ => [Fail("unknown-scenario", "unknown", "Scenario is not implemented.")]
            };

            bool passed = steps.All(step => step.Passed);

            return new GreeAliceYandexProviderSmokeScenarioResult(
                scenario.ScenarioId,
                scenario.DisplayName,
                passed,
                passed ? "passed" : "failed",
                steps);
        }
        catch (Exception ex)
        {
            return new GreeAliceYandexProviderSmokeScenarioResult(
                scenario.ScenarioId,
                scenario.DisplayName,
                Passed: false,
                "failed",
                [Fail("controlled-exception", "exception", ex.GetType().Name)]);
        }
    }

    private IReadOnlyList<GreeAliceYandexProviderSmokeStepResult> LinkedUserDevices()
    {
        GreeAliceRegistryScopeBinding scope = scopedRegistryResolver.Resolve(DummyLinkedUser);
        YandexDevicesResponse devices = yandexService.GetDevices();

        return
        [
            PassIf("resolve-dummy-linked-user", "Resolve dummy linked user", scope.Status == "active-template"),
            PassIf("resolve-registry-scope", "Resolve registry scope", scope.AllowedDeviceIds.Contains(SplitDeviceId)),
            PassIf("devices-contain-split", "Assert devices contain dummy-gree-ac-001", devices.Devices.Any(device => device.Id == SplitDeviceId)),
            PassIf("devices-contain-vrf-living", "Assert devices contain exposed VRF living child", devices.Devices.Any(device => device.Id == VrfLivingYandexDeviceId)),
            PassIf("devices-contain-vrf-bedroom", "Assert devices contain exposed VRF bedroom child", devices.Devices.Any(device => device.Id == VrfBedroomYandexDeviceId)),
            PassIf("gateway-not-exposed", "Assert gateway is not exposed", devices.Devices.All(device => device.Id != VrfGatewayId)),
            PassIf("stable-yandex-ids", "Assert returned Yandex device IDs are stable", devices.Devices.Any(device => device.Id == VrfLivingYandexDeviceId && device.Name.Length > 0 && device.Room.Length > 0))
        ];
    }

    private IReadOnlyList<GreeAliceYandexProviderSmokeStepResult> LinkedUserQuery()
    {
        YandexQueryResponse split = yandexService.QueryDevices(new YandexQueryRequest([new YandexDeviceRequestDto(SplitDeviceId)]));
        YandexQueryResponse child = yandexService.QueryDevices(new YandexQueryRequest([new YandexDeviceRequestDto(VrfLivingYandexDeviceId)]));

        return
        [
            PassIf("query-split", "Known split AC query returns offline fixture state", split.Devices.Single().Status == "offline-fixture"),
            PassIf("query-vrf-child", "Known VRF child query returns offline fixture state", child.Devices.Single().Status == "offline-fixture"),
            PassIf("query-no-live-gree", "Query smoke does not call live Gree+ Cloud", !GreeAliceYandexProviderSmokeSafetyBoundary.RunsAgainstLiveGreeCloud),
            PassIf("query-no-mqtt", "Query smoke does not call MQTT", !GreeAliceYandexProviderSmokeSafetyBoundary.RunsAgainstMqtt)
        ];
    }

    private IReadOnlyList<GreeAliceYandexProviderSmokeStepResult> LinkedUserActionFailClosed()
    {
        YandexActionDeviceResultDto split = Action(SplitDeviceId);
        YandexActionDeviceResultDto child = Action(VrfLivingYandexDeviceId);

        return
        [
            AssertFailClosed("action-split-fail-closed", "Known split AC action returns dry-run/fail-closed result", split),
            AssertFailClosed("action-vrf-child-fail-closed", "Known VRF child action returns dry-run/fail-closed result", child),
            PassIf("no-command-execution", "No command execution occurred", !GreeAliceYandexProviderSmokeSafetyBoundary.AllowsCommandExecution)
        ];
    }

    private IReadOnlyList<GreeAliceYandexProviderSmokeStepResult> LinkedUserUnlink()
    {
        YandexUnlinkResponse unlink = yandexService.Unlink(DummyLinkedUser);
        GreeAliceYandexAccountUnlinkResult templateUnlink = accountLinkingTemplateProvider.GetTemplate().UnlinkResult;

        return
        [
            PassIf("call-offline-unlink", "Call offline /unlink flow", unlink.Status == "offline-no-production-data-touched"),
            PassIf("unlink-template-result", "Assert unlink remains offline/template", templateUnlink.IsDummyOrTemplate),
            PassIf("unlink-revokes-scope", "Assert unlink revokes registry scope access in result", templateUnlink.RevokedAccessToRegistryScope),
            PassIf("unlink-no-real-state", "Assert unlink does not claim deleting real storage", !templateUnlink.DeletedSecrets && !templateUnlink.DeletedTokens)
        ];
    }

    private IReadOnlyList<GreeAliceYandexProviderSmokeStepResult> UnknownUserDevicesFailClosed()
    {
        GreeAliceRegistryScopeBinding unknown = scopedRegistryResolver.Resolve(UnknownUser);

        return
        [
            PassIf("resolve-unknown-user", "Resolve unknown user", unknown.Status == "fail-closed"),
            PassIf("unknown-user-empty-scope", "Assert unknown user fail-closed", unknown.AllowedDeviceIds.Count == 0),
            PassIf("unknown-user-no-split", "Unknown user does not receive dummy split AC", !unknown.AllowedDeviceIds.Contains(SplitDeviceId)),
            PassIf("unknown-user-no-vrf", "Unknown user does not receive VRF child units", !unknown.AllowedDeviceIds.Contains(VrfLivingInternalDeviceId) && !unknown.AllowedDeviceIds.Contains(VrfBedroomInternalDeviceId))
        ];
    }

    private IReadOnlyList<GreeAliceYandexProviderSmokeStepResult> UnknownDeviceQueryFailClosed()
    {
        YandexQueryDeviceDto unknown = yandexService
            .QueryDevices(new YandexQueryRequest([new YandexDeviceRequestDto(UnknownDeviceId)]))
            .Devices
            .Single();

        return
        [
            PassIf("query-unknown-device", "Query unknown device", unknown.Status == "offline-unknown"),
            PassIf("unknown-device-offline", "Assert unknown device fail-closed", !unknown.Online)
        ];
    }

    private IReadOnlyList<GreeAliceYandexProviderSmokeStepResult> UnknownDeviceActionFailClosed()
    {
        YandexActionDeviceResultDto unknown = Action(UnknownDeviceId);

        return [AssertFailClosed("action-unknown-device", "Action unknown device", unknown)];
    }

    private IReadOnlyList<GreeAliceYandexProviderSmokeStepResult> VrfChildUnitExposure()
    {
        YandexDevicesResponse devices = yandexService.GetDevices();
        YandexActionDeviceResultDto livingAction = Action(VrfLivingYandexDeviceId);
        YandexActionDeviceResultDto bedroomAction = Action(VrfBedroomYandexDeviceId);

        return
        [
            PassIf("vrf-living-exposed", "Assert living child is exposed as Yandex user device", devices.Devices.Any(device => device.Id == VrfLivingYandexDeviceId && device.Name.Length > 0 && device.Room.Length > 0)),
            PassIf("vrf-bedroom-exposed", "Assert bedroom child is exposed as Yandex user device", devices.Devices.Any(device => device.Id == VrfBedroomYandexDeviceId && device.Name.Length > 0 && device.Room.Length > 0)),
            PassIf("vrf-gateway-not-exposed", "Assert gateway is not exposed by default", devices.Devices.All(device => device.Id != VrfGatewayId)),
            AssertFailClosed("vrf-living-action-fail-closed", "Action for living child remains fail-closed", livingAction),
            AssertFailClosed("vrf-bedroom-action-fail-closed", "Action for bedroom child remains fail-closed", bedroomAction)
        ];
    }

    private IReadOnlyList<GreeAliceYandexProviderSmokeStepResult> GatewayNotExposed()
    {
        YandexDevicesResponse devices = yandexService.GetDevices();

        return [PassIf("gateway-not-exposed", "Assert gateway is not exposed", devices.Devices.All(device => device.Id != VrfGatewayId))];
    }

    private IReadOnlyList<GreeAliceYandexProviderSmokeStepResult> AccountLinkingTemplate()
    {
        GreeAliceYandexAccountLinkingTemplate template = accountLinkingTemplateProvider.GetTemplate();

        return
        [
            PassIf("account-linking-template", "Account linking dummy/template check", template.Session.IsDummyOrTemplate && template.ActiveBinding.IsDummyOrTemplate),
            PassIf("account-linking-masked-user", "Yandex user reference is masked", template.ActiveBinding.YandexUserReference.StartsWith("masked-yandex-user-", StringComparison.Ordinal))
        ];
    }

    private IReadOnlyList<GreeAliceYandexProviderSmokeStepResult> RegistryScopeTemplate()
    {
        GreeAliceRegistryScopeBinding scope = scopedRegistryResolver.Resolve(DummyLinkedUser);

        return
        [
            PassIf("registry-scope-template", "Scoped registry dummy/template check", scope.Status == "active-template"),
            PassIf("registry-scope-explicit", "Registry scope is explicit", scope.AllowedDeviceIds.Contains(SplitDeviceId) && scope.AllowedDeviceIds.Contains(VrfLivingInternalDeviceId)),
            PassIf("registry-scope-not-global", "Registry scope is not global", scope.AllowedDeviceIds.All(id => id is not "*" and not "all" and not "global"))
        ];
    }

    private IReadOnlyList<GreeAliceYandexProviderSmokeStepResult> ProviderReadinessNotReady()
    {
        GreeAliceYandexProviderReadinessReview readiness = readinessEvaluator.Evaluate();

        return
        [
            PassIf("provider-readiness-not-ready", "Assert provider readiness remains not-ready", readiness.Status == "not-ready"),
            PassIf("provider-registration-not-approved", "Provider registration remains not-approved", !readiness.ProviderRegistrationApproved),
            PassIf("real-oauth-not-implemented", "Real OAuth remains not implemented", !readiness.RealOAuthImplemented),
            PassIf("production-endpoint-disabled", "Production endpoint remains not configured", !readiness.ProductionCredentialsConfigured),
            PassIf("production-deploy-disabled", "Production deploy remains disabled", !readiness.ProductionDeploymentWiringEnabled),
            PassIf("live-control-disabled", "Live control remains disabled", !readiness.LiveControlEnabled)
        ];
    }

    private YandexActionDeviceResultDto Action(string deviceId)
    {
        return yandexService
            .ExecuteAction(new YandexActionRequest(
                [new YandexActionDeviceRequestDto(
                    deviceId,
                    [new YandexActionCapabilityRequestDto("devices.capabilities.on_off", "set", "true")])]))
            .Devices
            .Single();
    }

    private static GreeAliceYandexProviderSmokeStepResult AssertFailClosed(
        string stepId,
        string description,
        YandexActionDeviceResultDto result)
    {
        return PassIf(
            stepId,
            description,
            result.Status == "dry-run-fail-closed"
                && !result.SentToGreeCloud
                && !result.SentToMqtt
                && !result.SentToDevice);
    }

    private static GreeAliceYandexProviderSmokeStepResult PassIf(string stepId, string description, bool passed)
    {
        return new GreeAliceYandexProviderSmokeStepResult(
            stepId,
            "assert",
            passed,
            passed ? "passed" : "failed",
            description);
    }

    private static GreeAliceYandexProviderSmokeStepResult Fail(string stepId, string kind, string message)
    {
        return new GreeAliceYandexProviderSmokeStepResult(stepId, kind, Passed: false, "failed", message);
    }

    private static GreeAliceYandexProviderSmokeScenario Scenario(
        string id,
        string displayName,
        string description,
        string expectedResult)
    {
        return new GreeAliceYandexProviderSmokeScenario(
            id,
            displayName,
            description,
            IsOfflineOnly: true,
            UsesDummyOrTemplateData: true,
            expectedResult,
            [
                new GreeAliceYandexProviderSmokeStep(
                    id + "-step",
                    "offline-assertion",
                    description,
                    [new GreeAliceYandexProviderSmokeExpectation(id + "-expectation", expectedResult, IsRequired: true)])
            ]);
    }
}
