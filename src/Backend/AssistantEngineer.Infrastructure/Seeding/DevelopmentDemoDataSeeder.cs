using AssistantEngineer.Infrastructure.Persistence;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Ground;
using AssistantEngineer.Modules.Buildings.Domain.Ventilation;
using AssistantEngineer.Modules.Equipment.Domain;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Infrastructure.Seeding;

public sealed class DevelopmentDemoDataSeeder : IDevelopmentDemoDataSeeder
{
    private const string ClimateZoneName = "Demo Tashkent";
    private const string ProjectName = "Demo MVP Project";
    private const string BuildingName = "Demo Office Building";
    private const string FloorName = "Level 1";
    private const string RoomName = "Office 101";
    private const int WeatherYear = 2020;
    private const int DesignMonth = 7;

    private readonly AppDbContext _context;

    public DevelopmentDemoDataSeeder(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<DevelopmentDemoSeedResult>> SeedAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var climateZone = await EnsureClimateZoneAsync(cancellationToken);
            await EnsureMonthlyClimateDataAsync(climateZone, cancellationToken);
            await EnsureAnnualClimateDataAsync(climateZone, cancellationToken);

            var project = await EnsureProjectAsync(cancellationToken);
            var building = await EnsureBuildingAsync(project, climateZone, cancellationToken);
            var floor = await EnsureFloorAsync(building, cancellationToken);
            var room = await EnsureRoomAsync(floor, cancellationToken);
            var wall = await EnsureWallAsync(room, cancellationToken);
            var window = await EnsureWindowAsync(room, cancellationToken);
            await EnsureVentilationAsync(room, cancellationToken);
            await EnsureGroundContactAsync(room, cancellationToken);
            var equipmentIds = await EnsureEquipmentCatalogAsync(cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result<DevelopmentDemoSeedResult>.Success(new DevelopmentDemoSeedResult
            {
                ClimateZoneId = climateZone.Id,
                ProjectId = project.Id,
                BuildingId = building.Id,
                FloorId = floor.Id,
                RoomId = room.Id,
                WallId = wall.Id,
                WindowId = window.Id,
                VentilationParametersId = room.VentilationParametersId ?? 0,
                WeatherYear = WeatherYear,
                EquipmentCatalogItemIds = equipmentIds
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return Result<DevelopmentDemoSeedResult>.Failure(
                $"Failed to seed development demo data: {exception.Message}");
        }
    }

    private async Task<ClimateZone> EnsureClimateZoneAsync(CancellationToken cancellationToken)
    {
        var existing = await _context.ClimateZones
            .FirstOrDefaultAsync(zone => zone.Name == ClimateZoneName, cancellationToken);
        if (existing is not null)
            return existing;

        var zone = ClimateZone.Create(
            ClimateZoneName,
            Temperature.FromCelsius(38).Value,
            Temperature.FromCelsius(-12).Value).Value;

        _context.ClimateZones.Add(zone);
        await _context.SaveChangesAsync(cancellationToken);
        return zone;
    }

    private async Task EnsureMonthlyClimateDataAsync(
        ClimateZone climateZone,
        CancellationToken cancellationToken)
    {
        var existing = await _context.ClimateData
            .Include(data => data.HourlyData)
            .FirstOrDefaultAsync(
                data => data.ClimateZoneId == climateZone.Id && data.Month == DesignMonth,
                cancellationToken);

        if (existing is not null && HasCompleteMonthlyHours(existing))
            return;

        if (existing is not null)
        {
            _context.ClimateData.Remove(existing);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var climateData = ClimateData.Create(
            climateZone,
            DesignMonth,
            dayOfMonth: 15,
            dailyTemperatureRange: 14).Value;

        for (var hour = 0; hour < 24; hour++)
        {
            var weather = CreateDesignDayWeather(hour);
            var addResult = climateData.AddHourlyData(
                hour,
                weather.DryBulbTemperatureC,
                weather.DirectSolarRadiationWPerM2,
                weather.DiffuseSolarRadiationWPerM2,
                relativeHumidityPercent: 35,
                atmosphericPressurePa: 101_325,
                windSpeedMPerS: 2.5,
                windDirectionDegrees: 180);

            if (addResult.IsFailure)
                throw new InvalidOperationException(addResult.Error);
        }

        _context.ClimateData.Add(climateData);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureAnnualClimateDataAsync(
        ClimateZone climateZone,
        CancellationToken cancellationToken)
    {
        var existing = await _context.AnnualClimateData
            .Include(data => data.HourlyData)
            .FirstOrDefaultAsync(
                data => data.ClimateZoneId == climateZone.Id && data.Year == WeatherYear,
                cancellationToken);

        if (existing is not null && HasCompleteAnnualHours(existing))
            return;

        if (existing is not null)
        {
            _context.AnnualClimateData.Remove(existing);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var annual = AnnualClimateData.Create(climateZone, WeatherYear).Value;
        for (var hourOfYear = 0; hourOfYear < 8760; hourOfYear++)
        {
            var weather = CreateAnnualWeather(hourOfYear);
            var addResult = annual.AddHourlyData(
                hourOfYear,
                weather.DryBulbTemperatureC,
                weather.DirectSolarRadiationWPerM2,
                weather.DiffuseSolarRadiationWPerM2,
                relativeHumidityPercent: 45,
                atmosphericPressurePa: 101_325,
                windSpeedMPerS: 2.5,
                windDirectionDegrees: 180);

            if (addResult.IsFailure)
                throw new InvalidOperationException(addResult.Error);
        }

        _context.AnnualClimateData.Add(annual);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Project> EnsureProjectAsync(CancellationToken cancellationToken)
    {
        var existing = await _context.Projects
            .FirstOrDefaultAsync(project => project.Name == ProjectName, cancellationToken);
        if (existing is not null)
            return existing;

        var project = Project.Create(ProjectName).Value;
        _context.Projects.Add(project);
        await _context.SaveChangesAsync(cancellationToken);
        return project;
    }

    private async Task<Building> EnsureBuildingAsync(
        Project project,
        ClimateZone climateZone,
        CancellationToken cancellationToken)
    {
        var building = await _context.Buildings
            .Include(existing => existing.ClimateZone)
            .FirstOrDefaultAsync(
                existing => existing.ProjectId == project.Id && existing.Name == BuildingName,
                cancellationToken);

        if (building is not null)
        {
            if (building.ClimateZone?.Id != climateZone.Id)
            {
                building.SetClimateZone(climateZone);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return building;
        }

        var created = Building.Create(BuildingName, project, climateZone).Value;
        project.AddBuilding(created);
        _context.Buildings.Add(created);
        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    private async Task<Floor> EnsureFloorAsync(
        Building building,
        CancellationToken cancellationToken)
    {
        var existing = await _context.Floors
            .FirstOrDefaultAsync(
                floor => floor.BuildingId == building.Id && floor.Name == FloorName,
                cancellationToken);
        if (existing is not null)
            return existing;

        var created = building.AddFloor(FloorName).Value;
        _context.Floors.Add(created);
        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    private async Task<Room> EnsureRoomAsync(
        Floor floor,
        CancellationToken cancellationToken)
    {
        var existing = await _context.Rooms
            .Include(room => room.Walls)
            .Include(room => room.Windows)
            .Include(room => room.VentilationParameters)
            .FirstOrDefaultAsync(
                room => room.FloorId == floor.Id && room.Name == RoomName,
                cancellationToken);
        if (existing is not null)
            return existing;

        var created = floor.AddRoom(
            RoomName,
            Area.FromSquareMeters(24).Value,
            heightM: 3,
            Temperature.FromCelsius(22).Value,
            Temperature.FromCelsius(-12).Value,
            peopleCount: 2,
            Power.FromWatts(350).Value,
            Power.FromWatts(240).Value,
            RoomType.Office).Value;

        _context.Rooms.Add(created);
        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    private async Task<Wall> EnsureWallAsync(Room room, CancellationToken cancellationToken)
    {
        var existing = room.Walls.FirstOrDefault(wall =>
            wall.Orientation == CardinalDirection.South &&
            wall.BoundaryType == WallBoundaryType.External);
        if (existing is not null)
            return existing;

        var created = room.AddWall(
            Area.FromSquareMeters(18).Value,
            ThermalTransmittance.FromValue(0.35).Value,
            CardinalDirection.South,
            WallBoundaryType.External).Value;

        _context.Walls.Add(created);
        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    private async Task<Window> EnsureWindowAsync(Room room, CancellationToken cancellationToken)
    {
        var existing = room.Windows.FirstOrDefault(window => window.Orientation == CardinalDirection.South);
        if (existing is not null)
            return existing;

        var created = room.AddWindow(
            Area.FromSquareMeters(4.2).Value,
            ThermalTransmittance.FromValue(1.4).Value,
            SolarHeatGainCoefficient.FromValue(0.55).Value,
            CardinalDirection.South).Value;

        _context.Windows.Add(created);
        await _context.SaveChangesAsync(cancellationToken);
        return created;
    }

    private async Task EnsureVentilationAsync(Room room, CancellationToken cancellationToken)
    {
        if (room.VentilationParameters is not null)
            return;

        var ventilation = VentilationParameters.Create(
            airChangesPerHour: 1.2,
            heatRecoveryEfficiency: 0.45,
            infiltrationAirChangesPerHour: 0.2,
            windExposureFactor: 1.0,
            stackCoefficient: 0.04,
            windCoefficient: 0.12).Value;

        _context.VentilationParameters.Add(ventilation);
        room.SetVentilationParameters(ventilation);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureGroundContactAsync(Room room, CancellationToken cancellationToken)
    {
        if (room.GroundContactMetadata is not null)
            return;

        var ground = GroundContactMetadata.Create(
            GroundContactType.SlabOnGround,
            exposedPerimeterM: 12,
            burialDepthM: 0,
            wallHeightBelowGradeM: 0,
            horizontalInsulationWidthM: 0,
            perimeterInsulationDepthM: 0,
            underfloorVentilationAirChangesPerHour: 0).Value;

        room.SetGroundContactMetadata(ground);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<int>> EnsureEquipmentCatalogAsync(CancellationToken cancellationToken)
    {
        var items = new[]
        {
            new EquipmentSeed("DemoHVAC", "Split", "WallMounted", "DX-3.5", 3.5),
            new EquipmentSeed("DemoHVAC", "Split", "WallMounted", "DX-5.0", 5.0)
        };

        var ids = new List<int>(items.Length);
        foreach (var item in items)
        {
            var existing = await _context.EquipmentCatalogItems.FirstOrDefaultAsync(
                catalogItem =>
                    catalogItem.Manufacturer == item.Manufacturer &&
                    catalogItem.SystemType == item.SystemType &&
                    catalogItem.UnitType == item.UnitType &&
                    catalogItem.ModelName == item.ModelName,
                cancellationToken);

            if (existing is not null)
            {
                if (!existing.IsActive)
                    existing.Activate();

                ids.Add(existing.Id);
                continue;
            }

            var created = CoolingEquipmentCatalogItem.Create(
                item.Manufacturer,
                item.SystemType,
                item.UnitType,
                item.ModelName,
                Power.FromWatts(item.NominalCoolingCapacityKw * 1000).Value).Value;

            _context.EquipmentCatalogItems.Add(created);
            await _context.SaveChangesAsync(cancellationToken);
            ids.Add(created.Id);
        }

        return ids;
    }

    private static bool HasCompleteMonthlyHours(ClimateData data) =>
        data.HourlyData
            .Select(hour => hour.Hour)
            .Where(hour => hour.HasValue)
            .Select(hour => hour!.Value)
            .Distinct()
            .OrderBy(hour => hour)
            .SequenceEqual(Enumerable.Range(0, 24));

    private static bool HasCompleteAnnualHours(AnnualClimateData data) =>
        data.HourlyData
            .Select(hour => hour.HourOfYear)
            .Where(hour => hour.HasValue)
            .Select(hour => hour!.Value)
            .Distinct()
            .OrderBy(hour => hour)
            .SequenceEqual(Enumerable.Range(0, 8760));

    private static WeatherPoint CreateDesignDayWeather(int hour)
    {
        var daylight = Math.Max(0, Math.Sin(Math.PI * (hour - 6) / 12.0));
        var diurnal = Math.Sin(2 * Math.PI * (hour - 6) / 24.0);
        return new WeatherPoint(
            DryBulbTemperatureC: 31 + 7 * diurnal,
            DirectSolarRadiationWPerM2: 650 * daylight,
            DiffuseSolarRadiationWPerM2: 90 * daylight);
    }

    private static WeatherPoint CreateAnnualWeather(int hourOfYear)
    {
        var day = hourOfYear / 24.0;
        var hour = hourOfYear % 24;
        var seasonal = Math.Sin(2 * Math.PI * (day - 80) / 365.0);
        var daylight = Math.Max(0, Math.Sin(Math.PI * (hour - 6) / 12.0));
        var diurnal = Math.Sin(2 * Math.PI * (hour - 6) / 24.0);
        var summerFactor = Math.Clamp(0.45 + 0.55 * seasonal, 0.1, 1.0);

        return new WeatherPoint(
            DryBulbTemperatureC: 15 + 14 * seasonal + 5 * diurnal,
            DirectSolarRadiationWPerM2: 620 * daylight * summerFactor,
            DiffuseSolarRadiationWPerM2: 90 * daylight);
    }

    private sealed record WeatherPoint(
        double DryBulbTemperatureC,
        double DirectSolarRadiationWPerM2,
        double DiffuseSolarRadiationWPerM2);

    private sealed record EquipmentSeed(
        string Manufacturer,
        string SystemType,
        string UnitType,
        string ModelName,
        double NominalCoolingCapacityKw);
}
