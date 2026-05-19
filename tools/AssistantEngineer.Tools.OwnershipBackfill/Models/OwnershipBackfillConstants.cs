namespace AssistantEngineer.Tools.OwnershipBackfill.Models;

public static class OwnershipBackfillConstants
{
    public const int DefaultBatchSize = 500;
    public const double DefaultMaxUnresolvedRate = 0.05d;
    public const string ApplyConfirmationPhrase = "I_UNDERSTAND_THIS_WRITES_OWNERSHIP_METADATA";
    public const string PlanSignoffConfirmationPhrase = "I_REVIEWED_THE_OWNERSHIP_BACKFILL_PLAN";

    public static readonly IReadOnlyList<string> NonClaims =
    [
        "No ownership backfill execution claim.",
        "No full multi-tenant isolation claim yet.",
        "No database row-level security claim.",
        "No global EF query filter claim.",
        "No production security certification claim.",
        "No certified/certification claim."
    ];

    public static readonly IReadOnlyList<string> KnownRecordTypes =
    [
        "Project",
        "Building",
        "WorkflowState",
        "Scenario",
        "Job",
        "JobEvent",
        "ScenarioHistory"
    ];

    public static readonly IReadOnlySet<string> SupportedDatabaseProviders = new HashSet<string>(
        ["PostgreSQL", "SQLite", "None"],
        StringComparer.OrdinalIgnoreCase);
}
