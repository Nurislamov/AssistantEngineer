using System.Globalization;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Services.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.SharedKernel.Abstractions;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Tests;

public class EpwAnnualClimateDataImportServiceTests
{
    [Fact]
    public async Task ImportAsyncStoresAnnualClimateDataFromEpw()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var epwPath = Path.Combine(tempDirectory, "weather.epw");
            await File.WriteAllTextAsync(epwPath, CreateEpwContent(
                year: 2021,
                hourCount: 8760,
                lineFactory: CreateStandardEpwDataLine));

            var climateZone = CreateClimateZone();
            var annualRepository = new AnnualClimateDataRepositoryStub();
            var context = new UnitOfWorkStub();
            var service = new EpwAnnualClimateDataImportService(
                new ClimateZoneRepositoryStub(climateZone),
                annualRepository,
                context);

            await using var stream = File.OpenRead(epwPath);

            var result = await service.ImportAsync(
                climateZone.Id,
                year: 2021,
                stream,
                "weather.epw");

            Assert.True(result.IsSuccess, result.Error);
            Assert.Equal(8760, result.Value.HourlyRecordsImported);
            Assert.Equal(1, context.SaveChangesCallCount);
            Assert.NotNull(annualRepository.StoredAnnualData);

            var storedHours = annualRepository.StoredAnnualData.HourlyData
                .OrderBy(hour => hour.HourOfYear)
                .ToArray();

            Assert.Equal(8760, storedHours.Length);
            Assert.Equal(0, storedHours.First().HourOfYear);
            Assert.Equal(8759, storedHours.Last().HourOfYear);

            Assert.Equal(15, storedHours.First().DryBulbTemperature);
            Assert.Equal(100, storedHours.First().DirectSolarRadiation);
            Assert.Equal(20, storedHours.First().DiffuseSolarRadiation);
            Assert.Equal(50, storedHours.First().RelativeHumidityPercent);
            Assert.Equal(101325, storedHours.First().AtmosphericPressurePa);
            Assert.Equal(3.2, storedHours.First().WindSpeedMPerS);
            Assert.Equal(180, storedHours.First().WindDirectionDegrees);
            Assert.Equal(320, storedHours.First().HorizontalInfraredRadiationWPerM2);
            Assert.NotNull(storedHours.First().SkyTemperatureC);
            Assert.Equal(4, storedHours.First().TotalSkyCoverTenths);
            Assert.Equal(2, storedHours.First().OpaqueSkyCoverTenths);

            Assert.Contains("WindSpeed", result.Value.ImportedFields);
            Assert.Contains("HorizontalInfraredRadiation", result.Value.ImportedFields);
            Assert.Contains("SkyTemperature", result.Value.ImportedFields);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ImportAsyncNormalizesLeapYearEpwToNonLeap8760()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var epwPath = Path.Combine(tempDirectory, "leap-year-weather.epw");
            await File.WriteAllTextAsync(epwPath, CreateEpwContent(
                year: 2020,
                hourCount: 8784,
                lineFactory: CreateEncodedEpwDataLine));

            var climateZone = CreateClimateZone();
            var annualRepository = new AnnualClimateDataRepositoryStub();
            var context = new UnitOfWorkStub();
            var service = new EpwAnnualClimateDataImportService(
                new ClimateZoneRepositoryStub(climateZone),
                annualRepository,
                context);

            await using var stream = File.OpenRead(epwPath);

            var result = await service.ImportAsync(
                climateZone.Id,
                year: 2020,
                stream,
                "leap-year-weather.epw");

            Assert.True(result.IsSuccess, result.Error);
            Assert.Equal(8760, result.Value.HourlyRecordsImported);
            Assert.NotNull(annualRepository.StoredAnnualData);

            var storedHours = annualRepository.StoredAnnualData.HourlyData
                .OrderBy(hour => hour.HourOfYear)
                .ToArray();

            Assert.Equal(8760, storedHours.Length);
            Assert.Equal(Enumerable.Range(0, 8760), storedHours.Select(hour => hour.HourOfYear));

            Assert.DoesNotContain(storedHours, hour =>
                hour.DryBulbTemperature == 2 &&
                hour.DirectSolarRadiation == 29);

            var february28Hour23 = storedHours[1415];
            var march1Hour0 = storedHours[1416];

            Assert.Equal(2, february28Hour23.DryBulbTemperature);
            Assert.Equal(28, february28Hour23.DirectSolarRadiation);
            Assert.Equal(23, february28Hour23.DiffuseSolarRadiation);

            Assert.Equal(3, march1Hour0.DryBulbTemperature);
            Assert.Equal(1, march1Hour0.DirectSolarRadiation);
            Assert.Equal(0, march1Hour0.DiffuseSolarRadiation);

            Assert.Equal(12, storedHours.Last().DryBulbTemperature);
            Assert.Equal(31, storedHours.Last().DirectSolarRadiation);
            Assert.Equal(23, storedHours.Last().DiffuseSolarRadiation);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ImportAsyncRejectsEpwWhenNonLeapHourlyCountIsNot8760()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var epwPath = Path.Combine(tempDirectory, "short-weather.epw");
            await File.WriteAllTextAsync(epwPath, CreateEpwContent(
                year: 2021,
                hourCount: 8759,
                lineFactory: CreateStandardEpwDataLine));

            var climateZone = CreateClimateZone();
            var annualRepository = new AnnualClimateDataRepositoryStub();
            var context = new UnitOfWorkStub();
            var service = new EpwAnnualClimateDataImportService(
                new ClimateZoneRepositoryStub(climateZone),
                annualRepository,
                context);

            await using var stream = File.OpenRead(epwPath);

            var result = await service.ImportAsync(
                climateZone.Id,
                year: 2021,
                stream,
                "short-weather.epw");

            Assert.True(result.IsFailure);
            Assert.Equal(ResultErrorType.Validation, result.ErrorType);
            Assert.Contains("8760", result.Error, StringComparison.OrdinalIgnoreCase);
            Assert.Null(annualRepository.StoredAnnualData);
            Assert.Equal(0, context.SaveChangesCallCount);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ImportAsyncNormalizesNegativeAndMissingRadiationToZero()
    {
        var tempDirectory = CreateTempDirectory();

        try
        {
            var epwPath = Path.Combine(tempDirectory, "radiation-normalization.epw");
            await File.WriteAllTextAsync(epwPath, CreateEpwContent(
                year: 2021,
                hourCount: 8760,
                lineFactory: timestamp => CreateRadiationNormalizationEpwDataLine(timestamp)));

            var climateZone = CreateClimateZone();
            var annualRepository = new AnnualClimateDataRepositoryStub();
            var context = new UnitOfWorkStub();
            var service = new EpwAnnualClimateDataImportService(
                new ClimateZoneRepositoryStub(climateZone),
                annualRepository,
                context);

            await using var stream = File.OpenRead(epwPath);

            var result = await service.ImportAsync(
                climateZone.Id,
                year: 2021,
                stream,
                "radiation-normalization.epw");

            Assert.True(result.IsSuccess, result.Error);
            Assert.NotNull(annualRepository.StoredAnnualData);

            var firstHour = annualRepository.StoredAnnualData.HourlyData
                .OrderBy(hour => hour.HourOfYear)
                .First();

            Assert.Equal(0, firstHour.DirectSolarRadiation);
            Assert.Equal(0, firstHour.DiffuseSolarRadiation);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ImportAsyncReturnsValidationWhenSourceFileIsMissing()
    {
        var climateZone = CreateClimateZone();

        var service = new EpwAnnualClimateDataImportService(
            new ClimateZoneRepositoryStub(climateZone),
            new AnnualClimateDataRepositoryStub(),
            new UnitOfWorkStub());

        var result = await service.ImportAsync(
            climateZone.Id,
            year: 2020,
            null!,
            "missing.epw");

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
    }

    private static ClimateZone CreateClimateZone() =>
        ClimateZone.Create(
            "Imported climate",
            Temperature.FromCelsius(35).Value,
            Temperature.FromCelsius(-10).Value).Value;

    private static string CreateEpwContent(
        int year,
        int hourCount,
        Func<DateTime, string> lineFactory)
    {
        var lines = new List<string>
        {
            "LOCATION,Test,Test,USA,TMY,000000,0,0,0,0",
            "DESIGN CONDITIONS,0",
            "TYPICAL/EXTREME PERIODS,0",
            "GROUND TEMPERATURES,0",
            "HOLIDAYS/DAYLIGHT SAVINGS,No,0,0,0",
            "COMMENTS 1,Generated by tests",
            "COMMENTS 2,Generated by tests",
            "DATA PERIODS,1,1,Data,Sunday,1/1,12/31"
        };

        var start = new DateTime(year, 1, 1);

        for (var hour = 0; hour < hourCount; hour++)
        {
            var timestamp = start.AddHours(hour);
            lines.Add(lineFactory(timestamp));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string CreateStandardEpwDataLine(DateTime timestamp)
    {
        var fields = CreateBaseEpwFields(timestamp);

        fields[6] = "15";
        fields[7] = "10";
        fields[8] = "50";
        fields[9] = "101325";
        fields[12] = "320";
        fields[13] = "120";
        fields[14] = "100";
        fields[15] = "20";
        fields[20] = "180";
        fields[21] = "3.2";
        fields[22] = "4";
        fields[23] = "2";

        return string.Join(",", fields);
    }

    private static string CreateEncodedEpwDataLine(DateTime timestamp)
    {
        var fields = CreateBaseEpwFields(timestamp);

        fields[6] = timestamp.Month.ToString(CultureInfo.InvariantCulture);
        fields[7] = "10";
        fields[8] = "50";
        fields[9] = "101325";
        fields[12] = "320";
        fields[13] = "120";
        fields[14] = timestamp.Day.ToString(CultureInfo.InvariantCulture);
        fields[15] = timestamp.Hour.ToString(CultureInfo.InvariantCulture);
        fields[20] = "180";
        fields[21] = "3.2";
        fields[22] = "4";
        fields[23] = "2";

        return string.Join(",", fields);
    }

    private static string CreateRadiationNormalizationEpwDataLine(DateTime timestamp)
    {
        var fields = CreateBaseEpwFields(timestamp);

        fields[6] = "15";
        fields[7] = "10";
        fields[8] = "50";
        fields[9] = "101325";
        fields[12] = "320";
        fields[13] = "120";

        if (timestamp.Month == 1 &&
            timestamp.Day == 1 &&
            timestamp.Hour == 0)
        {
            fields[14] = "-5";
            fields[15] = "9999";
        }
        else
        {
            fields[14] = "100";
            fields[15] = "20";
        }

        fields[20] = "180";
        fields[21] = "3.2";
        fields[22] = "4";
        fields[23] = "2";

        return string.Join(",", fields);
    }

    private static string[] CreateBaseEpwFields(DateTime timestamp)
    {
        var fields = new string[35];
        Array.Fill(fields, "0");

        fields[0] = timestamp.Year.ToString(CultureInfo.InvariantCulture);
        fields[1] = timestamp.Month.ToString(CultureInfo.InvariantCulture);
        fields[2] = timestamp.Day.ToString(CultureInfo.InvariantCulture);
        fields[3] = (timestamp.Hour + 1).ToString(CultureInfo.InvariantCulture);
        fields[4] = "60";
        fields[5] = "?9?9?9?9";

        return fields;
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"assistant-engineer-epw-tests-{Guid.NewGuid():N}");

        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class ClimateZoneRepositoryStub : IClimateZoneRepository
    {
        private readonly ClimateZone _climateZone;

        public ClimateZoneRepositoryStub(ClimateZone climateZone)
        {
            _climateZone = climateZone;
        }

        public Task<ClimateZone?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ClimateZone?>(id == _climateZone.Id ? _climateZone : null);
    }

    private sealed class AnnualClimateDataRepositoryStub : IAnnualClimateDataRepository
    {
        public AnnualClimateData? StoredAnnualData { get; private set; }

        public Task<AnnualClimateData?> GetForClimateZoneAsync(
            int climateZoneId,
            int year,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AnnualClimateData?>(StoredAnnualData);

        public Task ReplaceForClimateZoneAsync(
            AnnualClimateData annualClimateData,
            CancellationToken cancellationToken = default)
        {
            StoredAnnualData = annualClimateData;
            return Task.CompletedTask;
        }
    }

    private sealed class UnitOfWorkStub : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }
    }
}