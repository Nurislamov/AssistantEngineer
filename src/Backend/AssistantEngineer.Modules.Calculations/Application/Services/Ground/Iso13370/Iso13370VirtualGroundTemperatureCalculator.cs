using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground.Iso13370;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ground.Iso13370;

public sealed class Iso13370VirtualGroundTemperatureCalculator
{
    private static readonly int[] MonthDays =
    [
        31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
    ];

    public Iso13370VirtualGroundResult Calculate(Iso13370VirtualGroundInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Geometry);
        ArgumentNullException.ThrowIfNull(input.GroundThermalProperties);

        ValidateInput(input);

        var options = input.Options ?? new Iso13370GroundCalculationOptions();
        var diagnostics = new List<Iso13370GroundBoundaryDiagnostics>();

        var area = input.Geometry.FloorAreaM2;
        var perimeter = input.Geometry.ExposedPerimeterM;
        var characteristicDimension = area / (0.5 * perimeter);

        var equivalentThickness = ResolveEquivalentGroundThickness(
            input.GroundThermalProperties.EquivalentGroundThicknessM,
            characteristicDimension,
            options);

        var floorResistance = Math.Max(0.0, input.Geometry.SlabThermalResistanceM2KPerW);
        var conductivity = input.GroundThermalProperties.GroundConductivityWPerMK;
        var groundResistance = equivalentThickness / conductivity;

        var uValue = 1.0 / Math.Max(1e-9, floorResistance + groundResistance);
        var baseHeatTransferCoefficient = uValue * area;
        var bridgeHeatTransfer = ResolveThermalBridgeHeatTransfer(input, options, perimeter);
        var annualHeatTransfer = Math.Max(0.01, baseHeatTransferCoefficient + bridgeHeatTransfer);

        var monthlyOutdoor = ResolveMonthlyOutdoorProfile(input, options);
        var monthlyVirtualGround = BuildMonthlyVirtualGroundProfile(
            input,
            options,
            characteristicDimension,
            equivalentThickness,
            monthlyOutdoor);

        var monthlyHeatTransfer = BuildMonthlyHeatTransferProfile(
            annualHeatTransfer,
            monthlyOutdoor,
            input.AnnualAverageOutdoorTemperatureC,
            options);

        var monthlyConditions = BuildMonthlyBoundaryConditions(monthlyOutdoor, monthlyVirtualGround, monthlyHeatTransfer);
        var hourlyVirtualGround = ExpandMonthlyToHourly(monthlyVirtualGround);
        var annualMeanVirtualGround = monthlyVirtualGround.Average();
        var annualMeanHeatTransfer = monthlyHeatTransfer.Average();

        diagnostics.Add(new Iso13370GroundBoundaryDiagnostics(
            Code: "Iso13370VirtualGround.Summary",
            Message: $"ISO13370-style virtual ground calculation completed. CharacteristicDimension={Round6(characteristicDimension)} m, EquivalentGroundThickness={Round6(equivalentThickness)} m, AnnualH={Round6(annualMeanHeatTransfer)} W/K."));

        return new Iso13370VirtualGroundResult(
            CharacteristicFloorDimensionM: Round6(characteristicDimension),
            EquivalentGroundThicknessM: Round6(equivalentThickness),
            MonthlyVirtualGroundTemperatureC: monthlyVirtualGround.Select(Round6).ToArray(),
            MonthlyEquivalentGroundHeatTransferCoefficientWPerK: monthlyHeatTransfer.Select(Round6).ToArray(),
            MonthlyBoundaryConditions: monthlyConditions,
            HourlyVirtualGroundTemperatureC: hourlyVirtualGround.Select(Round6).ToArray(),
            AnnualMeanVirtualGroundTemperatureC: Round6(annualMeanVirtualGround),
            AnnualEquivalentGroundHeatTransferCoefficientWPerK: Round6(annualMeanHeatTransfer),
            Diagnostics: diagnostics);
    }

    private static void ValidateInput(Iso13370VirtualGroundInput input)
    {
        if (input.Geometry.FloorAreaM2 <= 0.0)
            throw new InvalidOperationException("Iso13370 virtual ground input requires FloorAreaM2 > 0.");

        if (input.Geometry.ExposedPerimeterM <= 0.0)
            throw new InvalidOperationException("Iso13370 virtual ground input requires ExposedPerimeterM > 0.");

        if (input.GroundThermalProperties.GroundConductivityWPerMK <= 0.0)
            throw new InvalidOperationException("Iso13370 virtual ground input requires GroundConductivityWPerMK > 0.");

        if (input.MonthlyOutdoorTemperatureProfileC is not null && input.MonthlyOutdoorTemperatureProfileC.Count != 12)
            throw new InvalidOperationException("Iso13370 virtual ground input requires 12 monthly outdoor temperatures when profile is provided.");
    }

    private static double ResolveEquivalentGroundThickness(
        double explicitEquivalentThickness,
        double characteristicDimension,
        Iso13370GroundCalculationOptions options)
    {
        var minimum = Math.Max(0.1, options.MinimumEquivalentGroundThicknessM);
        var maximum = Math.Max(minimum, options.MaximumEquivalentGroundThicknessM);
        var autoEquivalentThickness = 0.5 * characteristicDimension;
        var resolved = explicitEquivalentThickness > 0.0 ? explicitEquivalentThickness : autoEquivalentThickness;
        return Math.Clamp(resolved, minimum, maximum);
    }

    private static IReadOnlyList<double> ResolveMonthlyOutdoorProfile(
        Iso13370VirtualGroundInput input,
        Iso13370GroundCalculationOptions options)
    {
        var profile = new double[12];
        for (var month = 1; month <= 12; month++)
        {
            var seasonal = options.EnableSeasonalComponent
                ? input.SeasonalAmplitudeC * Math.Cos(2.0 * Math.PI * (month - input.SeasonalPhaseShiftMonths) / 12.0)
                : 0.0;

            var seasonalOutdoor = input.AnnualAverageOutdoorTemperatureC + seasonal;
            if (input.MonthlyOutdoorTemperatureProfileC is not null)
            {
                profile[month - 1] = 0.5 * (input.MonthlyOutdoorTemperatureProfileC[month - 1] + seasonalOutdoor);
            }
            else
            {
                profile[month - 1] = seasonalOutdoor;
            }
        }

        return profile;
    }

    private static IReadOnlyList<double> BuildMonthlyVirtualGroundProfile(
        Iso13370VirtualGroundInput input,
        Iso13370GroundCalculationOptions options,
        double characteristicDimension,
        double equivalentThickness,
        IReadOnlyList<double> monthlyOutdoor)
    {
        var virtualGround = new double[12];
        var lagMonths = Math.Clamp(equivalentThickness / Math.Max(characteristicDimension, 0.1), 0.0, 3.0);

        var floorResistance = Math.Max(0.0, input.Geometry.SlabThermalResistanceM2KPerW);
        var conductivity = input.GroundThermalProperties.GroundConductivityWPerMK;
        var groundResistance = equivalentThickness / conductivity;
        var totalResistance = Math.Max(1e-9, floorResistance + groundResistance);

        var outdoorInfluence = options.SeasonalAttenuationFactor * (groundResistance / totalResistance);
        outdoorInfluence = Math.Clamp(outdoorInfluence, 0.05, 0.95);

        for (var month = 1; month <= 12; month++)
        {
            var seasonal = options.EnableSeasonalComponent
                ? input.SeasonalAmplitudeC * Math.Cos(2.0 * Math.PI * (month - (input.SeasonalPhaseShiftMonths + lagMonths)) / 12.0)
                : 0.0;

            var outdoorDeparture = monthlyOutdoor[month - 1] - input.AnnualAverageOutdoorTemperatureC;
            var smoothedDeparture = outdoorInfluence * outdoorDeparture + (1.0 - outdoorInfluence) * 0.25 * seasonal;
            virtualGround[month - 1] = input.AnnualAverageOutdoorTemperatureC + smoothedDeparture;
        }

        return virtualGround;
    }

    private static IReadOnlyList<double> BuildMonthlyHeatTransferProfile(
        double annualHeatTransfer,
        IReadOnlyList<double> monthlyOutdoor,
        double annualAverageOutdoorTemperatureC,
        Iso13370GroundCalculationOptions options)
    {
        var result = new double[12];
        var seasonalDenominator = Math.Max(1.0, monthlyOutdoor.Max() - monthlyOutdoor.Min());

        for (var month = 0; month < 12; month++)
        {
            var normalizedDeparture = (monthlyOutdoor[month] - annualAverageOutdoorTemperatureC) / seasonalDenominator;
            var variationFactor = 1.0 + options.MonthlyHeatTransferVariationFactor * normalizedDeparture;
            result[month] = Math.Max(0.01, annualHeatTransfer * variationFactor);
        }

        return result;
    }

    private static IReadOnlyList<MonthlyGroundBoundaryCondition> BuildMonthlyBoundaryConditions(
        IReadOnlyList<double> monthlyOutdoor,
        IReadOnlyList<double> monthlyVirtualGround,
        IReadOnlyList<double> monthlyHeatTransfer)
    {
        var result = new MonthlyGroundBoundaryCondition[12];
        for (var month = 1; month <= 12; month++)
        {
            result[month - 1] = new MonthlyGroundBoundaryCondition(
                Month: month,
                OutdoorTemperatureC: Round6(monthlyOutdoor[month - 1]),
                VirtualGroundTemperatureC: Round6(monthlyVirtualGround[month - 1]),
                EquivalentGroundHeatTransferCoefficientWPerK: Round6(monthlyHeatTransfer[month - 1]));
        }

        return result;
    }

    private static IReadOnlyList<double> ExpandMonthlyToHourly(IReadOnlyList<double> monthlyVirtualGround)
    {
        var hourly = new List<double>(8760);
        for (var month = 0; month < 12; month++)
        {
            var hours = MonthDays[month] * 24;
            for (var hour = 0; hour < hours; hour++)
            {
                hourly.Add(monthlyVirtualGround[month]);
            }
        }

        return hourly;
    }

    private static double ResolveThermalBridgeHeatTransfer(
        Iso13370VirtualGroundInput input,
        Iso13370GroundCalculationOptions options,
        double defaultPerimeterLength)
    {
        if (input.ThermalBridge is null || !input.ThermalBridge.Enabled || !options.EnablePerimeterThermalBridge)
            return 0.0;

        var psi = Math.Max(0.0, input.ThermalBridge.LinearThermalTransmittanceWPerMK);
        var length = input.ThermalBridge.BridgeLengthM > 0.0
            ? input.ThermalBridge.BridgeLengthM
            : defaultPerimeterLength;

        return psi * Math.Max(0.0, length);
    }

    private static double Round6(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);
}

