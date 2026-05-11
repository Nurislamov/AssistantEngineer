namespace AssistantEngineer.Tests.Calculations.Iso52016;





public class Iso52016MatrixOnlyGuardTests


{


    [Fact]


    public void SourceTree_DoesNotExposeSimulationEngineSelection()


    {


        var repoRoot = FindRepositoryRoot();





        var sourceText = string.Join(


            Environment.NewLine,


            Directory


                .GetFiles(Path.Combine(repoRoot, "src", "Backend"), "*.cs", SearchOption.AllDirectories)


                .Select(File.ReadAllText));





        Assert.DoesNotContain("Iso52016SimulationEngine", sourceText);


        Assert.DoesNotContain("SimulationEngine", sourceText);


        Assert.DoesNotContain("simulationEngine", sourceText);




        Assert.DoesNotContain("Legacy", sourceText);


    }





    [Fact]


    public void SourceTree_DoesNotReferenceRemovedSimplifiedHeatBalanceSolver()


    {


        var repoRoot = FindRepositoryRoot();





        var sourceText = string.Join(


            Environment.NewLine,


            Directory


                .GetFiles(Path.Combine(repoRoot, "src", "Backend"), "*.cs", SearchOption.AllDirectories)


                .Select(File.ReadAllText));





        Assert.DoesNotContain("ISo52016RoomHeatBalanceSolver", sourceText);


        Assert.DoesNotContain("Iso52016RoomHeatBalanceSolver", sourceText);


        Assert.DoesNotContain("Iso52016RoomHeatBalanceRequest", sourceText);


    }





    [Fact]


    public void Documentation_RecordsMatrixOnlyDecisionWithoutPublicEngineSwitch()


    {


        var repoRoot = FindRepositoryRoot();





        var docPath = Path.Combine(


            repoRoot,


            "docs",


            "calculations",


            "Iso52016MatrixOnly.md");





        Assert.True(File.Exists(docPath), $"Documentation was not found: {docPath}");





        var text = File.ReadAllText(docPath);





        Assert.Contains("Matrix-only", text);


        Assert.Contains("single Matrix calculation path", text);


        Assert.Contains("no public simulation engine selector", text);


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