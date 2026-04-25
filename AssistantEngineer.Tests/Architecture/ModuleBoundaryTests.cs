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
    private const string InfrastructureAssemblyName = "AssistantEngineer.Infrastructure";

    private static readonly Assembly SharedKernelAssembly = typeof(Result).Assembly;
    private static readonly Assembly BuildingsAssembly = typeof(AssistantEngineer.Modules.Buildings.DependencyInjection).Assembly;
    private static readonly Assembly CalculationsAssembly = typeof(AssistantEngineer.Modules.Calculations.DependencyInjection).Assembly;
    private static readonly Assembly EquipmentAssembly = typeof(AssistantEngineer.Modules.Equipment.DependencyInjection).Assembly;
    private static readonly Assembly ReportingAssembly = typeof(AssistantEngineer.Modules.Reporting.DependencyInjection).Assembly;
    private static readonly Assembly BenchmarksAssembly = typeof(AssistantEngineer.Modules.Benchmarks.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(AssistantEngineer.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    private static readonly Assembly[] ModuleAssemblies =
    [
        BuildingsAssembly,
        CalculationsAssembly,
        EquipmentAssembly,
        ReportingAssembly,
        BenchmarksAssembly
    ];

    private static readonly string[] ModuleAssemblyNames =
    [
        BuildingsAssemblyName,
        CalculationsAssemblyName,
        EquipmentAssemblyName,
        ReportingAssemblyName,
        BenchmarksAssemblyName
    ];

    [Fact]
    public void BuildingsModuleDoesNotDependOnCalculationsModule()
    {
        AssertNoAssemblyReferences(BuildingsAssembly, CalculationsAssemblyName);
        AssertNoTypeDependencies(BuildingsAssembly, CalculationsAssemblyName);
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
    public void ApiDoesNotDependOnModuleInternalServicesOrMappers()
    {
        AssertNoTypeDependencies(ApiAssembly, "AssistantEngineer.Modules.Buildings.Application.Services");
        AssertNoTypeDependencies(ApiAssembly, "AssistantEngineer.Modules.Calculations.Application.Services");
        AssertNoTypeDependencies(ApiAssembly, "AssistantEngineer.Modules.Equipment.Application.Services");
        AssertNoTypeDependencies(ApiAssembly, "AssistantEngineer.Modules.Reporting.Application.Services");
        AssertNoTypeDependencies(ApiAssembly, "AssistantEngineer.Modules.Benchmarks.Application.Services");
        AssertNoTypeDependencies(ApiAssembly, "AssistantEngineer.Modules.Calculations.Application.Mappers");
        AssertNoTypeDependencies(ApiAssembly, typeof(IBuildingEnergyAnalysisFacade).FullName!);
        AssertNoTypeDependencies(ApiAssembly, typeof(IBuildingComfortAnalysisFacade).FullName!);
        AssertNoTypeDependencies(ApiAssembly, typeof(IBuildingSizingAnalysisFacade).FullName!);
    }

    [Fact]
    public void ControllersDependOnlyOnModuleFacadeContracts()
    {
        var allowedDependencyTypes = new HashSet<Type>
        {
            typeof(IBenchmarksFacade),
            typeof(IBuildingsFacade),
            typeof(ICalculationsFacade),
            typeof(IEquipmentFacade),
            typeof(IReportsFacade)
        };

        var violatingConstructors = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type is { IsAbstract: false, IsPublic: true } &&
                typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(type))
            .SelectMany(type => type.GetConstructors()
                .Select(constructor => new
                {
                    Controller = type,
                    ViolatingParameters = constructor
                        .GetParameters()
                        .Select(parameter => parameter.ParameterType)
                        .Where(parameterType =>
                            parameterType.Assembly.GetName().Name?.StartsWith("AssistantEngineer.Modules.", StringComparison.Ordinal) == true &&
                            !allowedDependencyTypes.Contains(parameterType))
                        .Select(parameterType => parameterType.FullName ?? parameterType.Name)
                        .Order(StringComparer.Ordinal)
                        .ToArray()
                }))
            .Where(result => result.ViolatingParameters.Length > 0)
            .Select(result =>
                $"{result.Controller.FullName}: {string.Join(", ", result.ViolatingParameters)}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violatingConstructors.Length == 0,
            $"Controllers depend on non-facade module types: {string.Join("; ", violatingConstructors)}.");
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

    private static void AssertNoAssemblyReferences(Assembly sourceAssembly, params string[] forbiddenAssemblyNames)
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

    private static void AssertNoTypeDependencies(Assembly sourceAssembly, string forbiddenDependency)
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
