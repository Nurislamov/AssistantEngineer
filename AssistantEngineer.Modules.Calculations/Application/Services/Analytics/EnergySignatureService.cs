using AssistantEngineer.Modules.Calculations.Application.Contracts.Analytics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Analytics;

public sealed class EnergySignatureService
{
    public Result<EnergySignatureResult> Calculate(
        Iso52016AnnualEnergyNeedResult energyNeed,
        double heatingBaseTemperatureC = 18)
    {
        if (energyNeed.HourlyResults.Count == 0)
            return Result<EnergySignatureResult>.Validation("Hourly energy results are required.");

        var points = energyNeed.HourlyResults
            .GroupBy(hour => hour.Month)
            .Select(group =>
            {
                var heating = group.Sum(hour => hour.HeatingLoadW) / 1000.0;
                var hdd = group.Sum(hour => Math.Max(0, heatingBaseTemperatureC - hour.OutdoorTemperatureC)) / 24.0;
                return new EnergySignaturePoint(group.Key, Round(hdd), Round(heating));
            })
            .OrderBy(point => point.Month)
            .ToArray();

        var regression = LinearRegression(
            points.Select(point => point.HeatingDegreeDays).ToArray(),
            points.Select(point => point.HeatingDemandKWh).ToArray());

        return Result<EnergySignatureResult>.Success(new EnergySignatureResult(
            HeatingBaseTemperatureC: heatingBaseTemperatureC,
            Points: points,
            SlopeKWhPerHdd: Round(regression.Slope),
            InterceptKWh: Round(regression.Intercept),
            RSquared: Round(regression.RSquared)));
    }

    private static (double Slope, double Intercept, double RSquared) LinearRegression(
        IReadOnlyList<double> x,
        IReadOnlyList<double> y)
    {
        var xMean = x.Average();
        var yMean = y.Average();
        var ssX = x.Sum(value => Math.Pow(value - xMean, 2));
        if (ssX <= 0)
            return (0, yMean, 0);

        var slope = x.Zip(y).Sum(pair => (pair.First - xMean) * (pair.Second - yMean)) / ssX;
        var intercept = yMean - slope * xMean;
        var ssTot = y.Sum(value => Math.Pow(value - yMean, 2));
        var ssRes = x.Zip(y).Sum(pair => Math.Pow(pair.Second - (slope * pair.First + intercept), 2));
        var rSquared = ssTot <= 0 ? 1 : 1 - ssRes / ssTot;
        return (slope, intercept, Math.Clamp(rSquared, 0, 1));
    }

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}