using System.Diagnostics;

namespace AssistantEngineer.Tools.EngineeringCoreVerification;

internal interface IEngineeringCoreVerificationProcessRunner
{
    int RunProcess(string fileName, string arguments);
}

internal sealed class EngineeringCoreVerificationProcessRunner : IEngineeringCoreVerificationProcessRunner
{
    public int RunProcess(string fileName, string arguments)
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
}
