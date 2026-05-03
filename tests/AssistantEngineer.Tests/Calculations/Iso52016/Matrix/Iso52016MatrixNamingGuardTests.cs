namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixNamingGuardTests
{
    [Fact]
    public void SourceTree_DoesNotExposeOldVersionedNames()
    {
        var repoRoot = FindRepositoryRoot();

        var sourceText = string.Join(
            Environment.NewLine,
            Directory
                .GetFiles(Path.Combine(repoRoot, "src", "Backend"), "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.DoesNotContain("Iso52016" + "V2", sourceText);
        Assert.DoesNotContain("IIso52016" + "V2", sourceText);
        Assert.DoesNotContain(".Iso52016." + "V2", sourceText);
        Assert.DoesNotContain("V2Matrix", sourceText);
    }

    [Fact]
    public void Tests_DoNotUseOldVersionedNamespacesOrClassNames()
    {
        var repoRoot = FindRepositoryRoot();

        var testText = string.Join(
            Environment.NewLine,
            Directory
                .GetFiles(Path.Combine(repoRoot, "tests", "AssistantEngineer.Tests"), "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.DoesNotContain("Iso52016" + "V2", testText);
        Assert.DoesNotContain("IIso52016" + "V2", testText);
        Assert.DoesNotContain(".Iso52016." + "V2", testText);
    }

    [Fact]
    public void MatrixStageDocumentationAndVerificationUseMatrixNames()
    {
        var repoRoot = FindRepositoryRoot();

        var requiredFiles = new[]
        {
            Path.Combine(repoRoot, "docs", "calculations", "Iso52016MatrixSolverStage.md"),
            Path.Combine(repoRoot, "docs", "releases", "Iso52016MatrixSolverStageManifest.json"),
            Path.Combine(repoRoot, "docs", "traceability", "Iso52016MatrixSolverTraceabilityMatrix.json"),
            Path.Combine(repoRoot, "scripts", "iso52016", "verify-iso52016-matrix-solver-stage.ps1")
        };

        foreach (var file in requiredFiles)
        {
            Assert.True(File.Exists(file), $"Required Matrix stage file was not found: {file}");
            var text = File.ReadAllText(file);

            Assert.Contains("Matrix", text);
            Assert.DoesNotContain("Iso52016" + "V2", text);
            Assert.DoesNotContain("V2Matrix", text);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var src = Path.Combine(
                directory.FullName,
                "src",
                "Backend",
                "AssistantEngineer.Modules.Calculations");

            var tests = Path.Combine(
                directory.FullName,
                "tests",
                "AssistantEngineer.Tests");

            if (Directory.Exists(src) && Directory.Exists(tests))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate AssistantEngineer repository root from test base directory.");
    }
}