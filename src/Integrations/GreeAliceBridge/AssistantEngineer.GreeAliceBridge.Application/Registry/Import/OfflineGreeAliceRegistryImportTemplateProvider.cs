using AssistantEngineer.GreeAliceBridge.Contracts.Registry;
using AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;

namespace AssistantEngineer.GreeAliceBridge.Application.Registry.Import;

public sealed class OfflineGreeAliceRegistryImportTemplateProvider : IGreeAliceRegistryImportTemplateProvider
{
    public GreeAliceRegistryImportDraft GetTemplateDraft()
    {
        GreeAliceRegistryImportAccountDraft account = new(
            "dummy-import-account-001",
            "Dummy import account",
            IsMasked: true,
            IsDummyOrTemplate: true);

        GreeAliceRegistryImportHomeDraft home = new(
            "dummy-import-home-001",
            "Dummy import home",
            IsMasked: true,
            IsDummyOrTemplate: true);

        GreeAliceRegistryImportRoomDraft[] rooms =
        [
            new("dummy-import-room-living-001", "Гостиная", home.ImportHomeId, IsMasked: true, IsDummyOrTemplate: true),
            new("dummy-import-room-bedroom-001", "Спальня", home.ImportHomeId, IsMasked: true, IsDummyOrTemplate: true)
        ];

        GreeAliceRegistryImportDeviceDraft[] devices =
        [
            new(
                "dummy-import-split-ac-001",
                GreeAliceDeviceKind.SplitAc,
                "Dummy split AC",
                "dummy-import-room-living-001",
                "yandex-dummy-import-split-ac-001",
                ExposeToYandex: true,
                IsMasked: true,
                IsDummyOrTemplate: true)
        ];

        GreeAliceRegistryImportVrfGatewayDraft[] gateways =
        [
            new(
                "dummy-import-vrf-gateway-001",
                home.ImportHomeId,
                "Dummy GMV import system",
                "Dummy VRF import gateway",
                IsMasked: true,
                IsDummyOrTemplate: true)
        ];

        GreeAliceRegistryImportVrfChildUnitDraft[] childUnits =
        [
            new(
                "dummy-import-vrf-child-living-001",
                "dummy-import-vrf-gateway-001",
                "Кондиционер гостиная",
                "dummy-import-room-living-001",
                "yandex-dummy-import-vrf-child-living-001",
                ExposeToYandex: true,
                IsMasked: true,
                IsDummyOrTemplate: true),
            new(
                "dummy-import-vrf-child-bedroom-001",
                "dummy-import-vrf-gateway-001",
                "Кондиционер спальня",
                "dummy-import-room-bedroom-001",
                "yandex-dummy-import-vrf-child-bedroom-001",
                ExposeToYandex: true,
                IsMasked: true,
                IsDummyOrTemplate: true)
        ];

        GreeAliceRegistryImportExposureDecision[] decisions =
        [
            new(
                "dummy-import-split-ac-001",
                GreeAliceDeviceKind.SplitAc,
                ExposeToYandex: true,
                Reviewed: true,
                "yandex-dummy-import-split-ac-001",
                "dummy-import-room-living-001"),
            new(
                "dummy-import-vrf-gateway-001",
                GreeAliceDeviceKind.VrfGateway,
                ExposeToYandex: false,
                Reviewed: true,
                StableYandexDeviceId: null,
                RoomId: null),
            new(
                "dummy-import-vrf-child-living-001",
                GreeAliceDeviceKind.VrfChildIndoorUnit,
                ExposeToYandex: true,
                Reviewed: true,
                "yandex-dummy-import-vrf-child-living-001",
                "dummy-import-room-living-001"),
            new(
                "dummy-import-vrf-child-bedroom-001",
                GreeAliceDeviceKind.VrfChildIndoorUnit,
                ExposeToYandex: true,
                Reviewed: true,
                "yandex-dummy-import-vrf-child-bedroom-001",
                "dummy-import-room-bedroom-001")
        ];

        return new GreeAliceRegistryImportDraft(
            account,
            home,
            rooms,
            devices,
            gateways,
            childUnits,
            decisions,
            GreeAliceRegistryImportMode.OfflineTemplate);
    }
}
