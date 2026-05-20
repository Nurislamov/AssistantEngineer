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
            await standardOutput.WriteLineAsync(OwnershipBackfillHelpText.Build(args));
            return OwnershipBackfillExitCodes.Success;
        }

        if (!parseResult.IsSuccess || parseResult.CommandType is null)
        {
            if (!string.IsNullOrWhiteSpace(parseResult.ErrorMessage))
                await standardError.WriteLineAsync(OwnershipBackfillConsoleRedactor.RedactText(parseResult.ErrorMessage));

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
                _ => OwnershipBackfillExitCodes.InvalidInput
            };
        }
        catch (OwnershipBackfillPlanGateFailedException exception)
        {
            await standardError.WriteLineAsync(OwnershipBackfillConsoleRedactor.RedactText(exception.Message));
            return OwnershipBackfillExitCodes.ValidationFailed;
        }
        catch (OperationCanceledException)
        {
            await standardError.WriteLineAsync("Operation cancelled.");
            return OwnershipBackfillExitCodes.InvalidInput;
        }
        catch (Exception exception)
        {
            await standardError.WriteLineAsync($"Command failed: {OwnershipBackfillConsoleRedactor.RedactText(exception.Message)}");
            return OwnershipBackfillExitCodes.InvalidInput;
        }
    }

    public static string BuildUsage()
    {
        return OwnershipBackfillHelpText.Build();
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
            return OwnershipBackfillExitCodes.InvalidInput;
        }

        var options = parseResult.Options;
        var scanner = options.NoDataDryRun ? noDataScanner : databaseScanner;
        var result = await scanner.ScanAsync(options, cancellationToken);
        await evidenceWriter.WriteAsync(result, options.EvidenceOutputDirectory, cancellationToken);

        await standardOutput.WriteLineAsync("Ownership backfill dry-run completed.");
        await standardOutput.WriteLineAsync($"Evidence output directory: {Path.GetFullPath(options.EvidenceOutputDirectory)}");
        return OwnershipBackfillExitCodes.Success;
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
            return OwnershipBackfillExitCodes.InvalidInput;
        }

        var options = parseResult.GateOptions;
        var evidence = await evidenceLoader.LoadAsync(options, cancellationToken);
        var gateResult = gateEvaluator.Evaluate(evidence, options);

        await gateResultWriter.WriteAsync(gateResult, options.OutputDirectory, cancellationToken);
        await standardOutput.WriteLineAsync(gateResult.Summary);
        await standardOutput.WriteLineAsync($"Gate output directory: {Path.GetFullPath(options.OutputDirectory)}");

        return gateResult.Passed ? OwnershipBackfillExitCodes.Success : OwnershipBackfillExitCodes.ValidationFailed;
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
            return OwnershipBackfillExitCodes.InvalidInput;
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
            return OwnershipBackfillExitCodes.InvalidInput;
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
            return OwnershipBackfillExitCodes.InvalidInput;
        }

        var validator = new OwnershipBackfillStagingApplyPreflightValidator();
        var disabledExecutor = new DisabledStagingOwnershipBackfillApplyExecutor();
        var validation = validator.Validate(parseResult.StagingPreflightOptions);

        foreach (var finding in validation.Findings)
            await standardError.WriteLineAsync($"[{finding.Severity}] {finding.Code}: {finding.Message}");

        var disabled = await disabledExecutor.ExecuteAsync(parseResult.StagingPreflightOptions, CancellationToken.None);
        await standardOutput.WriteLineAsync(disabled.Message);

        if (!validation.Passed)
            return OwnershipBackfillExitCodes.ValidationFailed;

        await standardOutput.WriteLineAsync("Staging apply preflight validation passed.");
        return OwnershipBackfillExitCodes.Success;
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
            return OwnershipBackfillExitCodes.InvalidInput;
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
            return OwnershipBackfillExitCodes.InvalidInput;
        }

        var validation = applyPreconditionValidator.Validate(parseResult.ApplyOptions);
        if (!validation.Passed)
        {
            foreach (var finding in validation.Findings)
            {
                await standardError.WriteLineAsync(
                    OwnershipBackfillConsoleRedactor.RedactText($"[{finding.Severity}] {finding.Code}: {finding.Message}"));
            }
        }

        await standardError.WriteLineAsync(ApplyDisabledMessage);
        await standardOutput.WriteLineAsync(ApplyDisabledMessage);
        return OwnershipBackfillExitCodes.InvalidInput;
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
            return OwnershipBackfillExitCodes.InvalidInput;
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
        return OwnershipBackfillExitCodes.Success;
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
            return OwnershipBackfillExitCodes.InvalidInput;
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
        return OwnershipBackfillExitCodes.Success;
    }
}
