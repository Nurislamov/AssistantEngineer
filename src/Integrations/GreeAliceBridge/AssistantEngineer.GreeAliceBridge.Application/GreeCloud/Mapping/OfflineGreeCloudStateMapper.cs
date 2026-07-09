using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.Mapping;

namespace AssistantEngineer.GreeAliceBridge.Application.GreeCloud.Mapping;

public sealed class OfflineGreeCloudStateMapper : IGreeCloudStateMapper
{
    private static readonly string[] RequiredFields = ["Pow", "Mod", "SetTem", "TemSen", "WdSpd", "Online"];

    public GreeCloudStateMappingResult Map(GreeCloudMaskedRawStateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        List<GreeCloudStateMappingIssue> issues = [];
        Dictionary<string, string> fields = snapshot.Fields.ToDictionary(
            field => field.Name,
            field => field.MaskedValue,
            StringComparer.Ordinal);

        if (!snapshot.IsKnownDevice)
        {
            issues.Add(new GreeCloudStateMappingIssue(
                "unknown-device",
                null,
                "Device is not present in the offline masked fixture registry."));
        }

        foreach (string requiredField in RequiredFields)
        {
            if (!fields.ContainsKey(requiredField))
            {
                issues.Add(new GreeCloudStateMappingIssue(
                    "missing-field",
                    requiredField,
                    "Masked raw state field is missing."));
            }
        }

        bool isOnline = MapBool(fields, "Online", issues) ?? false;
        bool? isOn = MapBool(fields, "Pow", issues);
        string? mode = MapText(fields, "Mod", ["cool", "heat", "fan_only", "dry", "auto"], issues);
        int? targetTemperatureC = MapInt(fields, "SetTem", issues);
        int? currentTemperatureC = MapInt(fields, "TemSen", issues);
        string? fanSpeed = MapText(fields, "WdSpd", ["auto", "low", "medium", "high"], issues);
        string? swingVertical = MapText(fields, "SwUpDn", ["off", "fixed", "swing"], issues);
        string? swingHorizontal = MapText(fields, "SwLfRig", ["off", "fixed", "swing"], issues);

        GreeCloudNormalizedState state = new(
            snapshot.DeviceId,
            snapshot.IsKnownDevice,
            isOnline,
            isOn,
            mode,
            targetTemperatureC,
            currentTemperatureC,
            fanSpeed,
            swingVertical,
            swingHorizontal,
            "offline-masked-fixture",
            GreeCloudStateMappingSafetyBoundary.MappingMode,
            snapshot.SourceKind,
            issues.ToArray());

        return new GreeCloudStateMappingResult(
            state,
            issues.ToArray(),
            GreeCloudStateMappingSafetyBoundary.MappingMode);
    }

    private static bool? MapBool(
        IReadOnlyDictionary<string, string> fields,
        string fieldName,
        ICollection<GreeCloudStateMappingIssue> issues)
    {
        if (!fields.TryGetValue(fieldName, out string? value))
        {
            return null;
        }

        return value switch
        {
            "1" => true,
            "0" => false,
            _ => AddUnsupportedValueIssue<bool?>(issues, fieldName, value)
        };
    }

    private static int? MapInt(
        IReadOnlyDictionary<string, string> fields,
        string fieldName,
        ICollection<GreeCloudStateMappingIssue> issues)
    {
        if (!fields.TryGetValue(fieldName, out string? value))
        {
            return null;
        }

        if (int.TryParse(value, out int parsed))
        {
            return parsed;
        }

        return AddUnsupportedValueIssue<int?>(issues, fieldName, value);
    }

    private static string? MapText(
        IReadOnlyDictionary<string, string> fields,
        string fieldName,
        IReadOnlyCollection<string> supportedValues,
        ICollection<GreeCloudStateMappingIssue> issues)
    {
        if (!fields.TryGetValue(fieldName, out string? value))
        {
            return null;
        }

        if (supportedValues.Contains(value, StringComparer.Ordinal))
        {
            return value;
        }

        return AddUnsupportedValueIssue<string?>(issues, fieldName, value);
    }

    private static T? AddUnsupportedValueIssue<T>(
        ICollection<GreeCloudStateMappingIssue> issues,
        string fieldName,
        string value)
    {
        issues.Add(new GreeCloudStateMappingIssue(
            "unsupported-field-value",
            fieldName,
            "Masked raw state field has an unsupported value: " + value));

        return default;
    }
}
