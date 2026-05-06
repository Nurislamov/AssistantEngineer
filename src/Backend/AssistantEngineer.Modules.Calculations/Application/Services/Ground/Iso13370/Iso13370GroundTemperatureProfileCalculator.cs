using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground.Iso13370;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ground.Iso13370;

public sealed class Iso13370GroundTemperatureProfileCalculator
{
    public IReadOnlyList<double> BuildGroundMonthlyProfile(
        double annualMeanTemperatureC,
        double amplitudeC,
        double phaseShiftMonths)
    {
        var result = new double[12];
        var safeAmplitude = Math.Max(amplitudeC, 0.0);

        for (var monthIndex = 0; monthIndex < 12; monthIndex++)
        {
            var angle = 2.0 * Math.PI * ((monthIndex + 1.0) - phaseShiftMonths) / 12.0;
            result[monthIndex] = annualMeanTemperatureC + safeAmplitude * Math.Cos(angle);
        }

        return result;
    }

    public IReadOnlyList<double> ResolveOutdoorMonthlyProfile(
        IReadOnlyList<double>? outdoorMonthlyMeanTemperaturesC,
        double outdoorAnnualMeanTemperatureC)
    {
        if (outdoorMonthlyMeanTemperaturesC is not null && outdoorMonthlyMeanTemperaturesC.Count == 12)
            return outdoorMonthlyMeanTemperaturesC.ToArray();

        return Enumerable.Repeat(outdoorAnnualMeanTemperatureC, 12).ToArray();
    }

    public Iso13370GroundBoundaryTemperatureProfile BuildBoundaryProfile(
        IReadOnlyList<double> groundMonthlyTemperaturesC,
        IReadOnlyList<double> outdoorMonthlyTemperaturesC,
        double indoorAnnualMeanTemperatureC,
        double groundWeight,
        double outdoorWeight,
        double indoorWeight)
    {
        if (groundMonthlyTemperaturesC.Count != 12)
            throw new InvalidOperationException("Ground monthly profile must have 12 values.");

        if (outdoorMonthlyTemperaturesC.Count != 12)
            throw new InvalidOperationException("Outdoor monthly profile must have 12 values.");

        var records = new List<Iso13370MonthlyGroundBoundaryRecord>(capacity: 12);
        for (var month = 1; month <= 12; month++)
        {
            var ground = groundMonthlyTemperaturesC[month - 1];
            var outdoor = outdoorMonthlyTemperaturesC[month - 1];
            var boundary =
                groundWeight * ground +
                outdoorWeight * outdoor +
                indoorWeight * indoorAnnualMeanTemperatureC;

            records.Add(new Iso13370MonthlyGroundBoundaryRecord(
                Month: month,
                GroundTemperatureC: Round6(ground),
                OutdoorTemperatureC: Round6(outdoor),
                BoundaryTemperatureC: Round6(boundary)));
        }

        var annualMean = records.Average(item => item.BoundaryTemperatureC);

        return new Iso13370GroundBoundaryTemperatureProfile(
            MonthlyRecords: records,
            AnnualMeanBoundaryTemperatureC: Round6(annualMean));
    }

    private static double Round6(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);
}
