using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.Iso52016;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Validation;

internal sealed class Iso52016ManualIndependentReferenceCalculator
{
    public Iso52016ExternalValidationExpectedResult Calculate(Iso52016ExternalValidationFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        return fixture.Id switch
        {
            "manual-independent-steady-heating-simple-room" => CalculateSteadyHeating(fixture.Input),
            "manual-independent-steady-cooling-simple-room" => CalculateSteadyCooling(fixture.Input),
            "manual-independent-annual-8760-seasonal-loads" => CalculateAnnualSeasonal(fixture.Input),
            _ => throw new InvalidOperationException($"Unknown manual independent fixture id: {fixture.Id}")
        };
    }

    private static Iso52016ExternalValidationExpectedResult CalculateSteadyHeating(JsonElement input)
    {
        var heatingSetpointC = GetRequiredDouble(input, "heatingSetpointC");
        var outdoorTemperatureC = GetRequiredDouble(input, "outdoorTemperatureC");
        var internalGainW = GetRequiredDouble(input, "internalGainW");
        var solarGainW = GetRequiredDouble(input, "solarGainW");
        var uaWPerK = GetRequiredDouble(input, "uaWPerK");
        var heatingHoursPerMonth = GetRequiredDouble(input, "heatingHoursPerMonth");

        var peakHeatingW = Math.Max(0.0, uaWPerK * (heatingSetpointC - outdoorTemperatureC) - internalGainW - solarGainW);
        var monthlyHeatingKWh = peakHeatingW * heatingHoursPerMonth / 1000.0;
        var annualHeatingKWh = monthlyHeatingKWh * 12.0;

        var meanOffsetC = GetRequiredDouble(input, "meanOperativeOffsetC");
        var maxOffsetC = GetRequiredDouble(input, "maxOperativeOffsetC");
        var minOffsetC = GetRequiredDouble(input, "minOperativeOffsetC");

        return new Iso52016ExternalValidationExpectedResult(
            AnnualHeatingKWh: Round6(annualHeatingKWh),
            AnnualCoolingKWh: 0.0,
            PeakHeatingW: Round6(peakHeatingW),
            PeakCoolingW: 0.0,
            MeanOperativeTemperatureC: Round6(heatingSetpointC + meanOffsetC),
            MaxOperativeTemperatureC: Round6(heatingSetpointC + maxOffsetC),
            MinOperativeTemperatureC: Round6(heatingSetpointC + minOffsetC),
            HourlyResultCount: 24,
            MonthlyHeatingKWh: Enumerable.Repeat(Round6(monthlyHeatingKWh), 12).ToArray(),
            MonthlyCoolingKWh: Enumerable.Repeat(0.0, 12).ToArray());
    }

    private static Iso52016ExternalValidationExpectedResult CalculateSteadyCooling(JsonElement input)
    {
        var coolingSetpointC = GetRequiredDouble(input, "coolingSetpointC");
        var outdoorTemperatureC = GetRequiredDouble(input, "outdoorTemperatureC");
        var internalGainW = GetRequiredDouble(input, "internalGainW");
        var solarGainW = GetRequiredDouble(input, "solarGainW");
        var uaWPerK = GetRequiredDouble(input, "uaWPerK");
        var coolingHoursPerMonth = GetRequiredDouble(input, "coolingHoursPerMonth");

        var peakCoolingW = Math.Max(0.0, uaWPerK * (outdoorTemperatureC - coolingSetpointC) + internalGainW + solarGainW);
        var monthlyCoolingKWh = peakCoolingW * coolingHoursPerMonth / 1000.0;
        var annualCoolingKWh = monthlyCoolingKWh * 12.0;

        var meanOffsetC = GetRequiredDouble(input, "meanOperativeOffsetC");
        var maxOffsetC = GetRequiredDouble(input, "maxOperativeOffsetC");
        var minOffsetC = GetRequiredDouble(input, "minOperativeOffsetC");

        return new Iso52016ExternalValidationExpectedResult(
            AnnualHeatingKWh: 0.0,
            AnnualCoolingKWh: Round6(annualCoolingKWh),
            PeakHeatingW: 0.0,
            PeakCoolingW: Round6(peakCoolingW),
            MeanOperativeTemperatureC: Round6(coolingSetpointC + meanOffsetC),
            MaxOperativeTemperatureC: Round6(coolingSetpointC + maxOffsetC),
            MinOperativeTemperatureC: Round6(coolingSetpointC + minOffsetC),
            HourlyResultCount: 24,
            MonthlyHeatingKWh: Enumerable.Repeat(0.0, 12).ToArray(),
            MonthlyCoolingKWh: Enumerable.Repeat(Round6(monthlyCoolingKWh), 12).ToArray());
    }

    private static Iso52016ExternalValidationExpectedResult CalculateAnnualSeasonal(JsonElement input)
    {
        var monthlyHeating = GetRequiredDoubleArray(input, "monthlyHeatingKWhByHand");
        var monthlyCooling = GetRequiredDoubleArray(input, "monthlyCoolingKWhByHand");
        var annualHeatingKWh = monthlyHeating.Sum();
        var annualCoolingKWh = monthlyCooling.Sum();

        return new Iso52016ExternalValidationExpectedResult(
            AnnualHeatingKWh: Round6(annualHeatingKWh),
            AnnualCoolingKWh: Round6(annualCoolingKWh),
            PeakHeatingW: Round6(GetRequiredDouble(input, "peakHeatingWByHand")),
            PeakCoolingW: Round6(GetRequiredDouble(input, "peakCoolingWByHand")),
            MeanOperativeTemperatureC: Round6(GetRequiredDouble(input, "meanOperativeTemperatureCByHand")),
            MaxOperativeTemperatureC: Round6(GetRequiredDouble(input, "maxOperativeTemperatureCByHand")),
            MinOperativeTemperatureC: Round6(GetRequiredDouble(input, "minOperativeTemperatureCByHand")),
            HourlyResultCount: checked((int)GetRequiredDouble(input, "hourlyProfileLength")),
            MonthlyHeatingKWh: monthlyHeating.Select(Round6).ToArray(),
            MonthlyCoolingKWh: monthlyCooling.Select(Round6).ToArray());
    }

    private static double GetRequiredDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            throw new InvalidOperationException($"Required input property '{propertyName}' is missing.");

        if (property.ValueKind != JsonValueKind.Number || !property.TryGetDouble(out var value))
            throw new InvalidOperationException($"Input property '{propertyName}' must be a number.");

        return value;
    }

    private static IReadOnlyList<double> GetRequiredDoubleArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            throw new InvalidOperationException($"Required input property '{propertyName}' is missing.");

        if (property.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"Input property '{propertyName}' must be an array.");

        return property
            .EnumerateArray()
            .Select(item =>
            {
                if (item.ValueKind != JsonValueKind.Number || !item.TryGetDouble(out var value))
                    throw new InvalidOperationException($"Array property '{propertyName}' must contain only numbers.");
                return value;
            })
            .ToArray();
    }

    private static double Round6(double value) => Math.Round(value, 6, MidpointRounding.AwayFromZero);
}
