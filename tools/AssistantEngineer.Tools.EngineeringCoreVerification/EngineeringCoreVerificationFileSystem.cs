namespace AssistantEngineer.Tools.EngineeringCoreVerification;

internal sealed class EngineeringCoreVerificationFileSystem
{
    public string GetCurrentDirectory() => Directory.GetCurrentDirectory();

    public void SetCurrentDirectory(string path) => Directory.SetCurrentDirectory(path);

    public bool FileExists(string path) => File.Exists(path);

    public string ReadAllText(string path) => File.ReadAllText(path);

    public string FindRepositoryRoot(string startDirectory)
    {
        var current = new DirectoryInfo(startDirectory);

        while (current is not null)
        {
            if (FileExists(Path.Combine(current.FullName, "AssistantEngineer.sln")))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root with AssistantEngineer.sln was not found.");
    }

    public IEnumerable<string> EnumerateTextFiles(string path)
    {
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".git", "bin", "obj", "node_modules", "dist", "coverage", "TestResults"
        };

        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".md", ".json", ".yml", ".yaml", ".ps1", ".txt", ".tsx", ".ts", ".xml", ".csv", ".sln"
        };

        if (FileExists(path))
        {
            if (extensions.Contains(Path.GetExtension(path)))
                yield return path;

            yield break;
        }

        if (!Directory.Exists(path))
            yield break;

        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(path, file);
            var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (segments.Any(segment => excluded.Contains(segment)))
                continue;

            if (!extensions.Contains(Path.GetExtension(file)))
                continue;

            yield return file;
        }
    }
}
