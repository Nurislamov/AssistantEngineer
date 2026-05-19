using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Scanning;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class NoDataOwnershipBackfillDryRunScannerTests
{
    [Fact]
    public async Task ScanAsync_ReturnsZeroCountDryRunSummary()
    {
        var scanner = new NoDataOwnershipBackfillDryRunScanner();
        var options = new OwnershipBackfillOptions(
            BatchSize: 500,
            MaxUnresolvedRate: 0.05d,
            EvidenceOutputDirectory: "unused",
            ConnectionString: null,
            DatabaseProvider: "None",
            IncludeLegacyUnscoped: false,
            NoDataDryRun: true);

        var result = await scanner.ScanAsync(options, CancellationToken.None);

        Assert.Equal("DryRun", result.Summary.Mode);
        Assert.Equal(0, result.Summary.TotalRecordsScanned);
        Assert.Equal(0, result.Summary.TotalRecordsResolvable);
        Assert.Equal(0, result.Summary.TotalRecordsUnresolved);
        Assert.Empty(result.UnresolvedRecords);
        Assert.Empty(result.PreviousValues);
        Assert.NotEmpty(result.Summary.NonClaims);
        Assert.Contains("No ownership backfill execution claim.", result.Summary.NonClaims);
    }
}
