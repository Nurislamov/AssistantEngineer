namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillCliRedactionTests
{
    [Fact]
    public async Task ConnectionString_IsRedactedInErrors()
    {
        const string fakeSecret = "Data Source=fake.db;Password=super-secret";
        var cli = OwnershipBackfillCliTestFactory.Create();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(
            [fakeSecret],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.Equal(1, exitCode);
        var combined = stdout.ToString() + Environment.NewLine + stderr.ToString();
        Assert.DoesNotContain(fakeSecret, combined, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<redacted>", combined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SecretLikeArgValues_AreNotPrinted()
    {
        const string tokenValue = "TOP-SECRET-TOKEN-VALUE";
        var cli = OwnershipBackfillCliTestFactory.Create();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(
            ["--token", tokenValue],
            stdout,
            stderr,
            CancellationToken.None);

        Assert.Equal(1, exitCode);
        var combined = stdout.ToString() + Environment.NewLine + stderr.ToString();
        Assert.DoesNotContain(tokenValue, combined, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--token", combined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DisabledApply_DoesNotPrintConnectionString()
    {
        const string fakeConnection = "Data Source=fake.db;Password=ultra-secret";
        var cli = OwnershipBackfillCliTestFactory.Create();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = await cli.ExecuteAsync(
        [
            "apply",
            "--enable-apply",
            "--confirm", "I_UNDERSTAND_THIS_WRITES_OWNERSHIP_METADATA",
            "--evidence", "x",
            "--gate-result", "y",
            "--plan", "z",
            "--plan-signoff", "s",
            "--output", "o",
            "--database-provider", "SQLite",
            "--connection-string", fakeConnection
        ],
        stdout,
        stderr,
        CancellationToken.None);

        Assert.Equal(1, exitCode);
        var combined = stdout.ToString() + Environment.NewLine + stderr.ToString();
        Assert.DoesNotContain(fakeConnection, combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ultra-secret", combined, StringComparison.OrdinalIgnoreCase);
    }
}
