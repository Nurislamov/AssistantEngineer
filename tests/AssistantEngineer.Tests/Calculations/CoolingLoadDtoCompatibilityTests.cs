using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Cooling;

namespace AssistantEngineer.Tests;

public sealed class CoolingLoadDtoCompatibilityTests
{
    [Fact]
    public void CoolingLoadAliasesMapToCanonicalCalculationDtoFields()
    {
        var result = new RoomCalculationResult
        {
            CoolingLoadW = 1200,
            CoolingLoadKw = 1.2,
            PeakHourOfYear = 200
        };

#pragma warning disable CS0618
        Assert.Equal(result.CoolingLoadW, result.TotalHeatLoadW);
        Assert.Equal(result.CoolingLoadKw, result.TotalHeatLoadKw);
        Assert.Equal(result.PeakHourOfYear, result.PeakHour);
#pragma warning restore CS0618
    }

    [Fact]
    public void CoolingLoadAliasesMapToCanonicalReportDtoFields()
    {
        var report = new BuildingCoolingReport
        {
            CoolingLoadW = 2400,
            CoolingLoadKw = 2.4,
            PeakHourOfYear = 400
        };

#pragma warning disable CS0618
        Assert.Equal(report.CoolingLoadW, report.TotalHeatLoadW);
        Assert.Equal(report.CoolingLoadKw, report.TotalHeatLoadKw);
        Assert.Equal(report.PeakHourOfYear, report.PeakHour);
#pragma warning restore CS0618
    }

    [Fact]
    public void EquipmentSelectionLoadAliasMapsToCanonicalCoolingLoad()
    {
        var result = new EquipmentSelectionResult
        {
            CoolingLoadKw = 3.5
        };

#pragma warning disable CS0618
        Assert.Equal(result.CoolingLoadKw, result.TotalHeatLoadKw);
#pragma warning restore CS0618
    }
}
