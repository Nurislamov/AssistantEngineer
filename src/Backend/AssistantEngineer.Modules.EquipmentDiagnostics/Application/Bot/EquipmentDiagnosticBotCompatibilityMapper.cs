using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Diagnostics;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;

public static class EquipmentDiagnosticBotCompatibilityMapper
{
    public static DiagnosticCoreRequest ToCoreRequest(EquipmentDiagnosticBotRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new DiagnosticCoreRequest(
            request.Manufacturer,
            request.Code,
            request.FreeText,
            request.Series,
            request.ModelCode,
            request.Category,
            Map(request.EquipmentSide),
            Map(request.DisplayContext),
            request.PreferredLanguage,
            request.OperatorProvidedMeasurements,
            request.SiteContext);
    }

    public static EquipmentDiagnosticBotRequest ToBotRequest(DiagnosticCoreRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new EquipmentDiagnosticBotRequest(
            request.Manufacturer,
            request.Code,
            request.FreeText,
            request.Series,
            request.ModelCode,
            request.Category,
            Map(request.EquipmentSide),
            Map(request.DisplayContext),
            request.PreferredLanguage,
            request.OperatorProvidedMeasurements,
            request.SiteContext);
    }

    public static DiagnosticCoreResult ToCoreResult(
        EquipmentDiagnosticBotResponse response,
        IErrorKnowledgeLocalizationSource? localizedKnowledge = null)
    {
        ArgumentNullException.ThrowIfNull(response);

        var result = new DiagnosticCoreResult(
            Map(response.Status),
            response.Title,
            response.Message,
            response.NormalizedManufacturer,
            response.NormalizedCode,
            Map(response.EquipmentContext),
            new DiagnosticObservedCode(
                response.ObservedCode.Code,
                response.ObservedCode.NormalizedCode,
                response.ObservedCode.FreeText),
            Map(response.AnswerCard),
            Map(response.ClarificationQuestion),
            Map(response.SourceCard),
            new DiagnosticSafety(response.SafetyCard.Boundary, response.SafetyCard.Notes),
            response.VerificationRequired,
            response.Confidence,
            response.IsManualVerified,
            response.IsSeedKnowledge,
            response.OperatorNextSteps,
            response.Warnings,
            response.InternalDecisionTrace)
        {
            ApplicableContexts = response.ApplicableContexts
        };

        return localizedKnowledge is null
            ? result
            : AddSemanticKnowledge(result, localizedKnowledge);
    }

    public static EquipmentDiagnosticBotResponse ToBotResponse(DiagnosticCoreResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new EquipmentDiagnosticBotResponse(
            Map(result.Status),
            result.Title,
            result.Message,
            result.NormalizedManufacturer,
            result.CanonicalCode,
            Map(result.Match),
            new EquipmentDiagnosticBotObservedCodeContext(
                result.ObservedCode.ObservedCode,
                result.ObservedCode.CanonicalCode,
                result.ObservedCode.FreeText),
            Map(result.Answer),
            Map(result.Ambiguity),
            Map(result.Source),
            new EquipmentDiagnosticBotSafetyCard(result.Safety.Boundary, result.Safety.Notes),
            result.VerificationRequired,
            result.Confidence,
            result.IsManualVerified,
            result.IsSeedKnowledge,
            result.NextSteps,
            result.Warnings,
            result.InternalDecisionTrace)
        {
            ApplicableContexts = result.ApplicableContexts
        };
    }

    private static DiagnosticCoreResult AddSemanticKnowledge(
        DiagnosticCoreResult result,
        IErrorKnowledgeLocalizationSource localizedKnowledge)
    {
        var entries = localizedKnowledge.GetEntries()
            .Where(entry =>
                entry.Manufacturer.Equals(result.NormalizedManufacturer, StringComparison.OrdinalIgnoreCase) &&
                entry.Code.Equals(result.CanonicalCode, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var exactCodeEntries = entries
            .Where(entry => string.Equals(entry.Code, result.CanonicalCode, StringComparison.Ordinal))
            .ToArray();
        if (exactCodeEntries.Length > 0)
        {
            entries = exactCodeEntries;
        }

        if (!string.IsNullOrWhiteSpace(result.Match?.Series))
        {
            var seriesEntries = entries
                .Where(entry => string.Equals(
                    entry.Series,
                    result.Match.Series,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (seriesEntries.Length > 0)
            {
                entries = seriesEntries;
            }
        }

        var signalTypes = entries
            .Select(entry => entry.SignalType)
            .Distinct()
            .ToArray();
        var severities = entries
            .Select(entry => entry.Severity)
            .Distinct()
            .ToArray();
        var guidance = entries
            .SelectMany(entry => entry.Texts.Select(text => new DiagnosticLocalizedGuidance(
                text.Locale,
                Map(text.Audience),
                text.Title,
                entry.SourceMeaning,
                text.Summary,
                text.SafetyNote,
                text.PossibleCauses,
                text.CheckSteps,
                text.DoNotAdvise,
                text.RecommendedAction)))
            .Distinct()
            .OrderBy(item => item.Locale, StringComparer.Ordinal)
            .ThenBy(item => item.Audience)
            .ToArray();
        var sourceReferences = entries
            .SelectMany(entry => entry.SourceReferences)
            .Select(reference => new DiagnosticSourceReference(
                reference.SourceName,
                reference.DocumentCode,
                reference.SourceReference,
                reference.SourceType,
                reference.SourceLanguage,
                reference.VerificationStatus,
                reference.Confidence,
                reference.ManualId,
                reference.PackageId,
                reference.Notes))
            .Distinct()
            .ToArray();

        return result with
        {
            SignalType = signalTypes.Length == 1 ? signalTypes[0] : null,
            Severity = severities.Length == 1 ? severities[0] : null,
            LocalizedGuidance = guidance,
            SourceReferences = sourceReferences
        };
    }

    private static DiagnosticCoreStatus Map(EquipmentDiagnosticBotResponseStatus status) =>
        status switch
        {
            EquipmentDiagnosticBotResponseStatus.Answer => DiagnosticCoreStatus.Answer,
            EquipmentDiagnosticBotResponseStatus.ClarificationRequired => DiagnosticCoreStatus.ClarificationRequired,
            EquipmentDiagnosticBotResponseStatus.NotFound => DiagnosticCoreStatus.NotFound,
            EquipmentDiagnosticBotResponseStatus.ReferenceOnly => DiagnosticCoreStatus.ReferenceOnly,
            EquipmentDiagnosticBotResponseStatus.Unsupported => DiagnosticCoreStatus.Unsupported,
            EquipmentDiagnosticBotResponseStatus.UnsafeOrOutOfScope => DiagnosticCoreStatus.UnsafeOrOutOfScope,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

    private static EquipmentDiagnosticBotResponseStatus Map(DiagnosticCoreStatus status) =>
        status switch
        {
            DiagnosticCoreStatus.Answer => EquipmentDiagnosticBotResponseStatus.Answer,
            DiagnosticCoreStatus.ClarificationRequired => EquipmentDiagnosticBotResponseStatus.ClarificationRequired,
            DiagnosticCoreStatus.NotFound => EquipmentDiagnosticBotResponseStatus.NotFound,
            DiagnosticCoreStatus.ReferenceOnly => EquipmentDiagnosticBotResponseStatus.ReferenceOnly,
            DiagnosticCoreStatus.Unsupported => EquipmentDiagnosticBotResponseStatus.Unsupported,
            DiagnosticCoreStatus.UnsafeOrOutOfScope => EquipmentDiagnosticBotResponseStatus.UnsafeOrOutOfScope,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

    private static DiagnosticEquipmentSide? Map(EquipmentDiagnosticBotEquipmentSide? side) =>
        side is null ? null : (DiagnosticEquipmentSide)(int)side.Value;

    private static EquipmentDiagnosticBotEquipmentSide? Map(DiagnosticEquipmentSide? side) =>
        side is null ? null : (EquipmentDiagnosticBotEquipmentSide)(int)side.Value;

    private static DiagnosticEquipmentSide Map(EquipmentDiagnosticBotEquipmentSide side) =>
        (DiagnosticEquipmentSide)(int)side;

    private static EquipmentDiagnosticBotEquipmentSide Map(DiagnosticEquipmentSide side) =>
        (EquipmentDiagnosticBotEquipmentSide)(int)side;

    private static DiagnosticDisplayContext? Map(EquipmentDiagnosticBotDisplayContext? context) =>
        context is null ? null : (DiagnosticDisplayContext)(int)context.Value;

    private static EquipmentDiagnosticBotDisplayContext? Map(DiagnosticDisplayContext? context) =>
        context is null ? null : (EquipmentDiagnosticBotDisplayContext)(int)context.Value;

    private static DiagnosticDisplayContext Map(EquipmentDiagnosticBotDisplayContext context) =>
        (DiagnosticDisplayContext)(int)context;

    private static EquipmentDiagnosticBotDisplayContext Map(DiagnosticDisplayContext context) =>
        (EquipmentDiagnosticBotDisplayContext)(int)context;

    private static DiagnosticAudience Map(ErrorKnowledgeAudience audience) =>
        audience switch
        {
            ErrorKnowledgeAudience.Consumer => DiagnosticAudience.Consumer,
            ErrorKnowledgeAudience.Installer => DiagnosticAudience.Installer,
            ErrorKnowledgeAudience.Engineer => DiagnosticAudience.Engineer,
            _ => throw new ArgumentOutOfRangeException(nameof(audience), audience, null)
        };

    private static DiagnosticMatchIdentity? Map(EquipmentDiagnosticBotEquipmentContext? context) =>
        context is null
            ? null
            : new DiagnosticMatchIdentity(
                context.Manufacturer,
                context.Series,
                context.ModelCode,
                context.Category,
                Map(context.EquipmentSide),
                Map(context.DisplayContext));

    private static EquipmentDiagnosticBotEquipmentContext? Map(DiagnosticMatchIdentity? identity) =>
        identity is null
            ? null
            : new EquipmentDiagnosticBotEquipmentContext(
                identity.Manufacturer,
                identity.Series,
                identity.ModelCode,
                identity.Category,
                Map(identity.EquipmentSide),
                Map(identity.DisplayContext));

    private static DiagnosticAnswer? Map(EquipmentDiagnosticBotAnswerCard? answer) =>
        answer is null
            ? null
            : new DiagnosticAnswer(
                answer.Title,
                answer.Summary,
                answer.VerificationBanner,
                answer.LikelyCauses,
                answer.DiagnosticSteps,
                answer.RequiredMeasurements,
                answer.RecommendedChecks,
                answer.OperatorNotes);

    private static EquipmentDiagnosticBotAnswerCard? Map(DiagnosticAnswer? answer) =>
        answer is null
            ? null
            : new EquipmentDiagnosticBotAnswerCard(
                answer.Title,
                answer.Summary,
                answer.VerificationBanner,
                answer.LikelyCauses,
                answer.DiagnosticSteps,
                answer.RequiredMeasurements,
                answer.RecommendedChecks,
                answer.OperatorNotes);

    private static DiagnosticAmbiguity? Map(EquipmentDiagnosticBotClarificationQuestion? question) =>
        question is null
            ? null
            : new DiagnosticAmbiguity(
                question.Prompt,
                question.Options.Select(option => new DiagnosticAmbiguityCandidate(
                    option.Label,
                    option.Manufacturer,
                    option.Series,
                    option.Category,
                    Map(option.EquipmentSide),
                    Map(option.DisplayContext),
                    option.Code,
                    option.Explanation,
                    option.FollowUpPrompt)).ToArray());

    private static EquipmentDiagnosticBotClarificationQuestion? Map(DiagnosticAmbiguity? ambiguity) =>
        ambiguity is null
            ? null
            : new EquipmentDiagnosticBotClarificationQuestion(
                ambiguity.Prompt,
                ambiguity.Candidates.Select(candidate => new EquipmentDiagnosticBotClarificationOption(
                    candidate.Label,
                    candidate.Manufacturer,
                    candidate.Series,
                    candidate.Category,
                    Map(candidate.EquipmentSide),
                    Map(candidate.DisplayContext),
                    candidate.Code,
                    candidate.Explanation,
                    candidate.FollowUpPrompt)).ToArray());

    private static DiagnosticSource? Map(EquipmentDiagnosticBotSourceCard? source) =>
        source is null
            ? null
            : new DiagnosticSource(
                source.SourceType,
                source.EvidenceLevel,
                source.Summary,
                source.ManualTitle,
                source.ManualVersion,
                source.ManualDocumentCode,
                source.Page,
                source.Section,
                source.Limitations);

    private static EquipmentDiagnosticBotSourceCard? Map(DiagnosticSource? source) =>
        source is null
            ? null
            : new EquipmentDiagnosticBotSourceCard(
                source.SourceType,
                source.EvidenceLevel,
                source.Summary,
                source.ManualTitle,
                source.ManualVersion,
                source.ManualDocumentCode,
                source.Page,
                source.Section,
                source.Limitations);
}
