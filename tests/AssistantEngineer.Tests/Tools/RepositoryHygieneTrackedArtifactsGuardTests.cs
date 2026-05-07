using System.Diagnostics;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Tools;

public class RepositoryHygieneTrackedArtifactsGuardTests
{
    [Fact]
    public void GitIgnore_CoversLocalAndGeneratedArtifacts()
    {
        var gitIgnorePath = Path.Combine(TestPaths.RepoRoot, ".gitignore");
        Assert.True(File.Exists(gitIgnorePath), $".gitignore was not found: {gitIgnorePath}");

        var gitIgnore = File.ReadAllText(gitIgnorePath);
        var requiredEntries = new[]
        {
            ".vs/",
            "bin/",
            "obj/",
            "*.user",
            "*.suo",
            "*.wsuo",
            "coverage/",
            "artifacts/",
            "generated/",
            "TestResults/"
        };

        foreach (var requiredEntry in requiredEntries)
        {
            Assert.Contains(requiredEntry, gitIgnore, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void GitIndex_DoesNotTrackLocalOrGeneratedArtifacts()
    {
        var trackedFiles = RunGit("ls-files")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(path => path.Replace('\\', '/'))
            .ToArray();

        var violations = trackedFiles
            .Where(path =>
                path.StartsWith(".vs/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("artifacts/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("generated/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("TestResults/", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".user", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".suo", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".wsuo", StringComparison.OrdinalIgnoreCase) ||
                path.Split('/').Any(segment =>
                    segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                    segment.Equals("obj", StringComparison.OrdinalIgnoreCase)))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Tracked local/generated artifacts were found:\n" + string.Join('\n', violations.Select(item => " - " + item)));
    }

    private static string RunGit(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveGitExecutable(),
            Arguments = arguments,
            WorkingDirectory = TestPaths.RepoRoot,
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
}
