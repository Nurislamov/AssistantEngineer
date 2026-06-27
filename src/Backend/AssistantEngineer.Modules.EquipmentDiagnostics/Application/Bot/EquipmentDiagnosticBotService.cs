using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Guidance;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot.Routing;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;

public sealed class EquipmentDiagnosticBotService : IEquipmentDiagnosticBotService
{
    private const string GenericSafetyBoundary =
        "Use this guidance for preliminary review only. Electrical, refrigerant-circuit, and compressor checks require a qualified technician; keep safety protections active.";

    private readonly IEquipmentDiagnosticsService _diagnosticsService;
    private readonly IErrorKnowledgeLocalizationSource _localizedKnowledge;

    public EquipmentDiagnosticBotService(
        IEquipmentDiagnosticsService diagnosticsService,
        IErrorKnowledgeLocalizationSource localizedKnowledge)
    {
        _diagnosticsService = diagnosticsService;
        _localizedKnowledge = localizedKnowledge;
    }

    public async Task<EquipmentDiagnosticBotResponse> DiagnoseAsync(
        EquipmentDiagnosticBotRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var manufacturer = Normalize(request.Manufacturer);
        var code = Normalize(request.Code);
        var observedCode = request.Code?.Trim() ?? code;
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

        if (TryCanonicalizeVisualAlias(manufacturer, code, request.Series, request.FreeText, out var aliasCode))
        {
            trace.Add($"VisualCodeAlias:{code}->{aliasCode}");
            code = aliasCode;
        }

        var localizedResolution = ResolveLocalizedKnowledge(request, manufacturer, code, observedCode, trace);
        if (localizedResolution is not null)
        {
            return localizedResolution;
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

            var localizedMatches = FindLocalizedMatches(request, manufacturer, code);
            if (localizedMatches.Count == 1)
            {
                trace.Add("LocalizedKnowledgeMatch");
                return LocalizedKnowledgeResponse(
                    localizedMatches[0],
                    manufacturer,
                    observedCode,
                    request.FreeText,
                    trace);
            }

            if (localizedMatches.Count > 1)
            {
                trace.Add($"LocalizedKnowledgeAmbiguity:{localizedMatches.Count}");
                var options = localizedMatches
                    .Select(ToClarificationOption)
                    .ToArray();
                return new EquipmentDiagnosticBotResponse(
                    EquipmentDiagnosticBotResponseStatus.ClarificationRequired,
                    "Equipment context required",
                    "The displayed code matches multiple manual-backed equipment contexts.",
                    manufacturer,
                    code,
                    EquipmentContext: null,
                    new EquipmentDiagnosticBotObservedCodeContext(request.Code?.Trim() ?? code, code, request.FreeText),
                    AnswerCard: null,
                    new EquipmentDiagnosticBotClarificationQuestion(
                        "Which equipment context shows this code?",
                        options),
                    SourceCard: null,
                    Safety(),
                    VerificationRequired: false,
                    DiagnosticConfidence.High,
                    IsManualVerified: true,
                    IsSeedKnowledge: false,
                    options.Select(option => option.FollowUpPrompt).ToArray(),
                    [],
                    trace);
            }

            var notFoundContext = DiagnosticRoutingHintExtractor.ContextLabel(manufacturer, request.Series);
            trace.Add(string.IsNullOrWhiteSpace(request.Series)
                ? "RuntimeDiagnosticNotFound"
                : $"RuntimeDiagnosticNotFound:{request.Series}");
            return NonAnswer(
                EquipmentDiagnosticBotResponseStatus.NotFound,
                $"{notFoundContext} runtime diagnostic case not found",
                $"No runtime diagnostic case found for {notFoundContext}. Verify equipment family, display context, and service manual.",
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
            var localizedMatches = FindLocalizedMatches(request, manufacturer, code);
            if (localizedMatches.Count == 1)
            {
                trace.Add("RuntimeCaseLocalizedKnowledgeMatch");
                return LocalizedKnowledgeResponse(
                    localizedMatches[0],
                    manufacturer,
                    code,
                    request.FreeText,
                    trace);
            }

            if (localizedMatches.Count > 1)
            {
                trace.Add($"RuntimeCaseLocalizedKnowledgeAmbiguity:{localizedMatches.Count}");
                var options = localizedMatches
                    .Select(ToClarificationOption)
                    .ToArray();
                return new EquipmentDiagnosticBotResponse(
                    EquipmentDiagnosticBotResponseStatus.ClarificationRequired,
                    "Equipment context required",
                    "The displayed code matches multiple manual-backed equipment contexts.",
                    manufacturer,
                    code,
                    EquipmentContext: null,
                    new EquipmentDiagnosticBotObservedCodeContext(request.Code?.Trim() ?? code, code, request.FreeText),
                    AnswerCard: null,
                    new EquipmentDiagnosticBotClarificationQuestion(
                        "Which equipment context shows this code?",
                        options),
                    SourceCard: null,
                    Safety(),
                    VerificationRequired: false,
                    DiagnosticConfidence.High,
                    IsManualVerified: true,
                    IsSeedKnowledge: false,
                    options.Select(option => option.FollowUpPrompt).ToArray(),
                    [],
                    trace);
            }

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
        var canonicalCode = diagnosticCase.ErrorCode.Code;
        return new EquipmentDiagnosticBotResponse(
            EquipmentDiagnosticBotResponseStatus.Answer,
            guidance.Title,
            guidance.Summary,
            manufacturer,
            canonicalCode,
            ToEquipmentContext(match),
            new EquipmentDiagnosticBotObservedCodeContext(request.Code?.Trim() ?? match.Code, canonicalCode, request.FreeText),
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

    private EquipmentDiagnosticBotResponse? ResolveLocalizedKnowledge(
        EquipmentDiagnosticBotRequest request,
        string manufacturer,
        string code,
        string observedCode,
        List<string> trace)
    {
        var localizedMatches = FindLocalizedMatches(request, manufacturer, code);
        if (localizedMatches.Count == 0)
        {
            return null;
        }

        var hasExplicitSeries = !string.IsNullOrWhiteSpace(request.Series) &&
            !string.Equals(request.Series, "GMV", StringComparison.OrdinalIgnoreCase);
        var hasSameMeaningCollision = localizedMatches.Count > 1 &&
            localizedMatches
                .Select(entry => entry.MeaningGroupId)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() == 1 &&
            localizedMatches.All(entry => !string.IsNullOrWhiteSpace(entry.MeaningGroupId));
        if (!hasExplicitSeries && !hasSameMeaningCollision)
        {
            return null;
        }

        var resolver = new DiagnosticCandidateResolver();
        var resolution = resolver.Resolve(
            new DiagnosticCandidateResolverRequest(
                request.FreeText,
                code,
                manufacturer,
                request.Series,
                request.EquipmentSide,
                request.DisplayContext),
            localizedMatches.Select(entry => new DiagnosticRoutingCandidate(entry)).ToArray());

        if (resolution.Status == DiagnosticCandidateResolutionStatus.NotFound)
        {
            return null;
        }

        if (resolution.Status == DiagnosticCandidateResolutionStatus.DirectAnswer &&
            resolution.SelectedCandidate is not null)
        {
            var selected = resolution.SelectedCandidate.Entry;
            trace.Add(resolution.Candidates.Count > 1
                ? $"LocalizedKnowledgeMeaningGroup:{selected.MeaningGroupId}"
                : "LocalizedKnowledgeMatch");

            return LocalizedKnowledgeResponse(
                selected,
                manufacturer,
                observedCode,
                request.FreeText,
                trace,
                resolution.ApplicableContexts.Count > 1 ? resolution.ApplicableContexts : []);
        }

        trace.Add($"LocalizedKnowledgeAmbiguity:{resolution.Candidates.Count}:{resolution.ClarificationKind}");
        return LocalizedAmbiguityResponse(
            resolution.Candidates.Select(candidate => candidate.Entry).ToArray(),
            request,
            manufacturer,
            code,
            trace);
    }

    private static EquipmentDiagnosticBotResponse LocalizedAmbiguityResponse(
        IReadOnlyList<ErrorKnowledgeEntryV2> localizedMatches,
        EquipmentDiagnosticBotRequest request,
        string manufacturer,
        string code,
        IReadOnlyList<string> trace)
    {
        var options = localizedMatches
            .Select(ToClarificationOption)
            .ToArray();
        return new EquipmentDiagnosticBotResponse(
            EquipmentDiagnosticBotResponseStatus.ClarificationRequired,
            "Equipment context required",
            "The displayed code matches multiple manual-backed equipment contexts.",
            manufacturer,
            code,
            EquipmentContext: null,
            new EquipmentDiagnosticBotObservedCodeContext(request.Code?.Trim() ?? code, code, request.FreeText),
            AnswerCard: null,
            new EquipmentDiagnosticBotClarificationQuestion(
                "Which equipment context shows this code?",
                options),
            SourceCard: null,
            Safety(),
            VerificationRequired: false,
            DiagnosticConfidence.High,
            IsManualVerified: true,
            IsSeedKnowledge: false,
            options.Select(option => option.FollowUpPrompt).ToArray(),
            [],
            trace);
    }

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

    private IReadOnlyList<ErrorKnowledgeEntryV2> FindLocalizedMatches(
        EquipmentDiagnosticBotRequest request,
        string manufacturer,
        string code)
    {
        var matches = _localizedKnowledge.GetEntries()
            .Where(entry =>
                entry.Manufacturer.Equals(manufacturer, StringComparison.OrdinalIgnoreCase) &&
                entry.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
            .Where(EquipmentDiagnosticBotReferencePolicy.IsSearchableLocalizedEntry)
            .Where(entry => DiagnosticRoutingHintExtractor.MatchesSeries(entry.Series, request.Series))
            .Where(entry =>
                entry.Models.Count == 0 ||
                string.IsNullOrWhiteSpace(request.ModelCode) ||
                entry.Models.Contains(request.ModelCode, StringComparer.OrdinalIgnoreCase))
            .Where(entry =>
                request.EquipmentSide is null or EquipmentDiagnosticBotEquipmentSide.Unknown ||
                Side(entry.EquipmentType) == request.EquipmentSide)
            .Where(entry =>
                request.DisplayContext is null or EquipmentDiagnosticBotDisplayContext.Unknown ||
                DisplayContext(entry.DisplaySource) == request.DisplayContext)
            .ToArray();

        var exactCodeMatches = matches
            .Where(entry =>
                string.Equals(entry.Code, request.Code?.Trim(), StringComparison.Ordinal) ||
                string.Equals(entry.Code, code, StringComparison.Ordinal))
            .ToArray();
        if (exactCodeMatches.Length > 0)
        {
            matches = exactCodeMatches;
        }

        if (matches.Length <= 1 || string.IsNullOrWhiteSpace(request.FreeText))
        {
            return matches;
        }

        var normalizedFreeText = Normalize(request.FreeText);
        var hinted = matches.Where(entry =>
            (!string.IsNullOrWhiteSpace(entry.Series) &&
             normalizedFreeText.Contains(Normalize(entry.Series), StringComparison.Ordinal)) ||
            IsSignalHintMatch(entry, normalizedFreeText)).ToArray();
        return hinted.Length > 0 ? hinted : matches;
    }

    private static bool TryCanonicalizeVisualAlias(
        string manufacturer,
        string code,
        string? series,
        string? freeText,
        out string canonicalCode)
    {
        _ = series;
        _ = freeText;
        canonicalCode = code;
        if (string.Equals(manufacturer, "GREE", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(code, "HO", StringComparison.Ordinal))
        {
            canonicalCode = "H0";
            return true;
        }

        return false;
    }

    private static bool IsSignalHintMatch(ErrorKnowledgeEntryV2 entry, string normalizedFreeText) =>
        entry.SignalType switch
        {
            ErrorKnowledgeSignalType.Debug or ErrorKnowledgeSignalType.Commissioning =>
                normalizedFreeText.Contains("DEBUG", StringComparison.Ordinal) ||
                normalizedFreeText.Contains("НАЛАДК", StringComparison.Ordinal) ||
                normalizedFreeText.Contains("ПУСКОНАЛАД", StringComparison.Ordinal),
            ErrorKnowledgeSignalType.Status or ErrorKnowledgeSignalType.Maintenance =>
                normalizedFreeText.Contains("STATUS", StringComparison.Ordinal) ||
                normalizedFreeText.Contains("СТАТУС", StringComparison.Ordinal) ||
                normalizedFreeText.Contains("СОСТОЯНИ", StringComparison.Ordinal),
            _ => false
        };

    private static EquipmentDiagnosticBotResponse LocalizedKnowledgeResponse(
        ErrorKnowledgeEntryV2 entry,
        string manufacturer,
        string code,
        string? freeText,
        IReadOnlyList<string> trace,
        IReadOnlyList<string>? applicableContexts = null)
    {
        var referenceOnly = entry.SignalType is
            ErrorKnowledgeSignalType.Status or
            ErrorKnowledgeSignalType.Debug or
            ErrorKnowledgeSignalType.Commissioning or
            ErrorKnowledgeSignalType.Maintenance or
            ErrorKnowledgeSignalType.RemoteDisplay ||
            entry.PackageId.Contains("debugging", StringComparison.OrdinalIgnoreCase);
        var verified = entry.VerificationStatus is "ManualVerified" or "Verified" or "Reviewed";
        return new EquipmentDiagnosticBotResponse(
            referenceOnly
                ? EquipmentDiagnosticBotResponseStatus.ReferenceOnly
                : EquipmentDiagnosticBotResponseStatus.Answer,
            $"{entry.Manufacturer} {entry.Series} {entry.Code}",
            entry.SourceMeaning ?? "Manual-backed diagnostic knowledge entry.",
            manufacturer,
            entry.Code,
            new EquipmentDiagnosticBotEquipmentContext(
                entry.Manufacturer,
                entry.Series,
                entry.Models.Count == 1 ? entry.Models[0] : null,
                Category(entry.EquipmentType),
                Side(entry.EquipmentType),
                DisplayContext(entry.DisplaySource)),
            new EquipmentDiagnosticBotObservedCodeContext(code, entry.Code, freeText),
            AnswerCard: null,
            ClarificationQuestion: null,
            new EquipmentDiagnosticBotSourceCard(
                entry.SourceType,
                entry.VerificationStatus,
                entry.SourceName,
                entry.SourceName,
                ManualVersion: null,
                ManualDocumentCode: null,
                Page: entry.SourceReference,
                Section: entry.SourceReference,
                Limitations: []),
            Safety(),
            VerificationRequired: !verified,
            Confidence(entry.Confidence),
            IsManualVerified: verified,
            IsSeedKnowledge: false,
            [],
            [],
            trace)
        {
            ApplicableContexts = applicableContexts ?? []
        };
    }

    private static EquipmentDiagnosticBotClarificationOption ToClarificationOption(
        ErrorKnowledgeEntryV2 entry)
    {
        var context = entry.SignalType is ErrorKnowledgeSignalType.Debug or ErrorKnowledgeSignalType.Commissioning
            ? "debugging / commissioning"
            : entry.SignalType is ErrorKnowledgeSignalType.Status or ErrorKnowledgeSignalType.Maintenance
                ? "status"
                : Side(entry.EquipmentType).ToString();
        var label = $"{entry.Manufacturer} {entry.Series} ({context})";
        return new EquipmentDiagnosticBotClarificationOption(
            label,
            entry.Manufacturer,
            entry.Series,
            Category(entry.EquipmentType),
            Side(entry.EquipmentType),
            DisplayContext(entry.DisplaySource),
            entry.Code,
            $"{entry.Code} is present in manual-backed {context} knowledge.",
            $"Confirm {label} and resend {entry.Code} with that context.");
    }

    private static EquipmentCategory Category(ErrorKnowledgeEquipmentType equipmentType) =>
        equipmentType switch
        {
            ErrorKnowledgeEquipmentType.IndoorUnit => EquipmentCategory.VrfIndoorUnit,
            ErrorKnowledgeEquipmentType.WiredRemote or
            ErrorKnowledgeEquipmentType.CentralController or
            ErrorKnowledgeEquipmentType.Gateway => EquipmentCategory.Controller,
            ErrorKnowledgeEquipmentType.Chiller => EquipmentCategory.Chiller,
            _ => EquipmentCategory.VrfOutdoorUnit
        };

    private static EquipmentDiagnosticBotEquipmentSide Side(ErrorKnowledgeEquipmentType equipmentType) =>
        equipmentType switch
        {
            ErrorKnowledgeEquipmentType.IndoorUnit => EquipmentDiagnosticBotEquipmentSide.Indoor,
            ErrorKnowledgeEquipmentType.WiredRemote or
            ErrorKnowledgeEquipmentType.CentralController or
            ErrorKnowledgeEquipmentType.Gateway => EquipmentDiagnosticBotEquipmentSide.Controller,
            ErrorKnowledgeEquipmentType.Chiller => EquipmentDiagnosticBotEquipmentSide.Chiller,
            _ => EquipmentDiagnosticBotEquipmentSide.Outdoor
        };

    private static EquipmentDiagnosticBotDisplayContext DisplayContext(
        ErrorKnowledgeDisplaySource displaySource) =>
        displaySource switch
        {
            ErrorKnowledgeDisplaySource.IndoorUnit => EquipmentDiagnosticBotDisplayContext.IduDisplay,
            ErrorKnowledgeDisplaySource.WiredRemote => EquipmentDiagnosticBotDisplayContext.WiredController,
            ErrorKnowledgeDisplaySource.CentralController => EquipmentDiagnosticBotDisplayContext.CentralizedController,
            ErrorKnowledgeDisplaySource.Gateway or ErrorKnowledgeDisplaySource.Software =>
                EquipmentDiagnosticBotDisplayContext.MobileAppOrGateway,
            _ => EquipmentDiagnosticBotDisplayContext.OduMainBoardLed
        };

    private static DiagnosticConfidence Confidence(string confidence) =>
        confidence switch
        {
            "ManualVerified" => DiagnosticConfidence.ManualVerified,
            "High" => DiagnosticConfidence.High,
            "Medium" => DiagnosticConfidence.Medium,
            "Low" => DiagnosticConfidence.Low,
            _ => DiagnosticConfidence.Unknown
        };

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
