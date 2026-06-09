using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Guidance;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;

public sealed class EquipmentDiagnosticBotService : IEquipmentDiagnosticBotService
{
    private const string GenericSafetyBoundary =
        "Use this guidance for preliminary review only. Electrical, refrigerant-circuit, and compressor checks require a qualified technician; keep safety protections active.";

    private readonly IEquipmentDiagnosticsService _diagnosticsService;

    public EquipmentDiagnosticBotService(IEquipmentDiagnosticsService diagnosticsService)
    {
        _diagnosticsService = diagnosticsService;
    }

    public async Task<EquipmentDiagnosticBotResponse> DiagnoseAsync(
        EquipmentDiagnosticBotRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var manufacturer = Normalize(request.Manufacturer);
        var code = Normalize(request.Code);
        var trace = new List<string> { "RequestNormalized", "RuntimeCatalogOnly" };

        if (manufacturer.Length == 0 || code.Length == 0)
        {
            trace.Add("RequiredIdentityMissing");
            return NonAnswer(
                EquipmentDiagnosticBotResponseStatus.Unsupported,
                "Equipment identity required",
                "Provide both manufacturer and displayed code before diagnostic matching.",
                manufacturer,
                code,
                request.FreeText,
                ["Confirm the equipment manufacturer.", "Record the displayed code exactly."],
                ["Free text is not used to infer equipment identity in this deterministic flow."],
                trace);
        }

        var matches = await _diagnosticsService.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: request.Manufacturer,
                ErrorCode: request.Code,
                Series: request.Series,
                ModelCode: request.ModelCode,
                Category: request.Category),
            cancellationToken);

        matches = matches
            .Where(match => MatchesEquipmentSide(match.Category, request.EquipmentSide))
            .Where(match => MatchesDisplayContext(match.Category, request.DisplayContext))
            .OrderBy(match => match.Manufacturer, StringComparer.Ordinal)
            .ThenBy(match => match.SeriesName ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(match => match.Category.ToString(), StringComparer.Ordinal)
            .ThenBy(match => match.ModelCode ?? string.Empty, StringComparer.Ordinal)
            .ToArray();

        trace.Add($"RuntimeMatches:{matches.Count}");

        if (matches.Count == 0)
        {
            if (EquipmentDiagnosticBotReferencePolicy.IsControllerModelName(code))
            {
                trace.Add("ControllerModelNameRecognized");
                return NonAnswer(
                    EquipmentDiagnosticBotResponseStatus.Unsupported,
                    "Controller model is not a fault code",
                    "This looks like a controller or commissioning-tool model name, not a confirmed runtime diagnostic case.",
                    manufacturer,
                    code,
                    request.FreeText,
                    ["Confirm the actual code shown by the equipment or controller."],
                    ["Controller model names are not interpreted as equipment faults."],
                    trace);
            }

            if (EquipmentDiagnosticBotReferencePolicy.IsReferenceOnlyCode(code))
            {
                trace.Add("ReferenceOnlyPatternRecognized");
                return NonAnswer(
                    EquipmentDiagnosticBotResponseStatus.ReferenceOnly,
                    "Reference-only code pattern",
                    "This looks like a status, debug, query, setting, or controller code, not a confirmed runtime diagnostic case.",
                    manufacturer,
                    code,
                    request.FreeText,
                    ["Verify the display context and consult the exact equipment service manual."],
                    ["No runtime diagnostic case was used."],
                    trace);
            }

            trace.Add("RuntimeDiagnosticNotFound");
            return NonAnswer(
                EquipmentDiagnosticBotResponseStatus.NotFound,
                "Runtime diagnostic case not found",
                "No runtime diagnostic case found. Verify equipment family, display context, and service manual.",
                manufacturer,
                code,
                request.FreeText,
                ["Confirm the equipment family and model.", "Confirm where the code is displayed.", "Verify against the exact service manual."],
                ["Non-runtime staging, codebook, and preview knowledge is not used as a final diagnosis."],
                trace);
        }

        if (matches.Count > 1)
        {
            trace.Add("ClarificationRequired");
            var options = matches.Select(ToClarificationOption).ToArray();
            return new EquipmentDiagnosticBotResponse(
                EquipmentDiagnosticBotResponseStatus.ClarificationRequired,
                "Equipment context required",
                "The displayed code matches multiple runtime equipment contexts. Select the installed equipment context.",
                manufacturer,
                code,
                EquipmentContext: null,
                new EquipmentDiagnosticBotObservedCodeContext(request.Code?.Trim() ?? string.Empty, code, request.FreeText),
                AnswerCard: null,
                new EquipmentDiagnosticBotClarificationQuestion(
                    "Which equipment context shows this code?",
                    options),
                SourceCard: null,
                Safety(),
                VerificationRequired: true,
                DiagnosticConfidence.Unknown,
                IsManualVerified: false,
                IsSeedKnowledge: false,
                options.Select(option => option.FollowUpPrompt).ToArray(),
                ["Do not select a diagnostic case until the equipment context is confirmed."],
                trace);
        }

        var match = matches[0];
        var diagnosticCase = await _diagnosticsService.GetDiagnosticCaseAsync(
            match.Manufacturer,
            match.Code,
            match.SeriesName,
            match.ModelCode,
            cancellationToken);

        if (diagnosticCase is null)
        {
            trace.Add("RuntimeCaseUnavailable");
            return NonAnswer(
                EquipmentDiagnosticBotResponseStatus.NotFound,
                "Runtime diagnostic case unavailable",
                "A runtime code match was found, but no diagnostic case is available. Verify against the exact service manual.",
                manufacturer,
                code,
                request.FreeText,
                ["Confirm the equipment family and displayed code.", "Escalate for qualified technician review."],
                ["No incomplete runtime match is presented as a diagnosis."],
                trace);
        }

        trace.Add("RuntimeDiagnosticAnswer");
        var guidance = EquipmentDiagnosticOperatorGuidanceFormatter.Format(diagnosticCase);
        return new EquipmentDiagnosticBotResponse(
            EquipmentDiagnosticBotResponseStatus.Answer,
            guidance.Title,
            guidance.Summary,
            manufacturer,
            code,
            ToEquipmentContext(match),
            new EquipmentDiagnosticBotObservedCodeContext(request.Code?.Trim() ?? match.Code, code, request.FreeText),
            new EquipmentDiagnosticBotAnswerCard(
                guidance.Title,
                guidance.Summary,
                guidance.VerificationBanner,
                diagnosticCase.LikelyCauses.ToArray(),
                diagnosticCase.DiagnosticSteps.ToArray(),
                diagnosticCase.RequiredMeasurements.ToArray(),
                guidance.RecommendedChecks.ToArray(),
                guidance.OperatorNotes.ToArray()),
            ClarificationQuestion: null,
            new EquipmentDiagnosticBotSourceCard(
                diagnosticCase.Source.SourceType,
                diagnosticCase.Source.EvidenceLevel,
                diagnosticCase.SourceSummary,
                diagnosticCase.Source.ManualTitle,
                diagnosticCase.Source.ManualVersion,
                diagnosticCase.Source.ManualDocumentCode,
                diagnosticCase.Source.Page,
                diagnosticCase.Source.Section,
                diagnosticCase.Source.Limitations.ToArray()),
            new EquipmentDiagnosticBotSafetyCard(
                diagnosticCase.SafetyBoundary,
                diagnosticCase.SafetyNotes.ToArray()),
            diagnosticCase.VerificationRequired,
            diagnosticCase.Confidence,
            diagnosticCase.IsManualVerified,
            diagnosticCase.IsSeedKnowledge,
            diagnosticCase.RecommendedNextChecks.ToArray(),
            BuildWarnings(diagnosticCase),
            trace);
    }

    private static EquipmentDiagnosticBotResponse NonAnswer(
        EquipmentDiagnosticBotResponseStatus status,
        string title,
        string message,
        string manufacturer,
        string code,
        string? freeText,
        IReadOnlyList<string> nextSteps,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> trace) =>
        new(
            status,
            title,
            message,
            manufacturer,
            code,
            EquipmentContext: null,
            new EquipmentDiagnosticBotObservedCodeContext(code, code, freeText),
            AnswerCard: null,
            ClarificationQuestion: null,
            SourceCard: null,
            Safety(),
            VerificationRequired: true,
            DiagnosticConfidence.Unknown,
            IsManualVerified: false,
            IsSeedKnowledge: false,
            nextSteps,
            warnings,
            trace);

    private static EquipmentDiagnosticBotClarificationOption ToClarificationOption(EquipmentErrorCodeSummaryDto match)
    {
        var side = Side(match.Category);
        var display = DisplayContext(match.Category);
        var family = match.SeriesName ?? match.Category.ToString();
        var label = $"{match.Manufacturer} {family} ({side})";
        return new EquipmentDiagnosticBotClarificationOption(
            label,
            match.Manufacturer,
            match.SeriesName,
            match.Category,
            side,
            display,
            match.Code,
            $"{match.Code} is present in the runtime catalog for {family} {side}.",
            $"Confirm {label} and resend {match.Code} with that context.");
    }

    private static EquipmentDiagnosticBotEquipmentContext ToEquipmentContext(EquipmentErrorCodeSummaryDto match) =>
        new(match.Manufacturer, match.SeriesName, match.ModelCode, match.Category, Side(match.Category), DisplayContext(match.Category));

    private static EquipmentDiagnosticBotSafetyCard Safety() =>
        new(GenericSafetyBoundary, ["Stop and escalate when equipment identity, measurements, or safe access cannot be confirmed."]);

    private static IReadOnlyList<string> BuildWarnings(EquipmentDiagnosticCaseDto diagnosticCase)
    {
        var warnings = new List<string>();
        if (diagnosticCase.IsSeedKnowledge)
            warnings.Add("Seed knowledge requires verification against the exact installed equipment service manual.");
        else if (diagnosticCase.VerificationRequired)
            warnings.Add("Verification is required before final conclusion.");
        return warnings;
    }

    private static bool MatchesEquipmentSide(EquipmentCategory category, EquipmentDiagnosticBotEquipmentSide? requested) =>
        requested is null or EquipmentDiagnosticBotEquipmentSide.Unknown || Side(category) == requested;

    private static bool MatchesDisplayContext(EquipmentCategory category, EquipmentDiagnosticBotDisplayContext? requested) =>
        requested is null or EquipmentDiagnosticBotDisplayContext.Unknown || DisplayContext(category) == requested;

    private static EquipmentDiagnosticBotEquipmentSide Side(EquipmentCategory category) => category switch
    {
        EquipmentCategory.VrfIndoorUnit => EquipmentDiagnosticBotEquipmentSide.Indoor,
        EquipmentCategory.VrfOutdoorUnit => EquipmentDiagnosticBotEquipmentSide.Outdoor,
        EquipmentCategory.Chiller => EquipmentDiagnosticBotEquipmentSide.Chiller,
        EquipmentCategory.Controller => EquipmentDiagnosticBotEquipmentSide.Controller,
        _ => EquipmentDiagnosticBotEquipmentSide.Unknown
    };

    private static EquipmentDiagnosticBotDisplayContext DisplayContext(EquipmentCategory category) => category switch
    {
        EquipmentCategory.VrfIndoorUnit => EquipmentDiagnosticBotDisplayContext.IduDisplay,
        EquipmentCategory.VrfOutdoorUnit => EquipmentDiagnosticBotDisplayContext.OduMainBoardLed,
        EquipmentCategory.Controller => EquipmentDiagnosticBotDisplayContext.WiredController,
        _ => EquipmentDiagnosticBotDisplayContext.Unknown
    };

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
}
