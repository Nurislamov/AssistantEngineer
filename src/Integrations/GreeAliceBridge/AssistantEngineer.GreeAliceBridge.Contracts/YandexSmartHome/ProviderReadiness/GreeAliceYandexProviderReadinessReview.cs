namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.ProviderReadiness;

public sealed record GreeAliceYandexProviderReadinessReview(
    string Mode,
    string Status,
    IReadOnlyList<GreeAliceYandexProviderEndpointReadiness> Endpoints,
    IReadOnlyList<GreeAliceYandexProviderReadinessRequirement> Requirements,
    GreeAliceYandexProviderSubmissionChecklist SubmissionChecklist,
    GreeAliceYandexProviderSecurityChecklist SecurityChecklist,
    GreeAliceYandexProviderManualSmokePlan ManualSmokePlan,
    GreeAliceYandexProviderOperatorChecklist OperatorChecklist,
    bool ProviderRegistrationApproved,
    bool RealOAuthImplemented,
    bool ProductionCredentialsConfigured,
    bool ProductionDeploymentWiringEnabled,
    bool LiveControlEnabled);
