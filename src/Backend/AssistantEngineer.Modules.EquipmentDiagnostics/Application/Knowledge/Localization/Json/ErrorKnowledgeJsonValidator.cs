using System.Text.Json;
using System.Text.RegularExpressions;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization.Json;

public sealed partial class ErrorKnowledgeJsonValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false
    };

    private static readonly string[] AllowedLocales = ["ru", "en", "uz"];
    private static readonly string[] AllowedConfidences = ["Low", "Medium", "High", "ManualVerified"];
    private static readonly string[] AllowedVerificationStatuses =
        ["UnverifiedSeed", "PendingReview", "Reviewed", "Verified"];
    private static readonly string[] EnglishUiLeaks =
    [
        "Safety",
        "Step",
        "Source",
        "Confidence",
        "Low",
        "Medium",
        "High",
        "Preliminary diagnostic entry",
        "Recommended action"
    ];
    private static readonly string[] UnsafeConsumerAdvice =
    [
        "bypass protection",
        "disable protection",
        "open electrical panel",
        "measure voltage",
        "add refrigerant",
        "replace compressor",
        "replace inverter",
        "reset protection repeatedly",
        "обойти защит",
        "отключить защит",
        "открыть электрический щит",
        "измерить напряжение",
        "добавить хладагент",
        "заменить компрессор",
        "заменить инвертор",
        "многократно сбрасывать защит"
    ];

    public ErrorKnowledgeValidationResult Validate(
        IReadOnlyCollection<ErrorKnowledgeJsonSource> sources)
    {
        var issues = new List<ErrorKnowledgeValidationIssue>();
        var entries = new List<ErrorKnowledgeEntryV2>();

        foreach (var source in sources.OrderBy(item => item.Path, StringComparer.Ordinal))
        {
            try
            {
                var document = JsonSerializer.Deserialize<ErrorKnowledgeJsonDocument>(source.Json, JsonOptions);
                if (document is null)
                {
                    issues.Add(new(source.Path, "file is empty or does not contain a knowledge entry."));
                    continue;
                }

                var entry = ValidateAndMap(document, source.Path, issues);
                if (entry is not null)
                {
                    entries.Add(entry);
                }
            }
            catch (JsonException exception)
            {
                issues.Add(new(
                    source.Path,
                    $"invalid JSON at line {exception.LineNumber}, byte {exception.BytePositionInLine}: {exception.Message}"));
            }
        }

        ValidateDuplicates(entries, issues);
        return new(entries, issues);
    }

    private static ErrorKnowledgeEntryV2? ValidateAndMap(
        ErrorKnowledgeJsonDocument document,
        string path,
        ICollection<ErrorKnowledgeValidationIssue> issues)
    {
        var startIssueCount = issues.Count;
        var id = Required(document.Id, path, "id", issues);
        var manufacturer = Required(document.Manufacturer, path, "manufacturer", issues);
        var code = Required(document.Code, path, "code", issues);
        var sourceLanguage = Required(document.SourceLanguage, path, "sourceLanguage", issues);
        var sourceType = Required(document.SourceType, path, "sourceType", issues);
        var sourceName = Required(document.SourceName, path, "sourceName", issues);
        var confidence = Allowed(
            document.Confidence,
            path,
            "confidence",
            AllowedConfidences,
            issues);
        var verificationStatus = Allowed(
            document.VerificationStatus,
            path,
            "verificationStatus",
            AllowedVerificationStatuses,
            issues);

        if (sourceLanguage is not null && !AllowedLocales.Contains(sourceLanguage, StringComparer.Ordinal))
        {
            issues.Add(new(path, $"sourceLanguage '{sourceLanguage}' is invalid. Allowed: {string.Join(", ", AllowedLocales)}."));
        }

        if (document.CreatedAt is null)
        {
            issues.Add(new(path, "createdAt is required."));
        }

        if (document.UpdatedAt is null)
        {
            issues.Add(new(path, "updatedAt is required."));
        }

        var models = ValidateTextArray(document.Models, path, "models", required: true, issues);
        var texts = ValidateTexts(document.Texts, id, path, issues);

        if (issues.Count != startIssueCount ||
            id is null ||
            manufacturer is null ||
            code is null ||
            sourceLanguage is null ||
            sourceType is null ||
            sourceName is null ||
            confidence is null ||
            verificationStatus is null ||
            document.CreatedAt is null ||
            document.UpdatedAt is null)
        {
            return null;
        }

        return new ErrorKnowledgeEntryV2(
            id,
            manufacturer,
            Normalize(document.EquipmentType),
            Normalize(document.Series),
            models,
            code,
            sourceLanguage,
            sourceType,
            sourceName,
            Normalize(document.SourceReference),
            confidence,
            verificationStatus,
            document.CreatedAt.Value,
            document.UpdatedAt.Value,
            texts);
    }

    private static IReadOnlyList<ErrorKnowledgeTextV2> ValidateTexts(
        IReadOnlyList<ErrorKnowledgeJsonText>? texts,
        string? entryId,
        string path,
        ICollection<ErrorKnowledgeValidationIssue> issues)
    {
        if (texts is null || texts.Count == 0)
        {
            issues.Add(new(path, "texts must contain localized audience text."));
            return [];
        }

        var mapped = new List<ErrorKnowledgeTextV2>();
        for (var index = 0; index < texts.Count; index++)
        {
            var text = texts[index];
            var textPath = $"{path}:texts[{index}]";
            var startIssueCount = issues.Count;
            var id = Required(text.Id, textPath, "id", issues);
            var locale = Allowed(text.Locale, textPath, "locale", AllowedLocales, issues);
            var audienceText = Required(text.Audience, textPath, "audience", issues);
            var audience = ParseAudience(audienceText, textPath, issues);
            var title = Required(text.Title, textPath, "title", issues);
            var summary = Required(text.Summary, textPath, "summary", issues);
            var safetyNote = Required(text.SafetyNote, textPath, "safetyNote", issues);
            var possibleCauses = ValidateTextArray(text.PossibleCauses, textPath, "possibleCauses", true, issues);
            var checkSteps = ValidateTextArray(text.CheckSteps, textPath, "checkSteps", true, issues);
            var doNotAdvise = ValidateTextArray(text.DoNotAdvise, textPath, "doNotAdvise", true, issues);
            var recommendedAction = Required(text.RecommendedAction, textPath, "recommendedAction", issues);
            var sourceNote = Required(text.SourceNote, textPath, "sourceNote", issues);

            if (text.CreatedAt is null)
            {
                issues.Add(new(textPath, "createdAt is required."));
            }

            if (text.UpdatedAt is null)
            {
                issues.Add(new(textPath, "updatedAt is required."));
            }

            var visibleText = new[] { title, summary, safetyNote, recommendedAction, sourceNote }
                .Where(value => value is not null)
                .Cast<string>()
                .Concat(possibleCauses)
                .Concat(checkSteps)
                .ToArray();
            ValidateSensitiveContent(visibleText, textPath, issues);

            if (locale == "ru")
            {
                foreach (var leak in EnglishUiLeaks.Where(leak =>
                             visibleText.Any(value => ContainsPhrase(value, leak))))
                {
                    issues.Add(new(textPath, $"Russian text contains English UI label '{leak}'."));
                }
            }

            if (audience == ErrorKnowledgeAudience.Consumer)
            {
                foreach (var unsafeAdvice in UnsafeConsumerAdvice.Where(fragment =>
                             visibleText.Any(value => value.Contains(fragment, StringComparison.OrdinalIgnoreCase))))
                {
                    issues.Add(new(textPath, $"Consumer text contains unsafe advice '{unsafeAdvice}'."));
                }
            }

            if (issues.Count != startIssueCount ||
                entryId is null ||
                id is null ||
                locale is null ||
                audience is null ||
                title is null ||
                summary is null ||
                safetyNote is null ||
                recommendedAction is null ||
                sourceNote is null ||
                text.CreatedAt is null ||
                text.UpdatedAt is null)
            {
                continue;
            }

            mapped.Add(new ErrorKnowledgeTextV2(
                id,
                entryId,
                locale,
                audience.Value,
                title,
                summary,
                safetyNote,
                possibleCauses,
                checkSteps,
                doNotAdvise,
                recommendedAction,
                sourceNote,
                text.IsMachineTranslated,
                text.IsReviewed,
                text.CreatedAt.Value,
                text.UpdatedAt.Value));
        }

        foreach (var audience in Enum.GetValues<ErrorKnowledgeAudience>())
        {
            if (!mapped.Any(text => text.Locale == "ru" && text.Audience == audience))
            {
                issues.Add(new(path, $"missing required ru text for audience {audience}."));
            }
        }

        return mapped;
    }

    private static void ValidateDuplicates(
        IReadOnlyCollection<ErrorKnowledgeEntryV2> entries,
        ICollection<ErrorKnowledgeValidationIssue> issues)
    {
        var duplicates = entries
            .SelectMany(entry => entry.Texts.Select(text => new
            {
                Entry = entry,
                Text = text,
                Key = string.Join(
                    "|",
                    entry.Manufacturer.Trim().ToUpperInvariant(),
                    entry.EquipmentType?.Trim().ToUpperInvariant() ?? string.Empty,
                    entry.Series?.Trim().ToUpperInvariant() ?? string.Empty,
                    entry.Code.Trim().ToUpperInvariant(),
                    text.Locale.ToUpperInvariant(),
                    text.Audience)
            }))
            .GroupBy(item => item.Key, StringComparer.Ordinal)
            .Where(group => group.Count() > 1);

        foreach (var duplicate in duplicates)
        {
            issues.Add(new(
                string.Join(", ", duplicate.Select(item => item.Entry.Id).Distinct(StringComparer.Ordinal)),
                $"duplicate knowledge key '{duplicate.Key}'."));
        }
    }

    private static void ValidateSensitiveContent(
        IReadOnlyCollection<string> values,
        string path,
        ICollection<ErrorKnowledgeValidationIssue> issues)
    {
        foreach (var value in values)
        {
            if (PhoneNumberRegex().IsMatch(value))
            {
                issues.Add(new(path, "text contains a phone-number-like value."));
            }

            if (TelegramTokenRegex().IsMatch(value) || SecretAssignmentRegex().IsMatch(value))
            {
                issues.Add(new(path, "text contains a token/webhook-secret-like value."));
            }

            if (CallbackPayloadRegex().IsMatch(value))
            {
                issues.Add(new(path, "text contains a Telegram callback payload pattern."));
            }

            if (RawPlatformIdRegex().IsMatch(value))
            {
                issues.Add(new(path, "text contains a raw chat/platform-user-id-like value."));
            }
        }
    }

    private static ErrorKnowledgeAudience? ParseAudience(
        string? value,
        string path,
        ICollection<ErrorKnowledgeValidationIssue> issues)
    {
        if (value is null)
        {
            return null;
        }

        if (!Enum.TryParse<ErrorKnowledgeAudience>(value, ignoreCase: false, out var audience))
        {
            issues.Add(new(
                path,
                $"audience '{value}' is invalid. Allowed: {string.Join(", ", Enum.GetNames<ErrorKnowledgeAudience>())}."));
            return null;
        }

        return audience;
    }

    private static IReadOnlyList<string> ValidateTextArray(
        IReadOnlyList<string>? values,
        string path,
        string property,
        bool required,
        ICollection<ErrorKnowledgeValidationIssue> issues)
    {
        if (values is null)
        {
            if (required)
            {
                issues.Add(new(path, $"{property} must be present."));
            }

            return [];
        }

        var result = new List<string>();
        for (var index = 0; index < values.Count; index++)
        {
            var value = values[index];
            if (string.IsNullOrWhiteSpace(value))
            {
                issues.Add(new(path, $"{property}[{index}] must be non-empty."));
            }
            else
            {
                result.Add(value.Trim());
            }
        }

        return result;
    }

    private static string? Required(
        string? value,
        string path,
        string property,
        ICollection<ErrorKnowledgeValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add(new(path, $"{property} is required."));
            return null;
        }

        return value.Trim();
    }

    private static string? Allowed(
        string? value,
        string path,
        string property,
        IReadOnlyCollection<string> allowed,
        ICollection<ErrorKnowledgeValidationIssue> issues)
    {
        var required = Required(value, path, property, issues);
        if (required is not null && !allowed.Contains(required, StringComparer.Ordinal))
        {
            issues.Add(new(path, $"{property} '{required}' is invalid. Allowed: {string.Join(", ", allowed)}."));
            return null;
        }

        return required;
    }

    private static bool ContainsPhrase(string value, string phrase) =>
        Regex.IsMatch(value, $@"(?<![A-Za-z]){Regex.Escape(phrase)}(?![A-Za-z])", RegexOptions.IgnoreCase);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [GeneratedRegex(@"\+?\d[\d ()-]{6,}\d", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneNumberRegex();

    [GeneratedRegex(@"\b\d{6,10}:[A-Za-z0-9_-]{20,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex TelegramTokenRegex();

    [GeneratedRegex(@"(?i)\b(?:token|webhook[_ -]?secret)\s*[:=]\s*\S+")]
    private static partial Regex SecretAssignmentRegex();

    [GeneratedRegex(@"\b(?:sr|sq|au):[a-z0-9:.-]+\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CallbackPayloadRegex();

    [GeneratedRegex(@"(?<!\d)(?:-100)?\d{9,15}(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex RawPlatformIdRegex();
}
