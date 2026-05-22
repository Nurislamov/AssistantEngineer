using AssistantEngineer.Tools.OwnershipBackfill.Cli;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillArgumentReaderTests
{
    [Fact]
    public void Parse_KeyValueArguments_Succeeds()
    {
        var reader = new OwnershipBackfillArgumentReader();
        var descriptor = OwnershipBackfillCommandDescriptorCatalog.Get(OwnershipBackfillCommandType.DryRun);

        var parsedOk = reader.TryParse(
            ["--output", "out", "--batch-size", "100"],
            descriptor,
            out var parsed,
            out var error);

        Assert.True(parsedOk);
        Assert.Null(error);
        Assert.True(parsed.TryGetValue("--output", out var output));
        Assert.Equal("out", output);
        Assert.True(parsed.TryGetValue("--batch-size", out var batchSize));
        Assert.Equal("100", batchSize);
    }

    [Fact]
    public void Parse_MissingValue_ReturnsSafeError()
    {
        var reader = new OwnershipBackfillArgumentReader();
        var descriptor = OwnershipBackfillCommandDescriptorCatalog.Get(OwnershipBackfillCommandType.DryRun);

        var parsedOk = reader.TryParse(
            ["--output"],
            descriptor,
            out _,
            out var error);

        Assert.False(parsedOk);
        Assert.NotNull(error);
        Assert.Contains("--output requires a value.", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_UnknownOption_ReturnsSafeError()
    {
        var reader = new OwnershipBackfillArgumentReader();
        var descriptor = OwnershipBackfillCommandDescriptorCatalog.Get(OwnershipBackfillCommandType.DryRun);

        var parsedOk = reader.TryParse(
            ["--unexpected", "x"],
            descriptor,
            out _,
            out var error);

        Assert.False(parsedOk);
        Assert.NotNull(error);
        Assert.Contains("Unknown option: --unexpected", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_DoesNotLeakSecretLikeValuesInErrors()
    {
        const string secret = "Data Source=fake.db;Password=super-secret";
        var reader = new OwnershipBackfillArgumentReader();
        var descriptor = OwnershipBackfillCommandDescriptorCatalog.Get(OwnershipBackfillCommandType.DryRun);

        var parsedOk = reader.TryParse(
            ["--connection-string", secret, "--unexpected"],
            descriptor,
            out _,
            out var error);

        Assert.False(parsedOk);
        Assert.NotNull(error);
        Assert.DoesNotContain(secret, error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("super-secret", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_RepeatedOption_LastValueWins_AndRepeatedIsTracked()
    {
        var reader = new OwnershipBackfillArgumentReader();
        var descriptor = OwnershipBackfillCommandDescriptorCatalog.Get(OwnershipBackfillCommandType.DryRun);

        var parsedOk = reader.TryParse(
            ["--output", "a", "--output", "b"],
            descriptor,
            out var parsed,
            out var error);

        Assert.True(parsedOk);
        Assert.Null(error);
        Assert.True(parsed.TryGetValue("--output", out var output));
        Assert.Equal("b", output);
        Assert.Contains("--output", parsed.RepeatedOptions, StringComparer.OrdinalIgnoreCase);
    }
}
