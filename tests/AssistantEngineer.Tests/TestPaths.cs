namespace AssistantEngineer.Tests;

internal static class TestPaths
{
    public static string RepoRoot { get; } = FindRepoRoot();

    public static string ApiProjectPath =>
        Path.Combine(
            RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api");

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var solutionPath = Path.Combine(
                directory.FullName,
                "AssistantEngineer.sln");

            if (File.Exists(solutionPath))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Repository root containing AssistantEngineer.sln was not found.");
    }
}
