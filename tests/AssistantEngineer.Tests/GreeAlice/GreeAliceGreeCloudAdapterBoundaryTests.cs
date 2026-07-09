using System.Text.RegularExpressions;
using AssistantEngineer.GreeAliceBridge.Application.GreeCloud;
using AssistantEngineer.GreeAliceBridge.Application.Registry;
using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud;
using AssistantEngineer.GreeAliceBridge.Contracts.Registry;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceGreeCloudAdapterBoundaryTests
{
    [Fact]
    public void AdapterSafetyBoundaryIsOfflineFakeAndBlocksLiveOperations()
    {
        Assert.Equal("offline-fake", GreeCloudAdapterSafetyBoundary.AdapterMode);
        Assert.False(GreeCloudAdapterSafetyBoundary.UsesLiveGreeCloud);
        Assert.False(GreeCloudAdapterSafetyBoundary.UsesHttpNetwork);
        Assert.False(GreeCloudAdapterSafetyBoundary.UsesMqttNetwork);
        Assert.False(GreeCloudAdapterSafetyBoundary.AllowsMqttConnect);
        Assert.False(GreeCloudAdapterSafetyBoundary.AllowsMqttSubscribe);
        Assert.False(GreeCloudAdapterSafetyBoundary.AllowsMqttPublish);
        Assert.False(GreeCloudAdapterSafetyBoundary.AllowsDeviceControl);
        Assert.False(GreeCloudAdapterSafetyBoundary.AllowsRuntimeControl);
    }

    [Fact]
    public async Task ReadAdapterDiscoversOnlyDummyOfflineRegistryDevices()
    {
        IGreeCloudReadAdapter adapter = CreateReadAdapter();

        IReadOnlyList<GreeCloudDeviceDescriptor> devices = await adapter.DiscoverDevicesAsync();

        Assert.Equal(3, devices.Count);
        Assert.Contains(devices, device => device.DeviceId == "dummy-gree-ac-001" && device.Kind == GreeAliceDeviceKind.SplitAc);
        Assert.Contains(devices, device => device.DeviceId == "dummy-vrf-gateway-001" && device.Kind == GreeAliceDeviceKind.VrfGateway);
        Assert.Contains(devices, device => device.DeviceId == "dummy-vrf-child-001" && device.Kind == GreeAliceDeviceKind.VrfChildIndoorUnit);
        Assert.All(devices, device =>
        {
            Assert.StartsWith("dummy-", device.DeviceId, StringComparison.Ordinal);
            Assert.Equal("offline-fake", device.Source);
            Assert.Equal("offline-fake", device.AdapterMode);
        });
    }

    [Fact]
    public async Task ReadAdapterReturnsOfflineStateForKnownSplitAc()
    {
        IGreeCloudReadAdapter adapter = CreateReadAdapter();

        GreeCloudDeviceStateSnapshot state = await adapter.GetDeviceStateAsync("dummy-gree-ac-001");

        Assert.Equal("dummy-gree-ac-001", state.DeviceId);
        Assert.Equal("offline-fixture", state.Status);
        Assert.True(state.Online);
        Assert.True(state.On);
        Assert.Equal("cool", state.Mode);
        Assert.Equal(24, state.TargetTemperatureC);
        Assert.Equal("auto", state.FanSpeed);
        Assert.Equal("offline-fake", state.AdapterMode);
    }

    [Fact]
    public async Task ReadAdapterReturnsOfflineStateForKnownVrfChild()
    {
        IGreeCloudReadAdapter adapter = CreateReadAdapter();

        GreeCloudDeviceStateSnapshot state = await adapter.GetDeviceStateAsync("dummy-vrf-child-001");

        Assert.Equal("dummy-vrf-child-001", state.DeviceId);
        Assert.Equal("offline-fixture", state.Status);
        Assert.True(state.Online);
        Assert.True(state.On);
        Assert.Equal("cool", state.Mode);
        Assert.Equal("offline-fake", state.AdapterMode);
    }

    [Fact]
    public async Task ReadAdapterReturnsControlledUnknownStateForUnknownDevice()
    {
        IGreeCloudReadAdapter adapter = CreateReadAdapter();

        GreeCloudDeviceStateSnapshot state = await adapter.GetDeviceStateAsync("unknown-device");

        Assert.Equal("unknown-device", state.DeviceId);
        Assert.Equal("offline-unknown", state.Status);
        Assert.False(state.Online);
        Assert.Null(state.On);
        Assert.Equal("unknown", state.Mode);
        Assert.Equal("unknown", state.FanSpeed);
        Assert.Equal("offline-fake", state.AdapterMode);
    }

    [Fact]
    public async Task ReadAdapterDescriptorsDoNotExposeMacLikeOrSecretMaterial()
    {
        IGreeCloudReadAdapter adapter = CreateReadAdapter();
        Regex macLike = new(@"(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}", RegexOptions.Compiled);

        IReadOnlyList<GreeCloudDeviceDescriptor> devices = await adapter.DiscoverDevicesAsync();

        foreach (GreeCloudDeviceDescriptor device in devices)
        {
            foreach (string value in EnumerateDescriptorValues(device))
            {
                Assert.False(macLike.IsMatch(value), "Descriptor value must not look like a MAC identifier: " + value);
                Assert.DoesNotContain("token", value, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("password", value, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("secret", value, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("key", value, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Theory]
    [InlineData("dummy-gree-ac-001", "devices.capabilities.on_off")]
    [InlineData("unknown-device", "devices.capabilities.on_off")]
    [InlineData("dummy-gree-ac-001", "devices.capabilities.unknown")]
    public async Task ControlAdapterAlwaysFailsClosedAndSendsNothing(string deviceId, string capability)
    {
        IGreeCloudControlAdapter adapter = new OfflineGreeCloudControlAdapter();

        GreeCloudControlResult result = await adapter.ExecuteControlAsync(new GreeCloudControlRequest(
            deviceId,
            capability,
            "set",
            "true"));

        Assert.Equal(deviceId, result.DeviceId);
        Assert.Equal(capability, result.Capability);
        Assert.Equal("dry-run-fail-closed", result.Status);
        Assert.False(result.SentToGreeCloud);
        Assert.False(result.SentToMqtt);
        Assert.False(result.SentToDevice);
        Assert.Equal("offline-fake", result.AdapterMode);
        Assert.False(GreeCloudAdapterSafetyBoundary.AllowsRuntimeControl);
    }

    private static IGreeCloudReadAdapter CreateReadAdapter()
    {
        return new OfflineGreeCloudReadAdapter(new OfflineGreeAliceRegistryProvider());
    }

    private static IEnumerable<string> EnumerateDescriptorValues(GreeCloudDeviceDescriptor descriptor)
    {
        yield return descriptor.DeviceId;
        yield return descriptor.DisplayName;
        yield return descriptor.Kind;
        yield return descriptor.RoomRef;
        yield return descriptor.Source;
        yield return descriptor.AdapterMode;

        if (descriptor.ParentGatewayRef is not null)
        {
            yield return descriptor.ParentGatewayRef;
        }
    }
}
