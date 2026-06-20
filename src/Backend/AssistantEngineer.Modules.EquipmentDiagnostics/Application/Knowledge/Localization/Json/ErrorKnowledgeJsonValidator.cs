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
        var packages = new List<ErrorKnowledgePackageManifest>();
        var entries = new List<ErrorKnowledgeEntryV2>();

        foreach (var source in sources.OrderBy(item => item.Path, StringComparer.Ordinal))
        {
            if (IsPackagePath(source.Path))
            {
                ParsePackage(source, packages, issues);
            }
            else
            {
                ParseEntry(source, entries, issues);
            }
        }

        ValidateDuplicatePackages(packages, issues);
        ValidateDuplicateEntries(entries, issues);
        ValidatePackageLinks(entries, packages, issues);
        return new(packages, entries, issues);
    }

    public static bool IsPackagePath(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.StartsWith("packages/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("/packages/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains(".packages.", StringComparison.OrdinalIgnoreCase);
    }

    private static void ParsePackage(
        ErrorKnowledgeJsonSource source,
        ICollection<ErrorKnowledgePackageManifest> packages,
        ICollection<ErrorKnowledgeValidationIssue> issues)
    {
        try
        {
            var document = JsonSerializer.Deserialize<ErrorKnowledgePackageJsonDocument>(
                source.Json,
                JsonOptions);
            if (document is null)
            {
                issues.Add(new(source.Path, "file is empty or does not contain a package manifest."));
                return;
            }

            var package = ValidateAndMapPackage(document, source.Path, issues);
            if (package is not null)
            {
                packages.Add(package);
            }
        }
        catch (JsonException exception)
        {
            AddJsonIssue(source.Path, exception, issues);
        }
    }

    private static void ParseEntry(
        ErrorKnowledgeJsonSource source,
        ICollection<ErrorKnowledgeEntryV2> entries,
        ICollection<ErrorKnowledgeValidationIssue> issues)
    {
        try
        {
            var document = JsonSerializer.Deserialize<ErrorKnowledgeJsonDocument>(
                source.Json,
                JsonOptions);
            if (document is null)
            {
                issues.Add(new(source.Path, "file is empty or does not contain a knowledge entry."));
                return;
            }

            var entry = ValidateAndMapEntry(document, source.Path, issues);
            if (entry is not null)
            {
                entries.Add(entry);
            }
        }
        catch (JsonException exception)
        {
            AddJsonIssue(source.Path, exception, issues);
        }
    }

    private static ErrorKnowledgePackageManifest? ValidateAndMapPackage(
        ErrorKnowledgePackageJsonDocument document,
        string path,
        ICollection<ErrorKnowledgeValidationIssue> issues)
    {
        var startIssueCount = issues.Count;
        var packageId = Required(document.PackageId, path, "packageId", issues);
        var manufacturer = Required(document.Manufacturer, path, "manufacturer", issues);
        var equipmentFamily = ParseRequiredEnum<ErrorKnowledgeEquipmentFamily>(
            document.EquipmentFamily,
            path,
            "equipmentFamily",
            issues);
        var title = Required(document.Title, path, "title", issues);
        var description = Required(document.Description, path, "description", issues);
        var sourceLanguage = Allowed(
            document.SourceLanguage,
            path,
            "sourceLanguage",
            AllowedLocales,
            issues);
        var sourceType = Required(document.SourceType, path, "sourceType", issues);
        var sourceName = Required(document.SourceName, path, "sourceName", issues);
        var verificationStatus = Allowed(
            document.VerificationStatus,
            path,
            "verificationStatus",
            AllowedVerificationStatuses,
            issues);
        var confidence = Allowed(
            document.Confidence,
            path,
            "confidence",
            AllowedConfidences,
            issues);
        var signalTypes = ParseEnumArray<ErrorKnowledgeSignalType>(
            document.IntendedSignalTypes,
            path,
            "intendedSignalTypes",
            issues);
        var equipmentTypes = ParseEnumArray<ErrorKnowledgeEquipmentType>(
            document.IntendedEquipmentTypes,
            path,
            "intendedEquipmentTypes",
            issues);
        var displaySources = ParseEnumArray<ErrorKnowledgeDisplaySource>(
            document.IntendedDisplaySources,
            path,
            "intendedDisplaySources",
            issues);

        if (document.EntryCountExpected < 0)
        {
            issues.Add(new(path, "entryCountExpected must be non-negative when present."));
        }

        if (document.CreatedAt is null)
        {
            issues.Add(new(path, "createdAt is required."));
        }

        if (document.UpdatedAt is null)
        {
            issues.Add(new(path, "updatedAt is required."));
        }

        ValidateSensitiveContent(
            new[]
            {
                title,
                description,
                sourceName,
                document.SourceReference,
                document.Notes
            }.Where(value => value is not null).Cast<string>().ToArray(),
            path,
            issues);

        if (issues.Count != startIssueCount ||
            packageId is null ||
            manufacturer is null ||
            equipmentFamily is null ||
            title is null ||
            description is null ||
            sourceLanguage is null ||
            sourceType is null ||
            sourceName is null ||
            verificationStatus is null ||
            confidence is null ||
            document.CreatedAt is null ||
            document.UpdatedAt is null)
        {
            return null;
        }

        return new ErrorKnowledgePackageManifest(
            packageId,
            manufacturer,
            equipmentFamily.Value,
            Normalize(document.Series),
            title,
            description,
            sourceLanguage,
            sourceType,
            sourceName,
            Normalize(document.SourceReference),
            verificationStatus,
            confidence,
            signalTypes,
            equipmentTypes,
            displaySources,
            document.EntryCountExpected,
            Normalize(document.Notes),
            document.CreatedAt.Value,
            document.UpdatedAt.Value,
            path);
    }

    private static ErrorKnowledgeEntryV2? ValidateAndMapEntry(
        ErrorKnowledgeJsonDocument document,
        string path,
        ICollection<ErrorKnowledgeValidationIssue> issues)
    {
        var startIssueCount = issues.Count;
        var id = Required(document.Id, path, "id", issues);
        var manufacturer = Required(document.Manufacturer, path, "manufacturer", issues);
        var equipmentFamily = ParseRequiredEnum<ErrorKnowledgeEquipmentFamily>(
            document.EquipmentFamily,
            path,
            "equipmentFamily",
            issues);
        var equipmentType = ParseRequiredEnum<ErrorKnowledgeEquipmentType>(
            document.EquipmentType,
            path,
            "equipmentType",
            issues);
        var code = Required(document.Code, path, "code", issues);
        var signalType = ParseRequiredEnum<ErrorKnowledgeSignalType>(
            document.SignalType,
            path,
            "signalType",
            issues);
        var displaySource = ParseRequiredEnum<ErrorKnowledgeDisplaySource>(
            document.DisplaySource,
            path,
            "displaySource",
            issues);
        var systemPart = ParseRequiredEnum<ErrorKnowledgeSystemPart>(
            document.SystemPart,
            path,
            "systemPart",
            issues);
        var severity = ParseRequiredEnum<ErrorKnowledgeSeverity>(
            document.Severity,
            path,
            "severity",
            issues);
        var packageId = Required(document.PackageId, path, "packageId", issues);
        var sourceLanguage = Allowed(
            document.SourceLanguage,
            path,
            "sourceLanguage",
            AllowedLocales,
            issues);
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

        if (document.RequiresQualifiedService is null)
        {
            issues.Add(new(path, "requiresQualifiedService is required."));
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
            equipmentFamily is null ||
            equipmentType is null ||
            code is null ||
            signalType is null ||
            displaySource is null ||
            systemPart is null ||
            severity is null ||
            packageId is null ||
            sourceLanguage is null ||
            sourceType is null ||
            sourceName is null ||
            confidence is null ||
            verificationStatus is null ||
            document.RequiresQualifiedService is null ||
            document.CreatedAt is null ||
            document.UpdatedAt is null)
        {
            return null;
        }

        return new ErrorKnowledgeEntryV2(
            id,
            manufacturer,
            equipmentFamily.Value,
            equipmentType.Value,
            Normalize(document.Series),
            models,
            code,
            signalType.Value,
            displaySource.Value,
            systemPart.Value,
            severity.Value,
            document.RequiresQualifiedService.Value,
            document.CanCustomerContinueOperation,
            packageId,
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
            var audience = ParseRequiredEnum<ErrorKnowledgeAudience>(
                text.Audience,
                textPath,
                "audience",
                issues);
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

    private static void ValidateDuplicatePackages(
        IReadOnlyCollection<ErrorKnowledgePackageManifest> packages,
        ICollection<ErrorKnowledgeValidationIssue> issues)
    {
        foreach (var duplicate in packages
                     .GroupBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            issues.Add(new(
                string.Join(", ", duplicate.Select(package => package.SourcePath)),
                $"duplicate packageId '{duplicate.Key}'."));
        }
    }

    private static void ValidateDuplicateEntries(
        IReadOnlyCollection<ErrorKnowledgeEntryV2> entries,
        ICollection<ErrorKnowledgeValidationIssue> issues)
    {
        foreach (var duplicate in entries
                     .GroupBy(TaxonomyKey, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            issues.Add(new(
                string.Join(", ", duplicate.Select(entry => entry.Id)),
                $"duplicate entry taxonomy key '{duplicate.Key}'."));
        }

        foreach (var duplicate in entries
                     .SelectMany(entry => entry.Texts.Select(text => new
                     {
                         Entry = entry,
                         Text = text,
                         Key = $"{TaxonomyKey(entry)}|{text.Locale.ToUpperInvariant()}|{text.Audience}"
                     }))
                     .GroupBy(item => item.Key, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            issues.Add(new(
                string.Join(", ", duplicate.Select(item => item.Entry.Id).Distinct(StringComparer.Ordinal)),
                $"duplicate localized knowledge key '{duplicate.Key}'."));
        }
    }

    private static void ValidatePackageLinks(
        IReadOnlyCollection<ErrorKnowledgeEntryV2> entries,
        IReadOnlyCollection<ErrorKnowledgePackageManifest> packages,
        ICollection<ErrorKnowledgeValidationIssue> issues)
    {
        var packageLookup = packages
            .GroupBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (!packageLookup.TryGetValue(entry.PackageId, out var package))
            {
                issues.Add(new(entry.Id, $"packageId '{entry.PackageId}' does not reference an existing package."));
                continue;
            }

            if (!entry.Manufacturer.Equals(package.Manufacturer, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new(entry.Id, $"package '{package.PackageId}' manufacturer does not match entry manufacturer."));
            }

            if (entry.EquipmentFamily != package.EquipmentFamily)
            {
                issues.Add(new(entry.Id, $"package '{package.PackageId}' equipmentFamily does not match entry equipmentFamily."));
            }

            if (!string.IsNullOrWhiteSpace(package.Series) &&
                !string.Equals(entry.Series, package.Series, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new(entry.Id, $"package '{package.PackageId}' series does not match entry series."));
            }

            if (package.IntendedSignalTypes.Count > 0 &&
                !package.IntendedSignalTypes.Contains(entry.SignalType))
            {
                issues.Add(new(entry.Id, $"signalType {entry.SignalType} is not allowed by package '{package.PackageId}'."));
            }

            if (package.IntendedEquipmentTypes.Count > 0 &&
                !package.IntendedEquipmentTypes.Contains(entry.EquipmentType))
            {
                issues.Add(new(entry.Id, $"equipmentType {entry.EquipmentType} is not allowed by package '{package.PackageId}'."));
            }

            if (package.IntendedDisplaySources.Count > 0 &&
                !package.IntendedDisplaySources.Contains(entry.DisplaySource))
            {
                issues.Add(new(entry.Id, $"displaySource {entry.DisplaySource} is not allowed by package '{package.PackageId}'."));
            }
        }

        foreach (var package in packages)
        {
            if (package.EntryCountExpected is not null)
            {
                var actual = entries.Count(entry =>
                    entry.PackageId.Equals(package.PackageId, StringComparison.OrdinalIgnoreCase));
                if (actual != package.EntryCountExpected.Value)
                {
                    issues.Add(new(
                        package.SourcePath,
                        $"entryCountExpected is {package.EntryCountExpected.Value}, but actual package entry count is {actual}."));
                }
            }
        }
    }

    private static string TaxonomyKey(ErrorKnowledgeEntryV2 entry) =>
        string.Join(
            "|",
            entry.Manufacturer.Trim().ToUpperInvariant(),
            entry.EquipmentFamily,
            entry.EquipmentType,
            entry.Series?.Trim().ToUpperInvariant() ?? string.Empty,
            string.Join(",", entry.Models.OrderBy(model => model, StringComparer.OrdinalIgnoreCase)
                .Select(model => model.Trim().ToUpperInvariant())),
            entry.Code.Trim().ToUpperInvariant(),
            entry.SignalType,
            entry.DisplaySource);

    private static IReadOnlyList<TEnum> ParseEnumArray<TEnum>(
        IReadOnlyList<string>? values,
        string path,
        string property,
        ICollection<ErrorKnowledgeValidationIssue> issues)
        where TEnum : struct, Enum
    {
        if (values is null)
        {
            issues.Add(new(path, $"{property} must be present."));
            return [];
        }

        var result = new List<TEnum>();
        for (var index = 0; index < values.Count; index++)
        {
            var parsed = ParseRequiredEnum<TEnum>(
                values[index],
                path,
                $"{property}[{index}]",
                issues);
            if (parsed is not null)
            {
                result.Add(parsed.Value);
            }
        }

        return result;
    }

    private static TEnum? ParseRequiredEnum<TEnum>(
        string? value,
        string path,
        string property,
        ICollection<ErrorKnowledgeValidationIssue> issues)
        where TEnum : struct, Enum
    {
        var required = Required(value, path, property, issues);
        if (required is null)
        {
            return null;
        }

        if (!Enum.TryParse<TEnum>(required, ignoreCase: false, out var parsed) ||
            !Enum.IsDefined(parsed))
        {
            issues.Add(new(
                path,
                $"{property} '{required}' is invalid. Allowed: {string.Join(", ", Enum.GetNames<TEnum>())}."));
            return null;
        }

        return parsed;
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

    private static void AddJsonIssue(
        string path,
        JsonException exception,
        ICollection<ErrorKnowledgeValidationIssue> issues) =>
        issues.Add(new(
            path,
            $"invalid JSON at line {exception.LineNumber}, byte {exception.BytePositionInLine}: {exception.Message}"));

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
