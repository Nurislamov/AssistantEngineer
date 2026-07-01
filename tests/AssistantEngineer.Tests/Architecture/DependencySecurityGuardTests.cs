using System.Xml.Linq;

namespace AssistantEngineer.Tests.Architecture;

public sealed class DependencySecurityGuardTests
{
    [Fact]
    public void ApiProjectPinsMicrosoftOpenApiToPatchedVersion()
    {
        var projectPath = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Api",
            "AssistantEngineer.Api.csproj");
        var document = XDocument.Load(projectPath);
        var package = document
            .Descendants("PackageReference")
            .SingleOrDefault(element =>
                string.Equals((string?)element.Attribute("Include"), "Microsoft.OpenApi", StringComparison.Ordinal));

        Assert.NotNull(package);
        var versionText = (string?)package.Attribute("Version");
        Assert.True(Version.TryParse(versionText, out var version), $"Invalid Microsoft.OpenApi version: {versionText}");
        Assert.True(version >= new Version(2, 7, 5), $"Microsoft.OpenApi must stay on patched 2.x: {version}");
        Assert.True(version < new Version(3, 0, 0), $"Review compatibility before moving Microsoft.OpenApi out of 2.x: {version}");
    }

    [Fact]
    public void RestoredAssetsDoNotResolveVulnerableMicrosoftOpenApiVersion()
    {
        var assetsFiles = Directory
            .GetFiles(TestPaths.RepoRoot, "project.assets.json", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(assetsFiles);
        foreach (var path in assetsFiles)
        {
            var assets = File.ReadAllText(path);
            Assert.DoesNotContain("\"Microsoft.OpenApi/2.0.0\"", assets, StringComparison.OrdinalIgnoreCase);
        }
    }
}
