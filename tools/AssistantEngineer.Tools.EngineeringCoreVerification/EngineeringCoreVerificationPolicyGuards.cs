using System.Text.Json;

namespace AssistantEngineer.Tools.EngineeringCoreVerification;

internal sealed class EngineeringCoreVerificationPolicyGuards(
    EngineeringCoreVerificationFileSystem fileSystem)
{
    public void AssertNoForbiddenTerminologyAndClaims(string repoRoot)
    {
        var forbidden = BuildForbiddenTermsAndClaims();
        var scanTargets = new[]
        {
            Path.Combine(repoRoot, "README.md"),
            Path.Combine(repoRoot, "AssistantEngineer.sln"),
            Path.Combine(repoRoot, ".github"),
            Path.Combine(repoRoot, "docker"),
            Path.Combine(repoRoot, "src"),
            Path.Combine(repoRoot, "tests"),
            Path.Combine(repoRoot, "docs"),
            Path.Combine(repoRoot, "scripts"),
            Path.Combine(repoRoot, "tools")
        };

        var violations = new List<string>();
        foreach (var target in scanTargets)
        {
            foreach (var file in fileSystem.EnumerateTextFiles(target))
            {
                var text = fileSystem.ReadAllText(file);
                foreach (var marker in forbidden)
                {
                    if (text.Contains(marker, StringComparison.OrdinalIgnoreCase))
                        violations.Add($"{Path.GetRelativePath(repoRoot, file)} => {marker}");
                }
            }
        }

        if (violations.Count > 0)
        {
            throw new InvalidOperationException(
                "Forbidden terminology/claim wording found:\n" +
                string.Join('\n', violations.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v, StringComparer.OrdinalIgnoreCase)));
        }
    }

    public void AssertExternalComparisonWorkflowFoundation(string repoRoot)
    {
        var requiredPaths = new[]
        {
            Path.Combine(repoRoot, "docs", "validation", "ExternalComparisonCaseRegistry.json"),
            Path.Combine(repoRoot, "docs", "calculations", "ExternalComparisonWorkflow.md"),
            Path.Combine(repoRoot, "docs", "calculations", "EnergyPlusComparisonWorkflow.md"),
            Path.Combine(repoRoot, "docs", "calculations", "Ashrae140BestestStyleAnchors.md"),
            Path.Combine(repoRoot, "tests", "fixtures", "external-comparison", "energyplus", "README.md"),
            Path.Combine(repoRoot, "tests", "fixtures", "external-comparison", "ashrae140-style", "README.md")
        };

        var missing = requiredPaths.Where(path => !fileSystem.FileExists(path)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                "External comparison workflow foundation files are missing:\n" +
                string.Join('\n', missing.Select(path => Path.GetRelativePath(repoRoot, path))));
        }

        using var registry = JsonDocument.Parse(
            fileSystem.ReadAllText(Path.Combine(repoRoot, "docs", "validation", "ExternalComparisonCaseRegistry.json")));

        if (!registry.RootElement.TryGetProperty("cases", out var casesNode) || casesNode.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("ExternalComparisonCaseRegistry.json must contain a 'cases' array.");

        var unsupportedClaimMarkers = new[]
        {
            ("energyplus " + "validated").ToLowerInvariant(),
            ("ashrae 140 " + "validated").ToLowerInvariant(),
            ("bestest " + "passed").ToLowerInvariant()
        };

        var violations = new List<string>();
        foreach (var caseNode in casesNode.EnumerateArray())
        {
            var caseId = GetString(caseNode, "caseId");
            var status = GetString(caseNode, "status");
            var expectedOutput = caseNode.TryGetProperty("expectedOutput", out var expectedNode) ? expectedNode : default;
            var tolerance = caseNode.TryGetProperty("tolerance", out var toleranceNode) ? toleranceNode : default;
            var provenance = caseNode.TryGetProperty("provenance", out var provenanceNode) ? provenanceNode : default;

            var hasExpectedOutputPath =
                expectedOutput.ValueKind == JsonValueKind.Object &&
                !string.IsNullOrWhiteSpace(GetString(expectedOutput, "outputPath"));

            var hasTolerance =
                tolerance.ValueKind == JsonValueKind.Object &&
                tolerance.TryGetProperty("relativePercent", out var relativePercent) &&
                relativePercent.ValueKind == JsonValueKind.Number &&
                tolerance.TryGetProperty("absolute", out var absolute) &&
                absolute.ValueKind == JsonValueKind.Number;

            var hasProvenance =
                provenance.ValueKind == JsonValueKind.Object &&
                !string.IsNullOrWhiteSpace(GetString(provenance, "sourceTool")) &&
                !string.IsNullOrWhiteSpace(GetString(provenance, "artifactPath"));

            if (status.Equals("PassedTolerance", StringComparison.Ordinal))
            {
                if (!hasExpectedOutputPath || !hasTolerance || !hasProvenance)
                {
                    violations.Add(
                        $"{caseId}: PassedTolerance requires expectedOutput.outputPath + tolerance + provenance.");
                }
            }

            if (status.Equals("ExternalOutputImported", StringComparison.Ordinal))
            {
                if (!hasExpectedOutputPath || !hasProvenance)
                {
                    violations.Add(
                        $"{caseId}: ExternalOutputImported requires expectedOutput.outputPath + provenance.");
                }
            }

            if (status.Equals("Compared", StringComparison.Ordinal) || status.Equals("FailedTolerance", StringComparison.Ordinal))
            {
                if (!hasExpectedOutputPath || !hasTolerance)
                {
                    violations.Add(
                        $"{caseId}: {status} requires expectedOutput.outputPath + tolerance.");
                }
            }

            var claimBoundary = GetString(caseNode, "claimBoundary");
            var notes = caseNode.TryGetProperty("notes", out var notesNode) && notesNode.ValueKind == JsonValueKind.Array
                ? string.Join(" ", notesNode.EnumerateArray().Select(item => item.GetString() ?? string.Empty))
                : string.Empty;
            var fullText = $"{GetString(caseNode, "name")} {GetString(caseNode, "workflow")} {claimBoundary} {notes}".ToLowerInvariant();

            foreach (var marker in unsupportedClaimMarkers)
            {
                if (fullText.Contains(marker, StringComparison.Ordinal))
                    violations.Add($"{caseId}: unsupported claim marker '{marker}'.");
            }
        }

        if (violations.Count > 0)
        {
            throw new InvalidOperationException(
                "External comparison workflow guard violations found:\n" +
                string.Join('\n', violations.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(item => item, StringComparer.OrdinalIgnoreCase)));
        }

        static string GetString(JsonElement node, string propertyName)
        {
            return node.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
                ? property.GetString() ?? string.Empty
                : string.Empty;
        }
    }

    private static IReadOnlyList<string> BuildForbiddenTermsAndClaims()
    {
        var py = "py";
        var pe = "Building";
        var energy = "Energy";
        var be = "BE";
        var calc = "Calculation";
        var parity = "Parity";
        var underscore = "_";

        return
        [
            py + pe + energy,
            py + pe.ToLowerInvariant() + energy.ToLowerInvariant(),
            py + be,
            "donor" + " project",
            "reference" + " donor",
            "donor" + " methodology",
            py + pe + energy + "-style",
            "parity with " + py + pe + energy,
            "full " + "parity",
            "fully " + "validated",
            "EnergyPlus " + "parity",
            "ASHRAE 140 " + "validated",
            energy + " " + calc + " equivalence",
            energy + " " + calc.ToLowerInvariant() + " equivalence",
            "reference " + "implementation",
            "equivalence " + "target",
            py + pe + energy + " parity",
            energy + calc + parity,
            "ENERGY" + underscore + "CALCULATION" + underscore + "PARITY",
            parity + "Matrix",
            energy + calc + parity + "Plan",
            energy + calc + parity + "Verification",
            "energy" + calc + parity
        ];
    }
}
