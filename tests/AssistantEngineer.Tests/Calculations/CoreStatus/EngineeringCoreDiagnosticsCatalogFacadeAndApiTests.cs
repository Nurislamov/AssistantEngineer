using AssistantEngineer.Api.Controllers.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.CoreStatus;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Tests.Calculations.CoreStatus;

public class EngineeringCoreDiagnosticsCatalogFacadeAndApiTests
{
    [Fact]
    public void GetEngineeringCoreV1DiagnosticsCatalogReturnsClosedV1Catalog()
    {
        var facade = new EngineeringCoreStatusFacade();

        var result = facade.GetEngineeringCoreV1DiagnosticsCatalog();

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal("Engineering Core V1 Diagnostics Catalog", result.Value.CatalogName);
        Assert.Equal("v1", result.Value.Version);
        Assert.Equal("ClosedV1", result.Value.Status);
        Assert.NotEmpty(result.Value.Diagnostics);

        Assert.Contains(
            "CalculationDiagnosticSeverity.Error",
            result.Value.Rules.SuccessRule,
            StringComparison.Ordinal);
    }

    [Fact]
    public void DiagnosticsCatalogContainsUniqueCodesAndRequiredFields()
    {
        var facade = new EngineeringCoreStatusFacade();

        var result = facade.GetEngineeringCoreV1DiagnosticsCatalog();

        Assert.True(result.IsSuccess, result.Error);

        var duplicateCodes = result.Value.Diagnostics
            .GroupBy(item => item.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            duplicateCodes.Length == 0,
            $"Diagnostics catalog codes must be unique: {string.Join(", ", duplicateCodes)}.");

        Assert.All(result.Value.Diagnostics, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Code));
            Assert.False(string.IsNullOrWhiteSpace(item.Severity));
            Assert.False(string.IsNullOrWhiteSpace(item.Category));
            Assert.False(string.IsNullOrWhiteSpace(item.UserMessage));
            Assert.False(string.IsNullOrWhiteSpace(item.UserAction));
            Assert.False(string.IsNullOrWhiteSpace(item.ClosedV1Gate));
        });
    }

    [Fact]
    public void DiagnosticsCatalogContainsCoreAnnual8760SystemEquipmentAggregationAndAdjacentCodes()
    {
        var facade = new EngineeringCoreStatusFacade();

        var result = facade.GetEngineeringCoreV1DiagnosticsCatalog();

        Assert.True(result.IsSuccess, result.Error);

        var codes = result.Value.Diagnostics
            .Select(item => item.Code)
            .ToHashSet(StringComparer.Ordinal);

        var requiredCodes = new[]
        {
            "AnnualEnergy.Not8760",
            "AnnualEnergy.MonthlyBalanceAdapter",
            "AnnualEnergy.TrueHourlySimulationUsed",
            "SolarWeather.SyntheticWeatherUsed",
            "SystemEnergy.InvalidCoolingCop",
            "SystemEnergy.HeatingAssumptionMissing",
            "EquipmentSizing.InvalidSafetyFactor",
            "EquipmentSizing.NoRecommendedEquipment",
            "Aggregation.InvalidRoomArea",
            "Aggregation.HourlyUnavailable",
            "Transmission.MissingBoundaryTemperature"
        };

        foreach (var requiredCode in requiredCodes)
        {
            Assert.Contains(requiredCode, codes);
        }
    }

    [Fact]
    public void Annual8760WarningsTellUserNotToPresentAsTrue8760()
    {
        var facade = new EngineeringCoreStatusFacade();

        var result = facade.GetEngineeringCoreV1DiagnosticsCatalog();

        Assert.True(result.IsSuccess, result.Error);

        var annual8760Warnings = result.Value.Diagnostics
            .Where(item => item.Code is
                "AnnualEnergy.Not8760" or
                "AnnualEnergy.MonthlyBalanceAdapter")
            .ToArray();

        Assert.NotEmpty(annual8760Warnings);

        foreach (var warning in annual8760Warnings)
        {
            Assert.Equal("Warning", warning.Severity);

            Assert.Contains(
                "not",
                warning.UserAction,
                StringComparison.OrdinalIgnoreCase);

            Assert.Contains(
                "8760",
                warning.UserAction,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void EngineeringCoreStatusControllerExposesDiagnosticsCatalogRoute()
    {
        var method = typeof(EngineeringCoreStatusController)
            .GetMethod(nameof(EngineeringCoreStatusController.GetEngineeringCoreV1DiagnosticsCatalog));

        Assert.NotNull(method);

        var getAttribute = method
            .GetCustomAttributes(inherit: true)
            .OfType<HttpGetAttribute>()
            .Single();

        Assert.Equal("v1/diagnostics-catalog", getAttribute.Template);

        Assert.Equal(
            typeof(ActionResult<EngineeringCoreV1DiagnosticsCatalogResponse>),
            method.ReturnType);
    }
}
