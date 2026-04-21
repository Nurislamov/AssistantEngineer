using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Benchmarks.Application.Services;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests;

public class VerificationComparatorTests
{
    [Fact]
    public void CompareUsesInjectedTimeProvider()
    {
        var fixedTime = new DateTimeOffset(2026, 4, 19, 9, 15, 0, TimeSpan.Zero);
        var comparator = new VerificationComparator(
            Options.Create(new VerificationTolerance()),
            new FixedTimeProvider(fixedTime));

        var report = comparator.Compare(
            new BuildingCalculationResult
            {
                BuildingId = 7,
                BuildingName = "Main",
                CalculationMethod = "Iso52016",
                HourlyHeatLoadW = [1000, 1200, 900]
            },
            new EnergyPlusCalculationSummary
            {
                HourlyCoolingLoadW = [1000, 1200, 900]
            });

        Assert.Equal(fixedTime.UtcDateTime, report.ExecutedAtUtc);
    }
}
