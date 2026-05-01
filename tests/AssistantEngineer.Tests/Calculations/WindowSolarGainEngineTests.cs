using AssistantEngineer.Modules.Calculations.Application.Contracts.SolarGains;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Transmission;
using AssistantEngineer.Modules.Calculations.Application.Services.SolarGains;
using AssistantEngineer.Modules.Calculations.Application.Services.Transmission;

namespace AssistantEngineer.Tests;

public class WindowSolarGainEngineTests
{
    private readonly WindowSolarGainEngine _engine = new();

    [Fact]
    public void CalculatesWindowSolarGainWithoutShading()
    {
        var result = Calculate(CreateWindow(
            areaM2: 2,
            incidentIrradianceWPerM2: 500,
            shgc: 0.60));

        Assert.False(result.HasErrors);
        Assert.Equal(0.60, result.EffectiveSolarFactor);
        Assert.Equal(600, result.SolarGainW);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "SolarGains.EffectiveSolarFactor");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "SolarGains.IncidentIrradianceProvided");
    }

    [Fact]
    public void CalculatesWindowSolarGainWithShading()
    {
        var result = Calculate(CreateWindow(
            areaM2: 2,
            incidentIrradianceWPerM2: 500,
            shgc: 0.60,
            frameFactor: 0.90,
            internalShadingFactor: 0.80,
            externalShadingFactor: 0.75));

        Assert.False(result.HasErrors);
        Assert.Equal(0.324, result.EffectiveSolarFactor);
        Assert.Equal(324, result.SolarGainW);
    }

    [Fact]
    public void ZeroIrradianceReturnsZeroSolarGain()
    {
        var result = Calculate(CreateWindow(
            areaM2: 2,
            incidentIrradianceWPerM2: 0,
            shgc: 0.60));

        Assert.False(result.HasErrors);
        Assert.Equal(0, result.SolarGainW);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "SolarGains.ZeroIrradiance");
    }

    [Fact]
    public void NightReturnsZeroSolarGain()
    {
        var result = Calculate(CreateWindow(
            areaM2: 2,
            incidentIrradianceWPerM2: 500,
            shgc: 0.60,
            isNight: true));

        Assert.False(result.HasErrors);
        Assert.Equal(0, result.SolarGainW);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "SolarGains.Night");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "SolarWeather.NightSolarClampedToZero");
    }

    [Fact]
    public void InvalidShgcProducesDiagnosticsAndExcludesWindow()
    {
        var result = Calculate(CreateWindow(
            areaM2: 2,
            incidentIrradianceWPerM2: 500,
            shgc: 1.25));

        Assert.True(result.HasErrors);
        Assert.False(result.IsIncludedInLoad);
        Assert.Equal(0, result.SolarGainW);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "SolarGains.InvalidShgc");
    }

    [Fact]
    public void InvalidShadingFactorProducesDiagnosticsAndExcludesWindow()
    {
        var result = Calculate(CreateWindow(
            areaM2: 2,
            incidentIrradianceWPerM2: 500,
            shgc: 0.60,
            internalShadingFactor: 1.2));

        Assert.True(result.HasErrors);
        Assert.False(result.IsIncludedInLoad);
        Assert.Equal(0, result.SolarGainW);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "SolarGains.InvalidInternalShadingFactor");
    }

    [Fact]
    public void MissingFrameFactorUsesDocumentedDefaultWithDiagnostics()
    {
        var result = Calculate(CreateWindow(
            areaM2: 2,
            incidentIrradianceWPerM2: 500,
            shgc: 0.60,
            frameFactor: null));

        Assert.False(result.HasErrors);
        Assert.Equal(1.0, result.FrameFactor);
        Assert.Equal(600, result.SolarGainW);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "SolarGains.FrameFactorDefaulted");
    }

    [Fact]
    public void RoomSolarGainAggregatesMultipleWindows()
    {
        var result = _engine.CalculateRoom(
            new RoomWindowSolarGainRequest(
                RoomId: 101,
                Windows:
                [
                    CreateWindow(
                        windowId: 1,
                        areaM2: 2,
                        incidentIrradianceWPerM2: 500,
                        shgc: 0.60),
                    CreateWindow(
                        windowId: 2,
                        areaM2: 1.5,
                        incidentIrradianceWPerM2: 400,
                        shgc: 0.50)
                ]));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(900, result.Value.TotalSolarGainW);
        Assert.Equal(2, result.Value.WindowBreakdown.Count);
    }

    [Fact]
    public void SolarGainIsSeparateFromWindowTransmission()
    {
        var solar = Calculate(CreateWindow(
            areaM2: 2,
            incidentIrradianceWPerM2: 500,
            shgc: 0.60));
        var transmissionEngine = new TransmissionHeatTransferEngine();
        var transmission = transmissionEngine.Calculate(
            new TransmissionHeatTransferRequest(
            [
                new TransmissionElementInput(
                    ElementId: 1,
                    ElementType: TransmissionElementType.Window,
                    RoomId: 101,
                    AreaM2: 2,
                    UValueWPerM2K: 2,
                    IndoorTemperatureC: 20,
                    BoundaryType: TransmissionBoundaryType.Outdoor,
                    OutdoorTemperatureC: -5)
            ]));

        Assert.True(transmission.IsSuccess, transmission.Error);
        Assert.Equal(600, solar.SolarGainW);
        Assert.Equal(100, transmission.Value.TotalHeatLossW);
    }

    private WindowSolarGainResult Calculate(
        WindowSolarGainInput input)
    {
        var result = _engine.Calculate(input);
        Assert.True(result.IsSuccess, result.Error);
        return result.Value;
    }

    private static WindowSolarGainInput CreateWindow(
        double areaM2,
        double incidentIrradianceWPerM2,
        double? shgc,
        int windowId = 1,
        double? frameFactor = 1.0,
        double internalShadingFactor = 1.0,
        double externalShadingFactor = 1.0,
        double fixedShadingFactor = 1.0,
        bool isNight = false) =>
        new(
            WindowId: windowId,
            RoomId: 101,
            AreaM2: areaM2,
            OrientationAzimuthDeg: 180,
            TiltDeg: 90,
            Shgc: shgc,
            FrameFactor: frameFactor,
            InternalShadingFactor: internalShadingFactor,
            ExternalShadingFactor: externalShadingFactor,
            FixedShadingFactor: fixedShadingFactor,
            IncidentIrradianceWPerM2: incidentIrradianceWPerM2,
            IsNight: isNight,
            DiagnosticsContext: $"Window {windowId}");
}
