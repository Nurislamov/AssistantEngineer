using AssistantEngineer.GreeAliceBridge.Contracts.Registry;
using AssistantEngineer.GreeAliceBridge.Contracts.Registry.Vrf;

namespace AssistantEngineer.GreeAliceBridge.Application.Registry;

public sealed class OfflineGreeAliceRegistryProvider : IGreeAliceOfflineRegistryProvider
{
    private const string Source = "offline-fixture-registry";

    private static readonly GreeAliceDeviceCapabilities AcCapabilities = new(
        OnOff: true,
        Mode: true,
        Temperature: true,
        FanSpeed: true);

    private static readonly GreeAliceDeviceCapabilities GatewayCapabilities = new(
        OnOff: false,
        Mode: false,
        Temperature: false,
        FanSpeed: false);

    private static readonly GreeAliceRegistrySnapshot Snapshot = new(
        new GreeAliceBridgeAccount("dummy-account-001", "Demo Account", Source),
        [new GreeAliceHome("dummy-home-001", "dummy-account-001", "Demo Home", Source)],
        [
            new GreeAliceRoom("dummy-room-001", "dummy-home-001", "Demo Room", Source),
            new GreeAliceRoom("dummy-room-living-001", "dummy-home-001", "Гостиная", Source),
            new GreeAliceRoom("dummy-room-bedroom-001", "dummy-home-001", "Спальня", Source)
        ],
        new GreeAliceRegisteredDevice[]
        {
            new GreeAliceRegisteredDevice(
                "dummy-gree-ac-001",
                "Demo Gree Split AC",
                GreeAliceDeviceKind.SplitAc,
                "dummy-room-001",
                ParentGatewayRef: null,
                YandexExposed: true,
                AcCapabilities,
                Source),
            new GreeAliceRegisteredDevice(
                "dummy-vrf-gateway-001",
                "Demo VRF Gateway",
                GreeAliceDeviceKind.VrfGateway,
                "dummy-room-001",
                ParentGatewayRef: null,
                YandexExposed: false,
                GatewayCapabilities,
                Source),
            new GreeAliceRegisteredDevice(
                "dummy-vrf-child-001",
                "Demo VRF Child Indoor Unit",
                GreeAliceDeviceKind.VrfChildIndoorUnit,
                "dummy-room-001",
                ParentGatewayRef: "dummy-vrf-gateway-001",
                YandexExposed: true,
                AcCapabilities,
                Source)
        }.Concat(GreeAliceVrfSystemTopology.OfflineFixture.ChildUnits.Select(childUnit => new GreeAliceRegisteredDevice(
            childUnit.ChildUnitId,
            childUnit.DisplayName,
            GreeAliceDeviceKind.VrfChildIndoorUnit,
            childUnit.RoomId,
            childUnit.ParentGatewayId,
            childUnit.ExposeToYandex,
            AcCapabilities,
            Source))).ToArray(),
        GreeAliceRegistrySafetyBoundary.RegistryMode);

    public GreeAliceRegistrySnapshot GetSnapshot()
    {
        return Snapshot;
    }
}
