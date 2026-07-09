using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.ProviderReadiness;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.ProviderReadiness;

public sealed class OfflineGreeAliceYandexProviderReadinessEvaluator : IGreeAliceYandexProviderReadinessEvaluator
{
    public GreeAliceYandexProviderReadinessReview Evaluate()
    {
        IReadOnlyList<GreeAliceYandexProviderEndpointReadiness> endpoints = CreateEndpoints();
        IReadOnlyList<GreeAliceYandexProviderReadinessRequirement> requirements = CreateRequirements();
        GreeAliceYandexProviderSecurityChecklist securityChecklist = new(
            CreateSecurityItems(),
            GreeAliceYandexProviderReadinessStatus.NotApproved);
        GreeAliceYandexProviderManualSmokePlan smokePlan = new(
            CreateSmokeSteps(),
            LiveCallsAllowed: false,
            GreeAliceYandexProviderReadinessStatus.PendingManualReview);
        GreeAliceYandexProviderSubmissionChecklist submissionChecklist = new(
            "NOT APPROVED",
            CreateSubmissionItems());
        GreeAliceYandexProviderOperatorChecklist operatorChecklist = new(
            GreeAliceYandexProviderReadinessStatus.NotApproved,
            CreateOperatorItems());

        return new GreeAliceYandexProviderReadinessReview(
            GreeAliceYandexProviderReadinessBoundary.ProviderReadinessMode,
            GreeAliceYandexProviderReadinessBoundary.ProviderReadinessStatus,
            endpoints,
            requirements,
            submissionChecklist,
            securityChecklist,
            smokePlan,
            operatorChecklist,
            GreeAliceYandexProviderReadinessBoundary.ProviderRegistrationApproved,
            GreeAliceYandexProviderReadinessBoundary.RealOAuthImplemented,
            GreeAliceYandexProviderReadinessBoundary.RealYandexClientCredentialsConfigured,
            GreeAliceYandexProviderReadinessBoundary.ProductionDeploymentWiringEnabled,
            GreeAliceYandexProviderReadinessBoundary.AllowsLiveGreeControl);
    }

    private static IReadOnlyList<GreeAliceYandexProviderEndpointReadiness> CreateEndpoints()
    {
        return
        [
            Endpoint("smart-home-devices", "GET", "/v1.0/user/devices", GreeAliceYandexProviderReadinessStatus.OfflineContractPresent, implemented: true),
            Endpoint("smart-home-query", "POST", "/v1.0/user/devices/query", GreeAliceYandexProviderReadinessStatus.OfflineContractPresent, implemented: true),
            Endpoint("smart-home-action", "POST", "/v1.0/user/devices/action", GreeAliceYandexProviderReadinessStatus.OfflineContractPresentFailClosed, implemented: true),
            Endpoint("smart-home-unlink", "POST", "/v1.0/user/unlink", GreeAliceYandexProviderReadinessStatus.OfflineContractPresent, implemented: true),
            Endpoint("future-oauth-authorize", "GET", "/oauth/authorize", GreeAliceYandexProviderReadinessStatus.NotImplemented, implemented: false),
            Endpoint("future-oauth-token", "POST", "/oauth/token", GreeAliceYandexProviderReadinessStatus.NotImplemented, implemented: false),
            Endpoint("future-oauth-callback", "GET", "/oauth/callback", GreeAliceYandexProviderReadinessStatus.NotImplemented, implemented: false)
        ];
    }

    private static IReadOnlyList<GreeAliceYandexProviderReadinessRequirement> CreateRequirements()
    {
        return
        [
            Satisfied("smart-home-devices-contract", "Smart Home devices contract reviewed", "smart-home"),
            Satisfied("smart-home-query-contract", "Smart Home query contract reviewed", "smart-home"),
            Satisfied("smart-home-action-contract", "Smart Home action contract reviewed", "smart-home"),
            Satisfied("smart-home-unlink-contract", "Unlink contract reviewed", "smart-home"),
            Pending("account-linking-review", "Account linking boundary reviewed", "account-linking"),
            Pending("registry-scope-review", "Registry scope mapping reviewed", "registry"),
            Pending("vrf-child-exposure-review", "VRF/GMV child-unit exposure reviewed", "registry"),
            Pending("stable-yandex-id-review", "Stable Yandex IDs reviewed", "registry"),
            Pending("manual-smoke-plan-approved", "Manual smoke plan approved", "manual-smoke"),
            Pending("security-checklist-approved", "Security checklist approved", "security"),
            Pending("operator-checklist-approved", "Operator checklist approved", "operator"),
            Pending("secrets-storage-plan-approved", "Secrets storage plan approved outside repository", "security"),
            Pending("production-endpoint-plan-approved", "Production endpoint plan approved", "production"),
            Pending("rollback-plan-approved", "Rollback plan approved", "operations"),
            Pending("kill-switch-plan-approved", "Kill-switch plan approved", "operations"),
            Pending("monitoring-plan-approved", "Monitoring plan approved", "operations"),
            Pending("live-read-only-pilot-approved-separately", "Gree+ live read-only pilot approved separately", "gree-live"),
            Pending("live-control-approved-separately", "Gree+ live control approved separately", "gree-control")
        ];
    }

    private static IReadOnlyList<GreeAliceYandexProviderReadinessRequirement> CreateSecurityItems()
    {
        return
        [
            Pending("no-secrets-in-repo", "No secrets in repository", "security"),
            Pending("no-yandex-client-secret-in-repo", "No real Yandex client secret in repository", "security"),
            Pending("no-access-" + "token-in-repo", "No real access tokens in repository", "security"),
            Pending("no-refresh-" + "token-in-repo", "No real refresh tokens in repository", "security"),
            Pending("no-gree-credentials-in-repo", "No real Gree credentials in repository", "security"),
            Pending("no-gree-device-keys-in-repo", "No real Gree device keys in repository", "security"),
            Pending("no-mac-like-fixtures", "No MAC-like identifiers in repository fixtures", "security"),
            Pending("no-production-account-device-ids", "No production account/device IDs in repository fixtures", "security"),
            Pending("production-secrets-outside-repo", "Production secrets must be stored outside repository", "security"),
            Pending("credentials-rotation-plan-required", "Credentials rotation plan required", "security"),
            Pending("token-revocation-plan-required", "Token revocation plan required", "security"),
            Pending("audit-logging-required", "Audit logging required", "security"),
            Pending("masked-evidence-required", "Masked evidence required", "security"),
            Pending("least-scope-registry-exposure-required", "Least-scope registry exposure required", "security"),
            Pending("unknown-unlinked-users-fail-closed", "Unknown/unlinked users fail closed", "security"),
            Pending("action-endpoint-fail-closed", "Action endpoint remains fail-closed until explicit approval", "security"),
            Pending("mqtt-remains-blocked", "MQTT remains blocked", "security")
        ];
    }

    private static IReadOnlyList<GreeAliceYandexProviderReadinessRequirement> CreateSmokeSteps()
    {
        return
        [
            Pending("build", "Build", "manual-smoke"),
            Pending("tests", "Tests", "manual-smoke"),
            Pending("static-safety-scans", "Static safety scans", "manual-smoke"),
            Pending("local-bridge-health", "Local bridge health", "manual-smoke"),
            Pending("devices-offline-check", "/devices contract check", "manual-smoke"),
            Pending("query-offline-check", "/query contract check", "manual-smoke"),
            Pending("action-fail-closed-check", "/action fail-closed check", "manual-smoke"),
            Pending("unlink-offline-check", "/unlink offline check", "manual-smoke"),
            Pending("account-linking-template-check", "Account linking dummy/template check", "manual-smoke"),
            Pending("scoped-registry-template-check", "Scoped registry dummy/template check", "manual-smoke"),
            Pending("vrf-child-unit-exposure-check", "VRF child unit exposure check", "manual-smoke"),
            Pending("unknown-user-fail-closed-check", "Unknown user fail-closed check", "manual-smoke"),
            Pending("unknown-device-fail-closed-check", "Unknown device fail-closed check", "manual-smoke"),
            Pending("no-secret-scan", "No secret scan", "manual-smoke"),
            Pending("no-mqtt-scan", "No MQTT scan", "manual-smoke"),
            Pending("no-production-wiring-scan", "No production wiring scan", "manual-smoke"),
            Pending("no-live-call-step", "No live call step", "manual-smoke")
        ];
    }

    private static IReadOnlyList<GreeAliceYandexProviderReadinessRequirement> CreateSubmissionItems()
    {
        return
        [
            Pending("repository-commit-recorded", "Repository commit recorded", "submission"),
            Pending("full-validation-pass", "Full validation PASS", "submission"),
            Pending("smart-home-api-docs-reviewed", "Smart Home API docs reviewed", "submission"),
            Pending("devices-contract-reviewed", "/devices contract reviewed", "submission"),
            Pending("query-contract-reviewed", "/query contract reviewed", "submission"),
            Pending("action-contract-reviewed", "/action contract reviewed", "submission"),
            Pending("unlink-contract-reviewed", "/unlink contract reviewed", "submission"),
            Pending("account-linking-design-reviewed", "Account linking design reviewed", "submission"),
            Pending("oauth-implementation-reviewed", "OAuth implementation reviewed", "submission"),
            Pending("oauth-secrets-storage-plan-reviewed", "OAuth secrets storage plan reviewed", "submission"),
            Pending("registry-scope-model-reviewed", "Registry scope model reviewed", "submission"),
            Pending("vrf-child-unit-exposure-reviewed", "VRF/GMV child-unit exposure reviewed", "submission"),
            Pending("stable-yandex-ids-reviewed", "Stable Yandex IDs reviewed", "submission"),
            Pending("security-review-completed", "Security review completed", "submission"),
            Pending("manual-smoke-completed", "Manual smoke completed", "submission"),
            Pending("monitoring-plan-reviewed", "Monitoring plan reviewed", "submission"),
            Pending("rollback-plan-reviewed", "Rollback plan reviewed", "submission"),
            Pending("kill-switch-plan-reviewed", "Kill-switch plan reviewed", "submission"),
            Pending("read-only-pilot-approval-reference", "Read-only pilot approval reference", "submission"),
            Pending("control-pilot-approval-reference", "Control pilot approval reference if any", "submission"),
            Pending("operator-approval", "Operator approval", "submission"),
            Pending("final-decision", "Final decision", "submission")
        ];
    }

    private static IReadOnlyList<GreeAliceYandexProviderReadinessRequirement> CreateOperatorItems()
    {
        return
        [
            Pending("responsible-operator-selected", "Responsible operator selected", "operator"),
            Pending("repository-commit-recorded", "Repository commit recorded", "operator"),
            Pending("validation-result-recorded", "Validation result recorded", "operator"),
            Pending("manual-smoke-completed", "Manual smoke completed", "operator"),
            Pending("security-review-completed", "Security review completed", "operator"),
            Pending("provider-publication-decision-recorded", "Provider publication decision recorded", "operator"),
            Pending("oauth-implementation-decision-recorded", "OAuth implementation decision recorded", "operator"),
            Pending("production-endpoint-decision-recorded", "Production endpoint decision recorded", "operator"),
            Pending("read-only-pilot-decision-recorded-separately", "Read-only pilot decision recorded separately", "operator"),
            Pending("control-pilot-decision-recorded-separately", "Control pilot decision recorded separately", "operator"),
            Pending("rollback-owner-selected", "Rollback owner selected", "operator"),
            Pending("kill-switch-owner-selected", "Kill-switch owner selected", "operator"),
            Pending("monitoring-owner-selected", "Monitoring owner selected", "operator")
        ];
    }

    private static GreeAliceYandexProviderEndpointReadiness Endpoint(
        string endpointGroup,
        string method,
        string path,
        string status,
        bool implemented)
    {
        return new GreeAliceYandexProviderEndpointReadiness(
            endpointGroup,
            method,
            path,
            status,
            implemented,
            IsProductionReady: false);
    }

    private static GreeAliceYandexProviderReadinessRequirement Satisfied(
        string id,
        string title,
        string area)
    {
        return new GreeAliceYandexProviderReadinessRequirement(
            id,
            title,
            IsSatisfied: true,
            GreeAliceYandexProviderReadinessStatus.OfflineContractPresent,
            area);
    }

    private static GreeAliceYandexProviderReadinessRequirement Pending(
        string id,
        string title,
        string area)
    {
        return new GreeAliceYandexProviderReadinessRequirement(
            id,
            title,
            IsSatisfied: false,
            GreeAliceYandexProviderReadinessStatus.PendingManualReview,
            area);
    }
}
