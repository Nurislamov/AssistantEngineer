namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;

public static class GreeAliceRegistryImportExposurePolicy
{
    public static bool CanAutoExposeDiscoveredDevice => GreeAliceRegistryImportBoundary.AutoExposeDiscoveredDevices;

    public static bool CanExposeDevice(GreeAliceRegistryImportDeviceDraft device)
    {
        return device.ExposeToYandex
            && HasStableId(device.StableYandexDeviceId)
            && HasRoomBinding(device.RoomId);
    }

    public static bool CanExposeVrfGateway(GreeAliceRegistryImportVrfGatewayDraft gateway)
    {
        return gateway.ExposeToYandex && !gateway.IsTechnicalDevice;
    }

    public static bool CanExposeVrfChildUnit(GreeAliceRegistryImportVrfChildUnitDraft childUnit)
    {
        return childUnit.ExposeToYandex
            && HasStableId(childUnit.StableYandexDeviceId)
            && HasRoomBinding(childUnit.RoomId);
    }

    public static bool CanExposeUnknownOrInternalDevice(bool explicitExposure, bool isInternal)
    {
        return explicitExposure && !isInternal;
    }

    private static bool HasStableId(string? stableYandexDeviceId)
    {
        return !string.IsNullOrWhiteSpace(stableYandexDeviceId);
    }

    private static bool HasRoomBinding(string? roomId)
    {
        return !string.IsNullOrWhiteSpace(roomId);
    }
}
