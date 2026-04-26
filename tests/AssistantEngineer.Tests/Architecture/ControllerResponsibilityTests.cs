using AssistantEngineer.Api;
using AssistantEngineer.Api.Controllers.Buildings;
using AssistantEngineer.Api.Controllers.Calculations;
using AssistantEngineer.Api.Controllers.Equipment;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Modules.Equipment.Application.Facades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AssistantEngineer.Tests.Architecture;

public class ControllerResponsibilityTests
{
    [Fact]
    public void CrudControllersDoNotDependOnCalculationOrEquipmentFacades()
    {
        var crudControllerTypes = new[]
        {
            typeof(BuildingsController),
            typeof(FloorsController),
            typeof(RoomsController)
        };

        var forbiddenDependencyTypes = new HashSet<Type>
        {
            typeof(ILoadCalculationsFacade),
            typeof(IBuildingEnergyAnalysisFacade),
            typeof(IBuildingComfortAnalysisFacade),
            typeof(IBuildingSizingAnalysisFacade),
            typeof(IVentilationAnalysisFacade),
            typeof(IDomesticHotWaterFacade),
            typeof(IEquipmentFacade)
        };

        var violations = crudControllerTypes
            .SelectMany(controllerType => controllerType
                .GetConstructors()
                .SelectMany(constructor => constructor
                    .GetParameters()
                    .Where(parameter => forbiddenDependencyTypes.Contains(parameter.ParameterType))
                    .Select(parameter =>
                        $"{controllerType.Name} depends on {parameter.ParameterType.Name}")))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"CRUD controllers must not depend on calculation/equipment facades: {string.Join("; ", violations)}.");
    }

    [Fact]
    public void CrudControllersDependOnlyOnBuildingsFacade()
    {
        var crudControllerTypes = new[]
        {
            typeof(BuildingsController),
            typeof(FloorsController),
            typeof(RoomsController)
        };

        foreach (var controllerType in crudControllerTypes)
        {
            var constructor = Assert.Single(controllerType.GetConstructors());
            var parameter = Assert.Single(constructor.GetParameters());

            Assert.Equal(typeof(IBuildingsFacade), parameter.ParameterType);
        }
    }

    [Fact]
    public void LoadCalculationControllersDependOnlyOnLoadCalculationsFacade()
    {
        var loadControllerTypes = new[]
        {
            typeof(BuildingLoadCalculationsController),
            typeof(FloorLoadCalculationsController),
            typeof(RoomLoadCalculationsController)
        };

        foreach (var controllerType in loadControllerTypes)
        {
            var constructor = Assert.Single(controllerType.GetConstructors());
            var parameter = Assert.Single(constructor.GetParameters());

            Assert.Equal(typeof(ILoadCalculationsFacade), parameter.ParameterType);
        }
    }

    [Fact]
    public void RoomEquipmentSelectionControllerDependsOnlyOnRequiredOrchestrationFacades()
    {
        var constructor = Assert.Single(typeof(RoomEquipmentSelectionController).GetConstructors());

        var parameters = constructor
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                typeof(IEquipmentFacade),
                typeof(ILoadCalculationsFacade)
            }.OrderBy(type => type.FullName, StringComparer.Ordinal),
            parameters);
    }

    [Fact]
    public void ControllersDoNotExposeOldMixedResponsibilityRoutes()
    {
        var dedicatedLoadControllers = new HashSet<Type>
        {
            typeof(BuildingLoadCalculationsController),
            typeof(FloorLoadCalculationsController),
            typeof(RoomLoadCalculationsController)
        };

        var controllerTypes = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type is { IsAbstract: false, IsPublic: true } &&
                typeof(ControllerBase).IsAssignableFrom(type))
            .ToArray();

        var forbiddenRouteFragments = new[]
        {
            "/cooling-load",
            "/heating-load",
            "/energy-balance",
            "/select-equipment"
        };

        var violations = controllerTypes
            .SelectMany(controllerType => controllerType
                .GetMethods()
                .SelectMany(method => method
                    .GetCustomAttributes(inherit: true)
                    .OfType<HttpMethodAttribute>()
                    .SelectMany(attribute => attribute.Template is null
                        ? []
                        : forbiddenRouteFragments
                            .Where(fragment =>
                                !IsAllowedDedicatedRoute(controllerType, fragment, dedicatedLoadControllers) &&
                                (string.Equals(attribute.Template, fragment.TrimStart('/'), StringComparison.OrdinalIgnoreCase) ||
                                attribute.Template.EndsWith(fragment, StringComparison.OrdinalIgnoreCase)))
                            .Select(fragment =>
                                $"{controllerType.Name}.{method.Name} exposes old route fragment '{fragment}'"))))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Old mixed-responsibility routes must not be exposed: {string.Join("; ", violations)}.");
    }

    private static bool IsAllowedDedicatedRoute(
        Type controllerType,
        string routeFragment,
        ISet<Type> dedicatedLoadControllers) =>
        dedicatedLoadControllers.Contains(controllerType) &&
        routeFragment is "/cooling-load" or "/heating-load" or "/energy-balance";
}
