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
            { typeof(BuildingsController), nameof(BuildingsController.CalculateCoolingLoad) },
            { typeof(BuildingsController), nameof(BuildingsController.CalculateHeatingLoad) },
            { typeof(BuildingsController), nameof(BuildingsController.CalculateEnergyBalance) },
            { typeof(FloorsController), nameof(FloorsController.CalculateCoolingLoad) },
            { typeof(RoomsController), nameof(RoomsController.CalculateCoolingLoad) },
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
            { typeof(AnnualClimateDataController), nameof(AnnualClimateDataController.ImportFromEpw) },
            { typeof(BuildingEnergyAnalysisController), nameof(BuildingEnergyAnalysisController.GetIso52016Breakdown) },
            { typeof(BuildingEnergyAnalysisController), nameof(BuildingEnergyAnalysisController.GetEnergySignature) },
            { typeof(BuildingEnergyAnalysisController), nameof(BuildingEnergyAnalysisController.CalculateHeatingSystemEnergy) },
            { typeof(BuildingEnergyAnalysisController), nameof(BuildingEnergyAnalysisController.CalculateCoolingSystemEnergy) },
            { typeof(BuildingEnergyAnalysisController), nameof(BuildingEnergyAnalysisController.CalculateSummary) }
        };
}
