namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;

public static class EquipmentDiagnosticBotRequestLimits
{
    public const int Manufacturer = 80;
    public const int Code = 32;
    public const int FreeText = 500;
    public const int Series = 120;
    public const int ModelCode = 120;
    public const int SiteContext = 300;
    public const int PreferredLanguage = 16;
    public const int MeasurementCount = 20;
    public const int MeasurementName = 80;
    public const int MeasurementValue = 120;

    // Reserved for a future structured measurement contract. The current public contract is name -> value.
    public const int MeasurementUnit = 40;
}

public sealed record EquipmentDiagnosticBotRequestValidationResult(
    EquipmentDiagnosticBotRequest Request,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

public static class EquipmentDiagnosticBotRequestPolicy
{
    public static EquipmentDiagnosticBotRequestValidationResult ValidateAndNormalize(
        EquipmentDiagnosticBotRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var manufacturer = ValidateText(
            request.Manufacturer,
            nameof(request.Manufacturer),
            EquipmentDiagnosticBotRequestLimits.Manufacturer,
            required: true,
            errors);
        var code = ValidateText(
            request.Code,
            nameof(request.Code),
            EquipmentDiagnosticBotRequestLimits.Code,
            required: true,
            errors);
        var freeText = ValidateText(
            request.FreeText,
            nameof(request.FreeText),
            EquipmentDiagnosticBotRequestLimits.FreeText,
            required: false,
            errors);
        var series = ValidateText(
            request.Series,
            nameof(request.Series),
            EquipmentDiagnosticBotRequestLimits.Series,
            required: false,
            errors);
        var modelCode = ValidateText(
            request.ModelCode,
            nameof(request.ModelCode),
            EquipmentDiagnosticBotRequestLimits.ModelCode,
            required: false,
            errors);
        var preferredLanguage = ValidateText(
            request.PreferredLanguage,
            nameof(request.PreferredLanguage),
            EquipmentDiagnosticBotRequestLimits.PreferredLanguage,
            required: false,
            errors);
        var siteContext = ValidateText(
            request.SiteContext,
            nameof(request.SiteContext),
            EquipmentDiagnosticBotRequestLimits.SiteContext,
            required: false,
            errors);
        var measurements = ValidateMeasurements(request.OperatorProvidedMeasurements, errors);

        var normalized = request with
        {
            Manufacturer = manufacturer,
            Code = code,
            FreeText = freeText,
            Series = series,
            ModelCode = modelCode,
            PreferredLanguage = preferredLanguage,
            OperatorProvidedMeasurements = measurements,
            SiteContext = siteContext
        };

        return new EquipmentDiagnosticBotRequestValidationResult(
            normalized,
            errors.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<string>)pair.Value.ToArray(),
                StringComparer.Ordinal));
    }

    private static IReadOnlyDictionary<string, string>? ValidateMeasurements(
        IReadOnlyDictionary<string, string>? measurements,
        IDictionary<string, List<string>> errors)
    {
        if (measurements is null)
        {
            return null;
        }

        if (measurements.Count > EquipmentDiagnosticBotRequestLimits.MeasurementCount)
        {
            AddError(
                errors,
                nameof(EquipmentDiagnosticBotRequest.OperatorProvidedMeasurements),
                $"Operator-provided measurements must contain at most {EquipmentDiagnosticBotRequestLimits.MeasurementCount} items.");
        }

        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var measurement in measurements)
        {
            var key = ValidateText(
                measurement.Key,
                $"{nameof(EquipmentDiagnosticBotRequest.OperatorProvidedMeasurements)}.name",
                EquipmentDiagnosticBotRequestLimits.MeasurementName,
                required: true,
                errors) ?? string.Empty;
            var value = ValidateText(
                measurement.Value,
                $"{nameof(EquipmentDiagnosticBotRequest.OperatorProvidedMeasurements)}.value",
                EquipmentDiagnosticBotRequestLimits.MeasurementValue,
                required: true,
                errors) ?? string.Empty;

            if (key.Length > 0 && !normalized.TryAdd(key, value))
            {
                AddError(
                    errors,
                    nameof(EquipmentDiagnosticBotRequest.OperatorProvidedMeasurements),
                    $"Measurement name '{key}' is duplicated after trimming.");
            }
        }

        return normalized;
    }

    private static string? ValidateText(
        string? value,
        string field,
        int maxLength,
        bool required,
        IDictionary<string, List<string>> errors)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            if (required)
            {
                AddError(errors, field, $"{field} is required.");
            }

            return normalized;
        }

        if (normalized.Length > maxLength)
        {
            AddError(errors, field, $"{field} must be {maxLength} characters or fewer.");
        }

        if (normalized.Any(IsDisallowedControlCharacter))
        {
            AddError(errors, field, $"{field} contains disallowed control characters.");
        }

        return normalized;
    }

    private static bool IsDisallowedControlCharacter(char character) =>
        char.IsControl(character) && character is not ('\r' or '\n' or '\t');

    private static void AddError(
        IDictionary<string, List<string>> errors,
        string field,
        string error)
    {
        if (!errors.TryGetValue(field, out var fieldErrors))
        {
            fieldErrors = [];
            errors[field] = fieldErrors;
        }

        fieldErrors.Add(error);
    }
}
