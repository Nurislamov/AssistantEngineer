using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.InternalGains;
using AssistantEngineer.Modules.Calculations.Application.Contracts.RoomLoads;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SolarGains;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Transmission;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.InternalGains;
using AssistantEngineer.Modules.Calculations.Application.Services.SolarGains;
using AssistantEngineer.Modules.Calculations.Application.Services.Transmission;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.RoomLoads;

public sealed class RoomLoadCalculationEngine
{
    private const string Method = "Energy Calculation Parity / Room Heating Cooling Load";
    private const string Version = "2026.04-internal-deterministic";

    private readonly TransmissionHeatTransferEngine _transmission;
    private readonly WindowSolarGainEngine _solar;
    private readonly VentilationAndInfiltrationLoadEngine _ventilation;
    private readonly InternalGainEngine _internalGains;
    private readonly TimeProvider _timeProvider;

    public RoomLoadCalculationEngine(
        TransmissionHeatTransferEngine? transmission = null,
        WindowSolarGainEngine? solar = null,
        VentilationAndInfiltrationLoadEngine? ventilation = null,
        InternalGainEngine? internalGains = null,
        TimeProvider? timeProvider = null)
    {
        _transmission = transmission ?? new TransmissionHeatTransferEngine();
        _solar = solar ?? new WindowSolarGainEngine();
        _ventilation = ventilation ?? new VentilationAndInfiltrationLoadEngine();
        _internalGains = internalGains ?? new InternalGainEngine();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Result<RoomLoadCalculationResult> Calculate(RoomLoadCalculationInput input)
    {
        if (input is null)
            return Result<RoomLoadCalculationResult>.Validation("Room load calculation input is required.");

        var diagnostics = Validate(input);
        var assumptions = new List<string>
        {
            "Heating load sums positive heat-loss components only.",
            "Cooling load sums positive heat-gain components only.",
            "Internal and solar gains are not deducted from heating load in this design-point method."
        };

        var heating = new MutableHeatingBreakdown();
        var cooling = new MutableCoolingBreakdown();

        AddTransmission(input, heating, cooling, diagnostics);
        AddSolar(input, cooling, diagnostics);
        AddVentilation(input.HeatingVentilationAndInfiltration, isHeating: true, heating, cooling, diagnostics);
        AddVentilation(input.CoolingVentilationAndInfiltration, isHeating: false, heating, cooling, diagnostics);
        AddInternalGains(input.InternalGains, cooling, diagnostics);
        AddFixedComponents(input.FixedComponents, heating, cooling, diagnostics);

        if (input.WindowSolarGains is null)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Info,
                "RoomLoad.NoSolarInput",
                "No window solar gain input was supplied; solar cooling component is zero.",
                input.DiagnosticsContext));
        }

        if (input.InternalGains is null)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Info,
                "RoomLoad.NoInternalGainInput",
                "No internal gains input was supplied; internal cooling component is zero.",
                input.DiagnosticsContext));
        }

        var heatingBreakdown = heating.ToResult();
        var coolingBreakdown = cooling.ToResult();
        var heatingLoad = Round(Math.Max(0, heatingBreakdown.TotalW));
        var coolingLoad = Round(Math.Max(0, coolingBreakdown.TotalW));
        var heatingLoadWPerM2 = input.AreaM2 > 0 ? Round(heatingLoad / input.AreaM2) : 0;
        var coolingLoadWPerM2 = input.AreaM2 > 0 ? Round(coolingLoad / input.AreaM2) : 0;

        return Result<RoomLoadCalculationResult>.Success(new RoomLoadCalculationResult(
            input.RoomId,
            input.RoomCode,
            input.RoomName,
            Round(input.AreaM2),
            heatingLoad,
            coolingLoad,
            heatingLoadWPerM2,
            coolingLoadWPerM2,
            heatingBreakdown,
            coolingBreakdown,
            DominantHeatingComponent(heatingBreakdown),
            DominantCoolingComponent(coolingBreakdown),
            diagnostics,
            assumptions,
            Method,
            Version,
            _timeProvider.GetUtcNow()));
    }

    private static List<CalculationDiagnostic> Validate(RoomLoadCalculationInput input)
    {
        var diagnostics = new List<CalculationDiagnostic>();

        if (input.RoomId < 0)
            diagnostics.Add(Error("RoomLoad.InvalidRoomId", "Room id must not be negative.", input.DiagnosticsContext));

        if (input.AreaM2 <= 0)
            diagnostics.Add(Error("RoomLoad.InvalidArea", "Room area must be greater than zero.", input.DiagnosticsContext));

        if (input.VolumeM3 < 0)
            diagnostics.Add(Error("RoomLoad.InvalidVolume", "Room volume must not be negative.", input.DiagnosticsContext));

        return diagnostics;
    }

    private void AddTransmission(
        RoomLoadCalculationInput input,
        MutableHeatingBreakdown heating,
        MutableCoolingBreakdown cooling,
        List<CalculationDiagnostic> diagnostics)
    {
        if (input.TransmissionElements is null || input.TransmissionElements.Count == 0)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Info,
                "RoomLoad.NoTransmissionElements",
                "No transmission elements were supplied; transmission components use fixed inputs or zero.",
                input.DiagnosticsContext));
            return;
        }

        var result = _transmission.Calculate(new TransmissionHeatTransferRequest(input.TransmissionElements));
        if (result.IsFailure)
        {
            diagnostics.Add(Error("RoomLoad.TransmissionFailed", result.Error, input.DiagnosticsContext));
            return;
        }

        foreach (var diagnostic in result.Value.Diagnostics)
            diagnostics.Add(MapTransmissionDiagnostic(diagnostic));

        foreach (var element in result.Value.Elements.Where(element => element.IsIncludedInLoad))
        {
            var heatLoss = Math.Max(element.HeatFlowW, 0);
            var heatGain = Math.Max(-element.HeatFlowW, 0);
            var isWindow = element.ElementType == TransmissionElementType.Window;
            var isGround = element.BoundaryType == TransmissionBoundaryType.Ground;

            if (isGround)
            {
                heating.GroundW += heatLoss;
                cooling.GroundW += heatGain;
            }
            else if (isWindow)
            {
                heating.WindowTransmissionW += heatLoss;
                cooling.WindowTransmissionW += heatGain;
            }
            else
            {
                heating.TransmissionW += heatLoss;
                cooling.TransmissionW += heatGain;
            }
        }
    }

    private void AddSolar(
        RoomLoadCalculationInput input,
        MutableCoolingBreakdown cooling,
        List<CalculationDiagnostic> diagnostics)
    {
        if (input.WindowSolarGains is null)
            return;

        var result = _solar.CalculateRoom(input.WindowSolarGains);
        if (result.IsFailure)
        {
            diagnostics.Add(Error("RoomLoad.SolarFailed", result.Error, input.DiagnosticsContext));
            return;
        }

        cooling.SolarW += result.Value.TotalSolarGainW;

        foreach (var diagnostic in result.Value.Diagnostics)
            diagnostics.Add(MapSolarDiagnostic(diagnostic));
    }

    private void AddVentilation(
        VentilationAndInfiltrationLoadInput? input,
        bool isHeating,
        MutableHeatingBreakdown heating,
        MutableCoolingBreakdown cooling,
        List<CalculationDiagnostic> diagnostics)
    {
        if (input is null)
            return;

        var result = _ventilation.Calculate(input);
        if (result.IsFailure)
        {
            diagnostics.Add(Error("RoomLoad.VentilationFailed", result.Error, input.DiagnosticsContext));
            return;
        }

        if (isHeating)
        {
            heating.VentilationW +=
                result.Value.MechanicalVentilation.EffectiveHeatingLoadW +
                result.Value.NaturalVentilation.HeatingLoadW;
            heating.InfiltrationW += result.Value.Infiltration.HeatingLoadW;
        }
        else
        {
            cooling.VentilationW +=
                result.Value.MechanicalVentilation.EffectiveCoolingLoadW +
                result.Value.NaturalVentilation.CoolingLoadW;
            cooling.InfiltrationW += result.Value.Infiltration.CoolingLoadW;
        }

        foreach (var diagnostic in result.Value.Diagnostics)
            diagnostics.Add(MapVentilationDiagnostic(diagnostic));
    }

    private void AddInternalGains(
        InternalGainInput? input,
        MutableCoolingBreakdown cooling,
        List<CalculationDiagnostic> diagnostics)
    {
        if (input is null)
            return;

        var result = _internalGains.Calculate(input);
        if (result.IsFailure)
        {
            diagnostics.Add(Error("RoomLoad.InternalGainsFailed", result.Error, input.DiagnosticsContext));
            return;
        }

        cooling.InternalGainsW += result.Value.TotalSensibleGainW;

        foreach (var diagnostic in result.Value.Diagnostics)
            diagnostics.Add(MapInternalGainDiagnostic(diagnostic));
    }

    private static void AddFixedComponents(
        RoomLoadFixedComponentInput? fixedComponents,
        MutableHeatingBreakdown heating,
        MutableCoolingBreakdown cooling,
        List<CalculationDiagnostic> diagnostics)
    {
        if (fixedComponents is null)
            return;

        heating.TransmissionW += Positive(fixedComponents.HeatingTransmissionW, nameof(fixedComponents.HeatingTransmissionW), diagnostics);
        heating.WindowTransmissionW += Positive(fixedComponents.HeatingWindowTransmissionW, nameof(fixedComponents.HeatingWindowTransmissionW), diagnostics);
        heating.GroundW += Positive(fixedComponents.HeatingGroundW, nameof(fixedComponents.HeatingGroundW), diagnostics);
        heating.VentilationW += Positive(fixedComponents.HeatingVentilationW, nameof(fixedComponents.HeatingVentilationW), diagnostics);
        heating.InfiltrationW += Positive(fixedComponents.HeatingInfiltrationW, nameof(fixedComponents.HeatingInfiltrationW), diagnostics);

        cooling.TransmissionW += Positive(fixedComponents.CoolingTransmissionW, nameof(fixedComponents.CoolingTransmissionW), diagnostics);
        cooling.WindowTransmissionW += Positive(fixedComponents.CoolingWindowTransmissionW, nameof(fixedComponents.CoolingWindowTransmissionW), diagnostics);
        cooling.GroundW += Positive(fixedComponents.CoolingGroundW, nameof(fixedComponents.CoolingGroundW), diagnostics);
        cooling.VentilationW += Positive(fixedComponents.CoolingVentilationW, nameof(fixedComponents.CoolingVentilationW), diagnostics);
        cooling.InfiltrationW += Positive(fixedComponents.CoolingInfiltrationW, nameof(fixedComponents.CoolingInfiltrationW), diagnostics);
        cooling.SolarW += Positive(fixedComponents.CoolingSolarW, nameof(fixedComponents.CoolingSolarW), diagnostics);
        cooling.InternalGainsW += Positive(fixedComponents.CoolingInternalGainsW, nameof(fixedComponents.CoolingInternalGainsW), diagnostics);
    }

    private static double Positive(
        double value,
        string component,
        List<CalculationDiagnostic> diagnostics)
    {
        if (value >= 0)
            return value;

        diagnostics.Add(new CalculationDiagnostic(
            CalculationDiagnosticSeverity.Warning,
            "RoomLoad.NegativeFixedComponentClamped",
            $"Fixed component {component} was negative and was clamped to zero."));
        return 0;
    }

    private static CalculationDiagnostic MapTransmissionDiagnostic(TransmissionDiagnostic diagnostic) =>
        new(MapSeverity(diagnostic.Severity), diagnostic.Code, diagnostic.Message, diagnostic.Context);

    private static CalculationDiagnostic MapSolarDiagnostic(SolarGainDiagnostic diagnostic) =>
        new(MapSeverity(diagnostic.Severity), diagnostic.Code, diagnostic.Message, diagnostic.Context);

    private static CalculationDiagnostic MapVentilationDiagnostic(VentilationLoadDiagnostic diagnostic) =>
        new(MapSeverity(diagnostic.Severity), diagnostic.Code, diagnostic.Message, diagnostic.Context);

    private static CalculationDiagnostic MapInternalGainDiagnostic(InternalGainDiagnostic diagnostic) =>
        new(MapSeverity(diagnostic.Severity), diagnostic.Code, diagnostic.Message, diagnostic.Context);

    private static CalculationDiagnosticSeverity MapSeverity(TransmissionDiagnosticSeverity severity) =>
        severity switch
        {
            TransmissionDiagnosticSeverity.Error => CalculationDiagnosticSeverity.Error,
            TransmissionDiagnosticSeverity.Warning => CalculationDiagnosticSeverity.Warning,
            _ => CalculationDiagnosticSeverity.Info
        };

    private static CalculationDiagnosticSeverity MapSeverity(SolarGainDiagnosticSeverity severity) =>
        severity switch
        {
            SolarGainDiagnosticSeverity.Error => CalculationDiagnosticSeverity.Error,
            SolarGainDiagnosticSeverity.Warning => CalculationDiagnosticSeverity.Warning,
            _ => CalculationDiagnosticSeverity.Info
        };

    private static CalculationDiagnosticSeverity MapSeverity(VentilationLoadDiagnosticSeverity severity) =>
        severity switch
        {
            VentilationLoadDiagnosticSeverity.Error => CalculationDiagnosticSeverity.Error,
            VentilationLoadDiagnosticSeverity.Warning => CalculationDiagnosticSeverity.Warning,
            _ => CalculationDiagnosticSeverity.Info
        };

    private static CalculationDiagnosticSeverity MapSeverity(InternalGainDiagnosticSeverity severity) =>
        severity switch
        {
            InternalGainDiagnosticSeverity.Error => CalculationDiagnosticSeverity.Error,
            InternalGainDiagnosticSeverity.Warning => CalculationDiagnosticSeverity.Warning,
            _ => CalculationDiagnosticSeverity.Info
        };

    private static string DominantHeatingComponent(RoomHeatingLoadBreakdown breakdown)
    {
        var components = new Dictionary<string, double>
        {
            ["transmission"] = breakdown.TransmissionW,
            ["windowTransmission"] = breakdown.WindowTransmissionW,
            ["ground"] = breakdown.GroundW,
            ["ventilation"] = breakdown.VentilationW,
            ["infiltration"] = breakdown.InfiltrationW
        };

        return Dominant(components);
    }

    private static string DominantCoolingComponent(RoomCoolingLoadBreakdown breakdown)
    {
        var components = new Dictionary<string, double>
        {
            ["transmission"] = breakdown.TransmissionW,
            ["windowTransmission"] = breakdown.WindowTransmissionW,
            ["solar"] = breakdown.SolarW,
            ["ventilation"] = breakdown.VentilationW,
            ["infiltration"] = breakdown.InfiltrationW,
            ["internalGains"] = breakdown.InternalGainsW,
            ["ground"] = breakdown.GroundW
        };

        return Dominant(components);
    }

    private static string Dominant(IReadOnlyDictionary<string, double> components) =>
        components
            .OrderByDescending(component => component.Value)
            .ThenBy(component => component.Key, StringComparer.Ordinal)
            .FirstOrDefault(component => component.Value > 0)
            .Key ?? "none";

    private static CalculationDiagnostic Error(string code, string message, string? context) =>
        new(CalculationDiagnosticSeverity.Error, code, message, context);

    private static double Round(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);

    private sealed class MutableHeatingBreakdown
    {
        public double TransmissionW { get; set; }
        public double WindowTransmissionW { get; set; }
        public double GroundW { get; set; }
        public double VentilationW { get; set; }
        public double InfiltrationW { get; set; }

        public RoomHeatingLoadBreakdown ToResult() =>
            new(
                Round(TransmissionW),
                Round(WindowTransmissionW),
                Round(GroundW),
                Round(VentilationW),
                Round(InfiltrationW),
                UsefulSolarGainOffsetW: 0,
                UsefulInternalGainOffsetW: 0);
    }

    private sealed class MutableCoolingBreakdown
    {
        public double TransmissionW { get; set; }
        public double WindowTransmissionW { get; set; }
        public double SolarW { get; set; }
        public double VentilationW { get; set; }
        public double InfiltrationW { get; set; }
        public double InternalGainsW { get; set; }
        public double GroundW { get; set; }

        public RoomCoolingLoadBreakdown ToResult() =>
            new(
                Round(TransmissionW),
                Round(WindowTransmissionW),
                Round(SolarW),
                Round(VentilationW),
                Round(InfiltrationW),
                Round(InternalGainsW),
                Round(GroundW));
    }
}
