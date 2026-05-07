using AssistantEngineer.Api;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Modules.Calculations.Application.Services.Buildings;
using AssistantEngineer.Modules.Calculations.Application.Services.Floors;
using AssistantEngineer.Modules.Calculations.Application.Services.Rooms;
using AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;

namespace AssistantEngineer.Tests.Architecture;

public class LegacyCalculationServiceDependencyGuardTests
{
    private static readonly HashSet<Type> LegacyCalculationServiceTypes =
    [
        typeof(BuildingCoolingLoadService),
        typeof(FloorCalculationService),
        typeof(RoomCalculationService),
        typeof(BuildingHeatingLoadService)
    ];

    [Fact]
    public void FirstPartyControllers_DoNotDirectlyDependOnLegacyCalculationServices()
    {
        var controllerTypes = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type is { IsAbstract: false, IsPublic: true } &&
                type.Namespace?.Contains(".Controllers.", StringComparison.Ordinal) == true)
            .ToArray();

        var violations = FindLegacyConstructorDependencies(controllerTypes);

        Assert.True(
            violations.Length == 0,
            "Controllers must not directly depend on legacy calculation services: " + string.Join("; ", violations));
    }

    [Fact]
    public void FirstPartyFacades_DoNotDirectlyDependOnLegacyCalculationServices()
    {
        var assemblies = typeof(Program).Assembly
            .GetReferencedAssemblies()
            .Where(assemblyName => assemblyName.Name?.StartsWith("AssistantEngineer.", StringComparison.Ordinal) == true)
            .Select(AppDomain.CurrentDomain.Load)
            .Append(typeof(Program).Assembly)
            .Distinct()
            .ToArray();

        var facadeTypes = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type =>
                type is { IsAbstract: false, IsPublic: true } &&
                type.Namespace?.Contains(".Application.Facades", StringComparison.Ordinal) == true)
            .ToArray();

        var violations = FindLegacyConstructorDependencies(facadeTypes);

        Assert.True(
            violations.Length == 0,
            "Facades must not directly depend on legacy calculation services: " + string.Join("; ", violations));
    }

    [Fact]
    public void LoadCalculationsFacade_UsesPipelineAsActiveProductionPath()
    {
        var constructor = Assert.Single(typeof(LoadCalculationsFacade).GetConstructors());
        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal(typeof(EnergyCalculationPipelineService), parameter.ParameterType);
    }

    [Fact]
    public void RetiredLegacyCalculationServices_DoNotReappearInBackendSource()
    {
        var retiredServiceTypeNames = new[]
        {
            "BuildingEnergyBalanceService"
        };

        var sourceFiles = Directory.GetFiles(
                Path.Combine(TestPaths.RepoRoot, "src", "Backend"),
                "*.cs",
                SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var violations = new List<string>();

        foreach (var sourceFile in sourceFiles)
        {
            var normalizedPath = Path.GetFullPath(sourceFile);
            var content = File.ReadAllText(sourceFile);

            foreach (var retiredServiceTypeName in retiredServiceTypeNames)
            {
                if (content.Contains(retiredServiceTypeName, StringComparison.Ordinal))
                {
                    violations.Add($"{normalizedPath} references retired service {retiredServiceTypeName}.");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Retired legacy services must not be reintroduced in backend source: " + string.Join("; ", violations));
    }

    [Fact]
    public void LegacyCalculationServices_AreReferencedOnlyInCompatibilityDefinitionsAndCompositionRoots()
    {
        var allowedPathsByTypeName = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            ["BuildingCoolingLoadService"] =
            [
                NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Buildings/BuildingCoolingLoadService.cs"),
                NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Composition/LoadCalculationRegistration.cs")
            ],
            ["FloorCalculationService"] =
            [
                NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Floors/FloorCalculationService.cs"),
                NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Composition/LoadCalculationRegistration.cs")
            ],
            ["RoomCalculationService"] =
            [
                NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Rooms/RoomCalculationService.cs"),
                NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Composition/LoadCalculationRegistration.cs")
            ],
            ["BuildingHeatingLoadService"] =
            [
                NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Application/Services/Buildings/BuildingHeatingLoadService.cs"),
                NormalizePath("src/Backend/AssistantEngineer.Modules.Calculations/Composition/LoadCalculationRegistration.cs")
            ]
        };

        var sourceFiles = Directory.GetFiles(
                Path.Combine(TestPaths.RepoRoot, "src", "Backend"),
                "*.cs",
                SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var violations = new List<string>();

        foreach (var sourceFile in sourceFiles)
        {
            var normalizedPath = Path.GetFullPath(sourceFile);
            var content = File.ReadAllText(sourceFile);

            foreach (var serviceTypeName in allowedPathsByTypeName.Keys)
            {
                if (!content.Contains(serviceTypeName, StringComparison.Ordinal))
                    continue;

                if (!allowedPathsByTypeName[serviceTypeName].Contains(normalizedPath))
                {
                    violations.Add($"{normalizedPath} references {serviceTypeName} outside compatibility allowlist.");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Legacy calculation service references must stay fenced to compatibility definitions/DI roots: " + string.Join("; ", violations));
    }

    private static string[] FindLegacyConstructorDependencies(IEnumerable<Type> ownerTypes) =>
        ownerTypes
            .SelectMany(ownerType => ownerType
                .GetConstructors()
                .SelectMany(constructor => constructor
                    .GetParameters()
                    .Where(parameter => LegacyCalculationServiceTypes.Contains(parameter.ParameterType))
                    .Select(parameter => $"{ownerType.FullName} -> {parameter.ParameterType.FullName}")))
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static string NormalizePath(string relativePath) =>
        Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
}
