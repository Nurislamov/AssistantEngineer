namespace AssistantEngineer.Tools.EngineeringCoreVerification;

internal sealed class EngineeringCoreVerificationCommandRouter(
    EngineeringCoreVerificationFileSystem fileSystem,
    EngineeringCoreVerificationPolicyGuards policyGuards,
    EngineeringCoreVerificationCommandHandler commandHandler,
    EngineeringCoreVerificationReportWriter reportWriter)
{
    public int Run(string[] args)
    {
        if (IsHelpRequested(args))
        {
            reportWriter.WriteHelp();
            return 0;
        }

        var options = VerificationOptions.Parse(args);
        var repoRoot = fileSystem.FindRepositoryRoot(fileSystem.GetCurrentDirectory());

        fileSystem.SetCurrentDirectory(repoRoot);

        reportWriter.WriteSessionHeader(repoRoot, options, Environment.Version, DateTimeOffset.UtcNow);

        policyGuards.AssertNoForbiddenTerminologyAndClaims(repoRoot);
        policyGuards.AssertExternalComparisonWorkflowFoundation(repoRoot);

        var steps = BuildSteps(options);
        var stepResults = new List<StepResult>();

        foreach (var step in steps)
        {
            var result = commandHandler.Execute(step);
            stepResults.Add(result);
            if (result.ExitCode != 0)
            {
                reportWriter.WriteSummary(stepResults);
                return result.ExitCode;
            }
        }

        reportWriter.WriteSummary(stepResults);
        reportWriter.WriteCompletionChecklist();

        return 0;
    }

    internal static bool IsHelpRequested(IReadOnlyCollection<string> args) =>
        args.Any(arg => arg is "-h" or "--help" or "help");

    internal static IReadOnlyList<VerificationStep> BuildSteps(VerificationOptions options)
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
                "FormulaAudit|EngineeringCoreStatus|EngineeringCoreReportDisclosureTests",
                options),

            DotnetTest(
                "Engineering Core documentation guard tests",
                "EngineeringCoreV1ProjectDocumentationTests|EngineeringCoreV1ReleaseDocumentationTests|EngineeringCoreV1ScopeDocumentationTests|EngineeringCoreV1FrontendDisclosureDocumentationTests|Iso52016MultiZoneStageVerificationTests|Iso13370VirtualGroundTraceabilityTests|En15316SystemEnergyTraceabilityTests",
                options),

            DotnetTest(
                "Engineering Core test profile script guard tests",
                "EngineeringCoreV1TestProfileScriptsTests",
                options),

            DotnetTest(
                "Engineering Core release readiness gate tests",
                "EngineeringCoreV1ReleaseReadinessGateTests",
                options),

            DotnetTest(
                "Engineering Core repository communication guard tests",
                "EngineeringCoreV1RepositoryCommunicationTests",
                options),

            DotnetTest(
                "Engineering Core CI profile workflow guard tests",
                "EngineeringCoreV1CiProfileWorkflowTests",
                options),

            DotnetTest(
                "Engineering Core diagnostics catalog guard tests",
                "EngineeringCoreV1FormulaAuditDiagnosticsCatalogTests|EngineeringCoreDiagnosticsCatalogFacadeAndApiTests|EngineeringCoreDiagnosticsCatalogFrontendGuardTests",
                options),

            ScriptThenDotnetTest(
                "Engineering Core release evidence package guard tests",
                ".\\scripts\\engineering-core\\generate-engineering-core-v1-release-evidence.ps1",
                "EngineeringCoreV1ReleaseEvidencePackageTests",
                options),

            ScriptThenDotnetTest(
                "Engineering Core API contract snapshot guard tests",
                ".\\scripts\\engineering-core\\generate-engineering-core-v1-api-contract-snapshots.ps1",
                "EngineeringCoreV1ApiContractSnapshotTests",
                options),

            DotnetTest(
                "Engineering Core OpenAPI contract guard tests",
                "EngineeringCoreV1OpenApiContractTests",
                options),

            ScriptThenDotnetTest(
                "Engineering Core report contract snapshot guard tests",
                ".\\scripts\\engineering-core\\generate-engineering-core-v1-report-contract-snapshots.ps1",
                "EngineeringCoreV1ReportContractSnapshotTests",
                options),

            ScriptThenDotnetTest(
                "Engineering Core report export disclosure guard tests",
                ".\\scripts\\engineering-core\\generate-engineering-core-v1-export-disclosure-checklist.ps1",
                "EngineeringCoreV1ReportExportDisclosureGuardTests",
                options),

            ScriptThenDotnetTest(
                "Engineering Core validation registry guard tests",
                ".\\scripts\\engineering-core\\generate-engineering-core-v1-validation-readiness.ps1",
                "EnergyPlusValidationCaseRegistryTests",
                options),

            ScriptThenDotnetTest(
                "EnergyPlus smoke fixture scaffold guard tests",
                ".\\scripts\\engineering-core\\generate-ep-smoke-001-comparison-readiness.ps1",
                "EnergyPlusSmoke001FixtureScaffoldTests",
                options),

            ScriptThenDotnetTest(
                "EnergyPlus smoke fixture comparison harness tests",
                ".\\scripts\\engineering-core\\compare-ep-smoke-001-placeholder.ps1",
                "EnergyPlusSmoke001ComparisonHarnessTests",
                options),

            ScriptThenDotnetTest(
                "EnergyPlus validation comparison summary tests",
                ".\\scripts\\engineering-core\\generate-engineering-core-v1-validation-comparison-summary.ps1",
                "EnergyPlusValidationComparisonSummaryTests",
                options),

            ScriptThenDotnetTest(
                "EnergyPlus real fixture intake gate tests",
                ".\\scripts\\engineering-core\\assert-ep-smoke-001-real-fixture-ready.ps1",
                "EnergyPlusRealFixtureIntakeGateTests",
                options),

            ScriptThenDotnetTest(
                "Generic EnergyPlus validation fixture runner tests",
                ".\\scripts\\engineering-core\\compare-energyplus-validation-fixtures.ps1",
                "EnergyPlusValidationGenericComparisonRunnerTests",
                options),

            new VerificationStep(
                "EnergyPlus smoke 002/003 fixture scaffold tests",
                "pwsh",
                "-NoProfile -ExecutionPolicy Bypass -Command \"& .\\scripts\\engineering-core\\compare-energyplus-validation-fixtures.ps1; & .\\scripts\\engineering-core\\generate-engineering-core-v1-validation-comparison-summary.ps1; dotnet test .\\AssistantEngineer.sln -c Debug " +
                BuildDotnetTestFlags(options) +
                " --filter 'EnergyPlusSmoke002And003FixtureScaffoldTests'\""),

            ScriptThenDotnetTest(
                "EnergyPlus validation fixture catalog tests",
                ".\\scripts\\engineering-core\\generate-energyplus-validation-fixture-catalog.ps1",
                "EnergyPlusValidationFixtureCatalogTests",
                options),

            DotnetTest(
                "EnergyPlus validation fixture authoring kit tests",
                "EnergyPlusValidationFixtureAuthoringKitTests",
                options),

            DotnetTest(
                "EnergyPlus validation profile script tests",
                "EnergyPlusValidationProfileScriptsTests",
                options),

            ScriptThenDotnetTest(
                "EnergyPlus validation evidence package tests",
                ".\\scripts\\engineering-core\\generate-engineering-core-v1-validation-evidence.ps1",
                "EnergyPlusValidationEvidencePackageTests",
                options),

            ScriptThenDotnetTest(
                "Engineering Core traceability matrix guard tests",
                ".\\scripts\\engineering-core\\generate-engineering-core-v1-traceability-matrix.ps1",
                "EngineeringCoreV1TraceabilityMatrixTests",
                options),

            DotnetTest(
                "Engineering Core frontend visibility guard tests",
                "EngineeringCoreFrontendIntegrationGuardTests|EngineeringCoreDiagnosticsCatalogPanelFrontendGuardTests",
                options),

            DotnetTest(
                "Engineering Core weather and annual 8760 gate tests",
                "EpwAnnualClimateDataImportServiceTests|PvgisAnnualClimateDataImportServiceTests|AnnualEnergy8760ScenarioTests",
                options),

            DotnetTest(
                "Engineering Core hourly heat-balance, zone, ground and adjacent closure tests",
                "Iso52016EngineeringCoreV1ClosureTests|GroundSimplifiedEngineeringCoreV1ClosureTests|AdjacentZoneSimplifiedEngineeringCoreV1ClosureTests|Iso52016MultiZone|Iso13370VirtualGround",
                options),

            DotnetTest(
                "Engineering Core natural ventilation EN16798-style guard tests",
                "Iso16798NaturalVentilation|VentilationAndInfiltrationLoadEngineTests",
                options),

            DotnetTest(
                "EnergyPlus/ASHRAE 140 / BESTEST-style validation anchor harness guard tests",
                "EnergyPlusValidation",
                options)
        ]);

        if (!options.SkipFullDotnet && !options.Fast)
        {
            steps.Add(new VerificationStep(
                "Full backend test suite",
                "dotnet",
                "test .\\AssistantEngineer.sln -c Debug " + BuildDotnetTestFlags(options)));
        }

        return steps;
    }

    private static VerificationStep DotnetTest(string name, string filter, VerificationOptions options) =>
        new(
            name,
            "dotnet",
            $"test .\\AssistantEngineer.sln -c Debug {BuildDotnetTestFlags(options)} --filter \"{filter}\"");

    private static VerificationStep ScriptThenDotnetTest(string name, string script, string filter, VerificationOptions options) =>
        new(
            name,
            "pwsh",
            $"-NoProfile -ExecutionPolicy Bypass -Command \"& {script}; dotnet test .\\AssistantEngineer.sln -c Debug {BuildDotnetTestFlags(options)} --filter '{filter}'\"");

    private static string BuildDotnetTestFlags(VerificationOptions options)
    {
        var flags = new List<string>();
        if (options.NoRestore)
            flags.Add("--no-restore");

        if (options.NoBuild)
            flags.Add("--no-build");

        return string.Join(" ", flags);
    }
}
