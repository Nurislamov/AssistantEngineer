using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.HttpSmoke;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.HttpSmoke;

public sealed class OfflineGreeAliceLocalHttpSmokePlanProvider : IGreeAliceLocalHttpSmokePlanProvider
{
    public GreeAliceLocalHttpSmokeResult GetPlan()
    {
        IReadOnlyList<GreeAliceLocalHttpSmokeEndpoint> endpoints =
        [
            Endpoint("health", "GET", "/health", "Verify isolated bridge local health.", "offline-health"),
            Endpoint("devices", "GET", "/v1.0/user/devices", "Verify offline dummy user devices and hidden gateway.", "offline-devices"),
            Endpoint("query", "POST", "/v1.0/user/devices/query", "Verify known and unknown dummy device state responses.", "offline-query"),
            Endpoint("action", "POST", "/v1.0/user/devices/action", "Verify action remains dry-run fail-closed.", "dry-run-fail-closed"),
            Endpoint("unlink", "POST", "/v1.0/user/unlink", "Verify unlink remains offline/template.", "offline-unlink")
        ];

        IReadOnlyList<GreeAliceLocalHttpSmokeRequest> requests =
        [
            Request("health", "GET", "/health", null),
            Request("devices", "GET", "/v1.0/user/devices", null),
            Request("query", "POST", "/v1.0/user/devices/query", """
            {
              "devices": [
                { "id": "dummy-gree-ac-001" },
                { "id": "yandex-dummy-vrf-child-living-001" },
                { "id": "unknown-device-001" }
              ]
            }
            """),
            Request("action", "POST", "/v1.0/user/devices/action", """
            {
              "devices": [
                {
                  "id": "dummy-gree-ac-001",
                  "capabilities": []
                },
                {
                  "id": "yandex-dummy-vrf-child-living-001",
                  "capabilities": []
                },
                {
                  "id": "unknown-device-001",
                  "capabilities": []
                }
              ]
            }
            """),
            Request("unlink", "POST", "/v1.0/user/unlink", null)
        ];

        IReadOnlyList<GreeAliceLocalHttpSmokeExpectation> expectations =
        [
            Expectation("health", "success", ["local health response", "offline runtime mode"], requiresFailClosedAction: false),
            Expectation("devices", "success", ["dummy-gree-ac-001", "exposed VRF child units", "gateway hidden"], requiresFailClosedAction: false),
            Expectation("query", "controlled response", ["offline fixture state", "unknown device controlled result", "no live Gree+ Cloud", "no MQTT"], requiresFailClosedAction: false),
            Expectation("action", "dry-run-fail-closed", ["SentToGreeCloud=false", "SentToMqtt=false", "SentToDevice=false", "no command execution"], requiresFailClosedAction: true),
            Expectation("unlink", "offline-template", ["no real token deletion", "no real secret deletion", "template registry scope revoke result"], requiresFailClosedAction: false)
        ];

        IReadOnlyList<GreeAliceLocalHttpSmokeStep> steps = endpoints
            .Select(endpoint => new GreeAliceLocalHttpSmokeStep(
                endpoint.EndpointId + "-step",
                endpoint.EndpointId,
                endpoint.Purpose,
                requests.Single(request => request.EndpointId == endpoint.EndpointId),
                expectations.Single(expectation => expectation.EndpointId == endpoint.EndpointId)))
            .ToArray();

        return new GreeAliceLocalHttpSmokeResult(
            GreeAliceLocalHttpSmokeBoundary.HttpSmokeMode,
            GreeAliceLocalHttpSmokeBoundary.HttpSmokeStatus,
            endpoints,
            requests,
            expectations,
            steps,
            "localhost-only/no-real-yandex/no-oauth/no-secret-material/no-live-gree/no-mqtt/no-device-control/no-command-execution/no-production");
    }

    private static GreeAliceLocalHttpSmokeEndpoint Endpoint(
        string id,
        string method,
        string path,
        string purpose,
        string expectedSafetyResult)
    {
        return new GreeAliceLocalHttpSmokeEndpoint(
            id,
            method,
            path,
            purpose,
            IsLocalOnly: true,
            UsesDummyOrTemplateData: true,
            expectedSafetyResult);
    }

    private static GreeAliceLocalHttpSmokeRequest Request(
        string endpointId,
        string method,
        string path,
        string? bodyJson)
    {
        return new GreeAliceLocalHttpSmokeRequest(
            endpointId,
            method,
            path,
            bodyJson,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Content-Type"] = "application/json"
            },
            IsLocalOnly: true,
            UsesDummyOrTemplateData: true);
    }

    private static GreeAliceLocalHttpSmokeExpectation Expectation(
        string endpointId,
        string expectedStatus,
        IReadOnlyList<string> requiredEvidence,
        bool requiresFailClosedAction)
    {
        return new GreeAliceLocalHttpSmokeExpectation(
            endpointId,
            expectedStatus,
            requiredEvidence,
            RequiresNoExternalCalls: true,
            RequiresFailClosedAction: requiresFailClosedAction);
    }
}
