using System.Diagnostics;

namespace AssistantEngineer.Tools.EngineeringCoreRelease;

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

            var repoRoot = FindRepositoryRoot();
            Directory.SetCurrentDirectory(repoRoot);

            var command = args[0];
            var toolArgs = args.Skip(1).ToArray();

            return command switch
            {
                "regenerate-artifacts" => RegenerateArtifacts(Has(toolArgs, "--skip-missing") || Has(toolArgs, "-SkipMissing")),
                "verify-smoke" => VerifySmoke(
                    Has(toolArgs, "--skip-frontend") || Has(toolArgs, "-SkipFrontend"),
                    noRestore: Has(toolArgs, "--no-restore") || Has(toolArgs, "-NoRestore"),
                    noBuild: Has(toolArgs, "--no-build") || Has(toolArgs, "-NoBuild")),
                "verify-contracts" => VerifyContracts(
                    Has(toolArgs, "--skip-frontend") || Has(toolArgs, "-SkipFrontend"),
                    Has(toolArgs, "--skip-regenerate") || Has(toolArgs, "-SkipRegenerate"),
                    noRestore: Has(toolArgs, "--no-restore") || Has(toolArgs, "-NoRestore"),
                    noBuild: Has(toolArgs, "--no-build") || Has(toolArgs, "-NoBuild")),
                "verify-manifest" => VerifyManifest(
                    Has(toolArgs, "--skip-frontend") || Has(toolArgs, "-SkipFrontend"),
                    noRestore: Has(toolArgs, "--no-restore") || Has(toolArgs, "-NoRestore"),
                    noBuild: Has(toolArgs, "--no-build") || Has(toolArgs, "-NoBuild")),
                "assert-release-ready" => AssertReleaseReady(new ReleaseReadyOptions(
                    SkipFrontend: Has(toolArgs, "--skip-frontend") || Has(toolArgs, "-SkipFrontend"),
                    SkipFullDotnet: Has(toolArgs, "--skip-full-dotnet") || Has(toolArgs, "-SkipFullDotnet"),
                    SkipGitStatus: Has(toolArgs, "--skip-git-status") || Has(toolArgs, "-SkipGitStatus"),
                    Fast: Has(toolArgs, "--fast") || Has(toolArgs, "-Fast"),
                    NoRestore: Has(toolArgs, "--no-restore") || Has(toolArgs, "-NoRestore"),
                    NoBuild: Has(toolArgs, "--no-build") || Has(toolArgs, "-NoBuild"))),
                _ => Unknown(command)
            };
        }
        catch (Exception exception)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(exception.Message);
            Console.ResetColor();
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("AssistantEngineer Engineering Core release tools");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  regenerate-artifacts [--skip-missing]");
        Console.WriteLine("  verify-smoke [--skip-frontend]");
        Console.WriteLine("  verify-contracts [--skip-frontend] [--skip-regenerate]");
        Console.WriteLine("  verify-manifest [--skip-frontend]");
        Console.WriteLine("  assert-release-ready [--skip-frontend] [--skip-full-dotnet] [--skip-git-status] [--fast] [--no-restore] [--no-build]");
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp();
        return 1;
    }

    private static int RegenerateArtifacts(bool skipMissing)
    {
        Console.WriteLine("Engineering Core V1 artifact regeneration");

        var generators = new[]
        {
            ".\\scripts\\engineering-core\\generate-engineering-core-v1-release-evidence.ps1",
            ".\\scripts\\engineering-core\\generate-engineering-core-v1-api-contract-snapshots.ps1",
            ".\\scripts\\engineering-core\\generate-engineering-core-v1-report-contract-snapshots.ps1",
            ".\\scripts\\engineering-core\\generate-engineering-core-v1-export-disclosure-checklist.ps1",
            ".\\scripts\\engineering-core\\generate-engineering-core-v1-validation-readiness.ps1",
            ".\\scripts\\engineering-core\\generate-engineering-core-v1-traceability-matrix.ps1",
            ".\\scripts\\engineering-core\\generate-ep-smoke-001-comparison-readiness.ps1",
            ".\\scripts\\engineering-core\\compare-ep-smoke-001-placeholder.ps1",
            ".\\scripts\\engineering-core\\generate-engineering-core-v1-validation-comparison-summary.ps1",
            ".\\scripts\\engineering-core\\assert-ep-smoke-001-real-fixture-ready.ps1",
            ".\\scripts\\engineering-core\\compare-energyplus-validation-fixtures.ps1",
            ".\\scripts\\engineering-core\\generate-energyplus-validation-fixture-catalog.ps1",
            ".\\scripts\\engineering-core\\generate-engineering-core-v1-validation-evidence.ps1"
        };

        foreach (var generator in generators)
        {
            if (!File.Exists(generator))
            {
                if (skipMissing)
                {
                    WriteWarning($"SKIP missing generator: {generator}");
                    continue;
                }

                throw new FileNotFoundException($"Required generator not found: {generator}", generator);
            }

            var stepResult = RunStep($"Generate: {generator}", "pwsh", $"-NoProfile -ExecutionPolicy Bypass -File \"{generator}\"");
            if (stepResult.ExitCode != 0)
                return stepResult.ExitCode;
        }

        WriteSuccess("Engineering Core V1 artifact regeneration completed successfully.");
        return 0;
    }

    private static int VerifySmoke(bool skipFrontend, bool noRestore = false, bool noBuild = false)
    {
        Console.WriteLine("Engineering Core V1 smoke verification");
        if (skipFrontend)
        {
            WriteWarning("SkipFrontend override is enabled. Frontend build/type checks are intentionally skipped.");
        }
        else
        {
            Console.WriteLine("Frontend checks are enabled by default.");
        }

        if (!skipFrontend)
        {
            var frontendResult = RunStep(
                "Frontend build smoke",
                "npm",
                "--prefix .\\src\\Frontend run build");

            if (frontendResult.ExitCode != 0)
                return frontendResult.ExitCode;
        }

        var steps = new[]
        {
            DotnetTest(
                "Core formula/status/report/diagnostics smoke tests",
                "FormulaAudit|EngineeringCoreStatus|EngineeringCoreReportDisclosureTests|EngineeringCoreDiagnosticsCatalogFacadeAndApiTests",
                noRestore,
                noBuild),

            DotnetTest(
                "Frontend visibility smoke tests",
                "EngineeringCoreFrontendIntegrationGuardTests|EngineeringCoreDiagnosticsCatalogPanelFrontendGuardTests",
                noRestore,
                noBuild),

            DotnetTest(
                "Weather/annual/hourly closure smoke tests",
                "AnnualEnergy8760ScenarioTests|EpwAnnualClimateDataImportServiceTests|PvgisAnnualClimateDataImportServiceTests|Iso52016EngineeringCoreV1ClosureTests",
                noRestore,
                noBuild)
        };

        return RunSteps(steps, "Engineering Core V1 smoke verification completed successfully.");
    }

    private static int VerifyContracts(bool skipFrontend, bool skipRegenerate, bool noRestore = false, bool noBuild = false)
    {
        Console.WriteLine("Engineering Core V1 contracts verification");
        if (skipFrontend)
        {
            WriteWarning("SkipFrontend override is enabled. Frontend build/type checks are intentionally skipped.");
        }
        else
        {
            Console.WriteLine("Frontend checks are enabled by default.");
        }

        if (!skipRegenerate)
        {
            var regenerateResult = RunStep(
                "Regenerate Engineering Core V1 artifacts",
                "dotnet",
                "run --project .\\tools\\AssistantEngineer.Tools.EngineeringCoreRelease\\AssistantEngineer.Tools.EngineeringCoreRelease.csproj -- regenerate-artifacts");

            if (regenerateResult.ExitCode != 0)
                return regenerateResult.ExitCode;
        }

        if (!skipFrontend)
        {
            var frontendResult = RunStep(
                "Frontend build",
                "npm",
                "--prefix .\\src\\Frontend run build");

            if (frontendResult.ExitCode != 0)
                return frontendResult.ExitCode;
        }

        var steps = new[]
        {
            DotnetTest(
                "API/OpenAPI/report/export/diagnostics contract tests",
                "EngineeringCoreV1ApiContractSnapshotTests|EngineeringCoreV1OpenApiContractTests|EngineeringCoreV1ReportContractSnapshotTests|EngineeringCoreV1ReportExportDisclosureGuardTests|EngineeringCoreV1FormulaAuditDiagnosticsCatalogTests|EngineeringCoreDiagnosticsCatalogFacadeAndApiTests|EngineeringCoreDiagnosticsCatalogFrontendGuardTests",
                noRestore,
                noBuild),

            DotnetTest(
                "Release evidence/manifest/traceability/validation registry tests",
                "EngineeringCoreV1ReleaseEvidencePackageTests|EngineeringCoreV1ReleaseManifestTests|EngineeringCoreV1TraceabilityMatrixTests|EnergyPlusValidationCaseRegistryTests",
                noRestore,
                noBuild),

            DotnetTest(
                "Documentation and contribution guard tests",
                "EngineeringCoreV1ProjectDocumentationTests|EngineeringCoreV1ReleaseDocumentationTests|EngineeringCoreV1ScopeDocumentationTests|EngineeringCoreV1VerificationRunbookTests|EngineeringCoreV1CiWorkflowTests|EngineeringCoreV1ContributionGuardTests",
                noRestore,
                noBuild)
        };

        return RunSteps(steps, "Engineering Core V1 contracts verification completed successfully.");
    }

    private static int VerifyManifest(bool skipFrontend, bool noRestore = false, bool noBuild = false)
    {
        Console.WriteLine("Engineering Core V1 manifest verification");
        if (skipFrontend)
        {
            WriteWarning("SkipFrontend override is enabled. Frontend build/type checks are intentionally skipped.");
        }
        else
        {
            Console.WriteLine("Frontend checks are enabled by default.");
        }

        var steps = new List<CommandStep>
        {
            DotnetTest(
                "Manifest consistency tests",
                "EngineeringCoreV1ReleaseManifestTests",
                noRestore,
                noBuild),

            DotnetTest(
                "Release documentation guard tests",
                "EngineeringCoreV1ProjectDocumentationTests|EngineeringCoreV1ReleaseDocumentationTests|EngineeringCoreV1VerificationRunbookTests",
                noRestore,
                noBuild),

            DotnetTest(
                "Status/disclosure/frontend guard tests",
                "EngineeringCoreStatus|EngineeringCoreReportDisclosureTests|EngineeringCoreFrontendIntegrationGuardTests",
                noRestore,
                noBuild)
        };

        if (!skipFrontend)
        {
            steps.Add(new CommandStep(
                "Frontend build",
                "npm",
                "--prefix .\\src\\Frontend run build"));
        }

        return RunSteps(steps, "Engineering Core V1 manifest verification completed successfully.");
    }

    private static int AssertReleaseReady(ReleaseReadyOptions options)
    {
        Console.WriteLine("Engineering Core V1 release readiness gate");
        Console.WriteLine($"Repository: {Directory.GetCurrentDirectory()}");
        Console.WriteLine($".NET SDK: {Environment.Version}");
        Console.WriteLine($"Started (UTC): {DateTimeOffset.UtcNow:O}");
        if (options.SkipFrontend)
        {
            WriteWarning("SkipFrontend override is enabled. Frontend build/type checks are intentionally skipped.");
        }
        else
        {
            Console.WriteLine("Frontend checks are enabled by default.");
        }

        var requiredFiles = new[]
        {
            "docs/releases/EngineeringCoreV1Manifest.json",
            "docs/releases/EngineeringCoreV1ReleaseManifest.md",
            "docs/releases/EngineeringCoreV1ReleaseChecklist.md",
            "docs/releases/EngineeringCoreV1OwnerHandoff.md",
            "docs/reports/EngineeringCoreV1ReleaseEvidence.md",
            "docs/traceability/EngineeringCoreV1TraceabilityMatrix.json",
            "docs/traceability/EngineeringCoreV1TraceabilityMatrix.md",
            "docs/calculations/EngineeringCoreV1DiagnosticsCatalog.json",
            "docs/api/engineering-core-v1/status.sample.json",
            "docs/api/engineering-core-v1/diagnostics-catalog.sample.json",
            "docs/reports/engineering-core-v1/heating-report.sample.json",
            "docs/reports/engineering-core-v1/cooling-report.sample.json",
            "docs/reports/engineering-core-v1/annual-energy-disclosure.sample.json",
            "docs/validation/EnergyPlusValidationCaseRegistry.json",
            "docs/reports/EngineeringCoreV1ValidationReadiness.md",
            ".github/workflows/engineering-core-v1.yml",
            "scripts/engineering-core/verify-engineering-core-v1.ps1",
            "scripts/engineering-core/verify-engineering-core-v1-smoke.ps1",
            "scripts/engineering-core/verify-engineering-core-v1-contracts.ps1",
            "scripts/engineering-core/regenerate-engineering-core-v1-artifacts.ps1"
        };

        var missingFiles = requiredFiles
            .Where(file => !File.Exists(file))
            .ToArray();

        if (missingFiles.Length > 0)
        {
            throw new FileNotFoundException(
                "Required release readiness files are missing: " + string.Join(", ", missingFiles));
        }

        var stepResults = new List<StepResult>();

        if (!options.NoRestore)
        {
            var restoreResult = RunStep(
                "Restore solution",
                "dotnet",
                "restore .\\AssistantEngineer.sln");
            stepResults.Add(restoreResult);
            if (restoreResult.ExitCode != 0)
            {
                WriteSummary(stepResults);
                return restoreResult.ExitCode;
            }
        }

        if (!options.NoBuild)
        {
            var buildArgs = "build .\\AssistantEngineer.sln -c Debug";
            if (!options.NoRestore)
                buildArgs += " --no-restore";

            var buildResult = RunStep(
                "Build solution (Debug)",
                "dotnet",
                buildArgs);
            stepResults.Add(buildResult);
            if (buildResult.ExitCode != 0)
            {
                WriteSummary(stepResults);
                return buildResult.ExitCode;
            }
        }

        var regenerateResult = RunInternalStep(
            "Regenerate Engineering Core V1 generated artifacts",
            () => RegenerateArtifacts(skipMissing: false));
        stepResults.Add(regenerateResult);
        if (regenerateResult.ExitCode != 0)
        {
            WriteSummary(stepResults);
            return regenerateResult.ExitCode;
        }

        var smokeResult = RunInternalStep(
            "Smoke verification profile",
            () => VerifySmoke(
                options.SkipFrontend,
                noRestore: true,
                noBuild: true));
        stepResults.Add(smokeResult);
        if (smokeResult.ExitCode != 0)
        {
            WriteSummary(stepResults);
            return smokeResult.ExitCode;
        }

        var contractsResult = RunInternalStep(
            "Contracts verification profile",
            () => VerifyContracts(
                options.SkipFrontend,
                skipRegenerate: true,
                noRestore: true,
                noBuild: true));
        stepResults.Add(contractsResult);
        if (contractsResult.ExitCode != 0)
        {
            WriteSummary(stepResults);
            return contractsResult.ExitCode;
        }

        var manifestResult = RunInternalStep(
            "Manifest verification profile",
            () => VerifyManifest(
                options.SkipFrontend,
                noRestore: true,
                noBuild: true));
        stepResults.Add(manifestResult);
        if (manifestResult.ExitCode != 0)
        {
            WriteSummary(stepResults);
            return manifestResult.ExitCode;
        }

        if (!options.Fast)
        {
            var verificationArgs = new List<string>();
            if (options.SkipFrontend)
                verificationArgs.Add("--skip-frontend");
            if (options.SkipFullDotnet)
                verificationArgs.Add("--skip-full-dotnet");

            var verificationResult = RunStep(
                "Full Engineering Core V1 verification",
                "dotnet",
                "run --no-build --project .\\tools\\AssistantEngineer.Tools.EngineeringCoreVerification\\AssistantEngineer.Tools.EngineeringCoreVerification.csproj -- --no-restore --no-build " + string.Join(" ", verificationArgs));

            stepResults.Add(verificationResult);
            if (verificationResult.ExitCode != 0)
            {
                WriteSummary(stepResults);
                return verificationResult.ExitCode;
            }
        }

        if (!options.SkipGitStatus)
        {
            var gitStatusResult = RunStep(
                "Git working tree status",
                "git",
                "status --short");

            stepResults.Add(gitStatusResult);
            if (gitStatusResult.ExitCode != 0)
            {
                WriteSummary(stepResults);
                return gitStatusResult.ExitCode;
            }
        }

        Console.WriteLine();
        WriteSuccess("Engineering Core V1 release readiness gate completed successfully.");
        WriteSummary(stepResults);
        Console.WriteLine();
        Console.WriteLine("Release-ready interpretation:");
        Console.WriteLine("- Engineering Core V1 is closed as an engineering formula gate.");
        Console.WriteLine("- FormulaAuditMatrix, manifest, diagnostics, API contracts, report disclosures, frontend visibility, validation registry and traceability are verified.");
        Console.WriteLine("- This does not claim exact EnergyPlus numerical equivalence, exact StandardReference numerical equivalence or ASHRAE 140 / BESTEST-style validation anchor coverage.");
        Console.WriteLine("- Future validation remains comparative and tolerance-based.");

        return 0;
    }

    private static CommandStep DotnetTest(string name, string filter, bool noRestore, bool noBuild) =>
        new(
            name,
            "dotnet",
            $"test .\\AssistantEngineer.sln -c Debug {BuildDotnetTestFlags(noRestore, noBuild)} --filter \"{filter}\"");

    private static int RunSteps(IEnumerable<CommandStep> steps, string successMessage)
    {
        foreach (var step in steps)
        {
            var stepResult = RunStep(step.Name, step.FileName, step.Arguments);
            if (stepResult.ExitCode != 0)
                return stepResult.ExitCode;
        }

        WriteSuccess(successMessage);
        return 0;
    }

    private static StepResult RunStep(string name, string fileName, string arguments)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"==> {name}");
        Console.ResetColor();

        var stopwatch = Stopwatch.StartNew();
        var code = RunProcess(fileName, arguments);
        stopwatch.Stop();

        if (code == 0)
        {
            WriteSuccess($"OK: {name} ({FormatDuration(stopwatch.Elapsed)})");
            return new StepResult(name, 0, stopwatch.Elapsed);
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"FAILED: {name} ({FormatDuration(stopwatch.Elapsed)})");
        Console.ResetColor();
        return new StepResult(name, code, stopwatch.Elapsed);
    }

    private static string BuildDotnetTestFlags(bool noRestore, bool noBuild)
    {
        var flags = new List<string>();
        if (noRestore)
            flags.Add("--no-restore");

        if (noBuild)
            flags.Add("--no-build");

        return string.Join(" ", flags);
    }

    private static void WriteSummary(IReadOnlyCollection<StepResult> stepResults)
    {
        if (stepResults.Count == 0)
            return;

        var total = TimeSpan.FromTicks(stepResults.Sum(step => step.Duration.Ticks));
        Console.WriteLine();
        Console.WriteLine("Release readiness summary:");
        foreach (var step in stepResults)
        {
            var status = step.ExitCode == 0 ? "OK" : "FAIL";
            Console.WriteLine($"- {status,-4} {step.Name} ({FormatDuration(step.Duration)})");
        }

        Console.WriteLine($"Total duration: {FormatDuration(total)}");
        Console.WriteLine("Slowest 5 steps:");
        foreach (var step in stepResults.OrderByDescending(result => result.Duration).Take(5))
            Console.WriteLine($"- {step.Name}: {FormatDuration(step.Duration)}");
    }

    private static string FormatDuration(TimeSpan duration) =>
        duration.TotalMinutes >= 1
            ? $"{duration:mm\\:ss\\.fff}"
            : $"{duration:ss\\.fff}s";

    private static StepResult RunInternalStep(string name, Func<int> action)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"==> {name}");
        Console.ResetColor();

        var stopwatch = Stopwatch.StartNew();
        var exitCode = action();
        stopwatch.Stop();

        if (exitCode == 0)
        {
            WriteSuccess($"OK: {name} ({FormatDuration(stopwatch.Elapsed)})");
            return new StepResult(name, 0, stopwatch.Elapsed);
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"FAILED: {name} ({FormatDuration(stopwatch.Elapsed)})");
        Console.ResetColor();
        return new StepResult(name, exitCode, stopwatch.Elapsed);
    }

    private static int RunProcess(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveProcessFileName(fileName),
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
                Console.WriteLine(args.Data);
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
                Console.Error.WriteLine(args.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return process.ExitCode;
    }

    private static bool Has(IReadOnlyCollection<string> args, string option) =>
        args.Any(arg => string.Equals(arg, option, StringComparison.OrdinalIgnoreCase));

    private static string ResolveProcessFileName(string fileName)
    {
        if (!OperatingSystem.IsWindows())
            return fileName;

        var normalized = fileName.Trim();

        if (string.Equals(normalized, "pwsh", StringComparison.OrdinalIgnoreCase))
        {
            var pwsh = FindExecutableOnPath("pwsh", ".exe", ".cmd", ".bat");
            if (pwsh is not null)
                return pwsh;

            var powershell = FindExecutableOnPath("powershell", ".exe", ".cmd", ".bat");
            if (powershell is not null)
                return powershell;

            return "powershell.exe";
        }

        if (string.Equals(normalized, "npm", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "npx", StringComparison.OrdinalIgnoreCase))
        {
            var npm = FindExecutableOnPath(normalized, ".cmd", ".exe", ".bat");
            if (npm is not null)
                return npm;

            return normalized + ".cmd";
        }

        return fileName;
    }

    private static string? FindExecutableOnPath(string fileName, params string[] extensions)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmedDirectory = directory.Trim('"');

            foreach (var extension in extensions)
            {
                var candidate = Path.Combine(trimmedDirectory, fileName + extension);

                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AssistantEngineer.sln")))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root with AssistantEngineer.sln was not found.");
    }

    private static void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private sealed record CommandStep(
        string Name,
        string FileName,
        string Arguments);

    private sealed record ReleaseReadyOptions(
        bool SkipFrontend,
        bool SkipFullDotnet,
        bool SkipGitStatus,
        bool Fast,
        bool NoRestore,
        bool NoBuild);

    private sealed record StepResult(
        string Name,
        int ExitCode,
        TimeSpan Duration);
}
