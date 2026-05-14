namespace AssistantEngineer.Tools.EngineeringCoreVerification;

internal sealed class EngineeringCoreVerificationReportWriter
{
    public void WriteHelp()
    {
        Console.WriteLine("AssistantEngineer Engineering Core V1 verification tool");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --skip-frontend");
        Console.WriteLine("  --skip-full-dotnet");
        Console.WriteLine("  --fast");
        Console.WriteLine("  --no-restore");
        Console.WriteLine("  --no-build");
    }

    public void WriteSessionHeader(string repoRoot, VerificationOptions options, Version dotNetVersion, DateTimeOffset startedUtc)
    {
        Console.WriteLine("Engineering Core V1 verification");
        Console.WriteLine($"Repository: {repoRoot}");
        Console.WriteLine($".NET SDK: {dotNetVersion}");
        Console.WriteLine($"Started (UTC): {startedUtc:O}");

        if (options.SkipFrontend)
        {
            WriteWarning("SkipFrontend override is enabled. Frontend build/type checks are intentionally skipped.");
        }
        else
        {
            Console.WriteLine("Frontend checks are enabled by default.");
        }
    }

    public void WriteStepStart(string name)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"==> {name}");
        Console.ResetColor();
    }

    public void WriteStepFailure(string name, TimeSpan elapsed)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"FAILED: {name} ({EngineeringCoreVerificationDiagnosticsFormatter.FormatDuration(elapsed)})");
        Console.ResetColor();
    }

    public void WriteStepSuccess(string name, TimeSpan elapsed)
    {
        WriteSuccess($"OK: {name} ({EngineeringCoreVerificationDiagnosticsFormatter.FormatDuration(elapsed)})");
    }

    public void WriteSummary(IReadOnlyCollection<StepResult> results)
    {
        var lines = EngineeringCoreVerificationDiagnosticsFormatter.BuildSummaryLines(results);
        if (lines.Count == 0)
            return;

        Console.WriteLine();
        foreach (var line in lines)
            Console.WriteLine(line);
    }

    public void WriteCompletionChecklist()
    {
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
        Console.WriteLine("- ISO13370-style virtual ground lane guards");
        Console.WriteLine("- EN16798-style standard-based natural ventilation guards");
        Console.WriteLine("- multi-zone fixtures and documentation guards");
        Console.WriteLine("- EnergyPlus/ASHRAE 140 / BESTEST-style validation anchor harness scaffold");
        Console.WriteLine("- external comparison workflow foundation guardrails");
        Console.WriteLine("- release/scope/developer documentation");
    }

    public void WriteUnhandledError(Exception exception)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(exception.Message);
        Console.ResetColor();
    }

    public void WriteWarning(string message)
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
}
