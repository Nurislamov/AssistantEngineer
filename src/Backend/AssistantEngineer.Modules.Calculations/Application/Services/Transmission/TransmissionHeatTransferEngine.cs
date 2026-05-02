using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Transmission;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Transmission;

public sealed class TransmissionHeatTransferEngine
{
    private const double MinimumTemperatureC = -100.0;
    private const double MaximumTemperatureC = 100.0;

    public Result<TransmissionHeatTransferResult> Calculate(
        TransmissionHeatTransferRequest request)
    {
        if (request is null)
            return Result<TransmissionHeatTransferResult>.Validation("Transmission heat transfer request is required.");

        if (request.Elements is null)
            return Result<TransmissionHeatTransferResult>.Validation("Transmission heat transfer elements are required.");

        var elements = new List<TransmissionElementResult>(request.Elements.Count);
        var diagnostics = new List<CalculationDiagnostic>();

        foreach (var element in request.Elements)
        {
            var result = CalculateElement(element);

            elements.Add(result);
            diagnostics.AddRange(result.Diagnostics);
        }

        if (request.Elements.Count == 0)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "Transmission.NoElements",
                "No envelope elements were supplied for transmission heat transfer calculation."));
        }

        var included = elements
            .Where(element => element.IsIncludedInLoad)
            .ToArray();

        var totalHeatFlowW = included.Sum(element => element.HeatFlowW);

        var totalHeatLossW = included
            .Where(element => element.HeatFlowW > 0)
            .Sum(element => element.HeatFlowW);

        var totalHeatGainW = included
            .Where(element => element.HeatFlowW < 0)
            .Sum(element => -element.HeatFlowW);

        var totalHeatTransferCoefficientWPerK = included
            .Sum(element => element.AreaM2 * element.UValueWPerM2K);

        return Result<TransmissionHeatTransferResult>.Success(
            new TransmissionHeatTransferResult(
                TotalHeatFlowW: Round(totalHeatFlowW),
                TotalHeatLossW: Round(totalHeatLossW),
                TotalHeatGainW: Round(totalHeatGainW),
                TotalHeatTransferCoefficientWPerK: Round(totalHeatTransferCoefficientWPerK),
                Elements: elements,
                Diagnostics: diagnostics));
    }

    private static TransmissionElementResult CalculateElement(
        TransmissionElementInput element)
    {
        var diagnostics = ValidateElement(element);

        if (diagnostics.Any(diagnostic =>
                diagnostic.Severity == CalculationDiagnosticSeverity.Error))
        {
            return Excluded(element, diagnostics);
        }

        if (element.BoundaryType == TransmissionBoundaryType.InternalAdiabatic)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Info,
                "Transmission.InternalAdiabatic",
                "Element is excluded from transmission load because the boundary is internal adiabatic.",
                element.DiagnosticsContext));

            return new TransmissionElementResult(
                element.ElementId,
                element.ElementType,
                element.RoomId,
                element.BoundaryType,
                element.AreaM2,
                element.UValueWPerM2K,
                DeltaTC: 0,
                HeatFlowW: 0,
                IsIncludedInLoad: false,
                Diagnostics: diagnostics);
        }

        var boundaryTemperature = ResolveBoundaryTemperature(
            element,
            diagnostics);

        if (!boundaryTemperature.HasValue)
            return Excluded(element, diagnostics);

        var correctionFactor = element.CorrectionFactor ?? 1.0;

        if (element.BoundaryType == TransmissionBoundaryType.Ground)
        {
            diagnostics.Add(new CalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "Transmission.GroundSimplified",
                "Ground transmission heat transfer uses the supplied boundary temperature and correction factor.",
                element.DiagnosticsContext));
        }

        var deltaT = element.IndoorTemperatureC - boundaryTemperature.Value;
        var heatFlow = element.UValueWPerM2K * element.AreaM2 * deltaT * correctionFactor;

        return new TransmissionElementResult(
            element.ElementId,
            element.ElementType,
            element.RoomId,
            element.BoundaryType,
            element.AreaM2,
            element.UValueWPerM2K,
            Round(deltaT),
            Round(heatFlow),
            IsIncludedInLoad: true,
            Diagnostics: diagnostics);
    }

    private static List<CalculationDiagnostic> ValidateElement(
        TransmissionElementInput element)
    {
        var diagnostics = new List<CalculationDiagnostic>();

        if (element.AreaM2 <= 0)
        {
            diagnostics.Add(Error(
                "Transmission.InvalidArea",
                "Envelope element area must be greater than 0 m2.",
                element.DiagnosticsContext));
        }

        if (element.UValueWPerM2K <= 0)
        {
            diagnostics.Add(Error(
                "Transmission.InvalidUValue",
                "Envelope element U-value must be greater than 0 W/(m2 K).",
                element.DiagnosticsContext));
        }

        AddTemperatureDiagnostic(
            diagnostics,
            element.IndoorTemperatureC,
            "Transmission.InvalidIndoorTemperature",
            "Indoor temperature is outside the supported calculation range.",
            element.DiagnosticsContext);

        if (element.CorrectionFactor is < 0)
        {
            diagnostics.Add(Error(
                "Transmission.InvalidCorrectionFactor",
                "Transmission correction factor cannot be negative.",
                element.DiagnosticsContext));
        }

        return diagnostics;
    }

    private static double? ResolveBoundaryTemperature(
        TransmissionElementInput element,
        List<CalculationDiagnostic> diagnostics)
    {
        var temperature = element.BoundaryType switch
        {
            TransmissionBoundaryType.Outdoor =>
                element.OutdoorTemperatureC ?? element.BoundaryTemperatureC,

            TransmissionBoundaryType.AdjacentUnheatedSpace =>
                element.AdjacentTemperatureC ?? element.BoundaryTemperatureC,

            TransmissionBoundaryType.AdjacentConditionedZone =>
                element.AdjacentTemperatureC ?? element.BoundaryTemperatureC,

            TransmissionBoundaryType.Ground =>
                element.GroundTemperatureC ?? element.BoundaryTemperatureC,

            TransmissionBoundaryType.InternalAdiabatic =>
                element.IndoorTemperatureC,

            _ => null
        };

        if (!temperature.HasValue)
        {
            diagnostics.Add(Error(
                "Transmission.MissingBoundaryTemperature",
                $"Boundary temperature is required for {element.BoundaryType} transmission heat transfer.",
                element.DiagnosticsContext));

            return null;
        }

        AddTemperatureDiagnostic(
            diagnostics,
            temperature.Value,
            "Transmission.InvalidBoundaryTemperature",
            "Boundary temperature is outside the supported calculation range.",
            element.DiagnosticsContext);

        return diagnostics.Any(diagnostic =>
            diagnostic.Severity == CalculationDiagnosticSeverity.Error &&
            diagnostic.Code == "Transmission.InvalidBoundaryTemperature")
            ? null
            : temperature.Value;
    }

    private static void AddTemperatureDiagnostic(
        ICollection<CalculationDiagnostic> diagnostics,
        double temperatureC,
        string code,
        string message,
        string? context)
    {
        if (temperatureC is < MinimumTemperatureC or > MaximumTemperatureC)
            diagnostics.Add(Error(code, message, context));
    }

    private static TransmissionElementResult Excluded(
        TransmissionElementInput element,
        IReadOnlyList<CalculationDiagnostic> diagnostics) =>
        new(
            element.ElementId,
            element.ElementType,
            element.RoomId,
            element.BoundaryType,
            Math.Max(element.AreaM2, 0),
            Math.Max(element.UValueWPerM2K, 0),
            DeltaTC: 0,
            HeatFlowW: 0,
            IsIncludedInLoad: false,
            Diagnostics: diagnostics);

    private static CalculationDiagnostic Error(
        string code,
        string message,
        string? context) =>
        new(
            CalculationDiagnosticSeverity.Error,
            code,
            message,
            context);

    private static double Round(
        double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}