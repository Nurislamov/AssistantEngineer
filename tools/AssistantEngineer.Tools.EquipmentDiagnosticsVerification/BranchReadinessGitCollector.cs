using System.Diagnostics;
using System.Text;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;

namespace AssistantEngineer.Tools.EquipmentDiagnosticsVerification;

internal static class BranchReadinessGitCollector
{
    public static (string CurrentBranch, IReadOnlyList<BranchReadinessFileInput> Files) Collect(
        string repoRoot,
        string baseRef)
    {
        var currentBranch = RunGit(repoRoot, "branch", "--show-current").Trim();
        var files = new Dictionary<string, MutableFileState>(StringComparer.OrdinalIgnoreCase);

        AddNameStatus(files, RunGit(repoRoot, "diff", "--name-status", $"{baseRef}...HEAD"), state =>
            state.IsBranchChange = true);
        AddPorcelainStatus(files, RunGit(repoRoot, "status", "--porcelain=v1", "-uall"));

        return (
            currentBranch,
            files.Values
                .OrderBy(state => state.Path, StringComparer.Ordinal)
                .Select(state => new BranchReadinessFileInput(
                    Path: state.Path,
                    ChangeType: state.ChangeType,
                    IsBranchChange: state.IsBranchChange,
                    IsStaged: state.IsStaged,
                    IsUnstaged: state.IsUnstaged,
                    IsUntracked: state.IsUntracked,
                    Content: ReadContent(repoRoot, state)))
                .ToArray());
    }

    private static void AddPorcelainStatus(
        IDictionary<string, MutableFileState> files,
        string output)
    {
        foreach (var line in SplitRawLines(output))
        {
            if (line.Length < 4)
            {
                continue;
            }

            var indexStatus = line[0];
            var workingTreeStatus = line[1];
            var rawPath = line[3..];
            var path = rawPath.Contains(" -> ", StringComparison.Ordinal)
                ? rawPath[(rawPath.LastIndexOf(" -> ", StringComparison.Ordinal) + 4)..]
                : rawPath;
            var state = GetOrAdd(files, path.Trim('"'));

            if (indexStatus == '?' && workingTreeStatus == '?')
            {
                state.IsUntracked = true;
                state.ChangeType = "Added";
                continue;
            }

            state.IsStaged |= indexStatus != ' ';
            state.IsUnstaged |= workingTreeStatus != ' ';
            state.ChangeType = MapChangeType(
                indexStatus != ' ' ? indexStatus.ToString() : workingTreeStatus.ToString());
        }
    }

    private static void AddNameStatus(
        IDictionary<string, MutableFileState> files,
        string output,
        Action<MutableFileState> mark)
    {
        foreach (var line in SplitLines(output))
        {
            var parts = line.Split('\t');
            if (parts.Length < 2)
            {
                continue;
            }

            var status = parts[0];
            var path = parts[^1];
            var state = GetOrAdd(files, path);
            state.ChangeType = MapChangeType(status);
            mark(state);
        }
    }

    private static MutableFileState GetOrAdd(
        IDictionary<string, MutableFileState> files,
        string path)
    {
        var normalized = path.Replace('\\', '/');
        if (!files.TryGetValue(normalized, out var state))
        {
            state = new MutableFileState(normalized);
            files.Add(normalized, state);
        }

        return state;
    }

    private static string? ReadContent(string repoRoot, MutableFileState state)
    {
        if (state.ChangeType == "Deleted")
        {
            return null;
        }

        var path = Path.Combine(repoRoot, state.Path.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path) || new FileInfo(path).Length > 2_000_000)
        {
            return null;
        }

        try
        {
            return File.ReadAllText(path);
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
    }

    private static string MapChangeType(string status) =>
        status.Length == 0
            ? "Modified"
            : status[0] switch
            {
                'A' => "Added",
                'D' => "Deleted",
                'R' => "Renamed",
                'C' => "Copied",
                _ => "Modified"
            };

    private static IReadOnlyList<string> SplitLines(string output) =>
        output.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static IReadOnlyList<string> SplitRawLines(string output) =>
        output.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

    private static string RunGit(string repoRoot, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ??
            throw new InvalidOperationException("Failed to start git.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"git {string.Join(' ', arguments)} failed with exit code {process.ExitCode}: {error.Trim()}");
        }

        return output;
    }

    private sealed class MutableFileState(string path)
    {
        public string Path { get; } = path;
        public string ChangeType { get; set; } = "Modified";
        public bool IsBranchChange { get; set; }
        public bool IsStaged { get; set; }
        public bool IsUnstaged { get; set; }
        public bool IsUntracked { get; set; }
    }
}
