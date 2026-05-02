using AssistantEngineer.Api;

namespace AssistantEngineer.Tests.Architecture;

public class FacadeBoundaryTests
{
    private static readonly string[] AllowedFacadeTypeNames =
    [
        "AssistantEngineer.Modules.Benchmarks.Application.Facades.IBenchmarksFacade",
        "AssistantEngineer.Modules.Benchmarks.Application.Facades.BenchmarksFacade",

        "AssistantEngineer.Modules.Buildings.Application.Facades.IBuildingsFacade",
        "AssistantEngineer.Modules.Buildings.Application.Facades.BuildingsFacade",

        "AssistantEngineer.Modules.Calculations.Application.Facades.ILoadCalculationsFacade",
        "AssistantEngineer.Modules.Calculations.Application.Facades.LoadCalculationsFacade",

        "AssistantEngineer.Modules.Calculations.Application.Facades.IVentilationAnalysisFacade",
        "AssistantEngineer.Modules.Calculations.Application.Facades.VentilationAnalysisFacade",

        "AssistantEngineer.Modules.Calculations.Application.Facades.IDomesticHotWaterFacade",
        "AssistantEngineer.Modules.Calculations.Application.Facades.DomesticHotWaterFacade",

        "AssistantEngineer.Modules.Calculations.Application.Facades.IProfilesFacade",
        "AssistantEngineer.Modules.Calculations.Application.Facades.ProfilesFacade",

        "AssistantEngineer.Modules.Calculations.Application.Facades.IStandardReferenceDataFacade",
        "AssistantEngineer.Modules.Calculations.Application.Facades.StandardReferenceDataFacade",

        "AssistantEngineer.Modules.Calculations.Application.Facades.IBuildingEnergyAnalysisFacade",
        "AssistantEngineer.Modules.Calculations.Application.Facades.BuildingEnergyAnalysisFacade",

        "AssistantEngineer.Modules.Calculations.Application.Facades.IBuildingComfortAnalysisFacade",
        "AssistantEngineer.Modules.Calculations.Application.Facades.BuildingComfortAnalysisFacade",

        "AssistantEngineer.Modules.Calculations.Application.Facades.IBuildingSizingAnalysisFacade",
        "AssistantEngineer.Modules.Calculations.Application.Facades.BuildingSizingAnalysisFacade",

        "AssistantEngineer.Modules.Calculations.Application.Facades.IEngineeringCoreStatusFacade",
        "AssistantEngineer.Modules.Calculations.Application.Facades.EngineeringCoreStatusFacade",

        "AssistantEngineer.Modules.Equipment.Application.Facades.IEquipmentFacade",
        "AssistantEngineer.Modules.Equipment.Application.Facades.EquipmentFacade",

        "AssistantEngineer.Modules.Reporting.Application.Facades.IBuildingCoolingReportsFacade",
        "AssistantEngineer.Modules.Reporting.Application.Facades.BuildingCoolingReportsFacade",

        "AssistantEngineer.Modules.Reporting.Application.Facades.IBuildingHeatingReportsFacade",
        "AssistantEngineer.Modules.Reporting.Application.Facades.BuildingHeatingReportsFacade",

        "AssistantEngineer.Modules.Reporting.Application.Facades.IBuildingEnergyBalanceReportsFacade",
        "AssistantEngineer.Modules.Reporting.Application.Facades.BuildingEnergyBalanceReportsFacade"
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

        Assert.Equal(
            AllowedFacadeTypeNames.Order(StringComparer.Ordinal),
            facadeTypeNames);
    }
}