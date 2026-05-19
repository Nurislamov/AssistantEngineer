using System.Text.Json;
using AssistantEngineer.Tools.OwnershipBackfill.Models;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

public sealed class OwnershipBackfillPlanSignoffWriterTests
{
    [Fact]
    public async Task WritesJsonAndMarkdown()
    {
        var root = CreateTempDirectory();

        try
        {
            var writer = new OwnershipBackfillPlanSignoffWriter();
            var artifact = CreateArtifact();

            await writer.WriteAsync(artifact, root, cancellationToken: CancellationToken.None);

            Assert.Single(Directory.GetFiles(root, "ownership-backfill-plan-signoff-*.json"));
            Assert.Single(Directory.GetFiles(root, "ownership-backfill-plan-signoff-*.md"));
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task JsonArtifactParses()
    {
        var root = CreateTempDirectory();

        try
        {
            var writer = new OwnershipBackfillPlanSignoffWriter();
            var artifact = CreateArtifact();

            await writer.WriteAsync(artifact, root, cancellationToken: CancellationToken.None);
            var path = Directory.GetFiles(root, "ownership-backfill-plan-signoff-*.json").Single();

            using var _ = JsonDocument.Parse(await File.ReadAllTextAsync(path));
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
            var writer = new OwnershipBackfillPlanSignoffWriter();
            var artifact = CreateArtifact();

            await writer.WriteAsync(artifact, root, cancellationToken: CancellationToken.None);

            var parent = Directory.GetParent(root)!.FullName;
            var leaked = Directory.GetFiles(parent, "ownership-backfill-plan-signoff-*.json", SearchOption.TopDirectoryOnly)
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
    public async Task DoesNotStoreRawConfirmationPhrase()
    {
        var root = CreateTempDirectory();

        try
        {
            var writer = new OwnershipBackfillPlanSignoffWriter();
            var artifact = CreateArtifact();

            await writer.WriteAsync(artifact, root, cancellationToken: CancellationToken.None);
            var path = Directory.GetFiles(root, "ownership-backfill-plan-signoff-*.json").Single();
            var json = await File.ReadAllTextAsync(path);

            Assert.DoesNotContain(OwnershipBackfillConstants.PlanSignoffConfirmationPhrase, json, StringComparison.Ordinal);
            Assert.Contains("ConfirmationPhraseAccepted", json, StringComparison.Ordinal);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public async Task NoPayloadOrSecretFields()
    {
        var root = CreateTempDirectory();

        try
        {
            var writer = new OwnershipBackfillPlanSignoffWriter();
            var artifact = CreateArtifact();

            await writer.WriteAsync(artifact, root, cancellationToken: CancellationToken.None);
            var json = await File.ReadAllTextAsync(Directory.GetFiles(root, "ownership-backfill-plan-signoff-*.json").Single());

            Assert.DoesNotContain("payload", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    private static OwnershipBackfillPlanSignoffArtifact CreateArtifact()
    {
        return new OwnershipBackfillPlanSignoffArtifact
        {
            SignoffId = "20260518010101-abcd1234ef01",
            PlanId = "plan-001",
            PlanHash = "hash-001",
            PlanPath = @"D:\\plans\\ownership-backfill-apply-plan-plan-001.json",
            Reviewer = "reviewer-1",
            Ticket = "CHG-100",
            ConfirmationPhraseAccepted = true,
            SignedAtUtc = DateTimeOffset.Parse("2026-05-18T00:00:00Z"),
            ExpiresAtUtc = DateTimeOffset.Parse("2026-06-01T00:00:00Z"),
            ToolStage = "P6-06",
            Notes = "local review",
            NonClaims = OwnershipBackfillConstants.NonClaims
        };
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ae-signoff-writer-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
