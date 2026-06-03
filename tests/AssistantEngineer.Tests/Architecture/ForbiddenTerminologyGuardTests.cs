namespace AssistantEngineer.Tests.Architecture;

public sealed class ForbiddenTerminologyGuardTests
{
    [Fact]
    public void PublicAndProductAreas_DoNotContainForbiddenTerminology()
    {
        var scanTargets = new[]
        {
            Path.Combine(TestPaths.RepoRoot, "README.md"),
            Path.Combine(TestPaths.RepoRoot, "AssistantEngineer.sln"),
            Path.Combine(TestPaths.RepoRoot, ".github"),
            Path.Combine(TestPaths.RepoRoot, "docker"),
            Path.Combine(TestPaths.RepoRoot, "src"),
            Path.Combine(TestPaths.RepoRoot, "tests"),
            Path.Combine(TestPaths.RepoRoot, "docs"),
            Path.Combine(TestPaths.RepoRoot, "scripts"),
            Path.Combine(TestPaths.RepoRoot, "tools")
        };

        var forbidden = BuildForbiddenTerms();
        var allowedFile = Path.GetFullPath(Path.Combine(
            TestPaths.RepoRoot,
            "tests",
            "AssistantEngineer.Tests",
            "Architecture",
            "ForbiddenTerminologyGuardTests.cs"));
        var allowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            allowedFile,
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "terminology-and-claims-vocabulary.md")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "terminology-and-claims-vocabulary.json")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "terminology-claims-surface-cleanup.md")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "terminology-claims-surface-cleanup.json")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "p8-engineering-domain-hardening-closure.md")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "architecture", "p8-engineering-domain-hardening-closure.json")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "engineering-calculation-validation-roadmap.md")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "engineering-calculation-validation-roadmap.json")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-evidence-inventory.md")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-evidence-inventory.json")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-claims-policy.md")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-claims-policy.json")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-fixture-provenance-model.md")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-fixture-provenance-model.json")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-fixture-provenance-inventory.md")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-fixture-provenance-inventory.json")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-decomposition-review.md")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-decomposition-review.json")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-component-map.md")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-component-map.json")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-behavior-characterization-inventory.md")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-behavior-characterization-inventory.json")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-design.md")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-design.json")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-risk-register.md")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-seam-risk-register.json")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-characterization-hardening.md")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "docs", "validation", "iso52016-matrix-solver-characterization-hardening.json")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "tests", "AssistantEngineer.Tests", "Architecture", "P8TerminologyClaimsVocabularyTests.cs")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "tests", "AssistantEngineer.Tests", "Architecture", "P8TerminologyClaimsSurfaceCleanupTests.cs")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "tests", "AssistantEngineer.Tests", "Architecture", "P8EngineeringDomainHardeningClosureBoundaryTests.cs")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "tests", "AssistantEngineer.Tests", "Architecture", "P9ValidationFixtureProvenanceModelTests.cs")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "tests", "AssistantEngineer.Tests", "Architecture", "P9ValidationFixtureProvenanceInventoryTests.cs")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "tests", "AssistantEngineer.Tests", "Architecture", "P9Iso52016DecompositionReviewTests.cs")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "tests", "AssistantEngineer.Tests", "Architecture", "P9Iso52016BehaviorCharacterizationInventoryTests.cs")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "tests", "AssistantEngineer.Tests", "Architecture", "P9Iso52016BehaviorCharacterizationCoverageTests.cs")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "tests", "AssistantEngineer.Tests", "Architecture", "P9Iso52016MatrixSolverSeamDesignTests.cs")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "tests", "AssistantEngineer.Tests", "Architecture", "P9Iso52016MatrixSolverSeamRiskRegisterTests.cs")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "tests", "AssistantEngineer.Tests", "Architecture", "P9Iso52016SeamDesignNoPhysicsChangeTests.cs")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "tests", "AssistantEngineer.Tests", "Architecture", "P9Iso52016MatrixSolverCharacterizationHardeningTests.cs")),
            Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "tests", "AssistantEngineer.Tests", "Architecture", "P9Iso52016MatrixSolverCharacterizationCoverageTests.cs"))
        };

        var violations = new List<string>();
        foreach (var target in scanTargets)
        {
            foreach (var file in EnumerateTextFiles(target))
            {
                if (allowedFiles.Contains(Path.GetFullPath(file)))
                    continue;

                var text = File.ReadAllText(file);
                foreach (var marker in forbidden)
                {
                    if (text.Contains(marker, StringComparison.OrdinalIgnoreCase))
                    {
                        violations.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, file)} => {marker}");
                    }
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Forbidden terminology was found:\n" + string.Join('\n', violations.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v, StringComparer.OrdinalIgnoreCase)));
    }

    private static IReadOnlyList<string> BuildForbiddenTerms()
    {
        var py = "py";
        var be = "BE";
        var pe = "Building";
        var energy = "Energy";
        var calc = "Calculation";
        var parity = "Parity";
        var underscore = "_";

        return new[]
        {
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
        };
    }

    private static IEnumerable<string> EnumerateTextFiles(string path)
    {
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".git", "bin", "obj", "node_modules", "dist", "coverage", "TestResults"
        };

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".md", ".json", ".yml", ".yaml", ".ps1", ".txt", ".tsx", ".ts", ".xml", ".csv", ".sln"
        };

        if (File.Exists(path))
        {
            if (allowedExtensions.Contains(Path.GetExtension(path)))
                yield return path;

            yield break;
        }

        if (!Directory.Exists(path))
            yield break;

        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(path, file);
            var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (segments.Any(segment => excluded.Contains(segment)))
                continue;

            if (!allowedExtensions.Contains(Path.GetExtension(file)))
                continue;

            yield return file;
        }
    }
}
