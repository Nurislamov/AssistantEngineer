namespace AssistantEngineer.Tools.OwnershipBackfill.Cli;

public static class OwnershipBackfillCommandDescriptorCatalog
{
    private static readonly OwnershipBackfillCommandDescriptor[] Descriptors =
    [
        new(
            Name: "dry-run",
            CommandType: OwnershipBackfillCommandType.DryRun,
            RequiredArguments: ["--output"],
            OptionalArguments:
            [
                "--batch-size",
                "--max-unresolved-rate",
                "--connection-string",
                "--database-provider",
                "--include-legacy-unscoped"
            ],
            FlagArguments: [],
            SupportsHelp: true,
            ApplyEnabled: false,
            UsageSummary: "dry-run --output <path> [--batch-size <int>] [--max-unresolved-rate <double>] [--connection-string <connection-string>] [--database-provider <PostgreSQL|SQLite|None>] [--include-legacy-unscoped <true|false>]"),
        new(
            Name: "validate-evidence",
            CommandType: OwnershipBackfillCommandType.ValidateEvidence,
            RequiredArguments: ["--input", "--output"],
            OptionalArguments:
            [
                "--summary",
                "--max-total-unresolved-rate",
                "--max-project-unresolved-rate",
                "--max-scenario-unresolved-rate",
                "--max-job-unresolved-rate",
                "--max-ambiguous-records",
                "--fail-on-missing-record-type-metrics",
                "--fail-on-ambiguous-records",
                "--fail-on-schema-mismatch"
            ],
            FlagArguments: [],
            SupportsHelp: true,
            ApplyEnabled: false,
            UsageSummary: "validate-evidence --input <path> --output <path> [--summary <path>] [--max-total-unresolved-rate <double>] [--max-project-unresolved-rate <double>] [--max-scenario-unresolved-rate <double>] [--max-job-unresolved-rate <double>] [--max-ambiguous-records <int>] [--fail-on-missing-record-type-metrics <true|false>] [--fail-on-ambiguous-records <true|false>] [--fail-on-schema-mismatch <true|false>]"),
        new(
            Name: "plan-apply",
            CommandType: OwnershipBackfillCommandType.PlanApply,
            RequiredArguments: ["--evidence", "--gate-result", "--output"],
            OptionalArguments:
            [
                "--ruleset-version",
                "--max-planned-records",
                "--include-legacy-unscoped",
                "--force-overwrite"
            ],
            FlagArguments: [],
            SupportsHelp: true,
            ApplyEnabled: false,
            UsageSummary: "plan-apply --evidence <path> --gate-result <path> --output <path> [--ruleset-version <value>] [--max-planned-records <int>] [--include-legacy-unscoped <true|false>] [--force-overwrite <true|false>]"),
        new(
            Name: "signoff-plan",
            CommandType: OwnershipBackfillCommandType.SignoffPlan,
            RequiredArguments:
            [
                "--plan",
                "--expected-plan-hash",
                "--reviewer",
                "--ticket",
                "--output",
                "--confirm"
            ],
            OptionalArguments: ["--notes", "--expires-at", "--force-overwrite"],
            FlagArguments: [],
            SupportsHelp: true,
            ApplyEnabled: false,
            UsageSummary: "signoff-plan --plan <path> --expected-plan-hash <hash> --reviewer <name-or-id> --ticket <ticket> --output <path> --confirm I_REVIEWED_THE_OWNERSHIP_BACKFILL_PLAN [--notes <text>] [--expires-at <utc>] [--force-overwrite <true|false>]"),
        new(
            Name: "validate-apply-readiness",
            CommandType: OwnershipBackfillCommandType.ValidateApplyReadiness,
            RequiredArguments:
            [
                "--dry-run",
                "--gate-result",
                "--plan",
                "--signoff",
                "--previous-values",
                "--output"
            ],
            OptionalArguments:
            [
                "--max-signoff-age-hours",
                "--require-rollback-readiness",
                "--expected-plan-hash",
                "--ruleset-version"
            ],
            FlagArguments: [],
            SupportsHelp: true,
            ApplyEnabled: false,
            UsageSummary: "validate-apply-readiness --dry-run <path> --gate-result <path> --plan <path> --signoff <path> --previous-values <path> --output <path> [--max-signoff-age-hours <int>] [--require-rollback-readiness <true|false>] [--expected-plan-hash <hash>] [--ruleset-version <value>]"),
        new(
            Name: "validate-staging-preflight",
            CommandType: OwnershipBackfillCommandType.ValidateStagingPreflight,
            RequiredArguments:
            [
                "--environment",
                "--apply-input-hash",
                "--readiness-result",
                "--plan",
                "--signoff",
                "--backup-reference",
                "--rollback-readiness-reference",
                "--operator",
                "--schema-version"
            ],
            OptionalArguments: [],
            FlagArguments: ["--enable-staging-apply", "--confirm-no-production-connection"],
            SupportsHelp: true,
            ApplyEnabled: false,
            UsageSummary: "validate-staging-preflight --environment <Staging> --apply-input-hash <hash> --readiness-result <path> --plan <path> --signoff <path> --backup-reference <ref> --rollback-readiness-reference <ref> --operator <id> --schema-version <version> --enable-staging-apply --confirm-no-production-connection"),
        new(
            Name: "validate-staging-acceptance",
            CommandType: OwnershipBackfillCommandType.ValidateStagingAcceptance,
            RequiredArguments:
            [
                "--apply-result",
                "--post-apply-dry-run",
                "--post-apply-gate-result",
                "--tenant-isolation-result",
                "--regression-result",
                "--rollback-evidence",
                "--apply-input-hash",
                "--plan-hash",
                "--signoff-id",
                "--readiness-id",
                "--staging-preflight",
                "--operator",
                "--staging-change-id",
                "--output"
            ],
            OptionalArguments:
            [
                "--ruleset-version",
                "--max-post-apply-unresolved-rate",
                "--require-zero-failed-records",
                "--require-tenant-isolation-pass",
                "--require-regression-pass",
                "--require-rollback-evidence"
            ],
            FlagArguments: [],
            SupportsHelp: true,
            ApplyEnabled: false,
            UsageSummary: "validate-staging-acceptance --apply-result <path> --post-apply-dry-run <path> --post-apply-gate-result <path> --tenant-isolation-result <reference> --regression-result <reference> --rollback-evidence <reference> --apply-input-hash <hash> --plan-hash <hash> --signoff-id <id> --readiness-id <id> --staging-preflight <reference> --operator <id> --staging-change-id <id> --output <path>"),
        new(
            Name: "validate-production-promotion",
            CommandType: OwnershipBackfillCommandType.ValidateProductionPromotion,
            RequiredArguments:
            [
                "--staging-acceptance",
                "--production-dry-run",
                "--production-gate-result",
                "--production-plan",
                "--production-signoff",
                "--production-readiness",
                "--production-previous-values",
                "--production-change-request-id",
                "--output"
            ],
            OptionalArguments:
            [
                "--ruleset-version",
                "--max-staging-acceptance-age-hours",
                "--max-production-signoff-age-hours",
                "--require-separate-production-evidence",
                "--require-backup-reference",
                "--require-rollback-readiness"
            ],
            FlagArguments: [],
            SupportsHelp: true,
            ApplyEnabled: false,
            UsageSummary: "validate-production-promotion --staging-acceptance <path> --production-dry-run <path> --production-gate-result <path> --production-plan <path> --production-signoff <path> --production-readiness <path> --production-previous-values <path> --production-change-request-id <id> --output <path>"),
        new(
            Name: "apply",
            CommandType: OwnershipBackfillCommandType.Apply,
            RequiredArguments:
            [
                "--evidence",
                "--gate-result",
                "--plan",
                "--plan-signoff",
                "--output",
                "--database-provider",
                "--connection-string",
                "--confirm"
            ],
            OptionalArguments: ["--batch-size"],
            FlagArguments: ["--enable-apply"],
            SupportsHelp: true,
            ApplyEnabled: false,
            UsageSummary: "apply --evidence <path> --gate-result <path> --plan <path> --plan-signoff <path> --output <path> --database-provider <PostgreSQL|SQLite|None> --connection-string <connection-string> --enable-apply --confirm I_UNDERSTAND_THIS_WRITES_OWNERSHIP_METADATA (disabled in current stage)")
    ];

    private static readonly Dictionary<string, OwnershipBackfillCommandDescriptor> ByName =
        Descriptors.ToDictionary(
            descriptor => descriptor.Name,
            descriptor => descriptor,
            StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<OwnershipBackfillCommandDescriptor> All => Descriptors;

    public static IReadOnlyList<string> CommandNames => Descriptors.Select(descriptor => descriptor.Name).ToArray();

    public static bool TryGet(string commandName, out OwnershipBackfillCommandDescriptor descriptor) =>
        ByName.TryGetValue(commandName, out descriptor!);

    public static OwnershipBackfillCommandDescriptor Get(OwnershipBackfillCommandType commandType) =>
        Descriptors.Single(descriptor => descriptor.CommandType == commandType);
}
