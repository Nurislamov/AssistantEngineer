using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Governance;
using AssistantEngineer.Modules.Calculations.Application.Services.Governance;
using AssistantEngineer.Modules.Calculations.Application.Services.Rollup;

namespace AssistantEngineer.Tools.EngineeringGovernance;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
            {
                PrintHelp();
                return 0;
            }

            var command = args[0];
            var options = ToolOptions.Parse(args.Skip(1).ToArray());
            var repoRoot = ResolveRepositoryRoot(options.RepositoryRoot);

            return command switch
            {
                "list-stages" => ListStages(repoRoot),
                "verify-manifests" => VerifyManifests(repoRoot, options.FailOnWarning),
                "verify-claims" => VerifyClaims(repoRoot, options.FailOnWarning),
                "verify-release-readiness" => VerifyReleaseReadiness(repoRoot, options.FailOnWarning),
                "write-status-sample" => WriteStatusSample(repoRoot, options),
                _ => ExitInvalidArguments($"Unknown command: {command}")
            };
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("AssistantEngineer Engineering Governance tool");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  list-stages --repo-root <path>");
        Console.WriteLine("  verify-manifests --repo-root <path> [--fail-on-warning]");
        Console.WriteLine("  verify-claims --repo-root <path> [--fail-on-warning]");
        Console.WriteLine("  verify-release-readiness --repo-root <path> [--fail-on-warning]");
        Console.WriteLine("  write-status-sample --repo-root <path> [--output <path>] [--write-docs-sample]");
    }

    private static int ListStages(string repoRoot)
    {
        var provider = new EngineeringStageManifestRegistryProvider();
        var registry = provider.BuildRegistry(repoRoot);

        foreach (var stage in registry.Stages.OrderBy(item => item.StageId, StringComparer.Ordinal))
            Console.WriteLine($"{stage.StageId}\t{stage.Status}\t{stage.ManifestPath}");

        return 0;
    }

    private static int VerifyManifests(string repoRoot, bool failOnWarning)
    {
        var provider = new EngineeringStageManifestRegistryProvider();
        var validator = new EngineeringStageManifestRegistryValidator();

        var result = validator.Validate(provider.BuildRegistry(repoRoot));
        return EmitResult(result, failOnWarning);
    }

    private static int VerifyClaims(string repoRoot, bool failOnWarning)
    {
        var scanner = new EngineeringClaimBoundaryScanner();
        var result = scanner.ScanRepository(repoRoot);
        return EmitResult(result, failOnWarning);
    }

    private static int VerifyReleaseReadiness(string repoRoot, bool failOnWarning)
    {
        var service = BuildReleaseReadinessService();
        var result = service.Evaluate(repoRoot);
        return EmitResult(result, failOnWarning);
    }

    private static int WriteStatusSample(string repoRoot, ToolOptions options)
    {
        var service = BuildReleaseReadinessService();
        var result = service.Evaluate(repoRoot);

        var defaultOutputPath = Path.Combine(
            repoRoot,
            "artifacts",
            "generated",
            "engineering-core-v2",
            "engineering-release-readiness.generated.json");
        var outputPath = options.OutputPath is null
            ? defaultOutputPath
            : NormalizePath(repoRoot, options.OutputPath);

        var docsSamplePath = NormalizePath(repoRoot, "docs/api/engineering-core-v2/engineering-release-readiness.sample.json");
        if (string.Equals(outputPath, docsSamplePath, StringComparison.OrdinalIgnoreCase) && !options.WriteDocsSample)
        {
            throw new ArgumentException("Refusing to overwrite docs sample without --write-docs-sample.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? repoRoot);

        var payload = CreateStatusSample(result);
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputPath, json);

        Console.WriteLine($"Status sample written: {ToRelativePath(repoRoot, outputPath)}");

        if (options.WriteDocsSample)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(docsSamplePath) ?? repoRoot);
            File.WriteAllText(docsSamplePath, json);
            Console.WriteLine($"Docs sample updated: {ToRelativePath(repoRoot, docsSamplePath)}");
        }

        return EmitResult(result, options.FailOnWarning);
    }

    private static object CreateStatusSample(EngineeringGovernanceCheckResult result)
    {
        var claimBoundary = new[]
        {
            "Engineering Core V2 governance and internal release readiness.",
            "Internal deterministic engineering governance only.",
            "Compatibility behavior preserved by default.",
            "Inspired calculation paths remain opt-in.",
            "No full ISO/EN compliance claim.",
            "No StandardReference equivalence claim.",
            "No EnergyPlus comparison workflow claim.",
            "No ASHRAE 140 / BESTEST-style validation anchor claim.",
            "No external certification claim.",
            "No automatic production data mutation."
        };

        var optInFlags = new[]
        {
            new { flag = "NaturalVentilationOptions.UseIso16798InspiredCalculator", defaultValue = false },
            new { flag = "Iso13370GroundHeatTransferOptions.UseIso13370InspiredBoundaryCalculator", defaultValue = false },
            new { flag = "DomesticHotWaterOptions.UseIso12831InspiredCalculator", defaultValue = false },
            new { flag = "SystemEnergyOptions.UseEn15316InspiredChain", defaultValue = false },
            new { flag = "Iso52016ConstructionOptions.UseConstructionLayerMassInput", defaultValue = false }
        };

        var knownLimitations = new[]
        {
            "No EnergyPlus comparison workflow claim.",
            "No StandardReference equivalence claim.",
            "No ASHRAE 140 / BESTEST-style validation anchor claim.",
            "External numerical validation is still not complete.",
            "Inspired calculation paths remain opt-in.",
            "Building input validation suggests corrections but does not mutate production data."
        };

        var blockedStages = result.Diagnostics
            .Where(item => item.Severity is EngineeringGovernanceDiagnosticSeverity.Error or EngineeringGovernanceDiagnosticSeverity.Critical)
            .Select(item => item.StageId)
            .Where(stageId => !string.IsNullOrWhiteSpace(stageId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new
        {
            releaseReadinessId = "EngineeringCoreV2InternalReadiness",
            status = result.ReadinessStatus.ToString(),
            claimBoundary,
            defaultBehavior = new
            {
                compatibilityBehaviorPreserved = true,
                inspiredPathsRemainOptIn = true
            },
            optInFlags,
            closedStages = result.StageSummaries,
            internalOnlyStages = result.StageSummaries,
            blockedStages,
            knownLimitations,
            forbiddenClaims = new EngineeringClaimBoundaryScanner()
                .GetDefaultForbiddenTokens()
                .Select(token => $"No {token} claim.")
                .ToArray(),
            generatedArtifactsPolicy = new
            {
                generatedArtifactsMustNotBeCommitted = true,
                allowedFolders = new[] { "artifacts/", "generated/", "TestResults/", "bin/", "obj/" }
            },
            disclosureFiles = new[]
            {
                "docs/calculations/ExternalReferenceValidationVerification.md",
                "docs/calculations/EngineeringCoreV1Scope.md",
                "docs/api/engineering-core-v1/status.sample.json",
                "docs/api/engineering-core-v1/calculation-mode-rollup.sample.json",
                "docs/api/engineering-core-v2/status.sample.json",
                "docs/api/engineering-core-v2/engineering-release-readiness.sample.json"
            }
        };
    }

    private static int EmitResult(EngineeringGovernanceCheckResult result, bool failOnWarning)
    {
        Console.WriteLine($"Check: {result.CheckId}");
        Console.WriteLine($"Readiness: {result.ReadinessStatus}");
        Console.WriteLine($"Checks: {result.PassedChecks}/{result.TotalChecks}");
        Console.WriteLine($"Warnings: {result.WarningCount}; Errors: {result.ErrorCount}; Critical: {result.CriticalCount}");

        foreach (var diagnostic in result.Diagnostics)
        {
            var location = diagnostic.FilePath is null
                ? string.Empty
                : diagnostic.LineNumber is null
                    ? $" ({diagnostic.FilePath})"
                    : $" ({diagnostic.FilePath}:{diagnostic.LineNumber})";
            Console.WriteLine($"- [{diagnostic.Severity}] {diagnostic.Code}: {diagnostic.Message}{location}");
        }

        if (result.CriticalCount > 0 || result.ErrorCount > 0)
            return 1;

        if (failOnWarning && result.WarningCount > 0)
            return 1;

        return 0;
    }

    private static EngineeringCoreV2ReleaseReadinessService BuildReleaseReadinessService()
    {
        var registryProvider = new EngineeringStageManifestRegistryProvider();
        var registryValidator = new EngineeringStageManifestRegistryValidator();
        var scanner = new EngineeringClaimBoundaryScanner();
        var rollupProvider = new EngineeringCalculationModeCatalogProvider();

        return new EngineeringCoreV2ReleaseReadinessService(
            registryProvider,
            registryValidator,
            scanner,
            rollupProvider);
    }

    private static int ExitInvalidArguments(string message)
    {
        Console.Error.WriteLine(message);
        return 2;
    }

    private static string ResolveRepositoryRoot(string? explicitRoot)
    {
        if (!string.IsNullOrWhiteSpace(explicitRoot))
            return Path.GetFullPath(explicitRoot);

        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AssistantEngineer.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root containing AssistantEngineer.sln was not found.");
    }

    private static string NormalizePath(string repoRoot, string path)
    {
        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);

        return Path.GetFullPath(Path.Combine(repoRoot, path.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static string ToRelativePath(string repoRoot, string path)
    {
        var fullRoot = Path.GetFullPath(repoRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(path);

        if (fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            return fullPath.Substring(fullRoot.Length).Replace(Path.DirectorySeparatorChar, '/');

        return path;
    }

    private sealed record ToolOptions(
        string? RepositoryRoot,
        string? OutputPath,
        bool FailOnWarning,
        bool WriteDocsSample)
    {
        public static ToolOptions Parse(IReadOnlyList<string> args)
        {
            string? repositoryRoot = null;
            string? outputPath = null;
            var failOnWarning = false;
            var writeDocsSample = false;

            for (var index = 0; index < args.Count; index++)
            {
                var arg = args[index];

                if (string.Equals(arg, "--repo-root", StringComparison.OrdinalIgnoreCase))
                {
                    if (index + 1 >= args.Count)
                        throw new ArgumentException("--repo-root requires a value.");

                    repositoryRoot = args[++index];
                    continue;
                }

                if (string.Equals(arg, "--output", StringComparison.OrdinalIgnoreCase))
                {
                    if (index + 1 >= args.Count)
                        throw new ArgumentException("--output requires a value.");

                    outputPath = args[++index];
                    continue;
                }

                if (string.Equals(arg, "--fail-on-warning", StringComparison.OrdinalIgnoreCase))
                {
                    failOnWarning = true;
                    continue;
                }

                if (string.Equals(arg, "--write-docs-sample", StringComparison.OrdinalIgnoreCase))
                {
                    writeDocsSample = true;
                    continue;
                }

                throw new ArgumentException($"Unknown option: {arg}");
            }

            return new ToolOptions(repositoryRoot, outputPath, failOnWarning, writeDocsSample);
        }
    }
}
