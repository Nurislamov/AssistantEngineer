namespace AssistantEngineer.Tests.Architecture.Governance;

internal static class GovernancePathHelper
{
    public static string RepoRoot => TestPaths.RepoRoot;

    public static string SecurityDocsDirectory =>
        Path.Combine(RepoRoot, "docs", "security");

    public static string ArchitectureTestsDirectory =>
        Path.Combine(RepoRoot, "tests", "AssistantEngineer.Tests", "Architecture");

    public static string OwnershipBackfillToolDirectory =>
        Path.Combine(RepoRoot, "tools", "AssistantEngineer.Tools.OwnershipBackfill");

    public static string GitIgnorePath =>
        Path.Combine(RepoRoot, ".gitignore");

    public static string SecurityDocPath(string fileName) =>
        Path.Combine(SecurityDocsDirectory, fileName);

    public static string ResolveRepoPath(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(RepoRoot, normalized);
    }

    public static string ToRepoRelative(string absolutePath)
    {
        return Path.GetRelativePath(RepoRoot, absolutePath)
            .Replace(Path.DirectorySeparatorChar, '/');
    }
}
