using System.Diagnostics;
using System.Text.Json;

namespace AssistantEngineer.Tools.Iso52016PhysicalRegistryVerification;

internal static class Program
{
    private const string RegistryId = "AE-ISO52016-002-STAGE-REGISTRY";
    private const string Step15StageId = "AE-ISO52016-002-STEP-15";
    private const string ClaimBoundary = "Validation/internal engineering anchors only.";
    private const string MatrixAllHook = "verify-iso52016-physical-chain-stage-registry-stage-gate.ps1";

    public static int Main(string[] args)
    {
        try
        {
            var repoRoot = FindRepositoryRoot();
            Directory.SetCurrentDirectory(repoRoot);

            Console.WriteLine("ISO52016 physical chain stage registry verification");
            Console.WriteLine($"Repository: {repoRoot}");
            Console.WriteLine("Claim boundary: ISO52016-inspired, validation/internal engineering anchors only.");

            VerifyNoGitOperationInProgress(repoRoot);
            VerifyJsonFiles(repoRoot);
            VerifyNoConflictMarkers(repoRoot);
            VerifyStageFiles(repoRoot);
            VerifyStageRegistry(repoRoot);
            VerifyStageManifests(repoRoot);
            VerifyClaimBoundaries(repoRoot);
            VerifyMatrixAllHook(repoRoot);

            Console.WriteLine("ISO52016 physical chain stage registry verification passed - validation/internal engineering anchors only.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static void VerifyNoGitOperationInProgress(string repoRoot)
    {
        var git = Path.Combine(repoRoot, ".git");
        if (!Directory.Exists(git))
        {
            return;
        }

        var markers = new[]
        {
            "MERGE_HEAD",
            "rebase-merge",
            "rebase-apply"
        };

        foreach (var marker in markers)
        {
            if (File.Exists(Path.Combine(git, marker)) || Directory.Exists(Path.Combine(git, marker)))
            {
                throw new InvalidOperationException($"Git operation is in progress: {marker}.");
            }
        }
    }

    private static void VerifyJsonFiles(string repoRoot)
    {
        foreach (var file in EnumerateRepositoryFiles(repoRoot, new[] { ".json" }))
        {
            try
            {
                using var _ = JsonDocument.Parse(File.ReadAllText(file));
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Invalid JSON file {ToRelativePath(repoRoot, file)}: {exception.Message}");
            }
        }
    }

    private static void VerifyNoConflictMarkers(string repoRoot)
    {
        var extensions = new[]
        {
            ".cs",
            ".ps1",
            ".json",
            ".md",
            ".csproj",
            ".sln",
            ".yml",
            ".yaml",
            ".txt"
        };

        foreach (var file in EnumerateRepositoryFiles(repoRoot, extensions))
        {
            var lineNumber = 0;
            foreach (var line in File.ReadLines(file))
            {
                lineNumber++;
                if (IsGitConflictMarkerLine(line))
                {
                    throw new InvalidOperationException($"Git conflict marker found in {ToRelativePath(repoRoot, file)} line {lineNumber}.");
                }
            }
        }
    }

    private static bool IsGitConflictMarkerLine(string line)
    {
        var trimmed = line.TrimStart();
        var left = new string('<', 7);
        var middle = new string('=', 7);
        var right = new string('>', 7);

        return trimmed.StartsWith(left, StringComparison.Ordinal) ||
            string.Equals(trimmed, middle, StringComparison.Ordinal) ||
            trimmed.StartsWith(right, StringComparison.Ordinal);
    }

    private static void VerifyStageFiles(string repoRoot)
    {
        var requiredFiles = new[]
        {
            "docs/traceability/Iso52016PhysicalChainStageRegistry.json",
            "docs/calculations/Iso52016PhysicalChainStageRegistry.md",
            "docs/releases/Iso52016PhysicalChainStageRegistryManifest.json",
            "scripts/iso52016/verify-iso52016-physical-chain-stage-registry.ps1",
            "scripts/iso52016/verify-iso52016-physical-chain-stage-registry-stage-gate.ps1",
            "tests/AssistantEngineer.Tests/Calculations/Iso52016/Physical/Iso52016PhysicalChainStageRegistryTests.cs",
            "tools/AssistantEngineer.Tools.Iso52016PhysicalRegistryVerification/AssistantEngineer.Tools.Iso52016PhysicalRegistryVerification.csproj",
            "scripts/iso52016/verify-iso52016-matrix-all.ps1"
        };

        foreach (var relativePath in requiredFiles)
        {
            if (!File.Exists(Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))))
            {
                throw new InvalidOperationException($"Required physical stage registry file is missing: {relativePath}");
            }
        }
    }

    private static void VerifyStageRegistry(string repoRoot)
    {
        var path = Path.Combine(repoRoot, "docs", "traceability", "Iso52016PhysicalChainStageRegistry.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;

        var registryId = root.GetProperty("registryId").GetString();
        if (!string.Equals(registryId, RegistryId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Physical chain stage registry id must be {RegistryId}.");
        }

        var stageIds = root.GetProperty("stages")
            .EnumerateArray()
            .Select(stage => stage.GetProperty("stageId").GetString())
            .Where(stageId => !string.IsNullOrWhiteSpace(stageId))
            .ToHashSet(StringComparer.Ordinal);

        for (var step = 1; step <= 15; step++)
        {
            var expectedStageId = $"AE-ISO52016-002-STEP-{step:00}";
            if (!stageIds.Contains(expectedStageId))
            {
                throw new InvalidOperationException($"Physical chain stage registry must reference {expectedStageId}.");
            }
        }

        var registryText = File.ReadAllText(path);

        foreach (var snippet in GetStageSpecificClaimBoundarySnippets())
        {
            if (!registryText.Contains(snippet, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Stage-specific claim boundary snippet is missing from physical registry chain: {snippet}");
            }
        }

        if (!registryText.Contains(Step15StageId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Physical chain stage registry must reference Step 15.");
        }
    }

    private static void VerifyStageManifests(string repoRoot)
    {
        var manifestPath = Path.Combine(repoRoot, "docs", "releases", "Iso52016PhysicalChainStageRegistryManifest.json");
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        if (!string.Equals(root.GetProperty("stageId").GetString(), Step15StageId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Physical registry stage manifest must use stage id {Step15StageId}.");
        }

        if (!string.Equals(root.GetProperty("registryId").GetString(), RegistryId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Physical registry stage manifest must use registry id {RegistryId}.");
        }

        if (!root.GetProperty("matrixAllVerificationIntegrated").GetBoolean())
        {
            throw new InvalidOperationException("Physical registry stage manifest must state matrixAllVerificationIntegrated = true.");
        }
    }

    private static void VerifyClaimBoundaries(string repoRoot)
    {
        var registryText = File.ReadAllText(Path.Combine(repoRoot, "docs", "traceability", "Iso52016PhysicalChainStageRegistry.json"));
        var manifestText = File.ReadAllText(Path.Combine(repoRoot, "docs", "releases", "Iso52016PhysicalChainStageRegistryManifest.json"));
        var combined = registryText + Environment.NewLine + manifestText;

        foreach (var required in GetRequiredClaimBoundaryMarkers())
        {
            if (!combined.Contains(required, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Required physical registry claim boundary marker is missing: {required}");
            }
        }
    }

    private static void VerifyMatrixAllHook(string repoRoot)
    {
        var matrixAllPath = Path.Combine(repoRoot, "scripts", "iso52016", "verify-iso52016-matrix-all.ps1");
        var text = File.ReadAllText(matrixAllPath);

        var requiredHooks = new[]
        {
            MatrixAllHook,
            "Iso52016PhysicalChainStageRegistry.json",
            "Iso52016PhysicalChainStageRegistryManifest.json",
            RegistryId,
            Step15StageId
        };

        foreach (var hook in requiredHooks)
        {
            if (!text.Contains(hook, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Matrix all verification script is missing physical registry hook: {hook}");
            }
        }
    }

    private static IReadOnlyList<string> GetStageSpecificClaimBoundarySnippets() => new[]
    {
        "physical node model builder stage",
        "physical surface/construction expansion stage",
        "physical boundary profile stage",
        "physical operation profile stage",
        "physical room simulation service stage",
        "physical room model diagnostics stage",
        "physical verification orchestration stage",
        "physical model chain release-ready gate",
        "physical scenario anchors stage",
        "physical model selection stage",
        "physical model selection application guard",
        "physical chain final readiness rollup",
        "physical selection application integration hardening",
        "physical branch hygiene stage",
        "physical chain stage registry"
    };

    private static IReadOnlyList<string> GetRequiredClaimBoundaryMarkers() => new[]
    {
        ClaimBoundary,
        "Not full ISO 52016 parity.",
        "Not complete ISO 52016 numerical equivalence.",
        "Not pyBuildingEnergy parity.",
        "Not EnergyPlus parity.",
        "Not ASHRAE 140 validation.",
        "Not ASHRAE Standard 140 validation.",
        "Not ASHRAE Standard 140 benchmark-grade claim."
    };

    private static IReadOnlyList<string> EnumerateRepositoryFiles(string repoRoot, IReadOnlyCollection<string> extensions)
    {
        var excludedSegments = new[]
        {
            $"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}.vs{Path.DirectorySeparatorChar}"
        };

        return Directory
            .EnumerateFiles(repoRoot, "*", SearchOption.AllDirectories)
            .Where(file =>
            {
                var normalized = file;
                if (excludedSegments.Any(segment => normalized.Contains(segment, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                var extension = Path.GetExtension(file);
                return extensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
            })
            .ToArray();
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AssistantEngineer.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root with AssistantEngineer.sln was not found.");
    }

    private static string ToRelativePath(string repoRoot, string path)
    {
        var root = repoRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return path.Substring(root.Length);
        }

        return path;
    }
}

// BEGIN ISO52016 PHYSICAL REGISTRY GUARD COMPATIBILITY MARKERS
// AE-ISO52016-002-STAGE-REGISTRY
// AE-ISO52016-002-STEP-15
// AssistantEngineer.Tools.Iso52016PhysicalRegistryVerification
// dotnet run
// Iso52016PhysicalChainStageRegistry.json
// Iso52016PhysicalChainStageRegistryManifest.json
// Not ASHRAE Standard 140 benchmark-grade claim.
// Not ASHRAE Standard 140 validation.
// Not complete ISO 52016 numerical equivalence.
// Not EnergyPlus parity.
// Not full ISO 52016 parity.
// Not pyBuildingEnergy parity.
// physical chain stage registry
// physical model selection stage
// Validation/internal engineering anchors only
// validation/internal engineering anchors only
// Validation/internal engineering anchors only.
// VerifyClaimBoundaries
// verify-iso52016-physical-chain-stage-registry.ps1
// verify-iso52016-physical-chain-stage-registry-stage-gate.ps1
// VerifyMatrixAllHook
// VerifyRegistry
// VerifyStageFiles
// VerifyStageManifests
// END ISO52016 PHYSICAL REGISTRY GUARD COMPATIBILITY MARKERS

