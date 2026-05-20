namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillCliHelpTests
{
    [Fact]
    public async Task Help_ExitsZero_AndListsCommands()
    {
        var cli = OwnershipBackfillCliTestFactory.Create();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(["--help"], stdout, stderr, CancellationToken.None);

        Assert.Equal(0, exitCode);
        var help = stdout.ToString();
        Assert.Contains("dry-run", help, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validate-evidence", help, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("plan-apply", help, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("signoff-plan", help, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validate-apply-readiness", help, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validate-staging-preflight", help, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validate-staging-acceptance", help, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("validate-production-promotion", help, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("apply", help, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("apply is intentionally disabled", help, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Help_UsesPlaceholders_AndNoRealConnectionString()
    {
        var cli = OwnershipBackfillCliTestFactory.Create();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(["--help"], stdout, stderr, CancellationToken.None);
        Assert.Equal(0, exitCode);

        var help = stdout.ToString();
        Assert.Contains("<connection-string>", help, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<path>", help, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<hash>", help, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=", help, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=", help, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnknownCommand_ExitsOne_AndSuggestsHelp()
    {
        var cli = OwnershipBackfillCliTestFactory.Create();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(["unknown-command"], stdout, stderr, CancellationToken.None);

        Assert.Equal(1, exitCode);
        var output = stderr.ToString();
        Assert.Contains("Unknown command", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Available commands", output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Use --help", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CommandSpecificHelp_ExitsZero()
    {
        var cli = OwnershipBackfillCliTestFactory.Create();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(["dry-run", "--help"], stdout, stderr, CancellationToken.None);

        Assert.Equal(0, exitCode);
        Assert.Contains("Command-specific help requested for: dry-run", stdout.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}

