using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Common;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Heating;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class BuildingHeatingReportGenerator
{
    private readonly TimeProvider _timeProvider;

    public BuildingHeatingReportGenerator(
        TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public BuildingHeatingReport Generate(
        BuildingHeatingLoadResult data)
    {
        var rooms = data.Rooms;

        var transmissionLoss = rooms.Sum(room => room.TransmissionHeatLossW);
        var ventilationLoss = rooms.Sum(room => room.VentilationHeatLossW);
        var totalLoad = transmissionLoss + ventilationLoss;

        var outdoorTemp = WeightedAverage(
            rooms,
            room => room.OutdoorDesignTemperatureC,
            room => room.TotalDesignHeatingLoadW);

        var indoorTemp = WeightedAverage(
            rooms,
            room => room.IndoorDesignTemperatureC,
            room => room.TotalDesignHeatingLoadW);

        return new BuildingHeatingReport
        {
            ProjectName = data.ProjectName,
            BuildingName = data.BuildingName,
            CalculationMethod = data.CalculationMethod,
            GeneratedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,

            OutdoorDesignTemperatureC = outdoorTemp,
            IndoorDesignTemperatureC = indoorTemp,

            RoomsCount = rooms.Count,

            TotalTransmissionLossW = Round(transmissionLoss),
            TotalVentilationLossW = Round(ventilationLoss),
            TotalDesignHeatingLoadW = Round(totalLoad),
            TotalDesignHeatingLoadKw = Round(totalLoad / 1000.0),

            CalculationDisclosure = EngineeringCoreReportDisclosures.HeatingDesignPoint(
                data.CalculationMethod,
                string.IsNullOrWhiteSpace(data.ActualMethod)
                    ? data.CalculationMethod
                    : data.ActualMethod),

            Rooms = rooms
                .OrderBy(room => room.RoomId)
                .ToList()
        };
    }

    private static double WeightedAverage(
        IReadOnlyCollection<RoomHeatingLoadResult> rooms,
        Func<RoomHeatingLoadResult, double> valueSelector,
        Func<RoomHeatingLoadResult, double> weightSelector)
    {
        if (rooms.Count == 0)
            return 0;

        var totalWeight = rooms.Sum(weightSelector);

        if (totalWeight <= 0)
            return Round(rooms.Average(valueSelector));

        return Round(
            rooms.Sum(room => valueSelector(room) * weightSelector(room)) / totalWeight);
    }

    private static double Round(
        double value) =>
        Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
}