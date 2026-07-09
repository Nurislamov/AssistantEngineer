using AssistantEngineer.GreeAliceBridge.Contracts.Registry;

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
        [new GreeAliceRoom("dummy-room-001", "dummy-home-001", "Demo Room", Source)],
        [
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
        ],
        GreeAliceRegistrySafetyBoundary.RegistryMode);

    public GreeAliceRegistrySnapshot GetSnapshot()
    {
        return Snapshot;
    }
}
