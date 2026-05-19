using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Evidence;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Production;
using AssistantEngineer.Tools.OwnershipBackfill.Readiness;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;
using AssistantEngineer.Tools.OwnershipBackfill.Staging;
using AssistantEngineer.Tools.OwnershipBackfill.Staging.Acceptance;
using AssistantEngineer.Tools.OwnershipBackfill.Scanning;

namespace AssistantEngineer.Tools.OwnershipBackfill.Cli;

public sealed class OwnershipBackfillCli(
    OwnershipBackfillCommandLineParser parser,
    IOwnershipBackfillEvidenceWriter evidenceWriter,
    IOwnershipBackfillDryRunScanner noDataScanner,
    IOwnershipBackfillDryRunScanner databaseScanner,
    IOwnershipBackfillEvidenceLoader evidenceLoader,
    IOwnershipBackfillEvidenceGateEvaluator gateEvaluator,
    IOwnershipBackfillGateResultWriter gateResultWriter,
    OwnershipBackfillApplyPreconditionValidator applyPreconditionValidator,
    IOwnershipBackfillApplyPlanGenerator applyPlanGenerator,
    IOwnershipBackfillApplyPlanWriter applyPlanWriter,
    OwnershipBackfillPlanSignoffValidator planSignoffValidator,
    IOwnershipBackfillPlanSignoffWriter planSignoffWriter)
{
    private const string ApplyDisabledMessage = "Apply mode is designed but disabled in P6-04. No ownership metadata was written.";

    public async Task<int> ExecuteAsync(
        IReadOnlyList<string> args,
        TextWriter standardOutput,
        TextWriter standardError,
        CancellationToken cancellationToken)
    {
        var parseResult = parser.Parse(args);
        if (parseResult.ShowHelp)
        {
            await standardOutput.WriteLineAsync(BuildUsage());
            return 0;
        }

        if (!parseResult.IsSuccess || parseResult.CommandType is null)
        {
            if (!string.IsNullOrWhiteSpace(parseResult.ErrorMessage))
                await standardError.WriteLineAsync(parseResult.ErrorMessage);

            await standardError.WriteLineAsync("Use --help to view command usage.");
            return parseResult.ExitCode;
        }

        try
        {
            return parseResult.CommandType.Value switch
            {
                OwnershipBackfillCommandType.DryRun => await ExecuteDryRunAsync(parseResult, standardOutput, standardError, cancellationToken),
                OwnershipBackfillCommandType.ValidateEvidence => await ExecuteValidateEvidenceAsync(parseResult, standardOutput, standardError, cancellationToken),
                OwnershipBackfillCommandType.ValidateApplyReadiness => await ExecuteValidateApplyReadinessAsync(parseResult, standardOutput, standardError, cancellationToken),
                OwnershipBackfillCommandType.ValidateProductionPromotion => await ExecuteValidateProductionPromotionAsync(parseResult, standardOutput, standardError, cancellationToken),
                OwnershipBackfillCommandType.ValidateStagingPreflight => await ExecuteValidateStagingPreflightAsync(parseResult, standardOutput, standardError),
                OwnershipBackfillCommandType.ValidateStagingAcceptance => await ExecuteValidateStagingAcceptanceAsync(parseResult, standardOutput, standardError, cancellationToken),
                OwnershipBackfillCommandType.Apply => await ExecuteApplyDisabledAsync(parseResult, standardOutput, standardError),
                OwnershipBackfillCommandType.PlanApply => await ExecutePlanApplyAsync(parseResult, standardOutput, standardError, cancellationToken),
                OwnershipBackfillCommandType.SignoffPlan => await ExecuteSignoffPlanAsync(parseResult, standardOutput, standardError, cancellationToken),
                _ => 1
            };
        }
        catch (OwnershipBackfillPlanGateFailedException exception)
        {
            await standardError.WriteLineAsync(exception.Message);
            return 2;
        }
        catch (OperationCanceledException)
        {
            await standardError.WriteLineAsync("Operation cancelled.");
            return 1;
        }
        catch (Exception exception)
        {
            await standardError.WriteLineAsync($"Command failed: {exception.Message}");
            return 1;
        }
    }

    public static string BuildUsage()
    {
        return string.Join(
            Environment.NewLine,
        [
            "AssistantEngineer Ownership Backfill Tool",
            string.Empty,
            "Usage:",
            "  dry-run --output <directory> [--batch-size <int>] [--max-unresolved-rate <double>] [--connection-string <value>] [--database-provider <PostgreSQL|SQLite|None>] [--include-legacy-unscoped <true|false>]",
            "  validate-evidence --input <directory> --output <directory> [--summary <path>] [--max-total-unresolved-rate <double>] [--max-project-unresolved-rate <double>] [--max-scenario-unresolved-rate <double>] [--max-job-unresolved-rate <double>] [--max-ambiguous-records <int>] [--fail-on-missing-record-type-metrics <true|false>] [--fail-on-ambiguous-records <true|false>] [--fail-on-schema-mismatch <true|false>]",
            "  validate-apply-readiness --dry-run <dry-run-summary-json> --gate-result <gate-result-json> --plan <apply-plan-json> --signoff <plan-signoff-json> --previous-values <previous-values-json> --output <readiness-output-dir> [--max-signoff-age-hours <int>] [--require-rollback-readiness <true|false>] [--expected-plan-hash <hash>] [--ruleset-version <value>]",
            "  validate-production-promotion --staging-acceptance <path> --production-dry-run <path> --production-gate-result <path> --production-plan <path> --production-signoff <path> --production-readiness <path> --production-previous-values <path> --production-change-request-id <id> --output <directory> [--max-staging-acceptance-age-hours <int>] [--max-production-signoff-age-hours <int>] [--ruleset-version <value>] [--require-separate-production-evidence <true|false>] [--require-backup-reference <true|false>] [--require-rollback-readiness <true|false>]",
            "  validate-staging-preflight --environment <Staging> --apply-input-hash <hash> --readiness-result <path> --plan <path> --signoff <path> --backup-reference <ref> --rollback-readiness-reference <ref> --operator <id> --schema-version <version> --enable-staging-apply --confirm-no-production-connection",
            "  validate-staging-acceptance --apply-result <path> --post-apply-dry-run <path> --post-apply-gate-result <path> --tenant-isolation-result <reference> --regression-result <reference> --rollback-evidence <reference> --apply-input-hash <hash> --plan-hash <hash> --signoff-id <id> --readiness-id <id> --staging-preflight <reference> --operator <id> --staging-change-id <id> --output <directory> [--max-post-apply-unresolved-rate <double>] [--ruleset-version <value>] [--require-zero-failed-records <true|false>] [--require-rollback-evidence <true|false>] [--require-tenant-isolation-pass <true|false>] [--require-regression-pass <true|false>]",
            "  plan-apply --evidence <dry-run-dir> --gate-result <gate-result-json> --output <plan-output-dir> [--ruleset-version <value>] [--max-planned-records <int>] [--include-legacy-unscoped <true|false>] [--force-overwrite <true|false>]",
            "  signoff-plan --plan <plan-json> --expected-plan-hash <hash> --reviewer <name-or-id> --ticket <ticket> --output <signoff-output-dir> --confirm I_REVIEWED_THE_OWNERSHIP_BACKFILL_PLAN [--notes <text>] [--expires-at <utc>] [--force-overwrite <true|false>]",
            "  apply --evidence <dry-run-dir> --gate-result <gate-result-json> --plan <plan-json> --plan-signoff <signoff-json> --output <apply-output-dir> --database-provider <PostgreSQL|SQLite|None> --connection-string <value> --enable-apply --confirm I_UNDERSTAND_THIS_WRITES_OWNERSHIP_METADATA [--batch-size <int>] (designed but disabled in P6-04)",
            "  --help",
            string.Empty,
            "Exit codes:",
            "  0 - success",
            "  1 - invalid command/input or execution error; apply remains disabled",
            "  2 - governance validation failed (for example evidence gate, hash mismatch, or staging acceptance rejection)",
            string.Empty,
            "Defaults:",
            $"  --batch-size {OwnershipBackfillConstants.DefaultBatchSize}",
            $"  --max-unresolved-rate {OwnershipBackfillConstants.DefaultMaxUnresolvedRate.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}",
            "  --database-provider None",
            "  --include-legacy-unscoped false",
            "  --ruleset-version P6-05",
            "  --max-total-unresolved-rate 0.05",
            "  --max-project-unresolved-rate 0.0",
            "  --max-scenario-unresolved-rate 0.05",
            "  --max-job-unresolved-rate 0.10",
            "  --max-ambiguous-records 0",
            "  --max-signoff-age-hours 24",
            "  --max-staging-acceptance-age-hours 72",
            "  --max-production-signoff-age-hours 24",
            "  --require-rollback-readiness true",
            "  --max-post-apply-unresolved-rate 0.01",
            "  --require-zero-failed-records true",
            "  --require-rollback-evidence true",
            "  --require-tenant-isolation-pass true",
            "  --require-regression-pass true"
        ]);
    }

    private async Task<int> ExecuteDryRunAsync(
        OwnershipBackfillCommandLineParseResult parseResult,
        TextWriter standardOutput,
        TextWriter standardError,
        CancellationToken cancellationToken)
    {
        if (parseResult.Options is null)
        {
            await standardError.WriteLineAsync("dry-run options are missing.");
            return 1;
        }

        var options = parseResult.Options;
        var scanner = options.NoDataDryRun ? noDataScanner : databaseScanner;
        var result = await scanner.ScanAsync(options, cancellationToken);
        await evidenceWriter.WriteAsync(result, options.EvidenceOutputDirectory, cancellationToken);

        await standardOutput.WriteLineAsync("Ownership backfill dry-run completed.");
        await standardOutput.WriteLineAsync($"Evidence output directory: {Path.GetFullPath(options.EvidenceOutputDirectory)}");
        return 0;
    }

    private async Task<int> ExecuteValidateEvidenceAsync(
        OwnershipBackfillCommandLineParseResult parseResult,
        TextWriter standardOutput,
        TextWriter standardError,
        CancellationToken cancellationToken)
    {
        if (parseResult.GateOptions is null)
        {
            await standardError.WriteLineAsync("validate-evidence options are missing.");
            return 1;
        }

        var options = parseResult.GateOptions;
        var evidence = await evidenceLoader.LoadAsync(options, cancellationToken);
        var gateResult = gateEvaluator.Evaluate(evidence, options);

        await gateResultWriter.WriteAsync(gateResult, options.OutputDirectory, cancellationToken);
        await standardOutput.WriteLineAsync(gateResult.Summary);
        await standardOutput.WriteLineAsync($"Gate output directory: {Path.GetFullPath(options.OutputDirectory)}");

        return gateResult.Passed ? 0 : 2;
    }

    private static async Task<int> ExecuteValidateApplyReadinessAsync(
        OwnershipBackfillCommandLineParseResult parseResult,
        TextWriter standardOutput,
        TextWriter standardError,
        CancellationToken cancellationToken)
    {
        if (parseResult.ApplyReadinessOptions is null)
        {
            await standardError.WriteLineAsync("validate-apply-readiness options are missing.");
            return 1;
        }

        var validator = new OwnershipBackfillApplyReadinessValidator();
        var writer = new OwnershipBackfillApplyReadinessResultWriter();

        var validation = await validator.ValidateAsync(parseResult.ApplyReadinessOptions, cancellationToken);
        await writer.WriteAsync(validation.Result, parseResult.ApplyReadinessOptions.OutputDirectory!, cancellationToken);

        await standardOutput.WriteLineAsync(validation.Result.Passed
            ? "Ownership backfill apply readiness validation passed."
            : "Ownership backfill apply readiness validation failed.");
        await standardOutput.WriteLineAsync($"ReadinessId: {validation.Result.ReadinessId}");
        await standardOutput.WriteLineAsync($"PlanHash: {validation.Result.PlanHash}");
        await standardOutput.WriteLineAsync($"ApplyInputHash: {validation.Result.ApplyInputHash}");
        await standardOutput.WriteLineAsync($"Readiness output directory: {Path.GetFullPath(parseResult.ApplyReadinessOptions.OutputDirectory!)}");
        await standardOutput.WriteLineAsync("Apply mode remains disabled in this stage.");

        return validation.ExitCode;
    }

    private static async Task<int> ExecuteValidateProductionPromotionAsync(
        OwnershipBackfillCommandLineParseResult parseResult,
        TextWriter standardOutput,
        TextWriter standardError,
        CancellationToken cancellationToken)
    {
        if (parseResult.ProductionPromotionOptions is null)
        {
            await standardError.WriteLineAsync("validate-production-promotion options are missing.");
            return 1;
        }

        var validator = new OwnershipBackfillProductionPromotionValidator();
        var writer = new OwnershipBackfillProductionPromotionDecisionWriter();

        var validation = await validator.ValidateAsync(parseResult.ProductionPromotionOptions, cancellationToken);
        await writer.WriteAsync(validation.Decision, parseResult.ProductionPromotionOptions.OutputDirectory!, cancellationToken);

        await standardOutput.WriteLineAsync(validation.Decision.Ready
            ? "Ownership backfill production promotion readiness validation passed."
            : "Ownership backfill production promotion readiness validation failed.");
        await standardOutput.WriteLineAsync($"DecisionStatus: {validation.Decision.DecisionStatus}");
        await standardOutput.WriteLineAsync($"DecisionId: {validation.Decision.DecisionId}");
        await standardOutput.WriteLineAsync($"ProductionPromotionHash: {validation.Decision.ProductionPromotionHash}");
        await standardOutput.WriteLineAsync($"Production promotion output directory: {Path.GetFullPath(parseResult.ProductionPromotionOptions.OutputDirectory!)}");
        await standardOutput.WriteLineAsync("Production apply remains disabled in this stage.");

        return validation.ExitCode;
    }

    private static async Task<int> ExecuteValidateStagingPreflightAsync(
        OwnershipBackfillCommandLineParseResult parseResult,
        TextWriter standardOutput,
        TextWriter standardError)
    {
        if (parseResult.StagingPreflightOptions is null)
        {
            await standardError.WriteLineAsync("validate-staging-preflight options are missing.");
            return 1;
        }

        var validator = new OwnershipBackfillStagingApplyPreflightValidator();
        var disabledExecutor = new DisabledStagingOwnershipBackfillApplyExecutor();
        var validation = validator.Validate(parseResult.StagingPreflightOptions);

        foreach (var finding in validation.Findings)
            await standardError.WriteLineAsync($"[{finding.Severity}] {finding.Code}: {finding.Message}");

        var disabled = await disabledExecutor.ExecuteAsync(parseResult.StagingPreflightOptions, CancellationToken.None);
        await standardOutput.WriteLineAsync(disabled.Message);

        if (!validation.Passed)
            return 2;

        await standardOutput.WriteLineAsync("Staging apply preflight validation passed.");
        return 0;
    }

    private static async Task<int> ExecuteValidateStagingAcceptanceAsync(
        OwnershipBackfillCommandLineParseResult parseResult,
        TextWriter standardOutput,
        TextWriter standardError,
        CancellationToken cancellationToken)
    {
        if (parseResult.StagingAcceptanceOptions is null)
        {
            await standardError.WriteLineAsync("validate-staging-acceptance options are missing.");
            return 1;
        }

        var validator = new OwnershipBackfillStagingAcceptanceValidator();
        var writer = new OwnershipBackfillStagingAcceptanceResultWriter();

        var validation = await validator.ValidateAsync(parseResult.StagingAcceptanceOptions, cancellationToken);
        await writer.WriteAsync(validation.Result, parseResult.StagingAcceptanceOptions.OutputDirectory!, cancellationToken);

        await standardOutput.WriteLineAsync(validation.Result.Accepted
            ? "Ownership backfill staging acceptance validation passed."
            : "Ownership backfill staging acceptance validation failed.");
        await standardOutput.WriteLineAsync($"AcceptanceId: {validation.Result.AcceptanceId}");
        await standardOutput.WriteLineAsync($"StagingRunHash: {validation.Result.StagingRunHash}");
        await standardOutput.WriteLineAsync($"Staging acceptance output directory: {Path.GetFullPath(parseResult.StagingAcceptanceOptions.OutputDirectory!)}");
        await standardOutput.WriteLineAsync("Staging apply and production apply remain disabled.");

        return validation.ExitCode;
    }

    private async Task<int> ExecuteApplyDisabledAsync(
        OwnershipBackfillCommandLineParseResult parseResult,
        TextWriter standardOutput,
        TextWriter standardError)
    {
        if (parseResult.ApplyOptions is null)
        {
            await standardError.WriteLineAsync("apply options are missing.");
            await standardError.WriteLineAsync(ApplyDisabledMessage);
            return 1;
        }

        var validation = applyPreconditionValidator.Validate(parseResult.ApplyOptions);
        if (!validation.Passed)
        {
            foreach (var finding in validation.Findings)
            {
                await standardError.WriteLineAsync($"[{finding.Severity}] {finding.Code}: {finding.Message}");
            }
        }

        await standardError.WriteLineAsync(ApplyDisabledMessage);
        await standardOutput.WriteLineAsync(ApplyDisabledMessage);
        return 1;
    }

    private async Task<int> ExecutePlanApplyAsync(
        OwnershipBackfillCommandLineParseResult parseResult,
        TextWriter standardOutput,
        TextWriter standardError,
        CancellationToken cancellationToken)
    {
        if (parseResult.PlanOptions is null)
        {
            await standardError.WriteLineAsync("plan-apply options are missing.");
            return 1;
        }

        var planResult = await applyPlanGenerator.GenerateAsync(parseResult.PlanOptions, cancellationToken);
        await applyPlanWriter.WriteAsync(
            planResult,
            parseResult.PlanOptions.OutputDirectory!,
            parseResult.PlanOptions.ForceOverwrite,
            cancellationToken);

        await standardOutput.WriteLineAsync("Ownership backfill apply plan draft generated.");
        await standardOutput.WriteLineAsync($"PlanId: {planResult.PlanId}");
        await standardOutput.WriteLineAsync($"PlanHash: {planResult.PlanHash}");
        await standardOutput.WriteLineAsync($"Plan output directory: {Path.GetFullPath(parseResult.PlanOptions.OutputDirectory!)}");
        await standardOutput.WriteLineAsync("Apply mode remains disabled in this stage.");
        return 0;
    }

    private async Task<int> ExecuteSignoffPlanAsync(
        OwnershipBackfillCommandLineParseResult parseResult,
        TextWriter standardOutput,
        TextWriter standardError,
        CancellationToken cancellationToken)
    {
        if (parseResult.SignoffOptions is null)
        {
            await standardError.WriteLineAsync("signoff-plan options are missing.");
            return 1;
        }

        var validation = planSignoffValidator.Validate(parseResult.SignoffOptions);
        if (!validation.Passed || validation.Artifact is null)
        {
            foreach (var finding in validation.Findings)
                await standardError.WriteLineAsync($"[{finding.Severity}] {finding.Code}: {finding.Message}");

            return validation.ExitCode;
        }

        await planSignoffWriter.WriteAsync(
            validation.Artifact,
            parseResult.SignoffOptions.OutputDirectory!,
            parseResult.SignoffOptions.ForceOverwrite,
            cancellationToken);

        await standardOutput.WriteLineAsync("Ownership backfill plan signoff created.");
        await standardOutput.WriteLineAsync($"SignoffId: {validation.Artifact.SignoffId}");
        await standardOutput.WriteLineAsync($"PlanHash: {validation.Artifact.PlanHash}");
        await standardOutput.WriteLineAsync($"Signoff output directory: {Path.GetFullPath(parseResult.SignoffOptions.OutputDirectory!)}");
        await standardOutput.WriteLineAsync("Apply mode remains disabled in this stage.");
        return 0;
    }
}
