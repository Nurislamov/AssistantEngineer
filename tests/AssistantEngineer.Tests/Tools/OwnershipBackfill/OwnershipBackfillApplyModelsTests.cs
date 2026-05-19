using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Models;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillApplyModelsTests
{
    [Fact]
    public void ApplySummary_NonClaimsIncludeNoExecutionClaim()
    {
        var summary = new OwnershipBackfillApplySummary
        {
            RunId = "run",
            StartedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = DateTimeOffset.UtcNow,
            Mode = "ApplyDesignOnly",
            TotalRecordsPlanned = 0,
            TotalRecordsUpdated = 0,
            TotalRecordsSkipped = 0,
            TotalRecordsFailed = 0,
            NonClaims = OwnershipBackfillConstants.NonClaims
        };

        Assert.Contains("No ownership backfill execution claim.", summary.NonClaims, StringComparer.Ordinal);
    }

    [Fact]
    public void PlannedRecord_HasNoPayloadFields()
    {
        var propertyNames = typeof(OwnershipBackfillApplyPlannedRecord)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain(propertyNames, name => name.Contains("Payload", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, name => name.Contains("RequestJson", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, name => name.Contains("Token", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, name => name.Contains("Secret", StringComparison.OrdinalIgnoreCase));
    }
}
