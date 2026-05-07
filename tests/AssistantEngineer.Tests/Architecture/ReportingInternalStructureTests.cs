using AssistantEngineer.Modules.Reporting;

namespace AssistantEngineer.Tests.Architecture;

public class ReportingInternalStructureTests
{
    [Fact]
    public void GenericBuildingReportGeneratorDoesNotExist()
    {
        AssertReportingTypeDoesNotExist(
            "AssistantEngineer.Modules.Reporting.Application.Services.BuildingReportGenerator");
    }

    [Fact]
    public void GenericBuildingReportDataServiceDoesNotExist()
    {
        AssertReportingTypeDoesNotExist(
            "AssistantEngineer.Modules.Reporting.Application.Services.BuildingReportDataService");
    }

    [Fact]
    public void GenericBuildingReportCalculationServiceDoesNotExist()
    {
        AssertReportingTypeDoesNotExist(
            "AssistantEngineer.Modules.Reporting.Application.Services.BuildingReportCalculationService");
    }

    [Fact]
    public void GenericBuildingReportDataModelDoesNotExist()
    {
        AssertReportingTypeDoesNotExist(
            "AssistantEngineer.Modules.Reporting.Application.Models.BuildingReportData");
    }

    [Fact]
    public void ReportingUsesSpecializedReportGenerators()
    {
        var typeNames = GetReportingServiceTypeNames();

        Assert.Contains("BuildingCoolingReportGenerator", typeNames);
        Assert.Contains("BuildingHeatingReportGenerator", typeNames);
    }

    [Fact]
    public void ReportingUsesSpecializedReportDataServices()
    {
        var typeNames = GetReportingServiceTypeNames();

        Assert.Contains("BuildingCoolingReportDataService", typeNames);
        Assert.Contains("BuildingHeatingReportDataService", typeNames);

        Assert.DoesNotContain("BuildingReportDataService", typeNames);
    }

    [Fact]
    public void ReportingUsesSpecializedReportCalculationServices()
    {
        var typeNames = GetReportingServiceTypeNames();

        Assert.Contains("BuildingCoolingReportCalculationService", typeNames);
        Assert.Contains("BuildingHeatingReportCalculationService", typeNames);

        Assert.DoesNotContain("BuildingReportCalculationService", typeNames);
    }

    [Fact]
    public void ReportingUsesSpecializedInternalModels()
    {
        var typeNames = typeof(DependencyInjection).Assembly
            .GetTypes()
            .Where(type =>
                type.Namespace == "AssistantEngineer.Modules.Reporting.Application.Models")
            .Select(type => type.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("BuildingCoolingReportData", typeNames);
        Assert.Contains("RoomCoolingReportCalculation", typeNames);

        Assert.DoesNotContain("BuildingReportData", typeNames);
    }

    private static HashSet<string> GetReportingServiceTypeNames() =>
        typeof(DependencyInjection).Assembly
            .GetTypes()
            .Where(type =>
                type.Namespace == "AssistantEngineer.Modules.Reporting.Application.Services")
            .Select(type => type.Name)
            .ToHashSet(StringComparer.Ordinal);

    private static void AssertReportingTypeDoesNotExist(
        string fullName)
    {
        var type = typeof(DependencyInjection).Assembly
            .GetTypes()
            .FirstOrDefault(type =>
                string.Equals(
                    type.FullName,
                    fullName,
                    StringComparison.Ordinal));

        Assert.Null(type);
    }

    [Fact]
    public void ReportingContractsAreSplitByReportType()
    {
        var contractNamespaces = typeof(AssistantEngineer.Modules.Reporting.DependencyInjection).Assembly
            .GetTypes()
            .Where(type =>
                type.Namespace?.StartsWith(
                    "AssistantEngineer.Modules.Reporting.Application.Contracts.Reports",
                    StringComparison.Ordinal) == true)
            .Select(type => type.Namespace)
            .Where(namespaceName => namespaceName is not null)
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(
            "AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Cooling",
            contractNamespaces);

        Assert.Contains(
            "AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Heating",
            contractNamespaces);

        Assert.DoesNotContain(
            "AssistantEngineer.Modules.Reporting.Application.Contracts.Reports",
            contractNamespaces);
    }
}