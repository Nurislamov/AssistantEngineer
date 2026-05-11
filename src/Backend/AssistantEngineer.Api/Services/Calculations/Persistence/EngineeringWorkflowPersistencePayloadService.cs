using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantEngineer.Api.Contracts.Calculations;

namespace AssistantEngineer.Api.Services.Calculations.Persistence;

internal sealed class EngineeringWorkflowPersistencePayloadService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private readonly EngineeringWorkflowPayloadLimitsOptions _payloadLimits;

    public EngineeringWorkflowPersistencePayloadService(EngineeringWorkflowPayloadLimitsOptions payloadLimits)
    {
        _payloadLimits = payloadLimits;
    }

    public EngineeringWorkflowStateDto? DeserializeWorkflowState(string rawJson)
    {
        try
        {
            return JsonSerializer.Deserialize<EngineeringWorkflowStateDto>(rawJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    public IReadOnlyList<EngineeringWorkflowDiagnosticDto> SortAndDistinctDiagnostics(
        IEnumerable<EngineeringWorkflowDiagnosticDto> diagnostics)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        return diagnostics
            .OrderByDescending(item => SeverityRank(item.Severity))
            .ThenBy(item => item.SourceStep, StringComparer.Ordinal)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.Message, StringComparer.Ordinal)
            .Where(item => seen.Add($"{item.SourceStep}|{item.Code}|{item.Message}|{item.TargetField}"))
            .ToArray();
    }

    public PayloadContent SerializeStatePayload(
        EngineeringWorkflowStateDto state,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> normalizedDiagnostics)
    {
        var serializedState = JsonSerializer.Serialize(state, JsonOptions);
        if (!_payloadLimits.Enabled || Utf8ByteCount(serializedState) <= _payloadLimits.StateJsonMaxBytes)
        {
            return new PayloadContent(
                serializedState,
                normalizedDiagnostics,
                WasTruncated: false,
                OriginalBytes: Utf8ByteCount(serializedState),
                StoredBytes: Utf8ByteCount(serializedState));
        }

        var truncationDiagnostic = CreatePayloadDiagnostic(
            code: "WORKFLOW_STATE_JSON_TRUNCATED",
            message: $"Workflow state payload exceeded `{_payloadLimits.StateJsonMaxBytes}` bytes and was compacted for persistence.",
            sourceStep: "Validation",
            targetField: "workflowStateJson");
        var compactDiagnostics = SortAndDistinctDiagnostics(normalizedDiagnostics.Append(truncationDiagnostic));
        var compactState = state with
        {
            Zones = [],
            Boundaries = [],
            Diagnostics = compactDiagnostics,
            Assumptions = SortAndDistinctAssumptions(
                state.Assumptions.Append("Workflow state snapshot was compacted by persistence payload limits.")),
            Links = [],
            CalculationTraceSummary = null,
            ReportSummary = null,
            VentilationSettings = state.VentilationSettings with { Warnings = [] },
            Metadata = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["payloadStateJsonTruncated"] = "true",
                ["payloadTruncationMarker"] = _payloadLimits.TruncationMarker
            }
        };

        var compactSerializedState = JsonSerializer.Serialize(compactState, JsonOptions);
        if (Utf8ByteCount(compactSerializedState) > _payloadLimits.StateJsonMaxBytes)
        {
            var hardLimitedPayload = ApplyPayloadLimit(
                "workflow-state-json-fallback",
                compactSerializedState,
                _payloadLimits.StateJsonMaxBytes,
                contentType: "application/json");
            return new PayloadContent(
                hardLimitedPayload.Content,
                compactDiagnostics,
                WasTruncated: true,
                OriginalBytes: Utf8ByteCount(serializedState),
                StoredBytes: hardLimitedPayload.StoredBytes);
        }

        return new PayloadContent(
            compactSerializedState,
            compactDiagnostics,
            WasTruncated: true,
            OriginalBytes: Utf8ByteCount(serializedState),
            StoredBytes: Utf8ByteCount(compactSerializedState));
    }

    public ScenarioRecordBuildResult BuildScenarioRecord(
        EngineeringCalculationScenarioRequestDto scenarioRequest,
        EngineeringCalculationScenarioResultDto scenarioResult,
        IReadOnlyList<EngineeringWorkflowDiagnosticDto> diagnostics,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? startedAtUtc,
        DateTimeOffset? completedAtUtc)
    {
        var mutableDiagnostics = diagnostics.ToList();
        var requestJsonPayload = ApplyPayloadLimit(
            "scenario-request-json",
            JsonSerializer.Serialize(scenarioRequest, JsonOptions),
            _payloadLimits.RequestJsonMaxBytes,
            contentType: "application/json");
        if (requestJsonPayload.WasTruncated)
        {
            mutableDiagnostics.Add(CreatePayloadDiagnostic(
                code: "WORKFLOW_REQUEST_JSON_TRUNCATED",
                message: $"Scenario request payload exceeded `{_payloadLimits.RequestJsonMaxBytes}` bytes and was truncated for persistence.",
                sourceStep: "Review",
                targetField: "requestJson"));
        }

        var summary = new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["scenarioId"] = scenarioResult.ScenarioId,
            ["status"] = scenarioResult.Status.ToString(),
            ["executed"] = scenarioResult.Executed,
            ["executedModules"] = scenarioResult.ExecutedModules.Order(StringComparer.Ordinal).ToArray(),
            ["skippedModules"] = scenarioResult.SkippedModules.Order(StringComparer.Ordinal).ToArray(),
            ["unavailableModules"] = scenarioResult.UnavailableModules.Order(StringComparer.Ordinal).ToArray(),
            ["moduleSummaries"] = scenarioResult.ModuleSummaries,
            ["timings"] = scenarioResult.Timings.OrderBy(item => item.ModuleKind, StringComparer.Ordinal).ToArray()
        };

        var resultSummaryPayload = ApplyPayloadLimit(
            "scenario-result-summary-json",
            JsonSerializer.Serialize(summary, JsonOptions),
            _payloadLimits.ResultSummaryJsonMaxBytes,
            contentType: "application/json");
        if (resultSummaryPayload.WasTruncated)
        {
            mutableDiagnostics.Add(CreatePayloadDiagnostic(
                code: "WORKFLOW_RESULT_SUMMARY_JSON_TRUNCATED",
                message: $"Scenario result summary payload exceeded `{_payloadLimits.ResultSummaryJsonMaxBytes}` bytes and was truncated for persistence.",
                sourceStep: "Review",
                targetField: "resultSummaryJson"));
        }

        var normalizedDiagnostics = SortAndDistinctDiagnostics(mutableDiagnostics);
        var diagnosticsJsonPayload = ApplyPayloadLimit(
            "scenario-diagnostics-json",
            JsonSerializer.Serialize(normalizedDiagnostics, JsonOptions),
            _payloadLimits.DiagnosticsJsonMaxBytes,
            contentType: "application/json");
        if (diagnosticsJsonPayload.WasTruncated)
        {
            normalizedDiagnostics = SortAndDistinctDiagnostics(normalizedDiagnostics.Append(CreatePayloadDiagnostic(
                code: "WORKFLOW_DIAGNOSTICS_JSON_TRUNCATED",
                message: $"Scenario diagnostics payload exceeded `{_payloadLimits.DiagnosticsJsonMaxBytes}` bytes and was truncated for persistence.",
                sourceStep: "Validation",
                targetField: "diagnosticsJson")));
            diagnosticsJsonPayload = ApplyPayloadLimit(
                "scenario-diagnostics-json",
                JsonSerializer.Serialize(normalizedDiagnostics, JsonOptions),
                _payloadLimits.DiagnosticsJsonMaxBytes,
                contentType: "application/json");
        }

        var durationMilliseconds = scenarioResult.Timings.Sum(item => item.DurationMilliseconds);
        var projectId = scenarioRequest.ProjectId ?? scenarioRequest.State.ProjectId;

        return new ScenarioRecordBuildResult(
            Record: new EngineeringCalculationScenarioRecordDto(
                ScenarioId: scenarioResult.ScenarioId,
                ProjectId: projectId,
                BuildingId: scenarioRequest.BuildingId ?? scenarioRequest.State.BuildingId,
                ScenarioKind: scenarioRequest.ScenarioKind,
                ExecutionMode: scenarioRequest.ExecutionMode,
                Status: scenarioResult.Status,
                RequestJson: requestJsonPayload.Content,
                ResultSummaryJson: resultSummaryPayload.Content,
                CreatedAtUtc: createdAtUtc,
                StartedAtUtc: startedAtUtc,
                CompletedAtUtc: completedAtUtc,
                DurationMilliseconds: durationMilliseconds > 0.0 ? durationMilliseconds : null,
                DiagnosticsJson: diagnosticsJsonPayload.Content),
            Diagnostics: normalizedDiagnostics);
    }

    public PayloadContent ApplyPayloadLimit(
        string payloadName,
        string content,
        int maxBytes,
        string contentType)
    {
        var originalBytes = Utf8ByteCount(content);
        if (!_payloadLimits.Enabled || originalBytes <= maxBytes)
        {
            return new PayloadContent(content, [], WasTruncated: false, OriginalBytes: originalBytes, StoredBytes: originalBytes);
        }

        if (string.Equals(contentType, "application/json", StringComparison.OrdinalIgnoreCase))
        {
            var previewBudget = Math.Max(16, maxBytes / 3);
            string envelopeJson;
            do
            {
                var envelope = new SortedDictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["truncated"] = true,
                    ["payloadName"] = payloadName,
                    ["marker"] = _payloadLimits.TruncationMarker,
                    ["maxBytes"] = maxBytes,
                    ["originalBytes"] = originalBytes,
                    ["preview"] = TruncateUtf8WithMarker(content, previewBudget)
                };
                envelopeJson = JsonSerializer.Serialize(envelope, JsonOptions);
                previewBudget = Math.Max(8, previewBudget / 2);
            }
            while (Utf8ByteCount(envelopeJson) > maxBytes && previewBudget > 8);

            if (Utf8ByteCount(envelopeJson) > maxBytes)
            {
                envelopeJson = TruncateUtf8WithMarker(envelopeJson, maxBytes);
            }

            return new PayloadContent(
                envelopeJson,
                [],
                WasTruncated: true,
                OriginalBytes: originalBytes,
                StoredBytes: Utf8ByteCount(envelopeJson));
        }

        var truncated = TruncateUtf8WithMarker(content, maxBytes);
        return new PayloadContent(
            truncated,
            [],
            WasTruncated: true,
            OriginalBytes: originalBytes,
            StoredBytes: Utf8ByteCount(truncated));
    }

    private EngineeringWorkflowDiagnosticDto CreatePayloadDiagnostic(
        string code,
        string message,
        string sourceStep,
        string targetField)
    {
        return new EngineeringWorkflowDiagnosticDto(
            Severity: "warning",
            Code: code,
            Message: message,
            SourceStep: sourceStep,
            SourceModule: "Persistence",
            SuggestedCorrection: "Reduce payload size via compact detail level or request narrower report/trace scope.",
            TargetField: targetField);
    }

    private static IReadOnlyList<string> SortAndDistinctAssumptions(IEnumerable<string> assumptions)
    {
        return assumptions
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();
    }

    private static int Utf8ByteCount(string value) => Encoding.UTF8.GetByteCount(value);

    private string TruncateUtf8WithMarker(string value, int maxBytes)
    {
        if (maxBytes <= 0)
        {
            return string.Empty;
        }

        var marker = _payloadLimits.TruncationMarker;
        var markerBytes = Utf8ByteCount(marker);
        if (markerBytes >= maxBytes)
        {
            return marker[..Math.Min(marker.Length, maxBytes)];
        }

        var budget = maxBytes - markerBytes;
        var runeBuffer = new StringBuilder();
        var consumedBytes = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            var runeBytes = Utf8ByteCount(rune.ToString());
            if (consumedBytes + runeBytes > budget)
            {
                break;
            }

            runeBuffer.Append(rune.ToString());
            consumedBytes += runeBytes;
        }

        return string.Concat(runeBuffer.ToString(), marker);
    }

    private static int SeverityRank(string severity)
    {
        if (severity.Equals("error", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (severity.Equals("assumption", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return 1;
    }
}

internal sealed record ScenarioRecordBuildResult(
    EngineeringCalculationScenarioRecordDto Record,
    IReadOnlyList<EngineeringWorkflowDiagnosticDto> Diagnostics);

internal sealed record PayloadContent(
    string Content,
    IReadOnlyList<EngineeringWorkflowDiagnosticDto> Diagnostics,
    bool WasTruncated,
    int OriginalBytes,
    int StoredBytes);
