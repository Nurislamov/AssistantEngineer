using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AssistantEngineer.Tools.RepositoryBoundaries;

internal static partial class Program
{
    private const string StableGeneratedAtUtc = "2026-01-01 00:00:00 UTC";

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
            var failOnHeavyScripts = args.Any(arg =>
                string.Equals(arg, "--fail-on-heavy-scripts", StringComparison.OrdinalIgnoreCase));

            return command switch
            {
                "audit-script-boundaries" => AuditScriptBoundaries(failOnHeavyScripts),
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
        Console.WriteLine("AssistantEngineer repository boundary tools");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  audit-script-boundaries [--fail-on-heavy-scripts]");
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp();
        return 1;
    }

    private static int AuditScriptBoundaries(bool failOnHeavyScripts)
    {
        var scriptsRoot = "scripts";

        var scripts = Directory.Exists(scriptsRoot)
            ? Directory.GetFiles(scriptsRoot, "*.ps1", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray()
            : [];

        var scriptRows = scripts
            .Select(ClassifyScript)
            .ToArray();

        var thinScripts = scriptRows.Count(script => script.Classification == "ThinWrapper");
        var heavyScripts = scriptRows.Count(script => script.Classification == "HeavyPowerShellLogic");
        var unknownScripts = scriptRows.Count(script => script.Classification == "UnknownPowerShellScript");

        var report = new Dictionary<string, object?>
        {
            ["reportName"] = "Repository Script Boundary Audit",
            ["version"] = "v1",
            ["status"] = heavyScripts == 0 ? "Compliant" : "MigrationInProgress",
            ["generatedAtUtc"] = StableGeneratedAtUtc,
            ["policy"] = new Dictionary<string, object?>
            {
                ["srcBackend"] = "application code",
                ["srcFrontend"] = "frontend code",
                ["tests"] = "test code",
                ["docs"] = "documentation and generated evidence",
                ["tools"] = "C# automation, validation and release tools",
                ["scripts"] = "thin wrappers only",
                ["githubWorkflows"] = "CI entry points that call tools/scripts"
            },
            ["totals"] = new Dictionary<string, object?>
            {
                ["scripts"] = scriptRows.Length,
                ["thinScripts"] = thinScripts,
                ["heavyScripts"] = heavyScripts,
                ["unknownScripts"] = unknownScripts
            },
            ["scripts"] = scriptRows,
            ["requiredNextStep"] = heavyScripts == 0
                ? "Keep scripts as wrappers and implement new automation in tools."
                : "Move remaining heavy PowerShell logic into C# tools and leave scripts as wrappers only."
        };

        Directory.CreateDirectory("docs/reports/repository");

        var json = JsonSerializer.Serialize(
            report,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

        File.WriteAllText(
            "docs/reports/repository/ScriptBoundaryAudit.json",
            json + Environment.NewLine,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        File.WriteAllText(
            "docs/reports/repository/ScriptBoundaryAudit.md",
            BuildMarkdown(scriptRows, thinScripts, heavyScripts, unknownScripts),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Repository script boundary audit generated:");
        Console.ResetColor();
        Console.WriteLine("- docs/reports/repository/ScriptBoundaryAudit.json");
        Console.WriteLine("- docs/reports/repository/ScriptBoundaryAudit.md");
        Console.WriteLine($"Scripts: {scriptRows.Length}");
        Console.WriteLine($"Thin wrappers: {thinScripts}");
        Console.WriteLine($"Heavy PowerShell scripts: {heavyScripts}");
        Console.WriteLine($"Unknown PowerShell scripts: {unknownScripts}");

        if (failOnHeavyScripts && heavyScripts > 0)
        {
            Console.Error.WriteLine("Heavy PowerShell scripts remain. Move logic into C# tools before enabling strict mode.");
            return 1;
        }

        return 0;
    }

    private static ScriptBoundaryRow ClassifyScript(string path)
    {
        var content = File.ReadAllText(path);
        var normalizedPath = NormalizePath(path);
        var nonEmptyLines = content
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
            .ToArray();

        var invokesCSharpTool =
            content.Contains("dotnet run --project", StringComparison.Ordinal) &&
            content.Contains(@"\tools\", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("./tools/", StringComparison.OrdinalIgnoreCase) ||
            content.Contains(".\\tools\\", StringComparison.OrdinalIgnoreCase);

        var heavyPatterns = new[]
        {
            "ConvertTo-Json",
            "ConvertFrom-Json",
            "Get-ChildItem",
            "Import-Csv",
            "Select-String",
            "Get-FileHash",
            "Set-Content",
            "Out-File",
            "Add-Content",
            "New-Item",
            "Copy-Item",
            "Remove-Item",
            "Invoke-RestMethod",
            "Invoke-WebRequest",
            "function ",
            "class ",
            "ForEach-Object",
            "Where-Object"
        };

        var matchedHeavyPatterns = heavyPatterns
            .Where(pattern => content.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(pattern => pattern, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var classification = invokesCSharpTool && matchedHeavyPatterns.Length <= 2 && nonEmptyLines.Length <= 18
            ? "ThinWrapper"
            : matchedHeavyPatterns.Length > 0
                ? "HeavyPowerShellLogic"
                : "UnknownPowerShellScript";

        var targetTool = DetectTargetTool(content);

        return new ScriptBoundaryRow(
            normalizedPath,
            classification,
            targetTool,
            nonEmptyLines.Length,
            matchedHeavyPatterns);
    }

    private static string DetectTargetTool(string content)
    {
        if (content.Contains("AssistantEngineer.Tools.EngineeringCore", StringComparison.Ordinal))
            return "AssistantEngineer.Tools.EngineeringCore";

        if (content.Contains("AssistantEngineer.Tools.EnergyPlusValidation", StringComparison.Ordinal))
            return "AssistantEngineer.Tools.EnergyPlusValidation";

        if (content.Contains("AssistantEngineer.Tools.RepositoryBoundaries", StringComparison.Ordinal))
            return "AssistantEngineer.Tools.RepositoryBoundaries";

        return "";
    }

    private static string BuildMarkdown(
        IReadOnlyList<ScriptBoundaryRow> scripts,
        int thinScripts,
        int heavyScripts,
        int unknownScripts)
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Repository Script Boundary Audit");
        builder.AppendLine();
        builder.AppendLine($"Generated at: {StableGeneratedAtUtc}");
        builder.AppendLine();
        builder.AppendLine("## Status");
        builder.AppendLine();
        builder.AppendLine("| Field | Value |");
        builder.AppendLine("|---|---|");
        builder.AppendLine($"| Scripts | {scripts.Count} |");
        builder.AppendLine($"| Thin wrappers | {thinScripts} |");
        builder.AppendLine($"| Heavy PowerShell scripts | {heavyScripts} |");
        builder.AppendLine($"| Unknown PowerShell scripts | {unknownScripts} |");
        builder.AppendLine($"| Status | {(heavyScripts == 0 ? "Compliant" : "MigrationInProgress")} |");
        builder.AppendLine();
        builder.AppendLine("## Repository boundary");
        builder.AppendLine();
        builder.AppendLine("- `src/Backend` — application code.");
        builder.AppendLine("- `src/Frontend` — frontend code.");
        builder.AppendLine("- `tests` — test code.");
        builder.AppendLine("- `docs` — documentation and generated evidence.");
        builder.AppendLine("- `tools` — C# automation, validation and release tools.");
        builder.AppendLine("- `scripts` — thin wrappers only.");
        builder.AppendLine("- `.github/workflows` — CI entry points that call tools/scripts.");
        builder.AppendLine();
        builder.AppendLine("## Scripts");
        builder.AppendLine();
        builder.AppendLine("| Script | Classification | Target tool | Non-empty lines | Heavy patterns |");
        builder.AppendLine("|---|---|---|---:|---|");

        foreach (var script in scripts)
        {
            builder.AppendLine($"| `{script.Path}` | {script.Classification} | {script.TargetTool} | {script.NonEmptyLines} | {string.Join(", ", script.HeavyPatterns)} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Interpretation");
        builder.AppendLine();

        if (heavyScripts == 0)
        {
            builder.AppendLine("All audited PowerShell scripts are thin wrappers or contain no heavy automation logic.");
        }
        else
        {
            builder.AppendLine("Heavy PowerShell scripts remain. Their generation, validation or release logic must be moved into C# tools before strict mode is enabled.");
        }

        return builder.ToString();
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

    private static string NormalizePath(string path) =>
        path.Replace("\\", "/");

    private sealed record ScriptBoundaryRow(
        string Path,
        string Classification,
        string TargetTool,
        int NonEmptyLines,
        string[] HeavyPatterns);
}
