using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ground;

public sealed class GroundTemperatureProfileProvider : IGroundTemperatureProfileProvider
{
    private readonly IAnnualProfileShapeValidator _profileShapeValidator;
    private readonly IGroundTemperatureProfileCalculator _groundTemperatureProfileCalculator;

    public GroundTemperatureProfileProvider(
        IAnnualProfileShapeValidator profileShapeValidator,
        IGroundTemperatureProfileCalculator groundTemperatureProfileCalculator)
    {
        _profileShapeValidator = profileShapeValidator ?? throw new ArgumentNullException(nameof(profileShapeValidator));
        _groundTemperatureProfileCalculator = groundTemperatureProfileCalculator ?? throw new ArgumentNullException(nameof(groundTemperatureProfileCalculator));
    }

    public GroundTemperatureProfileResult BuildProfile(GroundClimateInput climate)
    {
        ArgumentNullException.ThrowIfNull(climate);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.AddRange(climate.Diagnostics);

        IReadOnlyList<double>? outdoorProfile = null;
        if (climate.HourlyOutdoorTemperaturesCelsius is { Count: > 0 } hourlyOutdoor)
        {
            var validation = _profileShapeValidator.ValidateHourlyNonLeapProfile(
                hourlyOutdoor,
                source: "GroundTemperatureProfileProvider.HourlyOutdoorTemperatures");
            diagnostics.AddRange(validation.Diagnostics);
            if (validation.IsValid)
            {
                outdoorProfile = hourlyOutdoor;
                diagnostics.Add(CreateInfo(
                    "AE-GROUND-CLIMATE-SOURCE-HOURLY",
                    "Ground profile input used hourly outdoor temperature source."));
            }
        }

        if (outdoorProfile is null &&
            climate.MonthlyOutdoorTemperaturesCelsius is { Count: > 0 } monthlyOutdoor)
        {
            var validation = _profileShapeValidator.ValidateMonthlyProfile(
                monthlyOutdoor,
                source: "GroundTemperatureProfileProvider.MonthlyOutdoorTemperatures");
            diagnostics.AddRange(validation.Diagnostics);
            if (validation.IsValid)
            {
                outdoorProfile = monthlyOutdoor;
                diagnostics.Add(CreateInfo(
                    "AE-GROUND-CLIMATE-SOURCE-MONTHLY",
                    "Ground profile input used monthly outdoor temperature source."));
            }
        }

        if (outdoorProfile is null)
        {
            diagnostics.Add(CreateInfo(
                "AE-GROUND-CLIMATE-SOURCE-ANNUAL-MEAN",
                "Ground profile input used annual mean outdoor fallback source."));
        }

        var mode = climate.TemperatureProfileMode;
        var groundAnnualMean = climate.AnnualMeanGroundTemperatureCelsius
                               ?? climate.AnnualMeanOutdoorTemperatureCelsius
                               ?? double.NaN;
        var request = new GroundTemperatureProfileRequest(
            TimeResolution: GroundProfileTimeResolution.Hourly,
            Mode: mode,
            OutdoorTemperatureProfileCelsius: outdoorProfile,
            AnnualMeanOutdoorTemperatureCelsius: climate.AnnualMeanOutdoorTemperatureCelsius,
            GroundAnnualMeanTemperatureCelsius: groundAnnualMean,
            GroundTemperatureAmplitudeCelsius: climate.GroundTemperatureAmplitudeCelsius ?? double.NaN,
            GroundTemperaturePhaseShiftDays: climate.GroundTemperaturePhaseShiftDays,
            NumberOfSteps: 8760,
            TimeStepHours: 1.0,
            ColdestMonthIndex: climate.ColdestMonthIndex);

        var calculated = _groundTemperatureProfileCalculator.Calculate(request);
        var mergedDiagnostics = diagnostics
            .Concat(calculated.Diagnostics)
            .OrderByDescending(item => item.Severity)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.Message, StringComparer.Ordinal)
            .ToArray();

        return calculated with
        {
            Diagnostics = mergedDiagnostics
        };
    }

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        GroundCalculationDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.BoundaryCondition,
            "GroundTemperatureProfileProvider");
}
