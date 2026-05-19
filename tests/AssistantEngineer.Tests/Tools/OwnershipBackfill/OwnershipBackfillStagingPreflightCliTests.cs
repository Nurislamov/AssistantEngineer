using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Cli;
using AssistantEngineer.Tools.OwnershipBackfill.Evidence;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Scanning;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillStagingPreflightCliTests
{
    [Fact]
    public async Task ValidateStagingPreflight_ValidInput_ExitsZero()
    {
        var root = CreateTempDirectory();
        try
        {
            var files = CreateRequiredFiles(root);
            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var exitCode = await cli.ExecuteAsync(
            [
                "validate-staging-preflight",
                "--environment", "Staging",
                "--apply-input-hash", "hash-001",
                "--readiness-result", files.Readiness,
                "--plan", files.Plan,
                "--signoff", files.Signoff,
                "--backup-reference", "backup-001",
                "--rollback-readiness-reference", "rollback-001",
                "--operator", "local-operator",
                "--schema-version", "schema-v1",
                "--enable-staging-apply",
                "--confirm-no-production-connection"
            ],
            stdout,
            stderr,
            CancellationToken.None);

            Assert.Equal(0, exitCode);
            Assert.Contains("disabled", stdout.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ValidateStagingPreflight_ProductionEnvironment_ExitsTwo()
    {
        var root = CreateTempDirectory();
        try
        {
            var files = CreateRequiredFiles(root);
            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var exitCode = await cli.ExecuteAsync(
            [
                "validate-staging-preflight",
                "--environment", "Production",
                "--apply-input-hash", "hash-001",
                "--readiness-result", files.Readiness,
                "--plan", files.Plan,
                "--signoff", files.Signoff,
                "--backup-reference", "backup-001",
                "--rollback-readiness-reference", "rollback-001",
                "--operator", "local-operator",
                "--schema-version", "schema-v1",
                "--enable-staging-apply",
                "--confirm-no-production-connection"
            ],
            stdout,
            stderr,
            CancellationToken.None);

            Assert.Equal(2, exitCode);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ValidateStagingPreflight_MissingBackup_ExitsTwo()
    {
        var root = CreateTempDirectory();
        try
        {
            var files = CreateRequiredFiles(root);
            var cli = CreateCli();
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();

            var exitCode = await cli.ExecuteAsync(
            [
                "validate-staging-preflight",
                "--environment", "Staging",
                "--apply-input-hash", "hash-001",
                "--readiness-result", files.Readiness,
                "--plan", files.Plan,
                "--signoff", files.Signoff,
                "--rollback-readiness-reference", "rollback-001",
                "--operator", "local-operator",
                "--schema-version", "schema-v1",
                "--enable-staging-apply",
                "--confirm-no-production-connection"
            ],
            stdout,
            stderr,
            CancellationToken.None);

            Assert.Equal(2, exitCode);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ValidateStagingPreflight_InvalidArgs_ExitsOne()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(
            ["validate-staging-preflight", "--environment", "Staging", "--unknown", "x"],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task ValidateStagingPreflight_DoesNotEchoSecrets()
    {
        const string fakeSecret = "super-secret-token";
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(
            ["validate-staging-preflight", "--environment", "Staging", "--unknown", fakeSecret],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.Equal(1, exitCode);
        var all = stdout.ToString() + Environment.NewLine + stderr.ToString();
        Assert.DoesNotContain(fakeSecret, all, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplyCommand_RemainsDisabled()
    {
        var cli = CreateCli();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(["apply"], stdout, stderr, CancellationToken.None);

        Assert.Equal(1, exitCode);
        Assert.Contains("disabled", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static OwnershipBackfillCli CreateCli()
    {
        return new OwnershipBackfillCli(
            new OwnershipBackfillCommandLineParser(),
            new OwnershipBackfillEvidenceWriter(),
            new NoDataOwnershipBackfillDryRunScanner(),
            new DatabaseOwnershipBackfillDryRunScanner(),
            new OwnershipBackfillEvidenceLoader(),
            new OwnershipBackfillEvidenceGateEvaluator(),
            new OwnershipBackfillGateResultWriter(),
            new OwnershipBackfillApplyPreconditionValidator(),
            new OwnershipBackfillApplyPlanGenerator(),
            new OwnershipBackfillApplyPlanWriter(),
            new OwnershipBackfillPlanSignoffValidator(),
            new OwnershipBackfillPlanSignoffWriter());
    }

    private static (string Readiness, string Plan, string Signoff) CreateRequiredFiles(string root)
    {
        var readiness = Path.Combine(root, "readiness.json");
        var plan = Path.Combine(root, "plan.json");
        var signoff = Path.Combine(root, "signoff.json");

        File.WriteAllText(readiness, "{}");
        File.WriteAllText(plan, "{}");
        File.WriteAllText(signoff, "{}");

        return (readiness, plan, signoff);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-staging-preflight-cli-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
