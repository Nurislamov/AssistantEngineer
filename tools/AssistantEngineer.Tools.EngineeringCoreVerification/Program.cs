using System.Diagnostics;

namespace AssistantEngineer.Tools.EngineeringCoreVerification;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.Any(arg => arg is "-h" or "--help" or "help"))
            {
                PrintHelp();
                return 0;
            }

            var options = VerificationOptions.Parse(args);
            var repoRoot = FindRepositoryRoot();

            Directory.SetCurrentDirectory(repoRoot);

            Console.WriteLine("Engineering Core V1 verification");
            Console.WriteLine($"Repository: {repoRoot}");

            var steps = BuildSteps(options);

            foreach (var step in steps)
            {
                var exitCode = RunStep(step);
                if (exitCode != 0)
                    return exitCode;
            }

            Console.WriteLine();
            WriteSuccess("Engineering Core V1 verification completed successfully.");
            Console.WriteLine();
            Console.WriteLine("Verified:");
            Console.WriteLine("- frontend build");
            Console.WriteLine("- formula audit matrix");
            Console.WriteLine("- Engineering Core V1 status endpoint/facade");
            Console.WriteLine("- report disclosures");
            Console.WriteLine("- diagnostics catalog");
            Console.WriteLine("- release evidence package");
            Console.WriteLine("- API contract snapshots");
            Console.WriteLine("- OpenAPI contract");
            Console.WriteLine("- report contract snapshots");
            Console.WriteLine("- report export disclosure policy");
            Console.WriteLine("- validation registry");
            Console.WriteLine("- traceability matrix");
            Console.WriteLine("- frontend visibility guards");
            Console.WriteLine("- EPW/PVGIS 8760 gates");
            Console.WriteLine("- annual true hourly 8760 gate");
            Console.WriteLine("- hourly heat-balance and single-zone gates");
            Console.WriteLine("- ground and adjacent simplified gates");
            Console.WriteLine("- EnergyPlus/ASHRAE 140 validation harness scaffold");
            Console.WriteLine("- release/scope/developer documentation");

            return 0;
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
        Console.WriteLine("AssistantEngineer Engineering Core V1 verification tool");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --skip-frontend");
        Console.WriteLine("  --skip-full-dotnet");
        Console.WriteLine("  --fast");
    }

    private static IReadOnlyList<VerificationStep> BuildSteps(VerificationOptions options)
    {
        var steps = new List<VerificationStep>();

        if (!options.SkipFrontend)
        {
            steps.Add(new VerificationStep(
                "Frontend TypeScript/Vite build",
                "npm",
                "--prefix .\\src\\Frontend run build"));
        }

        steps.AddRange(
        [
            DotnetTest(
                "Engineering Core status and formula audit tests",
                "FormulaAudit|EngineeringCoreStatus|EngineeringCoreReportDisclosureTests"),

            DotnetTest(
                "Engineering Core documentation guard tests",
                "EngineeringCoreV1ProjectDocumentationTests|EngineeringCoreV1ReleaseDocumentationTests|EngineeringCoreV1ScopeDocumentationTests|EngineeringCoreV1FrontendDisclosureDocumentationTests"),

            DotnetTest(
                "Engineering Core test profile script guard tests",
                "EngineeringCoreV1TestProfileScriptsTests"),

            DotnetTest(
                "Engineering Core release readiness gate tests",
                "EngineeringCoreV1ReleaseReadinessGateTests"),

            DotnetTest(
                "Engineering Core repository communication guard tests",
                "EngineeringCoreV1RepositoryCommunicationTests"),

            DotnetTest(
                "Engineering Core CI profile workflow guard tests",
                "EngineeringCoreV1CiProfileWorkflowTests"),

            DotnetTest(
                "Engineering Core diagnostics catalog guard tests",
                "EngineeringCoreV1FormulaAuditDiagnosticsCatalogTests|EngineeringCoreDiagnosticsCatalogFacadeAndApiTests|EngineeringCoreDiagnosticsCatalogFrontendGuardTests"),

            ScriptThenDotnetTest(
                "Engineering Core release evidence package guard tests",
                ".\\scripts\\engineering-core\\generate-engineering-core-v1-release-evidence.ps1",
                "EngineeringCoreV1ReleaseEvidencePackageTests"),

            ScriptThenDotnetTest(
                "Engineering Core API contract snapshot guard tests",
                ".\\scripts\\engineering-core\\generate-engineering-core-v1-api-contract-snapshots.ps1",
                "EngineeringCoreV1ApiContractSnapshotTests"),

            DotnetTest(
                "Engineering Core OpenAPI contract guard tests",
                "EngineeringCoreV1OpenApiContractTests"),

            ScriptThenDotnetTest(
                "Engineering Core report contract snapshot guard tests",
                ".\\scripts\\engineering-core\\generate-engineering-core-v1-report-contract-snapshots.ps1",
                "EngineeringCoreV1ReportContractSnapshotTests"),

            ScriptThenDotnetTest(
                "Engineering Core report export disclosure guard tests",
                ".\\scripts\\engineering-core\\generate-engineering-core-v1-export-disclosure-checklist.ps1",
                "EngineeringCoreV1ReportExportDisclosureGuardTests"),

            ScriptThenDotnetTest(
                "Engineering Core validation registry guard tests",
                ".\\scripts\\engineering-core\\generate-engineering-core-v1-validation-readiness.ps1",
                "EnergyPlusValidationCaseRegistryTests"),

            ScriptThenDotnetTest(
                "EnergyPlus smoke fixture scaffold guard tests",
                ".\\scripts\\engineering-core\\generate-ep-smoke-001-comparison-readiness.ps1",
                "EnergyPlusSmoke001FixtureScaffoldTests"),

            ScriptThenDotnetTest(
                "EnergyPlus smoke fixture comparison harness tests",
                ".\\scripts\\engineering-core\\compare-ep-smoke-001-placeholder.ps1",
                "EnergyPlusSmoke001ComparisonHarnessTests"),

            ScriptThenDotnetTest(
                "EnergyPlus validation comparison summary tests",
                ".\\scripts\\engineering-core\\generate-engineering-core-v1-validation-comparison-summary.ps1",
                "EnergyPlusValidationComparisonSummaryTests"),

            ScriptThenDotnetTest(
                "EnergyPlus real fixture intake gate tests",
                ".\\scripts\\engineering-core\\assert-ep-smoke-001-real-fixture-ready.ps1",
                "EnergyPlusRealFixtureIntakeGateTests"),

            ScriptThenDotnetTest(
                "Generic EnergyPlus validation fixture runner tests",
                ".\\scripts\\engineering-core\\compare-energyplus-validation-fixtures.ps1",
                "EnergyPlusValidationGenericComparisonRunnerTests"),

            new VerificationStep(
                "EnergyPlus smoke 002/003 fixture scaffold tests",
                "pwsh",
                "-NoProfile -ExecutionPolicy Bypass -Command \"& .\\scripts\\engineering-core\\compare-energyplus-validation-fixtures.ps1; & .\\scripts\\engineering-core\\generate-engineering-core-v1-validation-comparison-summary.ps1; dotnet test .\\AssistantEngineer.sln --filter 'EnergyPlusSmoke002And003FixtureScaffoldTests'\""),

            ScriptThenDotnetTest(
                "EnergyPlus validation fixture catalog tests",
                ".\\scripts\\engineering-core\\generate-energyplus-validation-fixture-catalog.ps1",
                "EnergyPlusValidationFixtureCatalogTests"),

            DotnetTest(
                "EnergyPlus validation fixture authoring kit tests",
                "EnergyPlusValidationFixtureAuthoringKitTests"),

            DotnetTest(
                "EnergyPlus validation profile script tests",
                "EnergyPlusValidationProfileScriptsTests"),

            ScriptThenDotnetTest(
                "EnergyPlus validation evidence package tests",
                ".\\scripts\\engineering-core\\generate-engineering-core-v1-validation-evidence.ps1",
                "EnergyPlusValidationEvidencePackageTests"),

            ScriptThenDotnetTest(
                "Engineering Core traceability matrix guard tests",
                ".\\scripts\\engineering-core\\generate-engineering-core-v1-traceability-matrix.ps1",
                "EngineeringCoreV1TraceabilityMatrixTests"),

            DotnetTest(
                "Engineering Core frontend visibility guard tests",
                "EngineeringCoreFrontendIntegrationGuardTests|EngineeringCoreDiagnosticsCatalogPanelFrontendGuardTests"),

            DotnetTest(
                "Engineering Core weather and annual 8760 gate tests",
                "EpwAnnualClimateDataImportServiceTests|PvgisAnnualClimateDataImportServiceTests|AnnualEnergy8760ScenarioTests"),

            DotnetTest(
                "Engineering Core hourly heat-balance, zone, ground and adjacent closure tests",
                "Iso52016EngineeringCoreV1ClosureTests|GroundSimplifiedEngineeringCoreV1ClosureTests|AdjacentZoneSimplifiedEngineeringCoreV1ClosureTests"),

            DotnetTest(
                "EnergyPlus/ASHRAE 140 validation harness guard tests",
                "EnergyPlusValidation")
        ]);

        if (!options.SkipFullDotnet && !options.Fast)
        {
            steps.Add(new VerificationStep(
                "Full backend test suite",
                "dotnet",
                "test .\\AssistantEngineer.sln"));
        }

        return steps;
    }

    private static VerificationStep DotnetTest(string name, string filter) =>
        new(
            name,
            "dotnet",
            $"test .\\AssistantEngineer.sln --filter \"{filter}\"");

    private static VerificationStep ScriptThenDotnetTest(string name, string script, string filter) =>
        new(
            name,
            "pwsh",
            $"-NoProfile -ExecutionPolicy Bypass -Command \"& {script}; dotnet test .\\AssistantEngineer.sln --filter '{filter}'\"");

    private static int RunStep(VerificationStep step)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"==> {step.Name}");
        Console.ResetColor();

        var exitCode = RunProcess(step.FileName, step.Arguments);

        if (exitCode != 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FAILED: {step.Name}");
            Console.ResetColor();
            return exitCode;
        }

        WriteSuccess($"OK: {step.Name}");
        return 0;
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

    private static string ResolveProcessFileName(string fileName)
    {
        if (!OperatingSystem.IsWindows())
            return fileName;

        if (!string.Equals(fileName, "npm", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(fileName, "npx", StringComparison.OrdinalIgnoreCase))
        {
            return fileName;
        }

        if (fileName.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return fileName;
        }

        var pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var candidateExtensions = new[] { ".cmd", ".exe", ".bat" };

        foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var extension in candidateExtensions)
            {
                var candidate = Path.Combine(directory.Trim('"'), fileName + extension);

                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return fileName + ".cmd";
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

    private static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private sealed record VerificationStep(
        string Name,
        string FileName,
        string Arguments);

    private sealed record VerificationOptions(
        bool SkipFrontend,
        bool SkipFullDotnet,
        bool Fast)
    {
        public static VerificationOptions Parse(IReadOnlyCollection<string> args) =>
            new(
                SkipFrontend: Has(args, "--skip-frontend") || Has(args, "-SkipFrontend"),
                SkipFullDotnet: Has(args, "--skip-full-dotnet") || Has(args, "-SkipFullDotnet"),
                Fast: Has(args, "--fast") || Has(args, "-Fast"));

        private static bool Has(IReadOnlyCollection<string> args, string option) =>
            args.Any(arg => string.Equals(arg, option, StringComparison.OrdinalIgnoreCase));
    }
}
