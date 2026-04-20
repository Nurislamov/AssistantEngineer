using AssistantEngineer.Domain.Models;

namespace AssistantEngineer.Application.Abstractions;

public interface IIso52016ReferenceDataProvider
{
    Task<IReadOnlyDictionary<CardinalDirection, IReadOnlyList<double>>> GetSolarRadiationAsync(
        ClimateZone climateZone,
        int month,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<double>?> GetOutdoorTemperatureProfileAsync(
        ClimateZone climateZone,
        int month,
        CancellationToken cancellationToken = default);

    Task<bool> HasClimateDataAsync(
        ClimateZone climateZone,
        int month,
        CancellationToken cancellationToken = default);

    double GetDefaultSolarRadiation(CardinalDirection orientation);

    double GetPeopleHeatGain(RoomType roomType);
}


