using AssistantEngineer.Tools.OwnershipBackfill.Cli;
using AssistantEngineer.Tools.OwnershipBackfill.Models;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillCommandLineParserCharacterizationTests
{
    [Fact]
    public void EmptyArgs_ReturnsHelp()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        var result = parser.Parse([]);

        Assert.True(result.IsSuccess);
        Assert.True(result.ShowHelp);
        Assert.Equal(OwnershipBackfillExitCodes.Success, result.ExitCode);
    }

    [Fact]
    public void HelpToken_ReturnsHelp()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        var result = parser.Parse(["--help"]);

        Assert.True(result.IsSuccess);
        Assert.True(result.ShowHelp);
        Assert.Equal(OwnershipBackfillExitCodes.Success, result.ExitCode);
    }

    [Fact]
    public void UnknownCommand_ReturnsInvalidInput()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        var result = parser.Parse(["unknown-command"]);

        Assert.False(result.IsSuccess);
        Assert.Equal(OwnershipBackfillExitCodes.InvalidInput, result.ExitCode);
        Assert.Contains("Unknown command", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CommandSpecificHelp_ReturnsHelpForAllSupportedCommands()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        foreach (var descriptor in OwnershipBackfillCommandDescriptorCatalog.All)
        {
            var result = parser.Parse([descriptor.Name, "--help"]);
            Assert.True(result.IsSuccess, descriptor.Name);
            Assert.True(result.ShowHelp, descriptor.Name);
            Assert.Equal(descriptor.CommandType, result.CommandType);
        }
    }

    [Fact]
    public void MissingRequiredArgumentBehavior_IsPreserved()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        var dryRun = parser.Parse(["dry-run"]);
        Assert.False(dryRun.IsSuccess);
        Assert.Contains("dry-run requires --output", dryRun.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        var validateEvidence = parser.Parse(["validate-evidence", "--input", "in"]);
        Assert.False(validateEvidence.IsSuccess);
        Assert.Contains("validate-evidence requires --output", validateEvidence.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnknownOptionBehavior_IsPreserved()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        var result = parser.Parse(["dry-run", "--output", "out", "--unexpected"]);

        Assert.False(result.IsSuccess);
        Assert.Contains("Unknown option: --unexpected", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void RepeatedOptionBehavior_IsPreserved_LastValueWins()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        var result = parser.Parse(["dry-run", "--output", "out-a", "--output", "out-b"]);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Options);
        Assert.Equal("out-b", result.Options.EvidenceOutputDirectory);
    }

    [Fact]
    public void ParseErrors_RedactSecretLikeValues()
    {
        var parser = new OwnershipBackfillCommandLineParser();
        const string secret = "Data Source=fake.db;Password=super-secret";

        var result = parser.Parse([secret]);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.DoesNotContain(secret, result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EverySupportedCommand_ParsesToExpectedCommandType()
    {
        var parser = new OwnershipBackfillCommandLineParser();

        foreach (var commandType in Enum.GetValues<OwnershipBackfillCommandType>())
        {
            var result = parser.Parse(BuildMinimalArgs(commandType));
            Assert.True(result.IsSuccess, commandType.ToString());
            Assert.Equal(commandType, result.CommandType);
        }
    }

    private static IReadOnlyList<string> BuildMinimalArgs(OwnershipBackfillCommandType commandType) =>
        commandType switch
        {
            OwnershipBackfillCommandType.DryRun =>
            [
                "dry-run",
                "--output", "artifacts/ownership-backfill/test"
            ],
            OwnershipBackfillCommandType.ValidateEvidence =>
            [
                "validate-evidence",
                "--input", "in",
                "--output", "out"
            ],
            OwnershipBackfillCommandType.PlanApply =>
            [
                "plan-apply",
                "--evidence", "evidence",
                "--gate-result", "gate.json",
                "--output", "out"
            ],
            OwnershipBackfillCommandType.SignoffPlan =>
            [
                "signoff-plan",
                "--plan", "plan.json",
                "--expected-plan-hash", "abc123",
                "--reviewer", "reviewer",
                "--ticket", "CHG-1",
                "--output", "out",
                "--confirm", OwnershipBackfillConstants.PlanSignoffConfirmationPhrase
            ],
            OwnershipBackfillCommandType.ValidateApplyReadiness =>
            [
                "validate-apply-readiness",
                "--dry-run", "dry-run.json",
                "--gate-result", "gate.json",
                "--plan", "plan.json",
                "--signoff", "signoff.json",
                "--previous-values", "previous-values.json",
                "--output", "out"
            ],
            OwnershipBackfillCommandType.ValidateStagingPreflight =>
            [
                "validate-staging-preflight",
                "--environment", "Staging",
                "--apply-input-hash", "hash",
                "--readiness-result", "readiness.json",
                "--plan", "plan.json",
                "--signoff", "signoff.json",
                "--backup-reference", "backup-ref",
                "--rollback-readiness-reference", "rollback-ref",
                "--operator", "operator",
                "--schema-version", "schema-v1",
                "--enable-staging-apply",
                "--confirm-no-production-connection"
            ],
            OwnershipBackfillCommandType.ValidateStagingAcceptance =>
            [
                "validate-staging-acceptance",
                "--apply-result", "apply.json",
                "--post-apply-dry-run", "post-dry.json",
                "--post-apply-gate-result", "post-gate.json",
                "--tenant-isolation-result", "tenant-ref",
                "--regression-result", "regression-ref",
                "--rollback-evidence", "rollback-ref",
                "--apply-input-hash", "hash",
                "--plan-hash", "plan-hash",
                "--signoff-id", "signoff-1",
                "--readiness-id", "readiness-1",
                "--staging-preflight", "preflight-1",
                "--operator", "operator-1",
                "--staging-change-id", "change-1",
                "--output", "out"
            ],
            OwnershipBackfillCommandType.ValidateProductionPromotion =>
            [
                "validate-production-promotion",
                "--staging-acceptance", "staging-acceptance.json",
                "--production-dry-run", "production-dry-run.json",
                "--production-gate-result", "production-gate.json",
                "--production-plan", "production-plan.json",
                "--production-signoff", "production-signoff.json",
                "--production-readiness", "production-readiness.json",
                "--production-previous-values", "production-previous-values.json",
                "--production-change-request-id", "CHG-1",
                "--output", "out"
            ],
            OwnershipBackfillCommandType.Apply =>
            [
                "apply",
                "--evidence", "evidence",
                "--gate-result", "gate.json",
                "--plan", "plan.json",
                "--plan-signoff", "signoff.json",
                "--output", "out",
                "--database-provider", "SQLite",
                "--connection-string", "Data Source=x.db",
                "--enable-apply",
                "--confirm", OwnershipBackfillConstants.ApplyConfirmationPhrase
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(commandType), commandType, null)
        };
}
