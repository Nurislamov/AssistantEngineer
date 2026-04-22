using AssistantEngineer.Api;

namespace AssistantEngineer.Tests.Architecture;

public class FacadeBoundaryTests
{
    private static readonly string[] AllowedFacadeTypeNames =
    [
        "AssistantEngineer.Modules.Reporting.Application.Facades.IReportsFacade",
        "AssistantEngineer.Modules.Reporting.Application.Facades.ReportsFacade",
        "AssistantEngineer.Modules.Calculations.Application.Facades.IBuildingEnergyAnalysisFacade",
        "AssistantEngineer.Modules.Calculations.Application.Facades.BuildingEnergyAnalysisFacade",
        "AssistantEngineer.Modules.Benchmarks.Application.Facades.IBenchmarksFacade",
        "AssistantEngineer.Modules.Benchmarks.Application.Facades.BenchmarksFacade"
    ];

    [Fact]
    public void OnlyOrchestrationFacadesRemain()
    {
        var assemblies = typeof(Program).Assembly
            .GetReferencedAssemblies()
            .Where(assemblyName =>
                assemblyName.Name is not null &&
                assemblyName.Name.StartsWith("AssistantEngineer.", StringComparison.Ordinal))
            .Select(assemblyName => AppDomain.CurrentDomain.Load(assemblyName))
            .Append(typeof(Program).Assembly)
            .Distinct()
            .ToArray();

        var facadeTypeNames = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type =>
                !type.IsNested &&
                type.Namespace?.Contains(".Application.Facades", StringComparison.Ordinal) == true)
            .Select(type => type.FullName ?? type.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(AllowedFacadeTypeNames.Order(StringComparer.Ordinal), facadeTypeNames);
    }
}
