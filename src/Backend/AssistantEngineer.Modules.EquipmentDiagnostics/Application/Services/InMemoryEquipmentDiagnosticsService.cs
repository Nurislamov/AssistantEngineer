using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;

public sealed class InMemoryEquipmentDiagnosticsService : IEquipmentDiagnosticsService
{
    private static readonly EquipmentManufacturer Gree = new(
        Id: "gree",
        Name: "Gree",
        NormalizedName: NormalizeRequired("Gree"));

    private static readonly ManualReference UnverifiedGreeServiceManualReference = new(
        Manufacturer: "Gree",
        ManualTitle: "Gree service manual for the matching series and model",
        ManualVersion: null,
        Page: null,
        Notes: "ED-00 deterministic seed only; exact manual page is not verified in this repository.");

    private static readonly IReadOnlyList<DiagnosticCase> SeedCases = BuildSeedCases();

    public Task<IReadOnlyList<EquipmentErrorCodeSummaryDto>> SearchErrorCodesAsync(
        SearchEquipmentErrorCodesQuery query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var manufacturer = Normalize(query.Manufacturer);
        var errorCode = Normalize(query.ErrorCode);
        var series = Normalize(query.Series);
        var modelCode = Normalize(query.ModelCode);

        var results = SeedCases
            .Where(diagnosticCase => Matches(diagnosticCase, manufacturer, errorCode, series, modelCode, query.Category))
            .Select(diagnosticCase => MapSummary(diagnosticCase.ErrorCode))
            .ToArray();

        return Task.FromResult<IReadOnlyList<EquipmentErrorCodeSummaryDto>>(results);
    }

    public Task<EquipmentDiagnosticCaseDto?> GetDiagnosticCaseAsync(
        string manufacturer,
        string errorCode,
        string? series,
        string? modelCode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedManufacturer = Normalize(manufacturer);
        var normalizedErrorCode = Normalize(errorCode);
        var normalizedSeries = Normalize(series);
        var normalizedModelCode = Normalize(modelCode);

        var result = SeedCases
            .Where(diagnosticCase => Matches(
                diagnosticCase,
                normalizedManufacturer,
                normalizedErrorCode,
                normalizedSeries,
                normalizedModelCode,
                category: null))
            .Select(MapCase)
            .FirstOrDefault();

        return Task.FromResult(result);
    }

    private static bool Matches(
        DiagnosticCase diagnosticCase,
        string? manufacturer,
        string? errorCode,
        string? series,
        string? modelCode,
        EquipmentCategory? category)
    {
        var code = diagnosticCase.ErrorCode;

        if (!string.IsNullOrWhiteSpace(manufacturer) &&
            code.Manufacturer.NormalizedName != manufacturer)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(errorCode) &&
            code.NormalizedCode != errorCode)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(series) &&
            Normalize(code.SeriesName) != series)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(modelCode) &&
            Normalize(code.ModelCode) != modelCode)
        {
            return false;
        }

        return category is null || GetCategory(code) == category;
    }

    private static IReadOnlyList<DiagnosticCase> BuildSeedCases() =>
    [
        CreateGreeGmvCase(
            code: "H5",
            title: "GMV protection alarm H5",
            meaning: "Preliminary diagnostic entry for a Gree GMV H5 alarm. Verify the exact meaning against the service manual for the installed model before concluding.",
            severity: "Service attention required",
            confidence: DiagnosticConfidence.Low,
            likelyCauses:
            [
                "Outdoor unit protection condition reported by the control board.",
                "Abnormal supply voltage, phase condition, or wiring issue.",
                "Compressor or inverter drive protection condition requiring qualified diagnosis."
            ],
            diagnosticSteps:
            [
                new DiagnosticStep(
                    Order: 1,
                    Title: "Confirm installed equipment identity",
                    Instruction: "Record the outdoor unit model, GMV series, serial plate data, and controller-displayed error code.",
                    ExpectedResult: "Manufacturer, series, model, and displayed H5 code are confirmed before diagnosis continues.",
                    IfFailedAction: "Stop classification and obtain the exact model information."),
                new DiagnosticStep(
                    Order: 2,
                    Title: "Check electrical supply condition",
                    Instruction: "A qualified technician should measure supply voltage and phase condition at the service point using approved procedures.",
                    ExpectedResult: "Supply values are within the equipment nameplate and local electrical requirements.",
                    IfFailedAction: "Correct the electrical supply issue through qualified service before resetting operation."),
                new DiagnosticStep(
                    Order: 3,
                    Title: "Inspect protection-related wiring",
                    Instruction: "Inspect compressor, inverter, control-board, and protection-sensor connections for visible damage, loose terminals, or water ingress.",
                    ExpectedResult: "Connections are secure and no visible damage is found.",
                    IfFailedAction: "Repair damaged wiring or components according to the manufacturer procedure.")
            ],
            measurements:
            [
                new RequiredMeasurement(
                    Name: "Supply voltage",
                    Unit: "V",
                    Description: "Measured line voltage and phase balance at the unit service point.",
                    RequiredBeforeConclusion: true),
                new RequiredMeasurement(
                    Name: "Operating current",
                    Unit: "A",
                    Description: "Measured compressor or inverter input current where the service procedure requires it.",
                    RequiredBeforeConclusion: true)
            ]),

        CreateGreeGmvCase(
            code: "C7",
            title: "GMV communication or configuration alarm C7",
            meaning: "Preliminary diagnostic entry for a Gree GMV C7 alarm. Confirm the exact model-specific meaning from the applicable manual.",
            severity: "Service attention required",
            confidence: DiagnosticConfidence.Low,
            likelyCauses:
            [
                "Indoor/outdoor communication fault or address/configuration mismatch.",
                "Damaged communication wiring or incorrect polarity.",
                "Controller or board communication issue requiring qualified service."
            ],
            diagnosticSteps:
            [
                new DiagnosticStep(
                    Order: 1,
                    Title: "Confirm network scope",
                    Instruction: "Record which controller, indoor unit, and outdoor unit display the C7 alarm.",
                    ExpectedResult: "The affected communication segment is identified.",
                    IfFailedAction: "Map the connected units before replacing parts."),
                new DiagnosticStep(
                    Order: 2,
                    Title: "Inspect communication wiring",
                    Instruction: "A qualified technician should inspect communication terminals, polarity, shielding, and continuity according to the installation manual.",
                    ExpectedResult: "Communication wiring is intact and connected to the correct terminals.",
                    IfFailedAction: "Correct wiring defects and re-test communication."),
                new DiagnosticStep(
                    Order: 3,
                    Title: "Check addressing and configuration",
                    Instruction: "Verify controller addressing and unit configuration against the commissioning records and service manual.",
                    ExpectedResult: "Addresses and configuration match the installed system topology.",
                    IfFailedAction: "Correct configuration through the supported commissioning procedure.")
            ],
            measurements:
            [
                new RequiredMeasurement(
                    Name: "Communication bus voltage",
                    Unit: "V",
                    Description: "Measured at the communication terminals when required by the service manual.",
                    RequiredBeforeConclusion: true),
                new RequiredMeasurement(
                    Name: "Cable continuity",
                    Unit: "Ohm",
                    Description: "Continuity and short-circuit check for the communication cable with power isolated.",
                    RequiredBeforeConclusion: true)
            ]),

        CreateGreeChillerCase()
    ];

    private static DiagnosticCase CreateGreeGmvCase(
        string code,
        string title,
        string meaning,
        string severity,
        DiagnosticConfidence confidence,
        IReadOnlyList<string> likelyCauses,
        IReadOnlyList<DiagnosticStep> diagnosticSteps,
        IReadOnlyList<RequiredMeasurement> measurements)
    {
        var errorCode = new EquipmentErrorCode(
            Manufacturer: Gree,
            SeriesName: "GMV",
            ModelCode: null,
            Code: code,
            NormalizedCode: NormalizeRequired(code),
            Title: title,
            Meaning: meaning,
            Severity: severity,
            Confidence: confidence,
            SourceManual: UnverifiedGreeServiceManualReference);

        return new DiagnosticCase(
            ErrorCode: errorCode,
            LikelyCauses: likelyCauses,
            DiagnosticSteps: diagnosticSteps,
            RequiredMeasurements: measurements,
            SafetyNotes: StandardSafetyNotes,
            ManualReferences: [UnverifiedGreeServiceManualReference],
            Confidence: confidence);
    }

    private static DiagnosticCase CreateGreeChillerCase()
    {
        var errorCode = new EquipmentErrorCode(
            Manufacturer: Gree,
            SeriesName: "Chiller",
            ModelCode: null,
            Code: "E6",
            NormalizedCode: NormalizeRequired("E6"),
            Title: "Chiller protection alarm E6",
            Meaning: "Preliminary diagnostic entry for a Gree chiller E6 alarm. Verify exact meaning and limits in the service manual for the installed chiller model.",
            Severity: "Service attention required",
            Confidence: DiagnosticConfidence.Low,
            SourceManual: UnverifiedGreeServiceManualReference);

        return new DiagnosticCase(
            ErrorCode: errorCode,
            LikelyCauses:
            [
                "Protection input active because a measured operating condition is outside allowed range.",
                "Water-flow, temperature-sensor, pressure, or electrical condition requiring model-specific confirmation.",
                "Control board input or field wiring issue."
            ],
            DiagnosticSteps:
            [
                new DiagnosticStep(
                    Order: 1,
                    Title: "Confirm model and operating mode",
                    Instruction: "Record the chiller model, current operating mode, displayed E6 code, and any secondary controller diagnostics.",
                    ExpectedResult: "The installed model and active diagnostic state are documented.",
                    IfFailedAction: "Do not classify the fault until the installed model and controller state are known."),
                new DiagnosticStep(
                    Order: 2,
                    Title: "Check water-side operating condition",
                    Instruction: "A qualified technician should confirm water pump status, flow indication, strainer condition, and leaving/entering water temperatures.",
                    ExpectedResult: "Water-side flow and temperatures are within the expected service range for the operating mode.",
                    IfFailedAction: "Resolve water-side restrictions or pump faults using the approved service process."),
                new DiagnosticStep(
                    Order: 3,
                    Title: "Inspect protection inputs",
                    Instruction: "Inspect relevant protection input wiring and sensors according to the exact chiller service manual.",
                    ExpectedResult: "Protection input wiring and sensors are intact and readings are plausible.",
                    IfFailedAction: "Repair wiring or sensor faults; replace components only after measurements support the conclusion.")
            ],
            RequiredMeasurements:
            [
                new RequiredMeasurement(
                    Name: "Entering and leaving water temperature",
                    Unit: "degC",
                    Description: "Measured water temperatures used to assess flow and operating condition.",
                    RequiredBeforeConclusion: true),
                new RequiredMeasurement(
                    Name: "Water flow indication",
                    Unit: "Manufacturer-specific",
                    Description: "Flow switch, differential pressure, or flow-meter indication required by the installed chiller design.",
                    RequiredBeforeConclusion: true),
                new RequiredMeasurement(
                    Name: "Supply voltage",
                    Unit: "V",
                    Description: "Measured supply voltage at the chiller service point by qualified personnel.",
                    RequiredBeforeConclusion: true)
            ],
            SafetyNotes: StandardSafetyNotes,
            ManualReferences: [UnverifiedGreeServiceManualReference],
            Confidence: DiagnosticConfidence.Low);
    }

    private static IReadOnlyList<string> StandardSafetyNotes =>
    [
        "Electrical, compressor, inverter, refrigerant, and chiller protection checks must be performed by a qualified technician.",
        "Do not bypass safety switches, protection inputs, current protection, pressure protection, flow protection, or controller safeguards.",
        "De-energize and lock out equipment where the service procedure requires it, and follow the manufacturer manual and local safety rules."
    ];

    private static EquipmentDiagnosticCaseDto MapCase(DiagnosticCase diagnosticCase) =>
        new(
            ErrorCode: MapSummary(diagnosticCase.ErrorCode),
            LikelyCauses: diagnosticCase.LikelyCauses,
            DiagnosticSteps: diagnosticCase.DiagnosticSteps
                .OrderBy(step => step.Order)
                .Select(step => new DiagnosticStepDto(
                    step.Order,
                    step.Title,
                    step.Instruction,
                    step.ExpectedResult,
                    step.IfFailedAction))
                .ToArray(),
            RequiredMeasurements: diagnosticCase.RequiredMeasurements
                .Select(measurement => new RequiredMeasurementDto(
                    measurement.Name,
                    measurement.Unit,
                    measurement.Description,
                    measurement.RequiredBeforeConclusion))
                .ToArray(),
            SafetyNotes: diagnosticCase.SafetyNotes,
            ManualReferences: diagnosticCase.ManualReferences
                .Select(MapManualReference)
                .ToArray(),
            Confidence: diagnosticCase.Confidence);

    private static EquipmentErrorCodeSummaryDto MapSummary(EquipmentErrorCode errorCode) =>
        new(
            Manufacturer: errorCode.Manufacturer.Name,
            SeriesName: errorCode.SeriesName,
            ModelCode: errorCode.ModelCode,
            Code: errorCode.Code,
            Title: errorCode.Title,
            Meaning: errorCode.Meaning,
            Severity: errorCode.Severity,
            Category: GetCategory(errorCode),
            Confidence: errorCode.Confidence,
            SourceManual: MapManualReference(errorCode.SourceManual));

    private static ManualReferenceDto MapManualReference(ManualReference reference) =>
        new(
            Manufacturer: reference.Manufacturer,
            ManualTitle: reference.ManualTitle,
            ManualVersion: reference.ManualVersion,
            Page: reference.Page,
            Notes: reference.Notes);

    private static EquipmentCategory GetCategory(EquipmentErrorCode errorCode) =>
        Normalize(errorCode.SeriesName) switch
        {
            "GMV" => EquipmentCategory.VrfOutdoorUnit,
            "CHILLER" => EquipmentCategory.Chiller,
            _ => EquipmentCategory.Unknown
        };

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedCharacters = value
            .Where(character => !char.IsWhiteSpace(character))
            .Select(char.ToUpperInvariant)
            .ToArray();

        return new string(normalizedCharacters);
    }

    private static string NormalizeRequired(string value) =>
        Normalize(value) ?? throw new ArgumentException("Value must contain at least one non-whitespace character.", nameof(value));
}
