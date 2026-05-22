using System.Reflection;
using AssistantEngineer.Api;
using AssistantEngineer.Modules.Benchmarks.Application.Facades;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Modules.Equipment.Application.Facades;
using AssistantEngineer.Modules.Reporting.Application.Facades;
using AssistantEngineer.SharedKernel.Primitives;
using NetArchTest.Rules;

namespace AssistantEngineer.Tests.Architecture;

public class ModuleBoundaryTests
{
    private const string SharedKernelAssemblyName = "AssistantEngineer.SharedKernel";
    private const string BuildingsAssemblyName = "AssistantEngineer.Modules.Buildings";
    private const string CalculationsAssemblyName = "AssistantEngineer.Modules.Calculations";
    private const string EquipmentAssemblyName = "AssistantEngineer.Modules.Equipment";
    private const string ReportingAssemblyName = "AssistantEngineer.Modules.Reporting";
    private const string BenchmarksAssemblyName = "AssistantEngineer.Modules.Benchmarks";
    private const string EngineeringWorkflowAssemblyName = "AssistantEngineer.Modules.EngineeringWorkflow";
    private const string IdentityAssemblyName = "AssistantEngineer.Modules.Identity";
    private const string InfrastructureAssemblyName = "AssistantEngineer.Infrastructure";

    private static readonly Assembly SharedKernelAssembly = typeof(Result).Assembly;
    private static readonly Assembly BuildingsAssembly = typeof(AssistantEngineer.Modules.Buildings.DependencyInjection).Assembly;
    private static readonly Assembly CalculationsAssembly = typeof(AssistantEngineer.Modules.Calculations.DependencyInjection).Assembly;
    private static readonly Assembly EquipmentAssembly = typeof(AssistantEngineer.Modules.Equipment.DependencyInjection).Assembly;
    private static readonly Assembly ReportingAssembly = typeof(AssistantEngineer.Modules.Reporting.DependencyInjection).Assembly;
    private static readonly Assembly BenchmarksAssembly = typeof(AssistantEngineer.Modules.Benchmarks.DependencyInjection).Assembly;
    private static readonly Assembly EngineeringWorkflowAssembly = typeof(AssistantEngineer.Modules.EngineeringWorkflow.DependencyInjection).Assembly;
    private static readonly Assembly IdentityAssembly = typeof(AssistantEngineer.Modules.Identity.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(AssistantEngineer.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    private static readonly Assembly[] ModuleAssemblies =
    [
        BuildingsAssembly,
        CalculationsAssembly,
        EquipmentAssembly,
        ReportingAssembly,
        BenchmarksAssembly,
        EngineeringWorkflowAssembly,
        IdentityAssembly
    ];

    private static readonly string[] ModuleAssemblyNames =
    [
        BuildingsAssemblyName,
        CalculationsAssemblyName,
        EquipmentAssemblyName,
        ReportingAssemblyName,
        BenchmarksAssemblyName,
        EngineeringWorkflowAssemblyName,
        IdentityAssemblyName
    ];

    [Fact]
    public void BuildingsModuleDoesNotDependOnCalculationsModule()
    {
        AssertNoAssemblyReferences(BuildingsAssembly, CalculationsAssemblyName);
        AssertNoTypeDependencies(BuildingsAssembly, CalculationsAssemblyName);
    }

    [Fact]
    public void EquipmentModuleDoesNotDependOnBuildingsModule()
    {
        AssertNoAssemblyReferences(EquipmentAssembly, BuildingsAssemblyName);
        AssertNoTypeDependencies(EquipmentAssembly, BuildingsAssemblyName);
    }

    [Fact]
    public void EquipmentModuleDoesNotDependOnCalculationsModule()
    {
        AssertNoAssemblyReferences(EquipmentAssembly, CalculationsAssemblyName);
        AssertNoTypeDependencies(EquipmentAssembly, CalculationsAssemblyName);
    }

    [Fact]
    public void SharedKernelDoesNotDependOnModules()
    {
        AssertNoAssemblyReferences(SharedKernelAssembly, ModuleAssemblyNames);

        foreach (var moduleAssemblyName in ModuleAssemblyNames)
            AssertNoTypeDependencies(SharedKernelAssembly, moduleAssemblyName);
    }

    [Fact]
    public void ApiDoesNotDependOnPersistenceLayerDetails()
    {
        AssertNoAssemblyReferences(ApiAssembly, "AssistantEngineer.Infrastructure.Persistence");
        AssertNoTypeDependencies(ApiAssembly, "AssistantEngineer.Infrastructure.Persistence");
    }


    [Fact]
    public void ReportingDoesNotDependOnCalculationImplementationDetails()
    {
        AssertNoTypeDependencies(ReportingAssembly, "AssistantEngineer.Modules.Calculations.Application.Services");
        AssertNoTypeDependencies(ReportingAssembly, "AssistantEngineer.Modules.Calculations.Application.Mappers");
        AssertNoTypeDependencies(ReportingAssembly, "AssistantEngineer.Modules.Calculations.Application.Validation");
        AssertNoTypeDependencies(ReportingAssembly, "AssistantEngineer.Modules.Calculations.Application.Abstractions");
    }

    [Fact]
    public void ReportingDoesNotDependOnEquipmentImplementationDetails()
    {
        AssertNoTypeDependencies(ReportingAssembly, "AssistantEngineer.Modules.Equipment.Application.Services");
        AssertNoTypeDependencies(ReportingAssembly, "AssistantEngineer.Modules.Equipment.Application.Abstractions");
        AssertNoTypeDependencies(ReportingAssembly, "AssistantEngineer.Modules.Equipment.Domain");
    }

    [Fact]
    public void EquipmentDoesNotCalculateLoadsInternally()
    {
        AssertNoTypeDependencies(EquipmentAssembly, "AssistantEngineer.Modules.Calculations.Application.Services");
        AssertNoTypeDependencies(EquipmentAssembly, "AssistantEngineer.Modules.Calculations.Application.Validation");
        AssertNoTypeDependencies(EquipmentAssembly, "AssistantEngineer.Modules.Calculations.Application.Mappers");
        AssertNoTypeDependencies(EquipmentAssembly, "AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories");
    }

    [Fact]
    public void InfrastructureMayReferenceApplicationModules()
    {
        var referencedAssemblyNames = InfrastructureAssembly
            .GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(BuildingsAssemblyName, referencedAssemblyNames);
        Assert.Contains(CalculationsAssemblyName, referencedAssemblyNames);
        Assert.Contains(EquipmentAssemblyName, referencedAssemblyNames);
        Assert.Contains(ReportingAssemblyName, referencedAssemblyNames);
        Assert.Contains(BenchmarksAssemblyName, referencedAssemblyNames);
    }

    [Fact]
    public void ModulesDoNotDependOnInfrastructure()
    {
        foreach (var moduleAssembly in ModuleAssemblies)
        {
            AssertNoAssemblyReferences(moduleAssembly, InfrastructureAssemblyName);
            AssertNoTypeDependencies(moduleAssembly, InfrastructureAssemblyName);
        }
    }

    private static void AssertNoAssemblyReferences(
        Assembly sourceAssembly,
        params string[] forbiddenAssemblyNames)
    {
        var referencedAssemblyNames = sourceAssembly
            .GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.Ordinal);

        var violations = forbiddenAssemblyNames
            .Where(referencedAssemblyNames.Contains)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"{sourceAssembly.GetName().Name} references forbidden assemblies: {string.Join(", ", violations)}.");
    }

    private static void AssertNoTypeDependencies(
        Assembly sourceAssembly,
        string forbiddenDependency)
    {
        var result = Types
            .InAssembly(sourceAssembly)
            .Should()
            .NotHaveDependencyOn(forbiddenDependency)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"{sourceAssembly.GetName().Name} has forbidden dependency on {forbiddenDependency}: {FormatFailingTypes(result)}.");
    }

    private static string FormatFailingTypes(TestResult result)
    {
        return result.FailingTypeNames is null
            ? "no failing type details returned"
            : string.Join(", ", result.FailingTypeNames);
    }
}
