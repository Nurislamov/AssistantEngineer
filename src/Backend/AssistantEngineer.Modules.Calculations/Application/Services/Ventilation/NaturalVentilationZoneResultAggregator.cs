using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

internal static class NaturalVentilationZoneResultAggregator
{
    internal sealed record RoomAggregationResult(
        IReadOnlyList<NaturalVentilationHourlyRoomResult> RoomResults,
        IReadOnlyList<NaturalVentilationHourlyOpeningCalculationResult> UnassignedOpenings,
        IReadOnlyList<NaturalVentilationHourlyRoomResult> UnassignedRooms);

    internal sealed record ZoneAggregationResult(
        IReadOnlyList<NaturalVentilationHourlyZoneResult> HourlyZones,
        IReadOnlyDictionary<string, IReadOnlyList<double>> ZoneAirflowProfiles,
        IReadOnlyDictionary<string, IReadOnlyList<double>> ZoneHveProfiles,
        IReadOnlyDictionary<string, IReadOnlyList<double>> ZoneLoadProfiles,
        IReadOnlyDictionary<string, IReadOnlyList<double>> ZoneAchProfiles);

    public static RoomAggregationResult BuildRoomResults(
        NaturalVentilationZoneIntegrationInput input,
        IReadOnlyList<NaturalVentilationHourlyOpeningCalculationResult> openingResults)
    {
        var roomsById = input.Topology.Rooms
            .Where(room => !string.IsNullOrWhiteSpace(room.RoomId))
            .ToDictionary(room => room.RoomId, room => room, StringComparer.Ordinal);

        var roomResults = new List<NaturalVentilationHourlyRoomResult>();
        foreach (var roomGroup in openingResults
                     .Where(opening => !string.IsNullOrWhiteSpace(opening.RoomId))
                     .GroupBy(opening => new { opening.HourIndex, RoomId = opening.RoomId! }))
        {
            var roomDiagnostics = new List<StandardCalculationDiagnostic>();
            var roomOpenings = roomGroup.ToArray();
            var zoneId = roomOpenings.Select(opening => opening.ZoneId).FirstOrDefault(zone => !string.IsNullOrWhiteSpace(zone));

            var totalM3PerS = roomOpenings.Sum(opening => opening.AirflowCubicMetersPerSecond);
            var totalM3PerH = roomOpenings.Sum(opening => opening.AirflowCubicMetersPerHour);
            var totalKgPerS = roomOpenings.Sum(opening => opening.AirflowKilogramsPerSecond);
            var totalHve = roomOpenings.Sum(opening => opening.VentilationHeatTransferCoefficientWPerKelvin ?? 0.0);
            var totalLoad = roomOpenings.Sum(opening => opening.SensibleVentilationLoadWatts ?? 0.0);

            double? ach = null;
            if (roomsById.TryGetValue(roomGroup.Key.RoomId, out var room) &&
                room.VolumeCubicMeters is > 0.0)
            {
                ach = totalM3PerH / room.VolumeCubicMeters.Value;
                roomDiagnostics.Add(NaturalVentilationZoneDiagnosticsBuilder.Info(
                    "AE-VENT-ZONE-ACH-CALCULATED",
                    $"Room '{room.RoomId}' ACH was calculated for hour {roomGroup.Key.HourIndex}."));
            }
            else
            {
                roomDiagnostics.Add(NaturalVentilationZoneDiagnosticsBuilder.Warning(
                    "AE-VENT-ZONE-VOLUME-MISSING",
                    $"Room '{roomGroup.Key.RoomId}' has missing/non-positive volume; ACH is unavailable."));
            }

            roomResults.Add(new NaturalVentilationHourlyRoomResult(
                HourIndex: roomGroup.Key.HourIndex,
                RoomId: roomGroup.Key.RoomId,
                ZoneId: zoneId,
                TotalAirflowCubicMetersPerSecond: totalM3PerS,
                TotalAirflowCubicMetersPerHour: totalM3PerH,
                TotalAirflowKilogramsPerSecond: totalKgPerS,
                AirChangesPerHour: ach,
                VentilationHeatTransferCoefficientWPerKelvin: totalHve,
                SensibleVentilationLoadWatts: totalLoad,
                Openings: roomOpenings,
                Diagnostics: roomDiagnostics));
        }

        var unassignedOpenings = openingResults
            .Where(opening => string.IsNullOrWhiteSpace(opening.RoomId) && string.IsNullOrWhiteSpace(opening.ZoneId))
            .OrderBy(opening => opening.HourIndex)
            .ThenBy(opening => opening.OpeningId, StringComparer.Ordinal)
            .ToArray();

        var unassignedRooms = roomResults
            .Where(room => string.IsNullOrWhiteSpace(room.ZoneId))
            .OrderBy(room => room.HourIndex)
            .ThenBy(room => room.RoomId, StringComparer.Ordinal)
            .ToArray();

        return new RoomAggregationResult(
            roomResults,
            unassignedOpenings,
            unassignedRooms);
    }

    public static ZoneAggregationResult BuildZoneResultsAndProfiles(
        NaturalVentilationZoneIntegrationInput input,
        IReadOnlyList<NaturalVentilationHourlyOpeningCalculationResult> openingResults,
        IReadOnlyList<NaturalVentilationHourlyRoomResult> roomResults,
        ICollection<StandardCalculationDiagnostic> diagnostics)
    {
        var zoneIds = new HashSet<string>(
            input.Topology.Zones
                .Select(zone => zone.ZoneId)
                .Where(zoneId => !string.IsNullOrWhiteSpace(zoneId)),
            StringComparer.Ordinal);
        foreach (var room in roomResults.Where(room => !string.IsNullOrWhiteSpace(room.ZoneId)))
        {
            zoneIds.Add(room.ZoneId!);
        }
        foreach (var opening in openingResults.Where(opening => !string.IsNullOrWhiteSpace(opening.ZoneId)))
        {
            zoneIds.Add(opening.ZoneId!);
        }

        var hourlyZones = new List<NaturalVentilationHourlyZoneResult>();
        foreach (var zoneId in zoneIds)
        {
            var zoneRoomVolumes = input.Topology.Rooms
                .Where(room => string.Equals(room.ZoneId, zoneId, StringComparison.Ordinal))
                .Select(room => room.VolumeCubicMeters ?? 0.0)
                .Where(volume => volume > 0.0)
                .ToArray();
            var zoneVolume = zoneRoomVolumes.Sum();

            var hours = roomResults
                .Where(room => string.Equals(room.ZoneId, zoneId, StringComparison.Ordinal))
                .Select(room => room.HourIndex)
                .Union(openingResults
                    .Where(opening => string.IsNullOrWhiteSpace(opening.RoomId) &&
                                      string.Equals(opening.ZoneId, zoneId, StringComparison.Ordinal))
                    .Select(opening => opening.HourIndex))
                .Distinct()
                .OrderBy(hour => hour)
                .ToArray();

            foreach (var hour in hours)
            {
                var zoneDiagnostics = new List<StandardCalculationDiagnostic>();
                var rooms = roomResults
                    .Where(room => room.HourIndex == hour &&
                                   string.Equals(room.ZoneId, zoneId, StringComparison.Ordinal))
                    .ToArray();
                var zoneUnassignedOpenings = openingResults
                    .Where(opening => opening.HourIndex == hour &&
                                      string.IsNullOrWhiteSpace(opening.RoomId) &&
                                      string.Equals(opening.ZoneId, zoneId, StringComparison.Ordinal))
                    .ToArray();

                var totalM3PerS = rooms.Sum(room => room.TotalAirflowCubicMetersPerSecond) +
                                  zoneUnassignedOpenings.Sum(opening => opening.AirflowCubicMetersPerSecond);
                var totalM3PerH = rooms.Sum(room => room.TotalAirflowCubicMetersPerHour) +
                                  zoneUnassignedOpenings.Sum(opening => opening.AirflowCubicMetersPerHour);
                var totalKgPerS = rooms.Sum(room => room.TotalAirflowKilogramsPerSecond) +
                                  zoneUnassignedOpenings.Sum(opening => opening.AirflowKilogramsPerSecond);
                var totalHve = rooms.Sum(room => room.VentilationHeatTransferCoefficientWPerKelvin) +
                               zoneUnassignedOpenings.Sum(opening => opening.VentilationHeatTransferCoefficientWPerKelvin ?? 0.0);
                var totalLoad = rooms.Sum(room => room.SensibleVentilationLoadWatts) +
                                zoneUnassignedOpenings.Sum(opening => opening.SensibleVentilationLoadWatts ?? 0.0);

                double? ach = null;
                if (zoneVolume > 0.0)
                {
                    ach = totalM3PerH / zoneVolume;
                }
                else
                {
                    zoneDiagnostics.Add(NaturalVentilationZoneDiagnosticsBuilder.Warning(
                        "AE-VENT-ZONE-VOLUME-MISSING",
                        $"Zone '{zoneId}' has missing/non-positive volume; ACH is unavailable at hour {hour}."));
                }

                hourlyZones.Add(new NaturalVentilationHourlyZoneResult(
                    HourIndex: hour,
                    ZoneId: zoneId,
                    TotalAirflowCubicMetersPerSecond: totalM3PerS,
                    TotalAirflowCubicMetersPerHour: totalM3PerH,
                    TotalAirflowKilogramsPerSecond: totalKgPerS,
                    AirChangesPerHour: ach,
                    VentilationHeatTransferCoefficientWPerKelvin: totalHve,
                    SensibleVentilationLoadWatts: totalLoad,
                    Rooms: rooms,
                    UnassignedOpenings: zoneUnassignedOpenings,
                    Diagnostics: zoneDiagnostics));
            }
        }

        hourlyZones = hourlyZones
            .OrderBy(zone => zone.HourIndex)
            .ThenBy(zone => zone.ZoneId, StringComparer.Ordinal)
            .ToList();

        foreach (var diagnostic in hourlyZones.SelectMany(zone => zone.Diagnostics))
        {
            diagnostics.Add(diagnostic);
        }

        var zoneAirflowProfiles = new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal);
        var zoneHveProfiles = new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal);
        var zoneLoadProfiles = new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal);
        var zoneAchProfiles = new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal);

        foreach (var zoneGroup in hourlyZones.GroupBy(zone => zone.ZoneId, StringComparer.Ordinal))
        {
            var zoneHourly = zoneGroup
                .OrderBy(result => result.HourIndex)
                .ToArray();

            zoneAirflowProfiles[zoneGroup.Key] = zoneHourly
                .Select(result => result.TotalAirflowCubicMetersPerHour)
                .ToArray();
            zoneHveProfiles[zoneGroup.Key] = zoneHourly
                .Select(result => result.VentilationHeatTransferCoefficientWPerKelvin)
                .ToArray();
            zoneLoadProfiles[zoneGroup.Key] = zoneHourly
                .Select(result => result.SensibleVentilationLoadWatts)
                .ToArray();
            zoneAchProfiles[zoneGroup.Key] = zoneHourly
                .Select(result => result.AirChangesPerHour ?? 0.0)
                .ToArray();

            var profileLength = zoneHourly.Length;
            if (profileLength != 24 && profileLength != 8760)
            {
                diagnostics.Add(NaturalVentilationZoneDiagnosticsBuilder.Warning(
                    "AE-VENT-ZONE-PROFILE-LENGTH-NONSTANDARD",
                    $"Zone '{zoneGroup.Key}' has nonstandard profile length {profileLength}."));
            }
        }

        diagnostics.Add(NaturalVentilationZoneDiagnosticsBuilder.Info(
            "AE-VENT-ZONE-PROFILE-BUILT",
            $"Zone profiles were built for {zoneAirflowProfiles.Count} zone(s)."));

        return new ZoneAggregationResult(
            hourlyZones,
            zoneAirflowProfiles,
            zoneHveProfiles,
            zoneLoadProfiles,
            zoneAchProfiles);
    }
}
