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
        typeof(BuildingEnergyBalanceService),
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
}
