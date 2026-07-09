using System.Text.RegularExpressions;
using AssistantEngineer.GreeAliceBridge.Application.Registry;
using AssistantEngineer.GreeAliceBridge.Contracts.Registry;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceOfflineRegistryBoundaryTests
{
    [Fact]
    public void RegistrySafetyBoundaryBlocksLiveDataAndControl()
    {
        Assert.False(GreeAliceRegistrySafetyBoundary.UsesRealGreeCloudData);
        Assert.False(GreeAliceRegistrySafetyBoundary.UsesRealAccountIdentifiers);
        Assert.False(GreeAliceRegistrySafetyBoundary.UsesRealDeviceIdentifiers);
        Assert.False(GreeAliceRegistrySafetyBoundary.AllowsRuntimeControl);
        Assert.False(GreeAliceRegistrySafetyBoundary.AllowsDeviceControl);
        Assert.False(GreeAliceRegistrySafetyBoundary.AllowsMqttConnect);
        Assert.False(GreeAliceRegistrySafetyBoundary.AllowsMqttSubscribe);
        Assert.False(GreeAliceRegistrySafetyBoundary.AllowsMqttPublish);
        Assert.Equal("offline-fixture-registry", GreeAliceRegistrySafetyBoundary.RegistryMode);
    }

    [Fact]
    public void RegistrySnapshotContainsDummyAccountHomeAndRoom()
    {
        GreeAliceRegistrySnapshot snapshot = CreateSnapshot();

        Assert.Equal("dummy-account-001", snapshot.Account.Id);
        Assert.Equal("Demo Account", snapshot.Account.DisplayName);
        Assert.Equal("offline-fixture-registry", snapshot.Account.Source);

        GreeAliceHome home = Assert.Single(snapshot.Homes);
        Assert.Equal("dummy-home-001", home.Id);
        Assert.Equal("dummy-account-001", home.AccountRef);

        GreeAliceRoom room = Assert.Single(snapshot.Rooms, item => item.Id == "dummy-room-001");
        Assert.Equal("dummy-room-001", room.Id);
        Assert.Equal("dummy-home-001", room.HomeRef);
        Assert.Contains(snapshot.Rooms, item => item.Id == "dummy-room-living-001" && item.Name == "Гостиная");
        Assert.Contains(snapshot.Rooms, item => item.Id == "dummy-room-bedroom-001" && item.Name == "Спальня");
    }

    [Fact]
    public void RegistrySnapshotContainsSplitAcVrfGatewayAndVrfChildIndoorUnit()
    {
        GreeAliceRegistrySnapshot snapshot = CreateSnapshot();

        GreeAliceRegisteredDevice splitAc = Assert.Single(snapshot.Devices, device => device.Id == "dummy-gree-ac-001");
        Assert.Equal(GreeAliceDeviceKind.SplitAc, splitAc.Kind);
        Assert.True(splitAc.YandexExposed);
        Assert.Equal("dummy-room-001", splitAc.RoomRef);
        Assert.Null(splitAc.ParentGatewayRef);

        GreeAliceRegisteredDevice gateway = Assert.Single(snapshot.Devices, device => device.Id == "dummy-vrf-gateway-001");
        Assert.Equal(GreeAliceDeviceKind.VrfGateway, gateway.Kind);
        Assert.False(gateway.YandexExposed);
        Assert.Equal("dummy-room-001", gateway.RoomRef);

        GreeAliceRegisteredDevice child = Assert.Single(snapshot.Devices, device => device.Id == "dummy-vrf-child-001");
        Assert.Equal(GreeAliceDeviceKind.VrfChildIndoorUnit, child.Kind);
        Assert.True(child.YandexExposed);
        Assert.Equal("dummy-vrf-gateway-001", child.ParentGatewayRef);
    }

    [Fact]
    public void VrfChildReferencesExistingParentGateway()
    {
        GreeAliceRegistrySnapshot snapshot = CreateSnapshot();

        foreach (GreeAliceRegisteredDevice child in snapshot.Devices.Where(device => device.Kind == GreeAliceDeviceKind.VrfChildIndoorUnit))
        {
            Assert.Contains(
                snapshot.Devices,
                device => device.Id == child.ParentGatewayRef && device.Kind == GreeAliceDeviceKind.VrfGateway);
        }
    }

    [Fact]
    public void RegistryValuesUseOnlyDummyIdentifiers()
    {
        GreeAliceRegistrySnapshot snapshot = CreateSnapshot();

        foreach (string value in EnumerateRegistryValues(snapshot))
        {
            if (value.EndsWith("Account", StringComparison.Ordinal)
                || value.EndsWith("Home", StringComparison.Ordinal)
                || value.EndsWith("Room", StringComparison.Ordinal)
                || value.Contains("Demo ", StringComparison.Ordinal)
                || value.StartsWith("Кондиционер ", StringComparison.Ordinal)
                || value is "Гостиная" or "Спальня"
                || value.StartsWith("offline-", StringComparison.Ordinal)
                || value == GreeAliceDeviceKind.SplitAc
                || value == GreeAliceDeviceKind.VrfGateway
                || value == GreeAliceDeviceKind.VrfChildIndoorUnit)
            {
                continue;
            }

            Assert.StartsWith("dummy-", value, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RegistryValuesDoNotContainMacLikeIdentifiersOrSecretMaterial()
    {
        GreeAliceRegistrySnapshot snapshot = CreateSnapshot();
        Regex macLike = new(@"(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}", RegexOptions.Compiled);

        foreach (string value in EnumerateRegistryValues(snapshot))
        {
            Assert.False(macLike.IsMatch(value), "Registry value must not look like a MAC identifier: " + value);
            Assert.DoesNotContain("token", value, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("password", value, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret", value, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("device-key", value, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static GreeAliceRegistrySnapshot CreateSnapshot()
    {
        return new OfflineGreeAliceRegistryProvider().GetSnapshot();
    }

    private static IEnumerable<string> EnumerateRegistryValues(GreeAliceRegistrySnapshot snapshot)
    {
        yield return snapshot.RegistryMode;
        yield return snapshot.Account.Id;
        yield return snapshot.Account.DisplayName;
        yield return snapshot.Account.Source;

        foreach (GreeAliceHome home in snapshot.Homes)
        {
            yield return home.Id;
            yield return home.AccountRef;
            yield return home.Name;
            yield return home.Source;
        }

        foreach (GreeAliceRoom room in snapshot.Rooms)
        {
            yield return room.Id;
            yield return room.HomeRef;
            yield return room.Name;
            yield return room.Source;
        }

        foreach (GreeAliceRegisteredDevice device in snapshot.Devices)
        {
            yield return device.Id;
            yield return device.DisplayName;
            yield return device.Kind;
            yield return device.RoomRef;
            yield return device.Source;

            if (device.ParentGatewayRef is not null)
            {
                yield return device.ParentGatewayRef;
            }
        }
    }
}
