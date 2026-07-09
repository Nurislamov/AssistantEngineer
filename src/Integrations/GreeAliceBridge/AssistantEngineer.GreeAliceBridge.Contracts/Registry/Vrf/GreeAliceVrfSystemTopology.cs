namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry.Vrf;

public sealed record GreeAliceVrfSystemTopology(
    IReadOnlyList<GreeAliceVrfGateway> Gateways,
    IReadOnlyList<GreeAliceVrfChildUnit> ChildUnits,
    IReadOnlyList<GreeAliceVrfRoomBinding> RoomBindings,
    string RuntimeMode)
{
    public static GreeAliceVrfSystemTopology OfflineFixture { get; } = new(
        [
            new GreeAliceVrfGateway(
                "dummy-vrf-gateway-001",
                "Demo VRF Gateway",
                "dummy-home-001",
                RoomId: null,
                "Demo GMV System",
                GatewayKind: "vrf-gateway",
                IsTechnicalDevice: true,
                ExposeToYandex: false,
                RuntimeMode: "offline-fixture")
        ],
        [
            new GreeAliceVrfChildUnit(
                "dummy-vrf-child-living-001",
                "dummy-vrf-gateway-001",
                "yandex-dummy-vrf-child-living-001",
                "Кондиционер гостиная",
                "dummy-room-living-001",
                "Гостиная",
                IndoorUnitAddress: null,
                IndoorUnitModel: null,
                CapacityKw: null,
                ExposeToYandex: true,
                DeviceKind: "vrf-child-indoor-unit",
                RuntimeMode: "offline-fixture"),
            new GreeAliceVrfChildUnit(
                "dummy-vrf-child-bedroom-001",
                "dummy-vrf-gateway-001",
                "yandex-dummy-vrf-child-bedroom-001",
                "Кондиционер спальня",
                "dummy-room-bedroom-001",
                "Спальня",
                IndoorUnitAddress: null,
                IndoorUnitModel: null,
                CapacityKw: null,
                ExposeToYandex: true,
                DeviceKind: "vrf-child-indoor-unit",
                RuntimeMode: "offline-fixture")
        ],
        [
            new GreeAliceVrfRoomBinding("dummy-vrf-child-living-001", "dummy-room-living-001", "Гостиная"),
            new GreeAliceVrfRoomBinding("dummy-vrf-child-bedroom-001", "dummy-room-bedroom-001", "Спальня")
        ],
        RuntimeMode: "offline-fixture");
}
