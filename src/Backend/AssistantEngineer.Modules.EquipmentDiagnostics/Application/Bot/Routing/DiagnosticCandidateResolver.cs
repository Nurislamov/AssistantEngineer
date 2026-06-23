using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot.Routing;

public enum DiagnosticCandidateResolutionStatus
{
    DirectAnswer,
    ClarificationRequired,
    NotFound
}

public enum DiagnosticCandidateClarificationKind
{
    Brand,
    EquipmentGroup,
    Series,
    DisplaySurface,
    Candidate
}

public sealed record DiagnosticCandidateResolverRequest(
    string? RawText,
    string? ParsedCode,
    string? Manufacturer,
    string? Series,
    EquipmentDiagnosticBotEquipmentSide? EquipmentSide,
    EquipmentDiagnosticBotDisplayContext? DisplayContext);

public sealed record DiagnosticRoutingCandidate(ErrorKnowledgeEntryV2 Entry)
{
    public string Manufacturer => Entry.Manufacturer;
    public string? Series => Entry.Series;
    public string Code => Entry.Code;
    public string? MeaningGroupId => Entry.MeaningGroupId;
    public ErrorKnowledgeEquipmentType EquipmentType => Entry.EquipmentType;
    public ErrorKnowledgeDisplaySource DisplaySource => Entry.DisplaySource;
}

public sealed record DiagnosticCandidateResolverResult(
    DiagnosticCandidateResolutionStatus Status,
    DiagnosticCandidateClarificationKind? ClarificationKind,
    DiagnosticRoutingCandidate? SelectedCandidate,
    IReadOnlyList<DiagnosticRoutingCandidate> Candidates,
    IReadOnlyList<string> ApplicableContexts);

public sealed class DiagnosticCandidateResolver
{
    public DiagnosticCandidateResolverResult Resolve(
        DiagnosticCandidateResolverRequest request,
        IReadOnlyList<DiagnosticRoutingCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(candidates);

        var filtered = candidates
            .Where(candidate => Matches(request, candidate))
            .OrderBy(candidate => candidate.Manufacturer, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Series ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.EquipmentType.ToString(), StringComparer.Ordinal)
            .ThenBy(candidate => candidate.DisplaySource.ToString(), StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Code, StringComparer.Ordinal)
            .ToArray();

        if (filtered.Length == 0)
        {
            return new DiagnosticCandidateResolverResult(
                DiagnosticCandidateResolutionStatus.NotFound,
                ClarificationKind: null,
                SelectedCandidate: null,
                Candidates: [],
                ApplicableContexts: []);
        }

        if (filtered.Length == 1)
        {
            return Direct(filtered[0], filtered);
        }

        var meaningGroups = filtered
            .Select(candidate => candidate.MeaningGroupId)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (meaningGroups.Length == 1 &&
            filtered.All(candidate => string.Equals(candidate.MeaningGroupId, meaningGroups[0], StringComparison.OrdinalIgnoreCase)))
        {
            var selected = PreferRequestedSeries(request.Series, filtered);
            return Direct(selected, filtered);
        }

        var clarificationKind = SelectClarificationKind(request, filtered);
        return new DiagnosticCandidateResolverResult(
            DiagnosticCandidateResolutionStatus.ClarificationRequired,
            clarificationKind,
            SelectedCandidate: null,
            filtered,
            ApplicableContexts(filtered));
    }

    private static bool Matches(DiagnosticCandidateResolverRequest request, DiagnosticRoutingCandidate candidate)
    {
        if (!string.IsNullOrWhiteSpace(request.Manufacturer) &&
            !candidate.Manufacturer.Equals(request.Manufacturer, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!DiagnosticRoutingHintExtractor.MatchesSeries(candidate.Series, request.Series))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(request.ParsedCode) &&
            !candidate.Code.Equals(request.ParsedCode, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static DiagnosticCandidateResolverResult Direct(
        DiagnosticRoutingCandidate selected,
        IReadOnlyList<DiagnosticRoutingCandidate> candidates) =>
        new(
            DiagnosticCandidateResolutionStatus.DirectAnswer,
            ClarificationKind: null,
            selected,
            candidates,
            ApplicableContexts(candidates));

    private static DiagnosticRoutingCandidate PreferRequestedSeries(
        string? requestedSeries,
        IReadOnlyList<DiagnosticRoutingCandidate> candidates)
    {
        if (!string.IsNullOrWhiteSpace(requestedSeries))
        {
            var requested = candidates.FirstOrDefault(candidate =>
                DiagnosticRoutingHintExtractor.MatchesSeries(candidate.Series, requestedSeries) &&
                !string.IsNullOrWhiteSpace(candidate.Series));
            if (requested is not null)
            {
                return requested;
            }
        }

        return candidates.FirstOrDefault(candidate => string.Equals(candidate.Series, "GMV6", StringComparison.OrdinalIgnoreCase)) ??
               candidates[0];
    }

    private static DiagnosticCandidateClarificationKind SelectClarificationKind(
        DiagnosticCandidateResolverRequest request,
        IReadOnlyList<DiagnosticRoutingCandidate> candidates)
    {
        if (string.IsNullOrWhiteSpace(request.Manufacturer) &&
            candidates.Select(candidate => candidate.Manufacturer).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
        {
            return DiagnosticCandidateClarificationKind.Brand;
        }

        if (string.IsNullOrWhiteSpace(request.Series) &&
            candidates.Select(candidate => candidate.Series ?? string.Empty).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
        {
            return DiagnosticCandidateClarificationKind.Series;
        }

        if ((request.EquipmentSide is null or EquipmentDiagnosticBotEquipmentSide.Unknown) &&
            candidates.Select(candidate => candidate.EquipmentType).Distinct().Count() > 1)
        {
            return DiagnosticCandidateClarificationKind.EquipmentGroup;
        }

        if ((request.DisplayContext is null or EquipmentDiagnosticBotDisplayContext.Unknown) &&
            candidates.Select(candidate => candidate.DisplaySource).Distinct().Count() > 1)
        {
            return DiagnosticCandidateClarificationKind.DisplaySurface;
        }

        return DiagnosticCandidateClarificationKind.Candidate;
    }

    private static IReadOnlyList<string> ApplicableContexts(IReadOnlyList<DiagnosticRoutingCandidate> candidates) =>
        candidates
            .Select(candidate => DiagnosticRoutingHintExtractor.ContextLabel(candidate.Manufacturer, candidate.Series))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
}
