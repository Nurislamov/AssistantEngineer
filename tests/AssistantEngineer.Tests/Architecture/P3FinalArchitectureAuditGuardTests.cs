using System.Text.RegularExpressions;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Architecture;

public sealed class P3FinalArchitectureAuditGuardTests
{
    [Fact]
    public void FinalAuditDocument_Exists_WithRequiredSections_AndNonClaims()
    {
        var path = Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "p3-final-architecture-audit.md");
        Assert.True(File.Exists(path), $"Final P3 audit document must exist: {path}");

        var content = File.ReadAllText(path);

        Assert.Contains("# P3 Final Architecture Audit", content, StringComparison.Ordinal);
        Assert.Contains("## Done", content, StringComparison.Ordinal);
        Assert.Contains("## Partial", content, StringComparison.Ordinal);
        Assert.Contains("## Remaining", content, StringComparison.Ordinal);
        Assert.Contains("## Hotspots after P3", content, StringComparison.Ordinal);
        Assert.Contains("## Guard coverage", content, StringComparison.Ordinal);
        Assert.Contains("## Non-claims", content, StringComparison.Ordinal);
        Assert.Contains("## P4 backlog candidates", content, StringComparison.Ordinal);

        Assert.Contains("calculation physics unchanged", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("public API routes unchanged", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no full external parity claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no certified compliance claim", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FinalAuditDocument_DoesNotContainForbiddenOverclaimPhrases()
    {
        var path = Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "p3-final-architecture-audit.md");
        Assert.True(File.Exists(path), $"Final P3 audit document must exist: {path}");

        var content = File.ReadAllText(path);

        var full = "full";
        var parity = "parity";
        var validation = "validation";
        var production = "production";
        var complete = "complete";
        var energyPlus = "EnergyPlus";
        var ashrae = "ASHRAE";
        var py = "py";
        var building = "Building";
        var energy = "Energy";

        var forbidden = new[]
        {
            full + " " + parity,
            full + " " + validation,
            production + " " + complete,
            energyPlus + " " + parity,
            ashrae + " 140 " + validation + " claim",
            py + building + energy
        };

        foreach (var marker in forbidden)
        {
            Assert.DoesNotContain(marker, content, StringComparison.OrdinalIgnoreCase);
        }

        var positiveCompliancePattern = new Regex(@"(?<!no\s)certified compliance", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        Assert.False(positiveCompliancePattern.IsMatch(content), "Final P3 audit must not contain positive certified-compliance wording.");
    }
}
