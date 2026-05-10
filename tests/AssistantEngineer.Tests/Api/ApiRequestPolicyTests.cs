using System.Reflection;
using AssistantEngineer.Api;
using AssistantEngineer.Api.Controllers.Analysis;
using AssistantEngineer.Api.Controllers.Benchmarks;
using AssistantEngineer.Api.Controllers.Calculations;
using AssistantEngineer.Api.Controllers.Equipment;
using AssistantEngineer.Api.Controllers.Reports;
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
            { typeof(BuildingLoadCalculationsController), nameof(BuildingLoadCalculationsController.CalculateCoolingLoad) },
            { typeof(BuildingLoadCalculationsController), nameof(BuildingLoadCalculationsController.CalculateHeatingLoad) },
            { typeof(BuildingLoadCalculationsController), nameof(BuildingLoadCalculationsController.CalculateEnergyBalance) },
            { typeof(FloorLoadCalculationsController), nameof(FloorLoadCalculationsController.CalculateCoolingLoad) },
            { typeof(FloorLoadCalculationsController), nameof(FloorLoadCalculationsController.CalculateHeatingLoad) },
            { typeof(RoomLoadCalculationsController), nameof(RoomLoadCalculationsController.CalculateCoolingLoad) },
            { typeof(RoomLoadCalculationsController), nameof(RoomLoadCalculationsController.CalculateHeatingLoad) },
            { typeof(RoomEquipmentSelectionController), nameof(RoomEquipmentSelectionController.SelectEquipment) },
            { typeof(BuildingCoolingReportsController), nameof(BuildingCoolingReportsController.GetCoolingReport) },
            { typeof(BuildingCoolingReportsController), nameof(BuildingCoolingReportsController.DownloadCoolingReportExcel) },
            { typeof(BuildingHeatingReportsController), nameof(BuildingHeatingReportsController.GetHeatingReport) },
            { typeof(BuildingEnergyBalanceReportsController), nameof(BuildingEnergyBalanceReportsController.DownloadEnergyBalanceReportExcel) },
            { typeof(BenchmarksController), nameof(BenchmarksController.RunEnergyPlus) },
            { typeof(BenchmarksController), nameof(BenchmarksController.ExportEnergyPlusModel) },
            { typeof(BenchmarksController), nameof(BenchmarksController.VerifyCalculation) },
            { typeof(BenchmarksController), nameof(BenchmarksController.RunIso52016ReferenceCases) },
            { typeof(AnnualClimateDataController), nameof(AnnualClimateDataController.ImportFromEpw) },
            { typeof(AnnualClimateDataController), nameof(AnnualClimateDataController.ImportFromPvgis) },
            { typeof(EngineeringWorkflowController), nameof(EngineeringWorkflowController.CreateOrRunJob) },
            { typeof(EngineeringWorkflowController), nameof(EngineeringWorkflowController.RunCalculation) },
            { typeof(BuildingEnergyAnalysisController), nameof(BuildingEnergyAnalysisController.GetIso52016Breakdown) },
            { typeof(BuildingEnergyAnalysisController), nameof(BuildingEnergyAnalysisController.SimulateIso52016) },
            { typeof(BuildingEnergyAnalysisController), nameof(BuildingEnergyAnalysisController.GetEnergySignature) },
            { typeof(BuildingEnergyAnalysisController), nameof(BuildingEnergyAnalysisController.CalculateHeatingSystemEnergy) },
            { typeof(BuildingEnergyAnalysisController), nameof(BuildingEnergyAnalysisController.CalculateCoolingSystemEnergy) },
            { typeof(BuildingEnergyAnalysisController), nameof(BuildingEnergyAnalysisController.CalculateSummary) }
        };
}
