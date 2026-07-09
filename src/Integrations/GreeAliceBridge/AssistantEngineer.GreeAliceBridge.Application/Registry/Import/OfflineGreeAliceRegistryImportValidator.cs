using System.Text.RegularExpressions;
using AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;

namespace AssistantEngineer.GreeAliceBridge.Application.Registry.Import;

public sealed class OfflineGreeAliceRegistryImportValidator : IGreeAliceRegistryImportValidator
{
    private static readonly Regex MacLikePattern = new(
        "(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public GreeAliceRegistryImportValidationResult Validate(GreeAliceRegistryImportDraft? draft)
    {
        List<GreeAliceRegistryImportValidationIssue> issues = [];

        if (draft is null)
        {
            issues.Add(CreateIssue("draft.null", "Import draft is required.", "draft"));

            return new GreeAliceRegistryImportValidationResult(false, issues);
        }

        if (draft.ImportMode != GreeAliceRegistryImportMode.OfflineTemplate)
        {
            issues.Add(CreateIssue("import.mode", "Only offline-template import mode is currently allowed.", "importMode"));
        }

        ValidateRecordSafety(draft.Account.ImportAccountId, draft.Account.DisplayName, draft.Account.IsMasked, draft.Account.IsDummyOrTemplate, "account", issues);
        ValidateRecordSafety(draft.Home.ImportHomeId, draft.Home.DisplayName, draft.Home.IsMasked, draft.Home.IsDummyOrTemplate, "home", issues);

        foreach (GreeAliceRegistryImportRoomDraft room in draft.Rooms)
        {
            ValidateRecordSafety(room.ImportRoomId, room.DisplayName, room.IsMasked, room.IsDummyOrTemplate, "rooms." + room.ImportRoomId, issues);

            if (room.HomeId != draft.Home.ImportHomeId)
            {
                issues.Add(CreateIssue("room.unknownHome", "Room must reference the draft home.", "rooms." + room.ImportRoomId + ".homeId"));
            }
        }

        HashSet<string> roomIds = draft.Rooms.Select(room => room.ImportRoomId).ToHashSet(StringComparer.Ordinal);
        HashSet<string> gatewayIds = draft.VrfGateways.Select(gateway => gateway.ImportGatewayId).ToHashSet(StringComparer.Ordinal);

        ValidateDuplicateIds(
            draft.Devices.Select(device => device.ImportDeviceId)
                .Concat(draft.VrfGateways.Select(gateway => gateway.ImportGatewayId))
                .Concat(draft.VrfChildUnits.Select(child => child.ImportChildUnitId)),
            "import.id",
            "Import object id must be unique.",
            issues);

        ValidateDuplicateIds(
            draft.Devices.Where(device => device.ExposeToYandex).Select(device => device.StableYandexDeviceId)
                .Concat(draft.VrfChildUnits.Where(child => child.ExposeToYandex).Select(child => child.StableYandexDeviceId)),
            "stableYandexDeviceId.duplicate",
            "Stable Yandex device id must be unique.",
            issues);

        foreach (GreeAliceRegistryImportDeviceDraft device in draft.Devices)
        {
            ValidateRecordSafety(device.ImportDeviceId, device.DisplayName, device.IsMasked, device.IsDummyOrTemplate, "devices." + device.ImportDeviceId, issues);
            ValidateStableId(device.StableYandexDeviceId, "devices." + device.ImportDeviceId + ".stableYandexDeviceId", issues);

            if (device.ExposeToYandex)
            {
                ValidateExposure(device.StableYandexDeviceId, device.RoomId, roomIds, "devices." + device.ImportDeviceId, issues);
            }
        }

        foreach (GreeAliceRegistryImportVrfGatewayDraft gateway in draft.VrfGateways)
        {
            ValidateRecordSafety(gateway.ImportGatewayId, gateway.DisplayName, gateway.IsMasked, gateway.IsDummyOrTemplate, "vrfGateways." + gateway.ImportGatewayId, issues);
            ValidateRecordSafety(gateway.ImportGatewayId, gateway.SystemName, gateway.IsMasked, gateway.IsDummyOrTemplate, "vrfGateways." + gateway.ImportGatewayId + ".system", issues);

            if (gateway.HomeId != draft.Home.ImportHomeId)
            {
                issues.Add(CreateIssue("gateway.unknownHome", "VRF gateway must reference the draft home.", "vrfGateways." + gateway.ImportGatewayId + ".homeId"));
            }

            if (gateway.ExposeToYandex || !gateway.IsTechnicalDevice)
            {
                issues.Add(CreateIssue("gateway.exposed", "VRF gateway remains internal at this stage.", "vrfGateways." + gateway.ImportGatewayId));
            }
        }

        foreach (GreeAliceRegistryImportVrfChildUnitDraft child in draft.VrfChildUnits)
        {
            ValidateRecordSafety(child.ImportChildUnitId, child.DisplayName, child.IsMasked, child.IsDummyOrTemplate, "vrfChildUnits." + child.ImportChildUnitId, issues);
            ValidateStableId(child.StableYandexDeviceId, "vrfChildUnits." + child.ImportChildUnitId + ".stableYandexDeviceId", issues);

            if (!gatewayIds.Contains(child.ParentGatewayId))
            {
                issues.Add(CreateIssue("vrfChild.unknownGateway", "VRF child unit must reference an existing gateway.", "vrfChildUnits." + child.ImportChildUnitId + ".parentGatewayId"));
            }

            if (child.ExposeToYandex)
            {
                ValidateExposure(child.StableYandexDeviceId, child.RoomId, roomIds, "vrfChildUnits." + child.ImportChildUnitId, issues);
            }
        }

        foreach (GreeAliceRegistryImportExposureDecision decision in draft.ExposureDecisions)
        {
            ValidateStableId(decision.StableYandexDeviceId, "exposureDecisions." + decision.ImportObjectId + ".stableYandexDeviceId", issues);

            if (decision.ExposeToYandex && !decision.Reviewed)
            {
                issues.Add(CreateIssue("exposure.reviewRequired", "Exposure requires manual review.", "exposureDecisions." + decision.ImportObjectId));
            }
        }

        return new GreeAliceRegistryImportValidationResult(issues.Count == 0, issues);
    }

    private static void ValidateExposure(
        string? stableYandexDeviceId,
        string? roomId,
        HashSet<string> roomIds,
        string path,
        List<GreeAliceRegistryImportValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(stableYandexDeviceId))
        {
            issues.Add(CreateIssue("stableYandexDeviceId.required", "Exposed device requires stable Yandex device id.", path + ".stableYandexDeviceId"));
        }

        if (string.IsNullOrWhiteSpace(roomId))
        {
            issues.Add(CreateIssue("room.required", "Exposed device requires room binding.", path + ".roomId"));
        }
        else if (!roomIds.Contains(roomId))
        {
            issues.Add(CreateIssue("room.unknown", "Room binding must reference an existing room.", path + ".roomId"));
        }
    }

    private static void ValidateStableId(
        string? stableYandexDeviceId,
        string path,
        List<GreeAliceRegistryImportValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(stableYandexDeviceId))
        {
            return;
        }

        if (MacLikePattern.IsMatch(stableYandexDeviceId))
        {
            issues.Add(CreateIssue("stableYandexDeviceId.macLike", "Stable Yandex device id must not be derived from hardware-like material.", path));
        }

        if (LooksLikeRealIdentifier(stableYandexDeviceId))
        {
            issues.Add(CreateIssue("stableYandexDeviceId.realLike", "Stable Yandex device id must be dummy/template material in this stage.", path));
        }
    }

    private static void ValidateRecordSafety(
        string id,
        string displayName,
        bool isMasked,
        bool isDummyOrTemplate,
        string path,
        List<GreeAliceRegistryImportValidationIssue> issues)
    {
        string combined = string.Join("|", id, displayName);

        if (!isMasked || !isDummyOrTemplate)
        {
            issues.Add(CreateIssue("record.notTemplate", "Import record must be masked dummy/template data.", path));
        }

        if (!IsDummyOrTemplateIdentifier(id))
        {
            issues.Add(CreateIssue("id.notTemplate", "Import id must use dummy/template material.", path + ".id"));
        }

        if (MacLikePattern.IsMatch(combined))
        {
            issues.Add(CreateIssue("id.macLike", "Import data must not contain hardware-like identifiers.", path));
        }

        if (ContainsSensitiveMarker(combined))
        {
            issues.Add(CreateIssue("value.sensitive", "Import data must not contain sensitive material.", path));
        }

        if (LooksLikeRealIdentifier(combined))
        {
            issues.Add(CreateIssue("id.realLike", "Import data must not contain real-looking account or device identifiers.", path));
        }
    }

    private static void ValidateDuplicateIds(
        IEnumerable<string?> values,
        string code,
        string message,
        List<GreeAliceRegistryImportValidationIssue> issues)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        HashSet<string> reported = new(StringComparer.Ordinal);

        foreach (string value in values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!))
        {
            if (!seen.Add(value) && reported.Add(value))
            {
                issues.Add(CreateIssue(code, message, value));
            }
        }
    }

    private static bool IsDummyOrTemplateIdentifier(string value)
    {
        return value.StartsWith("dummy-", StringComparison.Ordinal)
            || value.StartsWith("template-", StringComparison.Ordinal)
            || value.StartsWith("yandex-dummy-", StringComparison.Ordinal)
            || value.StartsWith("yandex-template-", StringComparison.Ordinal);
    }

    private static bool ContainsSensitiveMarker(string value)
    {
        string[] markers =
        [
            "cred" + "ential",
            "sec" + "ret",
            "tok" + "en",
            "pass" + "word",
            "api" + "key",
            "device" + "key",
            "auth" + "key"
        ];

        return markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static bool LooksLikeRealIdentifier(string value)
    {
        string[] markers =
        [
            "real-" + "account-",
            "real-" + "device-",
            "cloud-" + "account-",
            "cloud-" + "device-"
        ];

        return markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static GreeAliceRegistryImportValidationIssue CreateIssue(string code, string message, string path)
    {
        return new GreeAliceRegistryImportValidationIssue(code, message, "error", path);
    }
}
