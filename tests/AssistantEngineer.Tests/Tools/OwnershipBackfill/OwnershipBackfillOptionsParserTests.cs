using AssistantEngineer.Tools.OwnershipBackfill.Cli;
using AssistantEngineer.Tools.OwnershipBackfill.Models;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillOptionsParserTests
{
    [Fact]
    public void Parse_DryRunWithOutput_SucceedsWithDefaults()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        var result = parser.Parse(["dry-run", "--output", "artifacts/ownership-backfill/test"]);

        Assert.True(result.IsSuccess);
        Assert.False(result.ShowHelp);
        Assert.NotNull(result.Options);
        Assert.Equal(OwnershipBackfillCommandType.DryRun, result.CommandType);
        Assert.Equal(OwnershipBackfillConstants.DefaultBatchSize, result.Options.BatchSize);
        Assert.Equal(OwnershipBackfillConstants.DefaultMaxUnresolvedRate, result.Options.MaxUnresolvedRate);
        Assert.True(result.Options.NoDataDryRun);
        Assert.Equal("None", result.Options.DatabaseProvider);
    }

    [Fact]
    public void Parse_ValidateEvidenceWithInputOutput_SucceedsWithDefaults()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        var result = parser.Parse(["validate-evidence", "--input", "in", "--output", "out"]);

        Assert.True(result.IsSuccess);
        Assert.Equal(OwnershipBackfillCommandType.ValidateEvidence, result.CommandType);
        Assert.NotNull(result.GateOptions);
        Assert.Equal(0.05d, result.GateOptions.MaxTotalUnresolvedRate, 5);
        Assert.Equal(0d, result.GateOptions.MaxProjectUnresolvedRate, 5);
        Assert.Equal(0.05d, result.GateOptions.MaxScenarioUnresolvedRate, 5);
        Assert.Equal(0.10d, result.GateOptions.MaxJobUnresolvedRate, 5);
        Assert.Equal(0, result.GateOptions.MaxAmbiguousRecords);
        Assert.True(result.GateOptions.FailOnMissingRecordTypeMetrics);
        Assert.True(result.GateOptions.FailOnAmbiguousRecords);
        Assert.True(result.GateOptions.FailOnSchemaMismatch);
    }

    [Fact]
    public void Parse_ValidateApplyReadinessWithInputs_SucceedsWithDefaults()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        var result = parser.Parse(
        [
            "validate-apply-readiness",
            "--dry-run", "dry-run.json",
            "--gate-result", "gate.json",
            "--plan", "plan.json",
            "--signoff", "signoff.json",
            "--previous-values", "previous.json",
            "--output", "readiness-out"
        ]);

        Assert.True(result.IsSuccess);
        Assert.Equal(OwnershipBackfillCommandType.ValidateApplyReadiness, result.CommandType);
        Assert.NotNull(result.ApplyReadinessOptions);
        Assert.Equal(24, result.ApplyReadinessOptions.MaxSignoffAgeHours);
        Assert.True(result.ApplyReadinessOptions.RequireRollbackReadiness);
        Assert.Equal("P6-08", result.ApplyReadinessOptions.RulesetVersion);
    }

    [Fact]
    public void Parse_ValidateProductionPromotionWithInputs_SucceedsWithDefaults()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        var result = parser.Parse(
        [
            "validate-production-promotion",
            "--staging-acceptance", "staging-acceptance.json",
            "--production-dry-run", "prod-dry-run.json",
            "--production-gate-result", "prod-gate.json",
            "--production-plan", "prod-plan.json",
            "--production-signoff", "prod-signoff.json",
            "--production-readiness", "prod-readiness.json",
            "--production-previous-values", "prod-previous-values.json",
            "--production-change-request-id", "CHG-PROD-001",
            "--output", "prod-promotion-out"
        ]);

        Assert.True(result.IsSuccess);
        Assert.Equal(OwnershipBackfillCommandType.ValidateProductionPromotion, result.CommandType);
        Assert.NotNull(result.ProductionPromotionOptions);
        Assert.Equal("P6-13", result.ProductionPromotionOptions.RulesetVersion);
        Assert.Equal(72, result.ProductionPromotionOptions.MaxStagingAcceptanceAgeHours);
        Assert.Equal(24, result.ProductionPromotionOptions.MaxProductionSignoffAgeHours);
        Assert.True(result.ProductionPromotionOptions.RequireSeparateProductionEvidence);
        Assert.True(result.ProductionPromotionOptions.RequireBackupReference);
        Assert.True(result.ProductionPromotionOptions.RequireRollbackReadiness);
    }

    [Fact]
    public void Parse_ValidateStagingPreflightWithInputs_Succeeds()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        var result = parser.Parse(
        [
            "validate-staging-preflight",
            "--environment", "Staging",
            "--apply-input-hash", "hash-001",
            "--readiness-result", "readiness.json",
            "--plan", "plan.json",
            "--signoff", "signoff.json",
            "--backup-reference", "backup-001",
            "--rollback-readiness-reference", "rollback-001",
            "--operator", "operator-001",
            "--schema-version", "schema-v1",
            "--enable-staging-apply",
            "--confirm-no-production-connection"
        ]);

        Assert.True(result.IsSuccess);
        Assert.Equal(OwnershipBackfillCommandType.ValidateStagingPreflight, result.CommandType);
        Assert.NotNull(result.StagingPreflightOptions);
        Assert.Equal("Staging", result.StagingPreflightOptions.EnvironmentName);
        Assert.True(result.StagingPreflightOptions.EnableStagingApply);
        Assert.True(result.StagingPreflightOptions.ConfirmNoProductionConnection);
    }

    [Fact]
    public void Parse_ValidateStagingAcceptanceWithInputs_Succeeds()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        var result = parser.Parse(
        [
            "validate-staging-acceptance",
            "--apply-result", "apply.json",
            "--post-apply-dry-run", "post-dry.json",
            "--post-apply-gate-result", "post-gate.json",
            "--tenant-isolation-result", "tenant-ref",
            "--regression-result", "reg-ref",
            "--rollback-evidence", "rollback-ref",
            "--apply-input-hash", "apply-hash",
            "--plan-hash", "plan-hash",
            "--signoff-id", "signoff-1",
            "--readiness-id", "readiness-1",
            "--staging-preflight", "preflight-1",
            "--operator", "operator-1",
            "--staging-change-id", "change-1",
            "--output", "out"
        ]);

        Assert.True(result.IsSuccess);
        Assert.Equal(OwnershipBackfillCommandType.ValidateStagingAcceptance, result.CommandType);
        Assert.NotNull(result.StagingAcceptanceOptions);
        Assert.Equal("P6-12", result.StagingAcceptanceOptions.RulesetVersion);
        Assert.Equal(0.01d, result.StagingAcceptanceOptions.MaxPostApplyUnresolvedRate, 6);
        Assert.True(result.StagingAcceptanceOptions.RequireZeroFailedRecords);
    }

    [Fact]
    public void Parse_ApplyWithArguments_ReturnsApplyOptions()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        var result = parser.Parse(
        [
            "apply",
            "--evidence", "in",
            "--gate-result", "gate.json",
            "--plan", "plan.json",
            "--plan-signoff", "signoff.json",
            "--output", "out",
            "--database-provider", "SQLite",
            "--connection-string", "Data Source=x.db",
            "--enable-apply",
            "--confirm", OwnershipBackfillConstants.ApplyConfirmationPhrase,
            "--batch-size", "500"
        ]);

        Assert.True(result.IsSuccess);
        Assert.Equal(OwnershipBackfillCommandType.Apply, result.CommandType);
        Assert.NotNull(result.ApplyOptions);
        Assert.True(result.ApplyOptions.EnableApply);
        Assert.Equal(500, result.ApplyOptions.BatchSize);
        Assert.Equal("plan.json", result.ApplyOptions.PlanPath);
        Assert.Equal("signoff.json", result.ApplyOptions.PlanSignoffPath);
    }

    [Fact]
    public void Parse_PlanApplyWithArguments_ReturnsPlanOptions()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        var result = parser.Parse(
        [
            "plan-apply",
            "--evidence", "dry-run",
            "--gate-result", "gate.json",
            "--output", "plan-out",
            "--ruleset-version", "P6-05",
            "--max-planned-records", "12",
            "--include-legacy-unscoped", "false",
            "--force-overwrite", "true"
        ]);

        Assert.True(result.IsSuccess);
        Assert.Equal(OwnershipBackfillCommandType.PlanApply, result.CommandType);
        Assert.NotNull(result.PlanOptions);
        Assert.Equal("P6-05", result.PlanOptions.RulesetVersion);
        Assert.Equal(12, result.PlanOptions.MaxPlannedRecords);
        Assert.False(result.PlanOptions.IncludeLegacyUnscoped);
        Assert.True(result.PlanOptions.ForceOverwrite);
    }

    [Fact]
    public void Parse_DryRunMissingOutput_ReturnsError()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        var result = parser.Parse(["dry-run"]);

        Assert.False(result.IsSuccess);
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("--output", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_SignoffPlanWithArguments_ReturnsSignoffOptions()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        var result = parser.Parse(
        [
            "signoff-plan",
            "--plan", "plan.json",
            "--expected-plan-hash", "abc123",
            "--reviewer", "reviewer-1",
            "--ticket", "CHG-100",
            "--output", "signoff-out",
            "--notes", "local review",
            "--expires-at", "2026-12-01T00:00:00Z",
            "--confirm", OwnershipBackfillConstants.PlanSignoffConfirmationPhrase,
            "--force-overwrite", "true"
        ]);

        Assert.True(result.IsSuccess);
        Assert.Equal(OwnershipBackfillCommandType.SignoffPlan, result.CommandType);
        Assert.NotNull(result.SignoffOptions);
        Assert.Equal("plan.json", result.SignoffOptions.PlanPath);
        Assert.Equal("abc123", result.SignoffOptions.ExpectedPlanHash);
        Assert.Equal("reviewer-1", result.SignoffOptions.Reviewer);
        Assert.Equal("CHG-100", result.SignoffOptions.Ticket);
        Assert.Equal("signoff-out", result.SignoffOptions.OutputDirectory);
        Assert.Equal("local review", result.SignoffOptions.Notes);
        Assert.Equal(2026, result.SignoffOptions.ExpiresAtUtc!.Value.Year);
        Assert.True(result.SignoffOptions.ForceOverwrite);
    }

    [Fact]
    public void Parse_ValidateEvidenceMissingOutput_ReturnsError()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        var result = parser.Parse(["validate-evidence", "--input", "in"]);

        Assert.False(result.IsSuccess);
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("--output", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_PlanApplyMissingGateResult_ReturnsError()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        var result = parser.Parse(["plan-apply", "--evidence", "dry", "--output", "plan"]);

        Assert.False(result.IsSuccess);
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("--gate-result", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_UnknownOption_IsRejected()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        var result = parser.Parse(["dry-run", "--output", "out", "--unexpected"]);

        Assert.False(result.IsSuccess);
        Assert.Contains("Unknown option", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_DatabaseProviderWithoutConnectionString_IsRejected()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        var result = parser.Parse(["dry-run", "--output", "out", "--database-provider", "SQLite"]);

        Assert.False(result.IsSuccess);
        Assert.Contains("--connection-string", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseErrors_DoNotEchoConnectionString()
    {
        var parser = new OwnershipBackfillCommandLineParser();
        const string fakeSecret = "Server=x;Password=TOP-SECRET-123;";

        var result = parser.Parse(["dry-run", "--connection-string", fakeSecret, "--unknown"]);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.DoesNotContain(fakeSecret, result.ErrorMessage, StringComparison.Ordinal);
    }
}
