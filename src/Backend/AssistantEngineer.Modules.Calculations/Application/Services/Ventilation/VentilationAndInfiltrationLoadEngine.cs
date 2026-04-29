using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

public sealed class VentilationAndInfiltrationLoadEngine
{
    private const double MinimumTemperatureC = -100.0;
    private const double MaximumTemperatureC = 100.0;

    public Result<VentilationAndInfiltrationLoadResult> Calculate(
        VentilationAndInfiltrationLoadInput input)
    {
        if (input is null)
            return Result<VentilationAndInfiltrationLoadResult>.Validation("Ventilation and infiltration load input is required.");

        var diagnostics = Validate(input);
        var airDensity = input.AirDensityKgPerM3 ?? AirPhysicalConstants.AirDensityKgPerM3;
        var airSpecificHeat = input.AirSpecificHeatJPerKgK ?? AirPhysicalConstants.AirSpecificHeatJPerKgK;

        if (!input.AirDensityKgPerM3.HasValue || !input.AirSpecificHeatJPerKgK.HasValue)
        {
            diagnostics.Add(new VentilationLoadDiagnostic(
                VentilationLoadDiagnosticSeverity.Info,
                "Ventilation.AirConstantsUsed",
                $"Air constants used: density {airDensity} kg/m3, specific heat {airSpecificHeat} J/(kg K).",
                input.DiagnosticsContext));
        }

        var mechanicalAirflow = ResolveMechanicalAirflow(input, diagnostics);
        var infiltrationAirflow = ResolveInfiltrationAirflow(input, diagnostics);
        var naturalAirflow = ResolveNaturalVentilationAirflow(input, diagnostics);

        if (diagnostics.Any(diagnostic => diagnostic.Severity == VentilationLoadDiagnosticSeverity.Error))
        {
            return Result<VentilationAndInfiltrationLoadResult>.Success(
                CreateResult(
                    input,
                    airDensity,
                    airSpecificHeat,
                    MechanicalVentilationLoadResultZero(input.HeatRecoveryEfficiency ?? 0),
                    InfiltrationLoadResultZero(),
                    NaturalVentilationLoadResultZero(),
                    diagnostics));
        }

        var mechanical = CalculateMechanical(
            input,
            airDensity,
            airSpecificHeat,
            mechanicalAirflow);
        var infiltration = CalculateInfiltration(
            input,
            airDensity,
            airSpecificHeat,
            infiltrationAirflow);
        var natural = CalculateNaturalVentilation(
            input,
            airDensity,
            airSpecificHeat,
            naturalAirflow);

        return Result<VentilationAndInfiltrationLoadResult>.Success(
            CreateResult(
                input,
                airDensity,
                airSpecificHeat,
                mechanical,
                infiltration,
                natural,
                diagnostics));
    }

    private static VentilationAndInfiltrationLoadResult CreateResult(
        VentilationAndInfiltrationLoadInput input,
        double airDensity,
        double airSpecificHeat,
        MechanicalVentilationLoadResult mechanical,
        InfiltrationLoadResult infiltration,
        NaturalVentilationLoadResult natural,
        IReadOnlyList<VentilationLoadDiagnostic> diagnostics)
    {
        var totalHeating =
            mechanical.EffectiveHeatingLoadW +
            infiltration.HeatingLoadW +
            natural.HeatingLoadW;
        var totalCooling =
            mechanical.EffectiveCoolingLoadW +
            infiltration.CoolingLoadW +
            natural.CoolingLoadW;

        return new VentilationAndInfiltrationLoadResult(
            input.RoomId,
            input.IndoorTemperatureC,
            input.OutdoorTemperatureC,
            DeltaTC: Round(input.IndoorTemperatureC - input.OutdoorTemperatureC),
            Round(airDensity),
            Round(airSpecificHeat),
            mechanical,
            infiltration,
            natural,
            Round(totalHeating),
            Round(totalCooling),
            SignedHeatFlowW: Round(totalHeating - totalCooling),
            diagnostics);
    }

    private static MechanicalVentilationLoadResult CalculateMechanical(
        VentilationAndInfiltrationLoadInput input,
        double airDensity,
        double airSpecificHeat,
        AirflowResult airflow)
    {
        var loads = CalculateHeatingCoolingLoads(
            input,
            airflow.AirflowM3PerSecond,
            airDensity,
            airSpecificHeat);
        var heatRecoveryEfficiency = input.HeatRecoveryEfficiency ?? 0.0;
        var heatRecoveryFactor = 1.0 - heatRecoveryEfficiency;

        return new MechanicalVentilationLoadResult(
            Round(airflow.AirflowM3PerHour),
            Round(airflow.AirflowM3PerSecond),
            Round(loads.HeatingLoadW),
            Round(loads.CoolingLoadW),
            Round(heatRecoveryEfficiency),
            Round(loads.HeatingLoadW * heatRecoveryFactor),
            Round(loads.CoolingLoadW * heatRecoveryFactor));
    }

    private static InfiltrationLoadResult CalculateInfiltration(
        VentilationAndInfiltrationLoadInput input,
        double airDensity,
        double airSpecificHeat,
        AirflowResult airflow)
    {
        var loads = CalculateHeatingCoolingLoads(
            input,
            airflow.AirflowM3PerSecond,
            airDensity,
            airSpecificHeat);

        return new InfiltrationLoadResult(
            Round(airflow.AirChangesPerHour),
            Round(airflow.AirflowM3PerHour),
            Round(airflow.AirflowM3PerSecond),
            Round(loads.HeatingLoadW),
            Round(loads.CoolingLoadW));
    }

    private static NaturalVentilationLoadResult CalculateNaturalVentilation(
        VentilationAndInfiltrationLoadInput input,
        double airDensity,
        double airSpecificHeat,
        AirflowResult airflow)
    {
        var loads = CalculateHeatingCoolingLoads(
            input,
            airflow.AirflowM3PerSecond,
            airDensity,
            airSpecificHeat);

        return new NaturalVentilationLoadResult(
            Round(airflow.AirflowM3PerHour),
            Round(airflow.AirflowM3PerSecond),
            Round(loads.HeatingLoadW),
            Round(loads.CoolingLoadW));
    }

    private static HeatingCoolingLoad CalculateHeatingCoolingLoads(
        VentilationAndInfiltrationLoadInput input,
        double airflowM3PerSecond,
        double airDensity,
        double airSpecificHeat)
    {
        var airflowLoadPerK = airDensity * airSpecificHeat * airflowM3PerSecond;
        var deltaHeating = Math.Max(input.IndoorTemperatureC - input.OutdoorTemperatureC, 0.0);
        var deltaCooling = Math.Max(input.OutdoorTemperatureC - input.IndoorTemperatureC, 0.0);

        return new HeatingCoolingLoad(
            airflowLoadPerK * deltaHeating,
            airflowLoadPerK * deltaCooling);
    }

    private static AirflowResult ResolveMechanicalAirflow(
        VentilationAndInfiltrationLoadInput input,
        List<VentilationLoadDiagnostic> diagnostics)
    {
        var airflowM3PerHour = 0.0;
        var hasMechanicalInput = false;

        AddM3PerHour(input.MechanicalAirflowM3PerHour, "Ventilation.MechanicalAirflowM3PerHour", ref airflowM3PerHour, ref hasMechanicalInput, diagnostics, input.DiagnosticsContext);
        AddLitersPerSecond(input.AirflowLitersPerSecond, "Ventilation.AirflowLitersPerSecond", ref airflowM3PerHour, ref hasMechanicalInput, diagnostics, input.DiagnosticsContext);

        if (input.AirflowPerPersonLps.HasValue)
        {
            var lps = AirflowNormalizer.AirflowPerPersonToLitersPerSecond(
                input.AirflowPerPersonLps.Value,
                input.OccupancyPeople);
            AddConvertedLitersPerSecond(lps, "Ventilation.AirflowPerPerson", ref airflowM3PerHour, ref hasMechanicalInput, diagnostics, input.DiagnosticsContext);
        }

        if (input.AirflowPerAreaLpsM2.HasValue)
        {
            var lps = AirflowNormalizer.AirflowPerAreaToLitersPerSecond(
                input.AirflowPerAreaLpsM2.Value,
                input.AreaM2);
            AddConvertedLitersPerSecond(lps, "Ventilation.AirflowPerArea", ref airflowM3PerHour, ref hasMechanicalInput, diagnostics, input.DiagnosticsContext);
        }

        if (input.AirChangesPerHour.HasValue)
        {
            var m3PerHour = AirflowNormalizer.AirChangesPerHourToM3PerHour(
                input.VolumeM3,
                input.AirChangesPerHour.Value);
            AddConvertedM3PerHour(m3PerHour, "Ventilation.AirChangesPerHour", ref airflowM3PerHour, ref hasMechanicalInput, diagnostics, input.DiagnosticsContext);
        }

        if (!hasMechanicalInput)
        {
            diagnostics.Add(new VentilationLoadDiagnostic(
                VentilationLoadDiagnosticSeverity.Warning,
                "Ventilation.NoMechanicalAirflow",
                "No mechanical ventilation airflow was supplied; mechanical ventilation load is zero.",
                input.DiagnosticsContext));
        }

        airflowM3PerHour *= input.ScheduleFactor;
        return CreateAirflowResult(input.VolumeM3, airflowM3PerHour);
    }

    private static AirflowResult ResolveInfiltrationAirflow(
        VentilationAndInfiltrationLoadInput input,
        List<VentilationLoadDiagnostic> diagnostics)
    {
        var airflowM3PerHour = 0.0;
        var hasInfiltrationInput = false;

        AddM3PerHour(input.InfiltrationAirflowM3PerHour, "Ventilation.InfiltrationAirflowM3PerHour", ref airflowM3PerHour, ref hasInfiltrationInput, diagnostics, input.DiagnosticsContext);

        if (input.InfiltrationAirChangesPerHour.HasValue)
        {
            var m3PerHour = AirflowNormalizer.AirChangesPerHourToM3PerHour(
                input.VolumeM3,
                input.InfiltrationAirChangesPerHour.Value);
            AddConvertedM3PerHour(m3PerHour, "Ventilation.InfiltrationAirChangesPerHour", ref airflowM3PerHour, ref hasInfiltrationInput, diagnostics, input.DiagnosticsContext);
        }

        if (!hasInfiltrationInput)
        {
            diagnostics.Add(new VentilationLoadDiagnostic(
                VentilationLoadDiagnosticSeverity.Warning,
                "Ventilation.NoInfiltrationAirflow",
                "No infiltration airflow was supplied; no infiltration load is assumed.",
                input.DiagnosticsContext));
        }

        return CreateAirflowResult(input.VolumeM3, airflowM3PerHour);
    }

    private static AirflowResult ResolveNaturalVentilationAirflow(
        VentilationAndInfiltrationLoadInput input,
        List<VentilationLoadDiagnostic> diagnostics)
    {
        var airflowM3PerHour = 0.0;
        var hasNaturalInput = false;

        AddM3PerHour(input.NaturalVentilationAirflowM3PerHour, "Ventilation.NaturalVentilationAirflowM3PerHour", ref airflowM3PerHour, ref hasNaturalInput, diagnostics, input.DiagnosticsContext);

        if (!hasNaturalInput)
        {
            diagnostics.Add(new VentilationLoadDiagnostic(
                VentilationLoadDiagnosticSeverity.Info,
                "Ventilation.NoNaturalVentilationAirflow",
                "No natural ventilation airflow was supplied; natural ventilation load is zero.",
                input.DiagnosticsContext));
        }

        return CreateAirflowResult(input.VolumeM3, airflowM3PerHour);
    }

    private static AirflowResult CreateAirflowResult(
        double roomVolumeM3,
        double airflowM3PerHour)
    {
        var airflowM3PerSecond = AirflowNormalizer.M3PerHourToM3PerSecond(
            Math.Max(airflowM3PerHour, 0.0)).Value;
        var airChangesPerHour = roomVolumeM3 > 0
            ? airflowM3PerHour / roomVolumeM3
            : 0.0;

        return new AirflowResult(
            airflowM3PerHour,
            airflowM3PerSecond,
            airChangesPerHour);
    }

    private static List<VentilationLoadDiagnostic> Validate(
        VentilationAndInfiltrationLoadInput input)
    {
        var diagnostics = new List<VentilationLoadDiagnostic>();

        if (input.AreaM2 <= 0)
            diagnostics.Add(Error("Ventilation.InvalidArea", "Room area must be greater than zero.", input.DiagnosticsContext));

        if (input.VolumeM3 <= 0)
            diagnostics.Add(Error("Ventilation.InvalidVolume", "Room volume must be greater than zero.", input.DiagnosticsContext));

        if (input.OccupancyPeople < 0)
            diagnostics.Add(Error("Ventilation.InvalidOccupancy", "People count cannot be negative.", input.DiagnosticsContext));

        if (input.IndoorTemperatureC is < MinimumTemperatureC or > MaximumTemperatureC)
            diagnostics.Add(Error("Ventilation.InvalidIndoorTemperature", "Indoor temperature is outside the supported calculation range.", input.DiagnosticsContext));

        if (input.OutdoorTemperatureC is < MinimumTemperatureC or > MaximumTemperatureC)
            diagnostics.Add(Error("Ventilation.InvalidOutdoorTemperature", "Outdoor temperature is outside the supported calculation range.", input.DiagnosticsContext));

        if (input.HeatRecoveryEfficiency is < 0.0 or > 1.0)
            diagnostics.Add(Error("Ventilation.InvalidHeatRecoveryEfficiency", "Heat recovery efficiency must be between 0 and 1.", input.DiagnosticsContext));

        if (input.ScheduleFactor is < 0.0 or > 1.0)
            diagnostics.Add(Error("Ventilation.InvalidScheduleFactor", "Schedule factor must be between 0 and 1.", input.DiagnosticsContext));

        if (input.AirDensityKgPerM3 is <= 0)
            diagnostics.Add(Error("Ventilation.InvalidAirDensity", "Air density must be greater than zero.", input.DiagnosticsContext));

        if (input.AirSpecificHeatJPerKgK is <= 0)
            diagnostics.Add(Error("Ventilation.InvalidAirSpecificHeat", "Air specific heat must be greater than zero.", input.DiagnosticsContext));

        return diagnostics;
    }

    private static void AddM3PerHour(
        double? airflowM3PerHour,
        string code,
        ref double totalAirflowM3PerHour,
        ref bool hasInput,
        List<VentilationLoadDiagnostic> diagnostics,
        string? context)
    {
        if (!airflowM3PerHour.HasValue)
            return;

        if (airflowM3PerHour.Value < 0)
        {
            diagnostics.Add(Error(code, "Airflow cannot be negative.", context));
            return;
        }

        totalAirflowM3PerHour += airflowM3PerHour.Value;
        hasInput = true;
    }

    private static void AddLitersPerSecond(
        double? airflowLitersPerSecond,
        string code,
        ref double totalAirflowM3PerHour,
        ref bool hasInput,
        List<VentilationLoadDiagnostic> diagnostics,
        string? context)
    {
        if (!airflowLitersPerSecond.HasValue)
            return;

        var m3PerHour = AirflowNormalizer.LitersPerSecondToM3PerHour(
            airflowLitersPerSecond.Value);
        AddConvertedM3PerHour(m3PerHour, code, ref totalAirflowM3PerHour, ref hasInput, diagnostics, context);
    }

    private static void AddConvertedLitersPerSecond(
        Result<double> airflowLitersPerSecond,
        string code,
        ref double totalAirflowM3PerHour,
        ref bool hasInput,
        List<VentilationLoadDiagnostic> diagnostics,
        string? context)
    {
        if (airflowLitersPerSecond.IsFailure)
        {
            diagnostics.Add(Error(code, airflowLitersPerSecond.Error, context));
            return;
        }

        AddLitersPerSecond(airflowLitersPerSecond.Value, code, ref totalAirflowM3PerHour, ref hasInput, diagnostics, context);
    }

    private static void AddConvertedM3PerHour(
        Result<double> airflowM3PerHour,
        string code,
        ref double totalAirflowM3PerHour,
        ref bool hasInput,
        List<VentilationLoadDiagnostic> diagnostics,
        string? context)
    {
        if (airflowM3PerHour.IsFailure)
        {
            diagnostics.Add(Error(code, airflowM3PerHour.Error, context));
            return;
        }

        totalAirflowM3PerHour += airflowM3PerHour.Value;
        hasInput = true;
    }

    private static MechanicalVentilationLoadResult MechanicalVentilationLoadResultZero(
        double heatRecoveryEfficiency) =>
        new(
            AirflowM3PerHour: 0,
            AirflowM3PerSecond: 0,
            RawHeatingLoadW: 0,
            RawCoolingLoadW: 0,
            Math.Clamp(heatRecoveryEfficiency, 0, 1),
            EffectiveHeatingLoadW: 0,
            EffectiveCoolingLoadW: 0);

    private static InfiltrationLoadResult InfiltrationLoadResultZero() =>
        new(
            InfiltrationAirChangesPerHour: 0,
            InfiltrationAirflowM3PerHour: 0,
            InfiltrationAirflowM3PerSecond: 0,
            HeatingLoadW: 0,
            CoolingLoadW: 0);

    private static NaturalVentilationLoadResult NaturalVentilationLoadResultZero() =>
        new(
            AirflowM3PerHour: 0,
            AirflowM3PerSecond: 0,
            HeatingLoadW: 0,
            CoolingLoadW: 0);

    private static VentilationLoadDiagnostic Error(
        string code,
        string message,
        string? context) =>
        new(VentilationLoadDiagnosticSeverity.Error, code, message, context);

    private static double Round(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);

    private sealed record AirflowResult(
        double AirflowM3PerHour,
        double AirflowM3PerSecond,
        double AirChangesPerHour);

    private sealed record HeatingCoolingLoad(
        double HeatingLoadW,
        double CoolingLoadW);
}
