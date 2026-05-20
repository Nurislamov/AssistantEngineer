using System.Globalization;
using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Production;
using AssistantEngineer.Tools.OwnershipBackfill.Readiness;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;
using AssistantEngineer.Tools.OwnershipBackfill.Staging;
using AssistantEngineer.Tools.OwnershipBackfill.Staging.Acceptance;

namespace AssistantEngineer.Tools.OwnershipBackfill.Cli;

public sealed class OwnershipBackfillCommandLineParser
{
    public OwnershipBackfillCommandLineParseResult Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0 || IsHelpToken(args[0]))
            return OwnershipBackfillCommandLineParseResult.Help();

        var command = args[0];
        var commandSpecificHelp = args.Count >= 2 && IsHelpToken(args[1]);

        if (string.Equals(command, "dry-run", StringComparison.OrdinalIgnoreCase))
        {
            if (commandSpecificHelp)
                return OwnershipBackfillCommandLineParseResult.Help(OwnershipBackfillCommandType.DryRun);
            return ParseDryRunOptions(args.Skip(1).ToArray());
        }

        if (string.Equals(command, "validate-evidence", StringComparison.OrdinalIgnoreCase))
        {
            if (commandSpecificHelp)
                return OwnershipBackfillCommandLineParseResult.Help(OwnershipBackfillCommandType.ValidateEvidence);
            return ParseValidateEvidenceOptions(args.Skip(1).ToArray());
        }

        if (string.Equals(command, "validate-apply-readiness", StringComparison.OrdinalIgnoreCase))
        {
            if (commandSpecificHelp)
                return OwnershipBackfillCommandLineParseResult.Help(OwnershipBackfillCommandType.ValidateApplyReadiness);
            return ParseValidateApplyReadinessOptions(args.Skip(1).ToArray());
        }

        if (string.Equals(command, "validate-production-promotion", StringComparison.OrdinalIgnoreCase))
        {
            if (commandSpecificHelp)
                return OwnershipBackfillCommandLineParseResult.Help(OwnershipBackfillCommandType.ValidateProductionPromotion);
            return ParseValidateProductionPromotionOptions(args.Skip(1).ToArray());
        }

        if (string.Equals(command, "validate-staging-preflight", StringComparison.OrdinalIgnoreCase))
        {
            if (commandSpecificHelp)
                return OwnershipBackfillCommandLineParseResult.Help(OwnershipBackfillCommandType.ValidateStagingPreflight);
            return ParseValidateStagingPreflightOptions(args.Skip(1).ToArray());
        }

        if (string.Equals(command, "validate-staging-acceptance", StringComparison.OrdinalIgnoreCase))
        {
            if (commandSpecificHelp)
                return OwnershipBackfillCommandLineParseResult.Help(OwnershipBackfillCommandType.ValidateStagingAcceptance);
            return ParseValidateStagingAcceptanceOptions(args.Skip(1).ToArray());
        }

        if (string.Equals(command, "apply", StringComparison.OrdinalIgnoreCase))
        {
            if (commandSpecificHelp)
                return OwnershipBackfillCommandLineParseResult.Help(OwnershipBackfillCommandType.Apply);
            return ParseApplyOptions(args.Skip(1).ToArray());
        }

        if (string.Equals(command, "plan-apply", StringComparison.OrdinalIgnoreCase))
        {
            if (commandSpecificHelp)
                return OwnershipBackfillCommandLineParseResult.Help(OwnershipBackfillCommandType.PlanApply);
            return ParsePlanApplyOptions(args.Skip(1).ToArray());
        }

        if (string.Equals(command, "signoff-plan", StringComparison.OrdinalIgnoreCase))
        {
            if (commandSpecificHelp)
                return OwnershipBackfillCommandLineParseResult.Help(OwnershipBackfillCommandType.SignoffPlan);
            return ParseSignoffPlanOptions(args.Skip(1).ToArray());
        }

        return OwnershipBackfillCommandLineParseResult.Failure(
            OwnershipBackfillHelpText.BuildUnknownCommandMessage(command),
            exitCode: OwnershipBackfillExitCodes.InvalidInput);
    }

    private static OwnershipBackfillCommandLineParseResult ParseDryRunOptions(IReadOnlyList<string> args)
    {
        string? outputDirectory = null;
        var batchSize = OwnershipBackfillConstants.DefaultBatchSize;
        var maxUnresolvedRate = OwnershipBackfillConstants.DefaultMaxUnresolvedRate;
        string? connectionString = null;
        var databaseProvider = "None";
        var includeLegacyUnscoped = false;

        for (var index = 0; index < args.Count; index++)
        {
            var arg = args[index];

            if (IsHelpToken(arg))
                return OwnershipBackfillCommandLineParseResult.Help();

            if (string.Equals(arg, "--output", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--output requires a value.");

                outputDirectory = value;
                continue;
            }

            if (string.Equals(arg, "--batch-size", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--batch-size requires a value.");

                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out batchSize) || batchSize <= 0)
                    return OwnershipBackfillCommandLineParseResult.Failure("--batch-size must be a positive integer.");

                continue;
            }

            if (string.Equals(arg, "--max-unresolved-rate", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--max-unresolved-rate requires a value.");

                if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out maxUnresolvedRate) ||
                    maxUnresolvedRate < 0d ||
                    maxUnresolvedRate > 1d)
                {
                    return OwnershipBackfillCommandLineParseResult.Failure("--max-unresolved-rate must be a number between 0 and 1.");
                }

                continue;
            }

            if (string.Equals(arg, "--connection-string", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--connection-string requires a value.");

                connectionString = value;
                continue;
            }

            if (string.Equals(arg, "--database-provider", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--database-provider requires a value.");

                if (!OwnershipBackfillConstants.SupportedDatabaseProviders.Contains(value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--database-provider must be one of: PostgreSQL, SQLite, None.");

                databaseProvider = CanonicalizeDatabaseProvider(value);
                continue;
            }

            if (string.Equals(arg, "--include-legacy-unscoped", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--include-legacy-unscoped requires true or false.");

                if (!bool.TryParse(value, out includeLegacyUnscoped))
                    return OwnershipBackfillCommandLineParseResult.Failure("--include-legacy-unscoped must be true or false.");

                continue;
            }

            return OwnershipBackfillCommandLineParseResult.Failure($"Unknown option: {arg}");
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
            return OwnershipBackfillCommandLineParseResult.Failure("dry-run requires --output <directory>.");

        var isNoneProvider = string.Equals(databaseProvider, "None", StringComparison.OrdinalIgnoreCase);
        if (!isNoneProvider && string.IsNullOrWhiteSpace(connectionString))
        {
            return OwnershipBackfillCommandLineParseResult.Failure(
                "--connection-string is required when --database-provider is SQLite or PostgreSQL.");
        }

        var options = new OwnershipBackfillOptions(
            BatchSize: batchSize,
            MaxUnresolvedRate: maxUnresolvedRate,
            EvidenceOutputDirectory: outputDirectory,
            ConnectionString: connectionString,
            DatabaseProvider: databaseProvider,
            IncludeLegacyUnscoped: includeLegacyUnscoped,
            NoDataDryRun: isNoneProvider);

        return OwnershipBackfillCommandLineParseResult.DryRunSuccess(options);
    }

    private static OwnershipBackfillCommandLineParseResult ParseValidateEvidenceOptions(IReadOnlyList<string> args)
    {
        string? inputDirectory = null;
        string? outputDirectory = null;
        string? summaryPath = null;

        var maxTotalUnresolvedRate = 0.05d;
        var maxProjectUnresolvedRate = 0d;
        var maxScenarioUnresolvedRate = 0.05d;
        var maxJobUnresolvedRate = 0.10d;
        var maxAmbiguousRecords = 0;
        var failOnMissingRecordTypeMetrics = true;
        var failOnAmbiguousRecords = true;
        var failOnSchemaMismatch = true;

        for (var index = 0; index < args.Count; index++)
        {
            var arg = args[index];
            if (IsHelpToken(arg))
                return OwnershipBackfillCommandLineParseResult.Help();

            if (string.Equals(arg, "--input", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--input requires a value.");

                inputDirectory = value;
                continue;
            }

            if (string.Equals(arg, "--output", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--output requires a value.");

                outputDirectory = value;
                continue;
            }

            if (string.Equals(arg, "--summary", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--summary requires a value.");

                summaryPath = value;
                continue;
            }

            if (string.Equals(arg, "--max-total-unresolved-rate", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadDouble(args, ref index, out maxTotalUnresolvedRate))
                    return OwnershipBackfillCommandLineParseResult.Failure("--max-total-unresolved-rate must be a number between 0 and 1.");

                continue;
            }

            if (string.Equals(arg, "--max-project-unresolved-rate", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadDouble(args, ref index, out maxProjectUnresolvedRate))
                    return OwnershipBackfillCommandLineParseResult.Failure("--max-project-unresolved-rate must be a number between 0 and 1.");

                continue;
            }

            if (string.Equals(arg, "--max-scenario-unresolved-rate", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadDouble(args, ref index, out maxScenarioUnresolvedRate))
                    return OwnershipBackfillCommandLineParseResult.Failure("--max-scenario-unresolved-rate must be a number between 0 and 1.");

                continue;
            }

            if (string.Equals(arg, "--max-job-unresolved-rate", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadDouble(args, ref index, out maxJobUnresolvedRate))
                    return OwnershipBackfillCommandLineParseResult.Failure("--max-job-unresolved-rate must be a number between 0 and 1.");

                continue;
            }

            if (string.Equals(arg, "--max-ambiguous-records", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value) ||
                    !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out maxAmbiguousRecords) ||
                    maxAmbiguousRecords < 0)
                {
                    return OwnershipBackfillCommandLineParseResult.Failure("--max-ambiguous-records must be a non-negative integer.");
                }

                continue;
            }

            if (string.Equals(arg, "--fail-on-missing-record-type-metrics", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadBoolean(args, ref index, out failOnMissingRecordTypeMetrics))
                    return OwnershipBackfillCommandLineParseResult.Failure("--fail-on-missing-record-type-metrics must be true or false.");

                continue;
            }

            if (string.Equals(arg, "--fail-on-ambiguous-records", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadBoolean(args, ref index, out failOnAmbiguousRecords))
                    return OwnershipBackfillCommandLineParseResult.Failure("--fail-on-ambiguous-records must be true or false.");

                continue;
            }

            if (string.Equals(arg, "--fail-on-schema-mismatch", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadBoolean(args, ref index, out failOnSchemaMismatch))
                    return OwnershipBackfillCommandLineParseResult.Failure("--fail-on-schema-mismatch must be true or false.");

                continue;
            }

            return OwnershipBackfillCommandLineParseResult.Failure($"Unknown option: {arg}");
        }

        if (string.IsNullOrWhiteSpace(inputDirectory))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-evidence requires --input <directory>.");

        if (string.IsNullOrWhiteSpace(outputDirectory))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-evidence requires --output <directory>.");

        var gateOptions = new OwnershipBackfillGateOptions
        {
            EvidenceDirectory = inputDirectory,
            OutputDirectory = outputDirectory,
            SummaryPath = summaryPath,
            MaxTotalUnresolvedRate = maxTotalUnresolvedRate,
            MaxProjectUnresolvedRate = maxProjectUnresolvedRate,
            MaxScenarioUnresolvedRate = maxScenarioUnresolvedRate,
            MaxJobUnresolvedRate = maxJobUnresolvedRate,
            MaxAmbiguousRecords = maxAmbiguousRecords,
            FailOnMissingRecordTypeMetrics = failOnMissingRecordTypeMetrics,
            FailOnAmbiguousRecords = failOnAmbiguousRecords,
            FailOnSchemaMismatch = failOnSchemaMismatch
        };

        return OwnershipBackfillCommandLineParseResult.ValidateEvidenceSuccess(gateOptions);
    }

    private static OwnershipBackfillCommandLineParseResult ParseApplyOptions(IReadOnlyList<string> args)
    {
        string? evidenceDirectory = null;
        string? gateResultPath = null;
        string? planPath = null;
        string? planSignoffPath = null;
        string? outputDirectory = null;
        string? databaseProvider = null;
        string? connectionString = null;
        var enableApply = false;
        string? confirmationPhrase = null;
        var batchSize = OwnershipBackfillConstants.DefaultBatchSize;

        for (var index = 0; index < args.Count; index++)
        {
            var arg = args[index];
            if (IsHelpToken(arg))
                return OwnershipBackfillCommandLineParseResult.Help();

            if (string.Equals(arg, "--evidence", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--evidence requires a value.");

                evidenceDirectory = value;
                continue;
            }

            if (string.Equals(arg, "--gate-result", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--gate-result requires a value.");

                gateResultPath = value;
                continue;
            }

            if (string.Equals(arg, "--output", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--output requires a value.");

                outputDirectory = value;
                continue;
            }

            if (string.Equals(arg, "--plan", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--plan requires a value.");

                planPath = value;
                continue;
            }

            if (string.Equals(arg, "--plan-signoff", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--plan-signoff requires a value.");

                planSignoffPath = value;
                continue;
            }

            if (string.Equals(arg, "--database-provider", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--database-provider requires a value.");

                if (!OwnershipBackfillConstants.SupportedDatabaseProviders.Contains(value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--database-provider must be one of: PostgreSQL, SQLite, None.");

                databaseProvider = CanonicalizeDatabaseProvider(value);
                continue;
            }

            if (string.Equals(arg, "--connection-string", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--connection-string requires a value.");

                connectionString = value;
                continue;
            }

            if (string.Equals(arg, "--enable-apply", StringComparison.OrdinalIgnoreCase))
            {
                enableApply = true;
                continue;
            }

            if (string.Equals(arg, "--confirm", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--confirm requires a value.");

                confirmationPhrase = value;
                continue;
            }

            if (string.Equals(arg, "--batch-size", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value) ||
                    !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out batchSize))
                {
                    return OwnershipBackfillCommandLineParseResult.Failure("--batch-size must be a positive integer.");
                }

                continue;
            }

            return OwnershipBackfillCommandLineParseResult.Failure($"Unknown option: {arg}");
        }

        var options = new OwnershipBackfillApplyOptions
        {
            EvidenceDirectory = evidenceDirectory,
            GateResultPath = gateResultPath,
            PlanPath = planPath,
            PlanSignoffPath = planSignoffPath,
            OutputDirectory = outputDirectory,
            DatabaseProvider = databaseProvider,
            ConnectionString = connectionString,
            EnableApply = enableApply,
            ConfirmationPhrase = confirmationPhrase,
            BatchSize = batchSize
        };

        return OwnershipBackfillCommandLineParseResult.ApplySuccess(options);
    }

    private static OwnershipBackfillCommandLineParseResult ParseValidateProductionPromotionOptions(IReadOnlyList<string> args)
    {
        string? stagingAcceptancePath = null;
        string? productionDryRunPath = null;
        string? productionGatePath = null;
        string? productionPlanPath = null;
        string? productionSignoffPath = null;
        string? productionReadinessPath = null;
        string? productionPreviousValuesPath = null;
        string? productionChangeRequestId = null;
        string? outputDirectory = null;
        var maxStagingAcceptanceAgeHours = 72;
        var maxProductionSignoffAgeHours = 24;
        var rulesetVersion = "P6-13";
        var requireSeparateProductionEvidence = true;
        var requireBackupReference = true;
        var requireRollbackReadiness = true;

        for (var index = 0; index < args.Count; index++)
        {
            var arg = args[index];
            if (IsHelpToken(arg))
                return OwnershipBackfillCommandLineParseResult.Help();

            if (string.Equals(arg, "--staging-acceptance", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--staging-acceptance requires a value.");
                stagingAcceptancePath = value;
                continue;
            }

            if (string.Equals(arg, "--production-dry-run", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--production-dry-run requires a value.");
                productionDryRunPath = value;
                continue;
            }

            if (string.Equals(arg, "--production-gate-result", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--production-gate-result requires a value.");
                productionGatePath = value;
                continue;
            }

            if (string.Equals(arg, "--production-plan", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--production-plan requires a value.");
                productionPlanPath = value;
                continue;
            }

            if (string.Equals(arg, "--production-signoff", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--production-signoff requires a value.");
                productionSignoffPath = value;
                continue;
            }

            if (string.Equals(arg, "--production-readiness", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--production-readiness requires a value.");
                productionReadinessPath = value;
                continue;
            }

            if (string.Equals(arg, "--production-previous-values", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--production-previous-values requires a value.");
                productionPreviousValuesPath = value;
                continue;
            }

            if (string.Equals(arg, "--production-change-request-id", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--production-change-request-id requires a value.");
                productionChangeRequestId = value;
                continue;
            }

            if (string.Equals(arg, "--output", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--output requires a value.");
                outputDirectory = value;
                continue;
            }

            if (string.Equals(arg, "--max-staging-acceptance-age-hours", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value) ||
                    !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out maxStagingAcceptanceAgeHours) ||
                    maxStagingAcceptanceAgeHours <= 0)
                {
                    return OwnershipBackfillCommandLineParseResult.Failure("--max-staging-acceptance-age-hours must be a positive integer.");
                }

                continue;
            }

            if (string.Equals(arg, "--max-production-signoff-age-hours", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value) ||
                    !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out maxProductionSignoffAgeHours) ||
                    maxProductionSignoffAgeHours <= 0)
                {
                    return OwnershipBackfillCommandLineParseResult.Failure("--max-production-signoff-age-hours must be a positive integer.");
                }

                continue;
            }

            if (string.Equals(arg, "--ruleset-version", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--ruleset-version requires a value.");
                rulesetVersion = value;
                continue;
            }

            if (string.Equals(arg, "--require-separate-production-evidence", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadBoolean(args, ref index, out requireSeparateProductionEvidence))
                    return OwnershipBackfillCommandLineParseResult.Failure("--require-separate-production-evidence must be true or false.");
                continue;
            }

            if (string.Equals(arg, "--require-backup-reference", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadBoolean(args, ref index, out requireBackupReference))
                    return OwnershipBackfillCommandLineParseResult.Failure("--require-backup-reference must be true or false.");
                continue;
            }

            if (string.Equals(arg, "--require-rollback-readiness", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadBoolean(args, ref index, out requireRollbackReadiness))
                    return OwnershipBackfillCommandLineParseResult.Failure("--require-rollback-readiness must be true or false.");
                continue;
            }

            return OwnershipBackfillCommandLineParseResult.Failure($"Unknown option: {arg}");
        }

        if (string.IsNullOrWhiteSpace(stagingAcceptancePath))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-production-promotion requires --staging-acceptance <path>.");
        if (string.IsNullOrWhiteSpace(productionDryRunPath))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-production-promotion requires --production-dry-run <path>.");
        if (string.IsNullOrWhiteSpace(productionGatePath))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-production-promotion requires --production-gate-result <path>.");
        if (string.IsNullOrWhiteSpace(productionPlanPath))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-production-promotion requires --production-plan <path>.");
        if (string.IsNullOrWhiteSpace(productionSignoffPath))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-production-promotion requires --production-signoff <path>.");
        if (string.IsNullOrWhiteSpace(productionReadinessPath))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-production-promotion requires --production-readiness <path>.");
        if (string.IsNullOrWhiteSpace(productionPreviousValuesPath))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-production-promotion requires --production-previous-values <path>.");
        if (string.IsNullOrWhiteSpace(productionChangeRequestId))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-production-promotion requires --production-change-request-id <id>.");
        if (string.IsNullOrWhiteSpace(outputDirectory))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-production-promotion requires --output <directory>.");

        var options = new OwnershipBackfillProductionPromotionOptions
        {
            StagingAcceptancePath = stagingAcceptancePath,
            ProductionDryRunSummaryPath = productionDryRunPath,
            ProductionGateResultPath = productionGatePath,
            ProductionPlanPath = productionPlanPath,
            ProductionSignoffPath = productionSignoffPath,
            ProductionReadinessPath = productionReadinessPath,
            ProductionPreviousValuesPath = productionPreviousValuesPath,
            ProductionChangeRequestId = productionChangeRequestId,
            OutputDirectory = outputDirectory,
            RulesetVersion = rulesetVersion,
            MaxStagingAcceptanceAgeHours = maxStagingAcceptanceAgeHours,
            MaxProductionSignoffAgeHours = maxProductionSignoffAgeHours,
            RequireSeparateProductionEvidence = requireSeparateProductionEvidence,
            RequireBackupReference = requireBackupReference,
            RequireRollbackReadiness = requireRollbackReadiness
        };

        return OwnershipBackfillCommandLineParseResult.ValidateProductionPromotionSuccess(options);
    }

    private static OwnershipBackfillCommandLineParseResult ParseValidateStagingPreflightOptions(IReadOnlyList<string> args)
    {
        string? environmentName = null;
        string? applyInputHash = null;
        string? readinessResultPath = null;
        string? planPath = null;
        string? signoffPath = null;
        string? backupReference = null;
        string? rollbackReadinessReference = null;
        string? operatorId = null;
        string? schemaVersion = null;
        var enableStagingApply = false;
        var confirmNoProductionConnection = false;

        for (var index = 0; index < args.Count; index++)
        {
            var arg = args[index];
            if (IsHelpToken(arg))
                return OwnershipBackfillCommandLineParseResult.Help();

            if (string.Equals(arg, "--environment", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--environment requires a value.");

                environmentName = value;
                continue;
            }

            if (string.Equals(arg, "--apply-input-hash", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--apply-input-hash requires a value.");

                applyInputHash = value;
                continue;
            }

            if (string.Equals(arg, "--readiness-result", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--readiness-result requires a value.");

                readinessResultPath = value;
                continue;
            }

            if (string.Equals(arg, "--plan", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--plan requires a value.");

                planPath = value;
                continue;
            }

            if (string.Equals(arg, "--signoff", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--signoff requires a value.");

                signoffPath = value;
                continue;
            }

            if (string.Equals(arg, "--backup-reference", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--backup-reference requires a value.");

                backupReference = value;
                continue;
            }

            if (string.Equals(arg, "--rollback-readiness-reference", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--rollback-readiness-reference requires a value.");

                rollbackReadinessReference = value;
                continue;
            }

            if (string.Equals(arg, "--operator", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--operator requires a value.");

                operatorId = value;
                continue;
            }

            if (string.Equals(arg, "--schema-version", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--schema-version requires a value.");

                schemaVersion = value;
                continue;
            }

            if (string.Equals(arg, "--enable-staging-apply", StringComparison.OrdinalIgnoreCase))
            {
                enableStagingApply = true;
                continue;
            }

            if (string.Equals(arg, "--confirm-no-production-connection", StringComparison.OrdinalIgnoreCase))
            {
                confirmNoProductionConnection = true;
                continue;
            }

            return OwnershipBackfillCommandLineParseResult.Failure($"Unknown option: {arg}");
        }

        var options = new OwnershipBackfillStagingApplyPreflightOptions
        {
            EnvironmentName = environmentName,
            ApplyInputHash = applyInputHash,
            ReadinessResultPath = readinessResultPath,
            PlanPath = planPath,
            SignoffPath = signoffPath,
            BackupReference = backupReference,
            RollbackReadinessReference = rollbackReadinessReference,
            OperatorId = operatorId,
            SchemaVersion = schemaVersion,
            EnableStagingApply = enableStagingApply,
            ConfirmNoProductionConnection = confirmNoProductionConnection
        };

        return OwnershipBackfillCommandLineParseResult.ValidateStagingPreflightSuccess(options);
    }

    private static OwnershipBackfillCommandLineParseResult ParseValidateStagingAcceptanceOptions(IReadOnlyList<string> args)
    {
        string? applyResultPath = null;
        string? postApplyDryRunPath = null;
        string? postApplyGatePath = null;
        string? tenantIsolationResultReference = null;
        string? regressionResultReference = null;
        string? rollbackEvidenceReference = null;
        string? applyInputHash = null;
        string? planHash = null;
        string? signoffId = null;
        string? readinessId = null;
        string? stagingPreflightReference = null;
        string? operatorId = null;
        string? stagingChangeId = null;
        string? outputDirectory = null;
        var maxPostApplyUnresolvedRate = 0.01d;
        var rulesetVersion = "P6-12";
        var requireZeroFailedRecords = true;
        var requireRollbackEvidence = true;
        var requireTenantIsolationPass = true;
        var requireRegressionPass = true;

        for (var index = 0; index < args.Count; index++)
        {
            var arg = args[index];
            if (IsHelpToken(arg))
                return OwnershipBackfillCommandLineParseResult.Help();

            if (string.Equals(arg, "--apply-result", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--apply-result requires a value.");
                applyResultPath = value;
                continue;
            }

            if (string.Equals(arg, "--post-apply-dry-run", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--post-apply-dry-run requires a value.");
                postApplyDryRunPath = value;
                continue;
            }

            if (string.Equals(arg, "--post-apply-gate-result", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--post-apply-gate-result requires a value.");
                postApplyGatePath = value;
                continue;
            }

            if (string.Equals(arg, "--tenant-isolation-result", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--tenant-isolation-result requires a value.");
                tenantIsolationResultReference = value;
                continue;
            }

            if (string.Equals(arg, "--regression-result", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--regression-result requires a value.");
                regressionResultReference = value;
                continue;
            }

            if (string.Equals(arg, "--rollback-evidence", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--rollback-evidence requires a value.");
                rollbackEvidenceReference = value;
                continue;
            }

            if (string.Equals(arg, "--apply-input-hash", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--apply-input-hash requires a value.");
                applyInputHash = value;
                continue;
            }

            if (string.Equals(arg, "--plan-hash", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--plan-hash requires a value.");
                planHash = value;
                continue;
            }

            if (string.Equals(arg, "--signoff-id", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--signoff-id requires a value.");
                signoffId = value;
                continue;
            }

            if (string.Equals(arg, "--readiness-id", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--readiness-id requires a value.");
                readinessId = value;
                continue;
            }

            if (string.Equals(arg, "--staging-preflight", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--staging-preflight requires a value.");
                stagingPreflightReference = value;
                continue;
            }

            if (string.Equals(arg, "--operator", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--operator requires a value.");
                operatorId = value;
                continue;
            }

            if (string.Equals(arg, "--staging-change-id", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--staging-change-id requires a value.");
                stagingChangeId = value;
                continue;
            }

            if (string.Equals(arg, "--output", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--output requires a value.");
                outputDirectory = value;
                continue;
            }

            if (string.Equals(arg, "--max-post-apply-unresolved-rate", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadDouble(args, ref index, out maxPostApplyUnresolvedRate))
                    return OwnershipBackfillCommandLineParseResult.Failure("--max-post-apply-unresolved-rate must be a number between 0 and 1.");
                continue;
            }

            if (string.Equals(arg, "--ruleset-version", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--ruleset-version requires a value.");
                rulesetVersion = value;
                continue;
            }

            if (string.Equals(arg, "--require-zero-failed-records", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadBoolean(args, ref index, out requireZeroFailedRecords))
                    return OwnershipBackfillCommandLineParseResult.Failure("--require-zero-failed-records must be true or false.");
                continue;
            }

            if (string.Equals(arg, "--require-rollback-evidence", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadBoolean(args, ref index, out requireRollbackEvidence))
                    return OwnershipBackfillCommandLineParseResult.Failure("--require-rollback-evidence must be true or false.");
                continue;
            }

            if (string.Equals(arg, "--require-tenant-isolation-pass", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadBoolean(args, ref index, out requireTenantIsolationPass))
                    return OwnershipBackfillCommandLineParseResult.Failure("--require-tenant-isolation-pass must be true or false.");
                continue;
            }

            if (string.Equals(arg, "--require-regression-pass", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadBoolean(args, ref index, out requireRegressionPass))
                    return OwnershipBackfillCommandLineParseResult.Failure("--require-regression-pass must be true or false.");
                continue;
            }

            return OwnershipBackfillCommandLineParseResult.Failure($"Unknown option: {arg}");
        }

        if (string.IsNullOrWhiteSpace(applyResultPath))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-staging-acceptance requires --apply-result <path>.");
        if (string.IsNullOrWhiteSpace(postApplyDryRunPath))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-staging-acceptance requires --post-apply-dry-run <path>.");
        if (string.IsNullOrWhiteSpace(postApplyGatePath))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-staging-acceptance requires --post-apply-gate-result <path>.");
        if (string.IsNullOrWhiteSpace(tenantIsolationResultReference))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-staging-acceptance requires --tenant-isolation-result <reference>.");
        if (string.IsNullOrWhiteSpace(regressionResultReference))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-staging-acceptance requires --regression-result <reference>.");
        if (string.IsNullOrWhiteSpace(rollbackEvidenceReference))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-staging-acceptance requires --rollback-evidence <reference>.");
        if (string.IsNullOrWhiteSpace(applyInputHash))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-staging-acceptance requires --apply-input-hash <hash>.");
        if (string.IsNullOrWhiteSpace(planHash))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-staging-acceptance requires --plan-hash <hash>.");
        if (string.IsNullOrWhiteSpace(signoffId))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-staging-acceptance requires --signoff-id <id>.");
        if (string.IsNullOrWhiteSpace(readinessId))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-staging-acceptance requires --readiness-id <id>.");
        if (string.IsNullOrWhiteSpace(stagingPreflightReference))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-staging-acceptance requires --staging-preflight <reference>.");
        if (string.IsNullOrWhiteSpace(operatorId))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-staging-acceptance requires --operator <id>.");
        if (string.IsNullOrWhiteSpace(stagingChangeId))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-staging-acceptance requires --staging-change-id <id>.");
        if (string.IsNullOrWhiteSpace(outputDirectory))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-staging-acceptance requires --output <directory>.");

        var options = new OwnershipBackfillStagingAcceptanceOptions
        {
            ApplyResultPath = applyResultPath,
            PostApplyDryRunSummaryPath = postApplyDryRunPath,
            PostApplyGateResultPath = postApplyGatePath,
            TenantIsolationMatrixResultReference = tenantIsolationResultReference,
            RegressionTestResultReference = regressionResultReference,
            RollbackEvidenceReference = rollbackEvidenceReference,
            ApplyInputHash = applyInputHash,
            PlanHash = planHash,
            SignoffId = signoffId,
            ReadinessId = readinessId,
            StagingPreflightReference = stagingPreflightReference,
            OperatorId = operatorId,
            StagingChangeId = stagingChangeId,
            OutputDirectory = outputDirectory,
            RulesetVersion = rulesetVersion,
            MaxPostApplyUnresolvedRate = maxPostApplyUnresolvedRate,
            RequireZeroFailedRecords = requireZeroFailedRecords,
            RequireRollbackEvidence = requireRollbackEvidence,
            RequireTenantIsolationPass = requireTenantIsolationPass,
            RequireRegressionPass = requireRegressionPass
        };

        return OwnershipBackfillCommandLineParseResult.ValidateStagingAcceptanceSuccess(options);
    }

    private static OwnershipBackfillCommandLineParseResult ParseValidateApplyReadinessOptions(IReadOnlyList<string> args)
    {
        string? dryRunSummaryPath = null;
        string? gateResultPath = null;
        string? planPath = null;
        string? signoffPath = null;
        string? previousValuesPath = null;
        string? outputDirectory = null;
        string? expectedPlanHash = null;
        var maxSignoffAgeHours = 24;
        var requireRollbackReadiness = true;
        var rulesetVersion = "P6-08";

        for (var index = 0; index < args.Count; index++)
        {
            var arg = args[index];
            if (IsHelpToken(arg))
                return OwnershipBackfillCommandLineParseResult.Help();

            if (string.Equals(arg, "--dry-run", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--dry-run requires a value.");
                dryRunSummaryPath = value;
                continue;
            }

            if (string.Equals(arg, "--gate-result", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--gate-result requires a value.");
                gateResultPath = value;
                continue;
            }

            if (string.Equals(arg, "--plan", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--plan requires a value.");
                planPath = value;
                continue;
            }

            if (string.Equals(arg, "--signoff", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--signoff requires a value.");
                signoffPath = value;
                continue;
            }

            if (string.Equals(arg, "--previous-values", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--previous-values requires a value.");
                previousValuesPath = value;
                continue;
            }

            if (string.Equals(arg, "--output", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--output requires a value.");
                outputDirectory = value;
                continue;
            }

            if (string.Equals(arg, "--max-signoff-age-hours", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value) ||
                    !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out maxSignoffAgeHours) ||
                    maxSignoffAgeHours <= 0)
                {
                    return OwnershipBackfillCommandLineParseResult.Failure("--max-signoff-age-hours must be a positive integer.");
                }

                continue;
            }

            if (string.Equals(arg, "--require-rollback-readiness", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadBoolean(args, ref index, out requireRollbackReadiness))
                    return OwnershipBackfillCommandLineParseResult.Failure("--require-rollback-readiness must be true or false.");
                continue;
            }

            if (string.Equals(arg, "--expected-plan-hash", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--expected-plan-hash requires a value.");
                expectedPlanHash = value;
                continue;
            }

            if (string.Equals(arg, "--ruleset-version", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--ruleset-version requires a value.");
                rulesetVersion = value;
                continue;
            }

            return OwnershipBackfillCommandLineParseResult.Failure($"Unknown option: {arg}");
        }

        if (string.IsNullOrWhiteSpace(dryRunSummaryPath))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-apply-readiness requires --dry-run <dry-run-summary-json>.");

        if (string.IsNullOrWhiteSpace(gateResultPath))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-apply-readiness requires --gate-result <gate-result-json>.");

        if (string.IsNullOrWhiteSpace(planPath))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-apply-readiness requires --plan <apply-plan-json>.");

        if (string.IsNullOrWhiteSpace(signoffPath))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-apply-readiness requires --signoff <plan-signoff-json>.");

        if (string.IsNullOrWhiteSpace(previousValuesPath))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-apply-readiness requires --previous-values <previous-values-json>.");

        if (string.IsNullOrWhiteSpace(outputDirectory))
            return OwnershipBackfillCommandLineParseResult.Failure("validate-apply-readiness requires --output <readiness-output-dir>.");

        var options = new OwnershipBackfillApplyReadinessOptions
        {
            DryRunSummaryPath = dryRunSummaryPath,
            GateResultPath = gateResultPath,
            PlanPath = planPath,
            SignoffPath = signoffPath,
            PreviousValuesPath = previousValuesPath,
            OutputDirectory = outputDirectory,
            ExpectedPlanHash = expectedPlanHash,
            MaxSignoffAgeHours = maxSignoffAgeHours,
            RequireRollbackReadiness = requireRollbackReadiness,
            RulesetVersion = rulesetVersion
        };

        return OwnershipBackfillCommandLineParseResult.ValidateApplyReadinessSuccess(options);
    }

    private static OwnershipBackfillCommandLineParseResult ParsePlanApplyOptions(IReadOnlyList<string> args)
    {
        string? evidenceDirectory = null;
        string? gateResultPath = null;
        string? outputDirectory = null;
        var rulesetVersion = "P6-05";
        int? maxPlannedRecords = null;
        var includeLegacyUnscoped = false;
        var forceOverwrite = false;

        for (var index = 0; index < args.Count; index++)
        {
            var arg = args[index];
            if (IsHelpToken(arg))
                return OwnershipBackfillCommandLineParseResult.Help();

            if (string.Equals(arg, "--evidence", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--evidence requires a value.");

                evidenceDirectory = value;
                continue;
            }

            if (string.Equals(arg, "--gate-result", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--gate-result requires a value.");

                gateResultPath = value;
                continue;
            }

            if (string.Equals(arg, "--output", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--output requires a value.");

                outputDirectory = value;
                continue;
            }

            if (string.Equals(arg, "--ruleset-version", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--ruleset-version requires a value.");

                rulesetVersion = value;
                continue;
            }

            if (string.Equals(arg, "--max-planned-records", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value) ||
                    !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedMax) ||
                    parsedMax <= 0)
                {
                    return OwnershipBackfillCommandLineParseResult.Failure("--max-planned-records must be a positive integer.");
                }

                maxPlannedRecords = parsedMax;
                continue;
            }

            if (string.Equals(arg, "--include-legacy-unscoped", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadBoolean(args, ref index, out includeLegacyUnscoped))
                    return OwnershipBackfillCommandLineParseResult.Failure("--include-legacy-unscoped must be true or false.");

                continue;
            }

            if (string.Equals(arg, "--force-overwrite", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadBoolean(args, ref index, out forceOverwrite))
                    return OwnershipBackfillCommandLineParseResult.Failure("--force-overwrite must be true or false.");

                continue;
            }

            return OwnershipBackfillCommandLineParseResult.Failure($"Unknown option: {arg}");
        }

        if (string.IsNullOrWhiteSpace(evidenceDirectory))
            return OwnershipBackfillCommandLineParseResult.Failure("plan-apply requires --evidence <dry-run-dir>.");

        if (string.IsNullOrWhiteSpace(gateResultPath))
            return OwnershipBackfillCommandLineParseResult.Failure("plan-apply requires --gate-result <gate-result-json>.");

        if (string.IsNullOrWhiteSpace(outputDirectory))
            return OwnershipBackfillCommandLineParseResult.Failure("plan-apply requires --output <plan-output-dir>.");

        var options = new OwnershipBackfillPlanOptions
        {
            EvidenceDirectory = evidenceDirectory,
            GateResultPath = gateResultPath,
            OutputDirectory = outputDirectory,
            RulesetVersion = rulesetVersion,
            MaxPlannedRecords = maxPlannedRecords,
            IncludeLegacyUnscoped = includeLegacyUnscoped,
            ForceOverwrite = forceOverwrite
        };

        return OwnershipBackfillCommandLineParseResult.PlanApplySuccess(options);
    }

    private static OwnershipBackfillCommandLineParseResult ParseSignoffPlanOptions(IReadOnlyList<string> args)
    {
        string? planPath = null;
        string? expectedPlanHash = null;
        string? reviewer = null;
        string? ticket = null;
        string? outputDirectory = null;
        string? notes = null;
        DateTimeOffset? expiresAtUtc = null;
        string? confirmationPhrase = null;
        var forceOverwrite = false;

        for (var index = 0; index < args.Count; index++)
        {
            var arg = args[index];
            if (IsHelpToken(arg))
                return OwnershipBackfillCommandLineParseResult.Help();

            if (string.Equals(arg, "--plan", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--plan requires a value.");

                planPath = value;
                continue;
            }

            if (string.Equals(arg, "--expected-plan-hash", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--expected-plan-hash requires a value.");

                expectedPlanHash = value;
                continue;
            }

            if (string.Equals(arg, "--reviewer", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--reviewer requires a value.");

                reviewer = value;
                continue;
            }

            if (string.Equals(arg, "--ticket", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--ticket requires a value.");

                ticket = value;
                continue;
            }

            if (string.Equals(arg, "--output", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--output requires a value.");

                outputDirectory = value;
                continue;
            }

            if (string.Equals(arg, "--notes", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--notes requires a value.");

                notes = value;
                continue;
            }

            if (string.Equals(arg, "--expires-at", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--expires-at requires a value.");

                if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedExpiresAt))
                    return OwnershipBackfillCommandLineParseResult.Failure("--expires-at must be a valid UTC timestamp.");

                expiresAtUtc = parsedExpiresAt;
                continue;
            }

            if (string.Equals(arg, "--confirm", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadValue(args, ref index, out var value))
                    return OwnershipBackfillCommandLineParseResult.Failure("--confirm requires a value.");

                confirmationPhrase = value;
                continue;
            }

            if (string.Equals(arg, "--force-overwrite", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadBoolean(args, ref index, out forceOverwrite))
                    return OwnershipBackfillCommandLineParseResult.Failure("--force-overwrite must be true or false.");

                continue;
            }

            return OwnershipBackfillCommandLineParseResult.Failure($"Unknown option: {arg}");
        }

        var options = new OwnershipBackfillPlanSignoffOptions
        {
            PlanPath = planPath,
            ExpectedPlanHash = expectedPlanHash,
            Reviewer = reviewer,
            Ticket = ticket,
            OutputDirectory = outputDirectory,
            Notes = notes,
            ExpiresAtUtc = expiresAtUtc,
            ConfirmationPhrase = confirmationPhrase,
            ForceOverwrite = forceOverwrite
        };

        return OwnershipBackfillCommandLineParseResult.SignoffPlanSuccess(options);
    }

    private static bool TryReadValue(IReadOnlyList<string> args, ref int index, out string value)
    {
        if (index + 1 >= args.Count)
        {
            value = string.Empty;
            return false;
        }

        value = args[++index];
        return true;
    }

    private static bool TryReadDouble(IReadOnlyList<string> args, ref int index, out double value)
    {
        value = 0d;
        if (!TryReadValue(args, ref index, out var text))
            return false;

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
               value >= 0d &&
               value <= 1d;
    }

    private static bool TryReadBoolean(IReadOnlyList<string> args, ref int index, out bool value)
    {
        value = false;
        return TryReadValue(args, ref index, out var text) && bool.TryParse(text, out value);
    }

    private static bool IsHelpToken(string value) =>
        string.Equals(value, "--help", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "-h", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "help", StringComparison.OrdinalIgnoreCase);

    private static string CanonicalizeDatabaseProvider(string value)
    {
        if (string.Equals(value, "PostgreSQL", StringComparison.OrdinalIgnoreCase))
            return "PostgreSQL";
        if (string.Equals(value, "SQLite", StringComparison.OrdinalIgnoreCase))
            return "SQLite";

        return "None";
    }
}
