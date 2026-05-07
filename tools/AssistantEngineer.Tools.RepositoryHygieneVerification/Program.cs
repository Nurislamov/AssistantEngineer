using System.Diagnostics;
using System.Text.Json;

namespace AssistantEngineer.Tools.RepositoryHygieneVerification;

internal static class Program
{
    private static readonly string GitConflictStartMarker = new('<', 7);
    private static readonly string GitConflictMiddleMarker = new('=', 7);
    private static readonly string GitConflictEndMarker = new('>', 7);

    public static int Main(string[] args)
    {
        try
        {
            if (args.Any(arg => arg is "-h" or "--help" or "help"))
            {
                PrintHelp();
                return 0;
            }

            var options = RepositoryHygieneOptions.Parse(args);
            var repoRoot = ResolveRepositoryRoot(options.RepositoryRoot);

            Console.WriteLine("AssistantEngineer repository hygiene verification");
            Console.WriteLine($"Repository: {repoRoot}");
            Console.WriteLine("Scope: ISO52016 physical chain branch hygiene and JSON/conflict-marker checks.");

            AssertNoRebaseInProgress(repoRoot);
            AssertRequiredPhysicalChainFiles(repoRoot);
            AssertJsonFilesParse(repoRoot);
            AssertNoConflictMarkers(repoRoot);
            AssertNoTrackedLocalOrGeneratedArtifacts(repoRoot);

            if (options.CheckRootPatchScripts)
                AssertNoRootPatchScripts(repoRoot);

            if (options.RequireClean)
                AssertWorkingTreeClean(repoRoot);

            WriteSuccess("Repository hygiene verification passed.");
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
        Console.WriteLine("AssistantEngineer repository hygiene verification tool");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --repo-root <path>");
        Console.WriteLine("  --require-clean");
        Console.WriteLine("  --check-root-patch-scripts");
    }

    private static string ResolveRepositoryRoot(string? explicitRoot)
    {
        if (!string.IsNullOrWhiteSpace(explicitRoot))
            return Path.GetFullPath(explicitRoot);

        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AssistantEngineer.sln")))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root with AssistantEngineer.sln was not found.");
    }

    private static void AssertNoRebaseInProgress(string repoRoot)
    {
        var gitRoot = Path.Combine(repoRoot, ".git");
        if (!Directory.Exists(gitRoot))
            return;

        var forbiddenMarkers = new[]
        {
            "MERGE_HEAD",
            "CHERRY_PICK_HEAD",
            "REVERT_HEAD",
            Path.Combine("rebase-merge", "git-rebase-todo"),
            Path.Combine("rebase-apply", "next")
        };

        foreach (var marker in forbiddenMarkers)
        {
            var path = Path.Combine(gitRoot, marker);
            if (File.Exists(path))
                throw new InvalidOperationException($"Git operation is still in progress: {marker}.");
        }
    }

    private static void AssertRequiredPhysicalChainFiles(string repoRoot)
    {
        var requiredFiles = new[]
        {
            "docs/releases/Iso52016PhysicalChainFinalReadinessManifest.json",
            "docs/traceability/Iso52016PhysicalChainTraceabilityMatrix.json",
            "scripts/iso52016/assert-iso52016-physical-chain-final-ready.ps1",
            "scripts/iso52016/verify-iso52016-physical-model-chain.ps1",
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Iso52016/Physical/Iso52016PhysicalRoomModelBuilder.cs",
            "src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Iso52016/Physical/Iso52016PhysicalModelSelectionService.cs",
            "tests/AssistantEngineer.Tests/Calculations/Iso52016/Physical/Iso52016PhysicalChainFinalReadinessTests.cs"
        };

        foreach (var relativePath in requiredFiles)
        {
            var path = Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
                throw new FileNotFoundException($"Required ISO52016 physical chain file is missing: {relativePath}", path);
        }
    }

    private static void AssertNoConflictMarkers(string repoRoot)
    {
        var badEntries = new List<string>();

        foreach (var file in EnumerateTextFiles(repoRoot))
        {
            var lineNumber = 0;
            foreach (var line in File.ReadLines(file.FullName))
            {
                lineNumber++;
                if (IsGitConflictMarkerLine(line))
                    badEntries.Add($"{ToRelativePath(repoRoot, file.FullName)}:{lineNumber}");
            }
        }

        if (badEntries.Count > 0)
        {
            throw new InvalidOperationException(
                "Git conflict markers were found:\n" + string.Join("\n", badEntries.Select(entry => " - " + entry)));
        }
    }

    private static bool IsGitConflictMarkerLine(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith(GitConflictStartMarker, StringComparison.Ordinal) ||
            trimmed.StartsWith(GitConflictMiddleMarker, StringComparison.Ordinal) ||
            trimmed.StartsWith(GitConflictEndMarker, StringComparison.Ordinal);
    }

    private static void AssertJsonFilesParse(string repoRoot)
    {
        var invalidFiles = new List<string>();

        foreach (var file in EnumerateFilesByExtensions(repoRoot, new[] { ".json" }))
        {
            try
            {
                using var _ = JsonDocument.Parse(File.ReadAllText(file.FullName));
            }
            catch (JsonException exception)
            {
                invalidFiles.Add($"{ToRelativePath(repoRoot, file.FullName)}: {exception.Message}");
            }
        }

        if (invalidFiles.Count > 0)
        {
            throw new InvalidOperationException(
                "Invalid JSON files were found:\n" + string.Join("\n", invalidFiles.Select(entry => " - " + entry)));
        }
    }

    private static void AssertNoRootPatchScripts(string repoRoot)
    {
        var patchScripts = Directory
            .EnumerateFiles(repoRoot, "ae-iso52016-*.ps1", SearchOption.TopDirectoryOnly)
            .Select(path => Path.GetFileName(path))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (patchScripts.Length > 0)
        {
            throw new InvalidOperationException(
                "Root patch scripts must not be committed or left before release hygiene checks:\n" +
                string.Join("\n", patchScripts.Select(name => " - " + name)));
        }
    }

    private static void AssertWorkingTreeClean(string repoRoot)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveGitExecutable(),
            Arguments = "status --porcelain",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start git status.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"git status failed with exit code {process.ExitCode}: {error}");

        if (!string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidOperationException(
                "Working tree is not clean:\n" + output.TrimEnd());
        }
    }

    private static void AssertNoTrackedLocalOrGeneratedArtifacts(string repoRoot)
    {
        var trackedFiles = RunGit(repoRoot, "ls-files")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(path => path.Replace('\\', '/'))
            .ToArray();

        var forbiddenPrefixes = new[]
        {
            ".vs/",
            "artifacts/",
            "generated/",
            "TestResults/"
        };

        var forbiddenSuffixes = new[]
        {
            ".user",
            ".suo",
            ".wsuo"
        };

        var violations = trackedFiles
            .Where(path =>
                forbiddenPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) ||
                forbiddenSuffixes.Any(suffix => path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) ||
                path.Split('/').Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                                               segment.Equals("obj", StringComparison.OrdinalIgnoreCase)))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (violations.Length > 0)
        {
            throw new InvalidOperationException(
                "Tracked local/generated artifacts were found in git index:\n" +
                string.Join("\n", violations.Select(item => " - " + item)));
        }
    }

    private static IEnumerable<FileInfo> EnumerateTextFiles(string repoRoot) =>
        EnumerateFilesByExtensions(
            repoRoot,
            new[]
            {
                ".cs", ".csproj", ".props", ".targets", ".sln",
                ".json", ".md", ".ps1", ".yml", ".yaml",
                ".xml", ".txt", ".ts", ".tsx", ".js", ".jsx", ".html", ".css"
            });

    private static IEnumerable<FileInfo> EnumerateFilesByExtensions(string repoRoot, IReadOnlyCollection<string> extensions)
    {
        var excludedSegments = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".git", ".vs", "bin", "obj", "node_modules", "dist", "coverage", "TestResults"
        };

        var normalizedExtensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);

        foreach (var path in Directory.EnumerateFiles(repoRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = ToRelativePath(repoRoot, path);
            var segments = relativePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Any(segment => excludedSegments.Contains(segment)))
                continue;

            if (normalizedExtensions.Contains(Path.GetExtension(path)))
                yield return new FileInfo(path);
        }
    }

    private static string ToRelativePath(string repoRoot, string path)
    {
        var fullRoot = Path.GetFullPath(repoRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(path);

        if (fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            return fullPath.Substring(fullRoot.Length).Replace(Path.DirectorySeparatorChar, '/');

        return path;
    }

    private static string ResolveGitExecutable()
    {
        if (!OperatingSystem.IsWindows())
            return "git";

        var pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory.Trim('"'), "git.exe");
            if (File.Exists(candidate))
                return candidate;
        }

        return "git.exe";
    }

    private static string RunGit(string repoRoot, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveGitExecutable(),
            Arguments = arguments,
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start git {arguments}.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"git {arguments} failed with exit code {process.ExitCode}: {error}");

        return output;
    }

    private static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private sealed record RepositoryHygieneOptions(
        string? RepositoryRoot,
        bool RequireClean,
        bool CheckRootPatchScripts)
    {
        public static RepositoryHygieneOptions Parse(IReadOnlyList<string> args)
        {
            string? repoRoot = null;
            var requireClean = false;
            var checkRootPatchScripts = false;

            for (var i = 0; i < args.Count; i++)
            {
                var arg = args[i];

                if (string.Equals(arg, "--repo-root", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Count)
                        throw new ArgumentException("--repo-root requires a value.");

                    repoRoot = args[++i];
                    continue;
                }

                if (string.Equals(arg, "--require-clean", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "-RequireClean", StringComparison.OrdinalIgnoreCase))
                {
                    requireClean = true;
                    continue;
                }

                if (string.Equals(arg, "--check-root-patch-scripts", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "-CheckRootPatchScripts", StringComparison.OrdinalIgnoreCase))
                {
                    checkRootPatchScripts = true;
                    continue;
                }

                throw new ArgumentException($"Unknown option: {arg}");
            }

            return new RepositoryHygieneOptions(repoRoot, requireClean, checkRootPatchScripts);
        }
    }
}
