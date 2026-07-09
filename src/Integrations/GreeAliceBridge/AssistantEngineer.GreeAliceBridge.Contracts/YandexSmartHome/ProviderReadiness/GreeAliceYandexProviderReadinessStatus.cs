namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.ProviderReadiness;

public static class GreeAliceYandexProviderReadinessStatus
{
    public const string NotReady = "not-ready";
    public const string OfflineContractPresent = "offline-contract-present";
    public const string OfflineContractPresentFailClosed = "offline-contract-present-fail-closed";
    public const string NotImplemented = "not-implemented";
    public const string PendingManualReview = "pending-manual-review";
    public const string NotApproved = "not-approved";
}
