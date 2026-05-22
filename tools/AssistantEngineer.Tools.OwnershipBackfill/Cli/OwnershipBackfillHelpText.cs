using System.Globalization;
using AssistantEngineer.Tools.OwnershipBackfill.Models;

namespace AssistantEngineer.Tools.OwnershipBackfill.Cli;

public static class OwnershipBackfillHelpText
{
    private static readonly string[] CommandNames = OwnershipBackfillCommandDescriptorCatalog.CommandNames.ToArray();

    public static string Build(IReadOnlyList<string>? args = null)
    {
        var commandHint = TryGetCommandHint(args);

        var lines = new List<string>
        {
            "AssistantEngineer Ownership Backfill Tool",
            string.Empty,
            "Usage:",
            "  dry-run --output <path> [--batch-size <int>] [--max-unresolved-rate <double>] [--connection-string <connection-string>] [--database-provider <PostgreSQL|SQLite|None>] [--include-legacy-unscoped <true|false>]",
            "  validate-evidence --input <path> --output <path> [--summary <path>] [--max-total-unresolved-rate <double>] [--max-project-unresolved-rate <double>] [--max-scenario-unresolved-rate <double>] [--max-job-unresolved-rate <double>] [--max-ambiguous-records <int>] [--fail-on-missing-record-type-metrics <true|false>] [--fail-on-ambiguous-records <true|false>] [--fail-on-schema-mismatch <true|false>]",
            "  plan-apply --evidence <path> --gate-result <path> --output <path> [--ruleset-version <value>] [--max-planned-records <int>] [--include-legacy-unscoped <true|false>] [--force-overwrite <true|false>]",
            "  signoff-plan --plan <path> --expected-plan-hash <hash> --reviewer <name-or-id> --ticket <ticket> --output <path> --confirm I_REVIEWED_THE_OWNERSHIP_BACKFILL_PLAN [--notes <text>] [--expires-at <utc>] [--force-overwrite <true|false>]",
            "  validate-apply-readiness --dry-run <path> --gate-result <path> --plan <path> --signoff <path> --previous-values <path> --output <path> [--max-signoff-age-hours <int>] [--require-rollback-readiness <true|false>] [--expected-plan-hash <hash>] [--ruleset-version <value>]",
            "  validate-staging-preflight --environment <Staging> --apply-input-hash <hash> --readiness-result <path> --plan <path> --signoff <path> --backup-reference <ref> --rollback-readiness-reference <ref> --operator <id> --schema-version <version> --enable-staging-apply --confirm-no-production-connection",
            "  validate-staging-acceptance --apply-result <path> --post-apply-dry-run <path> --post-apply-gate-result <path> --tenant-isolation-result <reference> --regression-result <reference> --rollback-evidence <reference> --apply-input-hash <hash> --plan-hash <hash> --signoff-id <id> --readiness-id <id> --staging-preflight <reference> --operator <id> --staging-change-id <id> --output <path>",
            "  validate-production-promotion --staging-acceptance <path> --production-dry-run <path> --production-gate-result <path> --production-plan <path> --production-signoff <path> --production-readiness <path> --production-previous-values <path> --production-change-request-id <id> --output <path>",
            "  apply --evidence <path> --gate-result <path> --plan <path> --plan-signoff <path> --output <path> --database-provider <PostgreSQL|SQLite|None> --connection-string <connection-string> --enable-apply --confirm I_UNDERSTAND_THIS_WRITES_OWNERSHIP_METADATA (disabled in current stage)",
            "  --help",
            string.Empty,
            "Commands:",
            $"  {string.Join(", ", CommandNames)}",
            string.Empty,
            "Exit codes:",
            $"  {OwnershipBackfillExitCodes.Success} - success",
            $"  {OwnershipBackfillExitCodes.InvalidInput} - invalid command/input or disabled command boundary",
            $"  {OwnershipBackfillExitCodes.ValidationFailed} - governance validation failed",
            string.Empty,
            "Safety:",
            "  - apply is intentionally disabled (no ownership metadata writes).",
            "  - commands do not execute destructive SQL.",
            "  - connection strings and secret-like values are redacted in console output.",
            "  - generated artifacts are intended for ignored artifact directories (for example artifacts/ownership-backfill/).",
            string.Empty,
            "Examples (placeholder values only):",
            "  dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- dry-run --output <path>",
            "  dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- validate-evidence --input <path> --output <path>",
            "  dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- plan-apply --evidence <path> --gate-result <path> --output <path>",
            "  dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- signoff-plan --plan <path> --expected-plan-hash <hash> --reviewer <name-or-id> --ticket <ticket> --output <path> --confirm I_REVIEWED_THE_OWNERSHIP_BACKFILL_PLAN",
            "  dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- validate-apply-readiness --dry-run <path> --gate-result <path> --plan <path> --signoff <path> --previous-values <path> --output <path>",
            "  dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- validate-staging-preflight --environment Staging --apply-input-hash <hash> --readiness-result <path> --plan <path> --signoff <path> --backup-reference <ref> --rollback-readiness-reference <ref> --operator <id> --schema-version <version> --enable-staging-apply --confirm-no-production-connection",
            "  dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- validate-staging-acceptance --apply-result <path> --post-apply-dry-run <path> --post-apply-gate-result <path> --tenant-isolation-result <reference> --regression-result <reference> --rollback-evidence <reference> --apply-input-hash <hash> --plan-hash <hash> --signoff-id <id> --readiness-id <id> --staging-preflight <reference> --operator <id> --staging-change-id <id> --output <path>",
            "  dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- validate-production-promotion --staging-acceptance <path> --production-dry-run <path> --production-gate-result <path> --production-plan <path> --production-signoff <path> --production-readiness <path> --production-previous-values <path> --production-change-request-id <id> --output <path>",
            "  dotnet run --project tools/AssistantEngineer.Tools.OwnershipBackfill -- apply --enable-apply --confirm I_UNDERSTAND_THIS_WRITES_OWNERSHIP_METADATA --evidence <path> --gate-result <path> --plan <path> --plan-signoff <path> --output <path> --database-provider SQLite --connection-string <connection-string>",
            string.Empty,
            "Defaults:",
            $"  --batch-size {OwnershipBackfillConstants.DefaultBatchSize}",
            $"  --max-unresolved-rate {OwnershipBackfillConstants.DefaultMaxUnresolvedRate.ToString("0.00", CultureInfo.InvariantCulture)}",
            "  --database-provider None",
            "  --include-legacy-unscoped false"
        };

        if (!string.IsNullOrWhiteSpace(commandHint))
        {
            lines.Insert(2, $"Command-specific help requested for: {commandHint}");
            lines.Insert(3, string.Empty);
        }

        return string.Join(Environment.NewLine, lines);
    }

    public static string BuildUnknownCommandMessage(string command)
    {
        var safeCommand = OwnershipBackfillConsoleRedactor.RedactText(command);
        return $"Unknown command: {safeCommand}. Available commands: {string.Join(", ", CommandNames)}.";
    }

    private static string? TryGetCommandHint(IReadOnlyList<string>? args)
    {
        if (args is null || args.Count < 2)
            return null;

        var candidate = args[0];
        var isCommandSpecificHelp =
            string.Equals(args[1], "--help", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(args[1], "-h", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(args[1], "help", StringComparison.OrdinalIgnoreCase);

        if (!isCommandSpecificHelp)
            return null;

        return CommandNames.Contains(candidate, StringComparer.OrdinalIgnoreCase) ? candidate : null;
    }
}
