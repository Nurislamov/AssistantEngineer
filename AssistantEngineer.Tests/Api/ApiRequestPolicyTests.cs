using System.Reflection;
using AssistantEngineer.Api;
using AssistantEngineer.Api.Controllers;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Configuration;

namespace AssistantEngineer.Tests;

public class ApiRequestPolicyTests
{
    [Theory]
    [MemberData(nameof(LongRunningActions))]
    public void LongRunningEndpointsUseLongRunningTimeoutPolicy(Type controllerType, string actionName)
    {
        var method = controllerType.GetMethod(actionName);

        Assert.NotNull(method);
        var attribute = Assert.Single(method.GetCustomAttributes<RequestTimeoutAttribute>());
        Assert.Equal(RequestPolicies.LongRunning, attribute.PolicyName);
    }

    [Fact]
    public void AppSettingsDefinesRequestLimits()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false)
            .Build();

        Assert.True(configuration.GetValue<long>("RequestLimits:MaxRequestBodyBytes") > 0);
        Assert.True(configuration.GetValue<int>("RequestLimits:DefaultTimeoutSeconds") > 0);
        Assert.True(configuration.GetValue<int>("RequestLimits:LongRunningTimeoutSeconds") >
            configuration.GetValue<int>("RequestLimits:DefaultTimeoutSeconds"));
    }

    public static TheoryData<Type, string> LongRunningActions() =>
        new()
        {
            { typeof(BuildingsController), nameof(BuildingsController.Calculate) },
            { typeof(BuildingsController), nameof(BuildingsController.CalculateHeatingLoad) },
            { typeof(BuildingsController), nameof(BuildingsController.CalculateEnergyBalance) },
            { typeof(FloorsController), nameof(FloorsController.Calculate) },
            { typeof(RoomsController), nameof(RoomsController.Calculate) },
            { typeof(RoomsController), nameof(RoomsController.CalculateHeatingLoad) },
            { typeof(RoomsController), nameof(RoomsController.SelectEquipment) },
            { typeof(ReportsController), nameof(ReportsController.GetBuildingReport) },
            { typeof(ReportsController), nameof(ReportsController.DownloadBuildingReportExcel) },
            { typeof(ReportsController), nameof(ReportsController.GetHeatingReport) },
            { typeof(ReportsController), nameof(ReportsController.DownloadEnergyBalanceReportExcel) },
            { typeof(BenchmarksController), nameof(BenchmarksController.RunEnergyPlus) },
            { typeof(BenchmarksController), nameof(BenchmarksController.ExportEnergyPlusModel) },
            { typeof(BenchmarksController), nameof(BenchmarksController.VerifyCalculation) },
            { typeof(BenchmarksController), nameof(BenchmarksController.RunIso52016ReferenceCases) },
            { typeof(ClimateDataController), nameof(ClimateDataController.ImportEpwWeather) },
            { typeof(BuildingPerformanceController), nameof(BuildingPerformanceController.GetIso52016Breakdown) },
            { typeof(BuildingPerformanceController), nameof(BuildingPerformanceController.GetEnergySignature) },
            { typeof(BuildingPerformanceController), nameof(BuildingPerformanceController.CalculateHeatingSystemEnergy) },
            { typeof(BuildingPerformanceController), nameof(BuildingPerformanceController.CalculateCoolingSystemEnergy) },
            { typeof(BuildingPerformanceController), nameof(BuildingPerformanceController.CalculateSummary) }
        };
}
