using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Models;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillApplyExecutionResultWriterTests
{
    [Fact]
    public async Task WritesJsonMarkdownAndPreviousValuesArtifacts()
    {
        var root = CreateTempDirectory();

        try
        {
            var writer = new OwnershipBackfillApplyExecutionResultWriter();
            await writer.WriteAsync(CreateResult(), root, CancellationToken.None);

            Assert.Single(Directory.GetFiles(root, "ownership-backfill-apply-rehearsal-result-*.json"));
            Assert.Single(Directory.GetFiles(root, "ownership-backfill-apply-rehearsal-result-*.md"));
            Assert.Single(Directory.GetFiles(root, "ownership-backfill-rehearsal-previous-values-*.json"));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task WrittenJsonIsParsable()
    {
        var root = CreateTempDirectory();

        try
        {
            var writer = new OwnershipBackfillApplyExecutionResultWriter();
            await writer.WriteAsync(CreateResult(), root, CancellationToken.None);

            foreach (var path in Directory.GetFiles(root, "*.json"))
            {
                using var _ = JsonDocument.Parse(await File.ReadAllTextAsync(path));
            }
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task DoesNotWriteOutsideOutputDirectory()
    {
        var root = CreateTempDirectory();

        try
        {
            var writer = new OwnershipBackfillApplyExecutionResultWriter();
            await writer.WriteAsync(CreateResult(), root, CancellationToken.None);

            var parent = Directory.GetParent(root)!.FullName;
            var leaked = Directory.GetFiles(parent, "ownership-backfill-apply-rehearsal-result-*.json", SearchOption.TopDirectoryOnly)
                .Where(path => !path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Assert.Empty(leaked);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task ArtifactsContainNoSecretOrPayloadFields()
    {
        var root = CreateTempDirectory();

        try
        {
            var writer = new OwnershipBackfillApplyExecutionResultWriter();
            await writer.WriteAsync(CreateResult(), root, CancellationToken.None);

            var content = string.Join(Environment.NewLine, Directory.GetFiles(root, "*.json").Select(File.ReadAllText));
            Assert.DoesNotContain("payload", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("token", content, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static OwnershipBackfillApplyExecutionResult CreateResult()
    {
        return new OwnershipBackfillApplyExecutionResult
        {
            Succeeded = true,
            ExecutionId = "20260518060000-a1b2c3d4e5f6",
            Mode = "TestOnlyRehearsal",
            TotalRecordsPlanned = 1,
            TotalRecordsUpdated = 1,
            TotalRecordsSkipped = 0,
            TotalRecordsFailed = 0,
            Findings =
            [
                new OwnershipBackfillApplyExecutionFinding
                {
                    Code = "TEST_ONLY_EXECUTION_APPLIED",
                    Severity = "Info",
                    Message = "Applied in test-only rehearsal mode.",
                    RecordType = "Project",
                    RecordId = "11"
                }
            ],
            PreviousValues =
            [
                new OwnershipBackfillPreviousValueSnapshot
                {
                    RecordType = "Project",
                    RecordId = "11",
                    PreviousProjectId = 11,
                    PreviousBuildingId = null,
                    PreviousOrganizationId = null,
                    PreviousOwnerUserId = null
                }
            ],
            NonClaims = OwnershipBackfillConstants.NonClaims
        };
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-apply-rehearsal-writer-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}

