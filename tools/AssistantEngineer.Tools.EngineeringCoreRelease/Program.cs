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
                "verify-smoke" => VerifySmoke(Has(toolArgs, "--skip-frontend") || Has(toolArgs, "-SkipFrontend")),
                "verify-contracts" => VerifyContracts(
                    Has(toolArgs, "--skip-frontend") || Has(toolArgs, "-SkipFrontend"),
                    Has(toolArgs, "--skip-regenerate") || Has(toolArgs, "-SkipRegenerate")),
                "verify-manifest" => VerifyManifest(Has(toolArgs, "--skip-frontend") || Has(toolArgs, "-SkipFrontend")),
                "assert-release-ready" => AssertReleaseReady(new ReleaseReadyOptions(
                    SkipFrontend: Has(toolArgs, "--skip-frontend") || Has(toolArgs, "-SkipFrontend"),
                    SkipFullDotnet: Has(toolArgs, "--skip-full-dotnet") || Has(toolArgs, "-SkipFullDotnet"),
                    SkipGitStatus: Has(toolArgs, "--skip-git-status") || Has(toolArgs, "-SkipGitStatus"),
                    Fast: Has(toolArgs, "--fast") || Has(toolArgs, "-Fast"))),
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
        Console.WriteLine("  assert-release-ready [--skip-frontend] [--skip-full-dotnet] [--skip-git-status] [--fast]");
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

            var code = RunStep($"Generate: {generator}", "pwsh", $"-NoProfile -ExecutionPolicy Bypass -File \"{generator}\"");
            if (code != 0)
                return code;
        }

        WriteSuccess("Engineering Core V1 artifact regeneration completed successfully.");
        return 0;
    }

    private static int VerifySmoke(bool skipFrontend)
    {
        Console.WriteLine("Engineering Core V1 smoke verification");

        if (!skipFrontend)
        {
            var frontendCode = RunStep(
                "Frontend build smoke",
                "npm",
                "--prefix .\\src\\Frontend run build");

            if (frontendCode != 0)
                return frontendCode;
        }

        var steps = new[]
        {
            DotnetTest(
                "Core formula/status/report/diagnostics smoke tests",
                "FormulaAudit|EngineeringCoreStatus|EngineeringCoreReportDisclosureTests|EngineeringCoreDiagnosticsCatalogFacadeAndApiTests"),

            DotnetTest(
                "Frontend visibility smoke tests",
                "EngineeringCoreFrontendIntegrationGuardTests|EngineeringCoreDiagnosticsCatalogPanelFrontendGuardTests"),

            DotnetTest(
                "Weather/annual/hourly closure smoke tests",
                "AnnualEnergy8760ScenarioTests|EpwAnnualClimateDataImportServiceTests|PvgisAnnualClimateDataImportServiceTests|Iso52016EngineeringCoreV1ClosureTests")
        };

        return RunSteps(steps, "Engineering Core V1 smoke verification completed successfully.");
    }

    private static int VerifyContracts(bool skipFrontend, bool skipRegenerate)
    {
        Console.WriteLine("Engineering Core V1 contracts verification");

        if (!skipRegenerate)
        {
            var regenerateCode = RunStep(
                "Regenerate Engineering Core V1 artifacts",
                "dotnet",
                "run --project .\\tools\\AssistantEngineer.Tools.EngineeringCoreRelease\\AssistantEngineer.Tools.EngineeringCoreRelease.csproj -- regenerate-artifacts");

            if (regenerateCode != 0)
                return regenerateCode;
        }

        if (!skipFrontend)
        {
            var frontendCode = RunStep(
                "Frontend build",
                "npm",
                "--prefix .\\src\\Frontend run build");

            if (frontendCode != 0)
                return frontendCode;
        }

        var steps = new[]
        {
            DotnetTest(
                "API/OpenAPI/report/export/diagnostics contract tests",
                "EngineeringCoreV1ApiContractSnapshotTests|EngineeringCoreV1OpenApiContractTests|EngineeringCoreV1ReportContractSnapshotTests|EngineeringCoreV1ReportExportDisclosureGuardTests|EngineeringCoreV1FormulaAuditDiagnosticsCatalogTests|EngineeringCoreDiagnosticsCatalogFacadeAndApiTests|EngineeringCoreDiagnosticsCatalogFrontendGuardTests"),

            DotnetTest(
                "Release evidence/manifest/traceability/validation registry tests",
                "EngineeringCoreV1ReleaseEvidencePackageTests|EngineeringCoreV1ReleaseManifestTests|EngineeringCoreV1TraceabilityMatrixTests|EnergyPlusValidationCaseRegistryTests"),

            DotnetTest(
                "Documentation and contribution guard tests",
                "EngineeringCoreV1ProjectDocumentationTests|EngineeringCoreV1ReleaseDocumentationTests|EngineeringCoreV1ScopeDocumentationTests|EngineeringCoreV1VerificationRunbookTests|EngineeringCoreV1CiWorkflowTests|EngineeringCoreV1ContributionGuardTests")
        };

        return RunSteps(steps, "Engineering Core V1 contracts verification completed successfully.");
    }

    private static int VerifyManifest(bool skipFrontend)
    {
        Console.WriteLine("Engineering Core V1 manifest verification");

        var steps = new List<CommandStep>
        {
            DotnetTest(
                "Manifest consistency tests",
                "EngineeringCoreV1ReleaseManifestTests"),

            DotnetTest(
                "Release documentation guard tests",
                "EngineeringCoreV1ProjectDocumentationTests|EngineeringCoreV1ReleaseDocumentationTests|EngineeringCoreV1VerificationRunbookTests"),

            DotnetTest(
                "Status/disclosure/frontend guard tests",
                "EngineeringCoreStatus|EngineeringCoreReportDisclosureTests|EngineeringCoreFrontendIntegrationGuardTests")
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

        var regenerateCode = RunStep(
            "Regenerate Engineering Core V1 generated artifacts",
            "dotnet",
            "run --project .\\tools\\AssistantEngineer.Tools.EngineeringCoreRelease\\AssistantEngineer.Tools.EngineeringCoreRelease.csproj -- regenerate-artifacts");

        if (regenerateCode != 0)
            return regenerateCode;

        if (!options.SkipFrontend)
        {
            var frontendCode = RunStep(
                "Frontend build",
                "npm",
                "--prefix .\\src\\Frontend run build");

            if (frontendCode != 0)
                return frontendCode;
        }

        var smokeCode = RunStep(
            "Smoke verification profile",
            "dotnet",
            "run --project .\\tools\\AssistantEngineer.Tools.EngineeringCoreRelease\\AssistantEngineer.Tools.EngineeringCoreRelease.csproj -- verify-smoke " + (options.SkipFrontend ? "--skip-frontend" : ""));

        if (smokeCode != 0)
            return smokeCode;

        var contractsCode = RunStep(
            "Contracts verification profile",
            "dotnet",
            "run --project .\\tools\\AssistantEngineer.Tools.EngineeringCoreRelease\\AssistantEngineer.Tools.EngineeringCoreRelease.csproj -- verify-contracts --skip-regenerate " + (options.SkipFrontend ? "--skip-frontend" : ""));

        if (contractsCode != 0)
            return contractsCode;

        var manifestCode = RunStep(
            "Manifest verification",
            "dotnet",
            "run --project .\\tools\\AssistantEngineer.Tools.EngineeringCoreRelease\\AssistantEngineer.Tools.EngineeringCoreRelease.csproj -- verify-manifest " + (options.SkipFrontend ? "--skip-frontend" : ""));

        if (manifestCode != 0)
            return manifestCode;

        if (!options.Fast)
        {
            var verificationArgs = new List<string>();
            if (options.SkipFrontend)
                verificationArgs.Add("--skip-frontend");
            if (options.SkipFullDotnet)
                verificationArgs.Add("--skip-full-dotnet");

            var verificationCode = RunStep(
                "Full Engineering Core V1 verification",
                "dotnet",
                "run --project .\\tools\\AssistantEngineer.Tools.EngineeringCoreVerification\\AssistantEngineer.Tools.EngineeringCoreVerification.csproj -- " + string.Join(" ", verificationArgs));

            if (verificationCode != 0)
                return verificationCode;
        }

        if (!options.SkipFullDotnet && !options.Fast)
        {
            var fullTestCode = RunStep(
                "Full backend test suite",
                "dotnet",
                "test .\\AssistantEngineer.sln");

            if (fullTestCode != 0)
                return fullTestCode;
        }

        if (!options.SkipGitStatus)
        {
            var gitStatusCode = RunStep(
                "Git working tree status",
                "git",
                "status --short");

            if (gitStatusCode != 0)
                return gitStatusCode;
        }

        Console.WriteLine();
        WriteSuccess("Engineering Core V1 release readiness gate completed successfully.");
        Console.WriteLine();
        Console.WriteLine("Release-ready interpretation:");
        Console.WriteLine("- Engineering Core V1 is closed as an engineering formula gate.");
        Console.WriteLine("- FormulaAuditMatrix, manifest, diagnostics, API contracts, report disclosures, frontend visibility, validation registry and traceability are verified.");
        Console.WriteLine("- This does not claim exact EnergyPlus numerical parity, exact pyBuildingEnergy numerical parity or ASHRAE 140 validation coverage.");
        Console.WriteLine("- Future validation remains comparative and tolerance-based.");

        return 0;
    }

    private static CommandStep DotnetTest(string name, string filter) =>
        new(
            name,
            "dotnet",
            $"test .\\AssistantEngineer.sln --filter \"{filter}\"");

    private static int RunSteps(IEnumerable<CommandStep> steps, string successMessage)
    {
        foreach (var step in steps)
        {
            var code = RunStep(step.Name, step.FileName, step.Arguments);
            if (code != 0)
                return code;
        }

        WriteSuccess(successMessage);
        return 0;
    }

    private static int RunStep(string name, string fileName, string arguments)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"==> {name}");
        Console.ResetColor();

        var code = RunProcess(fileName, arguments);

        if (code == 0)
        {
            WriteSuccess($"OK: {name}");
            return 0;
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"FAILED: {name}");
        Console.ResetColor();
        return code;
    }

    private static int RunProcess(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
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
        bool Fast);
}
