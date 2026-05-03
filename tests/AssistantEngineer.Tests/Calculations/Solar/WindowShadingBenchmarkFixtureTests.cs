using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SolarGains;
using AssistantEngineer.Modules.Calculations.Application.Services.SolarGains;

namespace AssistantEngineer.Tests.Calculations.Solar;

public class WindowShadingBenchmarkFixtureTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Theory]
    [InlineData("window-shading-no-shading.json")]
    [InlineData("window-shading-fixed-factor.json")]
    [InlineData("window-shading-overhang-south-summer.json")]
    [InlineData("window-shading-night-zero.json")]
    public void Calculate_MatchesWindowShadingBenchmarkFixture(
        string fixtureFileName)
    {
        var fixture = ReadFixture(fixtureFileName);

        var input = fixture.Input.ToInput(
            diagnosticsContext: fixture.Name);

        var result = new WindowSolarGainEngine()
            .Calculate(input);

        Assert.True(result.IsSuccess);

        var value = result.Value;

        Assert.Equal(fixture.Expected.EffectiveSolarFactor, value.EffectiveSolarFactor, precision: 6);
        Assert.Equal(fixture.Expected.DirectSolarGainW, value.DirectSolarGainW, precision: 6);
        Assert.Equal(fixture.Expected.DiffuseSolarGainW, value.DiffuseSolarGainW, precision: 6);
        Assert.Equal(fixture.Expected.GroundReflectedSolarGainW, value.GroundReflectedSolarGainW, precision: 6);
        Assert.Equal(fixture.Expected.TotalSolarGainW, value.SolarGainW, precision: 6);

        if (fixture.Input.IsNight)
        {
            Assert.Contains(value.Diagnostics, diagnostic =>
                diagnostic.Code == "SolarWeather.NightSolarClampedToZero");
        }
    }

    private static WindowShadingBenchmarkFixture ReadFixture(
        string fixtureFileName)
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Calculations",
            "Solar",
            "Fixtures",
            fixtureFileName);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Window shading fixture was not copied to test output: {path}",
                path);
        }

        var json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<WindowShadingBenchmarkFixture>(
                   json,
                   JsonOptions)
               ?? throw new InvalidOperationException(
                   $"Unable to deserialize fixture {fixtureFileName}.");
    }

    public sealed record WindowShadingBenchmarkFixture(
        string Name,
        WindowSolarGainInputFixture Input,
        WindowSolarGainExpectedFixture Expected);

    public sealed record WindowSolarGainInputFixture(
        int WindowId,
        int RoomId,
        double AreaM2,
        double OrientationAzimuthDeg,
        double TiltDeg,
        double Shgc,
        double FrameFactor,
        double InternalShadingFactor,
        double ExternalShadingFactor,
        double FixedShadingFactor,
        double DirectIrradianceWPerM2,
        double DiffuseIrradianceWPerM2,
        double GroundReflectedIrradianceWPerM2,
        int HourIndex,
        bool IsNight)
    {
        public WindowSolarGainInput ToInput(
            string diagnosticsContext) =>
            new(
                WindowId: WindowId,
                RoomId: RoomId,
                AreaM2: AreaM2,
                OrientationAzimuthDeg: OrientationAzimuthDeg,
                TiltDeg: TiltDeg,
                Shgc: Shgc,
                FrameFactor: FrameFactor,
                InternalShadingFactor: InternalShadingFactor,
                ExternalShadingFactor: ExternalShadingFactor,
                FixedShadingFactor: FixedShadingFactor,
                DirectIrradianceWPerM2: DirectIrradianceWPerM2,
                DiffuseIrradianceWPerM2: DiffuseIrradianceWPerM2,
                GroundReflectedIrradianceWPerM2: GroundReflectedIrradianceWPerM2,
                HourIndex: HourIndex,
                IsNight: IsNight,
                DiagnosticsContext: diagnosticsContext);
    }

    public sealed record WindowSolarGainExpectedFixture(
        double EffectiveSolarFactor,
        double DirectSolarGainW,
        double DiffuseSolarGainW,
        double GroundReflectedSolarGainW,
        double TotalSolarGainW);
}
