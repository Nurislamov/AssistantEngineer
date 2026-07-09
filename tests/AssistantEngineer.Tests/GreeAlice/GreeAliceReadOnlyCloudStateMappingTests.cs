using System.Text.RegularExpressions;
using AssistantEngineer.GreeAliceBridge.Application.GreeCloud.Mapping;
using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.Mapping;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceReadOnlyCloudStateMappingTests
{
    [Fact]
    public void MappingSafetyBoundaryUsesOfflineMaskedFixtureOnly()
    {
        Assert.Equal("offline-masked-fixture", GreeCloudStateMappingSafetyBoundary.MappingMode);
        Assert.True(GreeCloudStateMappingSafetyBoundary.UsesMaskedInputOnly);
        Assert.False(GreeCloudStateMappingSafetyBoundary.UsesLiveGreeCloud);
        Assert.False(GreeCloudStateMappingSafetyBoundary.UsesHttpNetwork);
        Assert.False(GreeCloudStateMappingSafetyBoundary.UsesMqttNetwork);
        Assert.False(GreeCloudStateMappingSafetyBoundary.AllowsMqttConnect);
        Assert.False(GreeCloudStateMappingSafetyBoundary.AllowsMqttSubscribe);
        Assert.False(GreeCloudStateMappingSafetyBoundary.AllowsMqttPublish);
        Assert.False(GreeCloudStateMappingSafetyBoundary.AllowsDeviceControl);
        Assert.False(GreeCloudStateMappingSafetyBoundary.AllowsRuntimeControl);
        Assert.False(GreeCloudStateMappingSafetyBoundary.AllowsRawSecrets);
    }

    [Theory]
    [InlineData("dummy-gree-ac-001")]
    [InlineData("dummy-vrf-child-001")]
    public void MaskedFixtureExistsForKnownDummyDevices(string deviceId)
    {
        GreeCloudMaskedRawStateSnapshot snapshot = CreateFixtureProvider().GetSnapshot(deviceId);

        Assert.Equal(deviceId, snapshot.DeviceId);
        Assert.True(snapshot.IsKnownDevice);
        Assert.Equal("offline-masked-fixture", snapshot.SourceKind);
        Assert.Equal("offline-masked-fixture", snapshot.RuntimeMode);
        Assert.Contains(snapshot.Fields, field => field.Name == "Pow");
        Assert.Contains(snapshot.Fields, field => field.Name == "Mod");
        Assert.Contains(snapshot.Fields, field => field.Name == "SetTem");
        Assert.Contains(snapshot.Fields, field => field.Name == "TemSen");
        Assert.Contains(snapshot.Fields, field => field.Name == "WdSpd");
        Assert.Contains(snapshot.Fields, field => field.Name == "SwUpDn");
        Assert.Contains(snapshot.Fields, field => field.Name == "SwLfRig");
        Assert.Contains(snapshot.Fields, field => field.Name == "Online");
        Assert.All(snapshot.Fields, field => Assert.True(field.IsMasked));
    }

    [Fact]
    public void MaskedFixtureForUnknownDeviceIsControlled()
    {
        GreeCloudMaskedRawStateSnapshot snapshot = CreateFixtureProvider().GetSnapshot("unknown-device");

        Assert.Equal("unknown-device", snapshot.DeviceId);
        Assert.False(snapshot.IsKnownDevice);
        Assert.Empty(snapshot.Fields);
        Assert.Equal("offline-masked-fixture", snapshot.RuntimeMode);
    }

    [Fact]
    public void MaskedFixturesContainOnlySafeFieldNamesAndValues()
    {
        IGreeCloudMaskedStateFixtureProvider provider = CreateFixtureProvider();
        Regex macLike = new(@"(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}", RegexOptions.Compiled);
        string[] allowedFields = ["Pow", "Mod", "SetTem", "TemSen", "WdSpd", "SwUpDn", "SwLfRig", "Online"];

        foreach (string deviceId in new[] { "dummy-gree-ac-001", "dummy-vrf-child-001" })
        {
            GreeCloudMaskedRawStateSnapshot snapshot = provider.GetSnapshot(deviceId);
            Assert.All(snapshot.Fields, field => Assert.Contains(field.Name, allowedFields));

            foreach (GreeCloudMaskedRawStateField field in snapshot.Fields)
            {
                Assert.False(macLike.IsMatch(field.MaskedValue), "Masked fixture value must not look like a MAC identifier.");
                Assert.DoesNotContain("token", field.MaskedValue, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("password", field.MaskedValue, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("secret", field.MaskedValue, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("account", field.MaskedValue, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void MapperMapsMaskedFieldsToNormalizedState()
    {
        GreeCloudMaskedRawStateSnapshot snapshot = CreateFixtureProvider().GetSnapshot("dummy-gree-ac-001");

        GreeCloudStateMappingResult result = CreateMapper().Map(snapshot);

        Assert.Equal("offline-masked-fixture", result.RuntimeMode);
        Assert.Empty(result.Issues);
        Assert.Equal("dummy-gree-ac-001", result.State.DeviceId);
        Assert.True(result.State.IsKnownDevice);
        Assert.True(result.State.IsOnline);
        Assert.True(result.State.IsOn);
        Assert.Equal("cool", result.State.Mode);
        Assert.Equal(24, result.State.TargetTemperatureC);
        Assert.Equal(23, result.State.CurrentTemperatureC);
        Assert.Equal("auto", result.State.FanSpeed);
        Assert.Equal("fixed", result.State.SwingVertical);
        Assert.Equal("off", result.State.SwingHorizontal);
        Assert.Equal("offline-masked-fixture", result.State.UpdatedBy);
        Assert.Equal("offline-masked-fixture", result.State.RuntimeMode);
        Assert.Equal("offline-masked-fixture", result.State.SourceKind);
    }

    [Fact]
    public void MapperAddsIssuesForMissingFieldsWithoutThrowing()
    {
        GreeCloudMaskedRawStateSnapshot snapshot = new(
            "dummy-gree-ac-001",
            IsKnownDevice: true,
            [new GreeCloudMaskedRawStateField("Pow", "1", IsMasked: true)],
            "offline-masked-fixture",
            "offline-masked-fixture");

        GreeCloudStateMappingResult result = CreateMapper().Map(snapshot);

        Assert.Contains(result.Issues, issue => issue.Code == "missing-field" && issue.FieldName == "Mod");
        Assert.Contains(result.Issues, issue => issue.Code == "missing-field" && issue.FieldName == "Online");
        Assert.Equal("offline-masked-fixture", result.State.RuntimeMode);
    }

    [Fact]
    public void MapperAddsIssuesForUnknownValuesWithoutThrowing()
    {
        GreeCloudMaskedRawStateSnapshot snapshot = new(
            "dummy-gree-ac-001",
            IsKnownDevice: true,
            [
                new GreeCloudMaskedRawStateField("Pow", "maybe", IsMasked: true),
                new GreeCloudMaskedRawStateField("Mod", "unsupported", IsMasked: true),
                new GreeCloudMaskedRawStateField("SetTem", "hot", IsMasked: true),
                new GreeCloudMaskedRawStateField("TemSen", "cold", IsMasked: true),
                new GreeCloudMaskedRawStateField("WdSpd", "turbo", IsMasked: true),
                new GreeCloudMaskedRawStateField("Online", "maybe", IsMasked: true)
            ],
            "offline-masked-fixture",
            "offline-masked-fixture");

        GreeCloudStateMappingResult result = CreateMapper().Map(snapshot);

        Assert.Contains(result.Issues, issue => issue.Code == "unsupported-field-value" && issue.FieldName == "Pow");
        Assert.Contains(result.Issues, issue => issue.Code == "unsupported-field-value" && issue.FieldName == "Mod");
        Assert.Contains(result.Issues, issue => issue.Code == "unsupported-field-value" && issue.FieldName == "SetTem");
        Assert.Contains(result.Issues, issue => issue.Code == "unsupported-field-value" && issue.FieldName == "Online");
        Assert.Equal("offline-masked-fixture", result.State.RuntimeMode);
    }

    [Fact]
    public void MapperReturnsControlledResultForUnknownDevice()
    {
        GreeCloudMaskedRawStateSnapshot snapshot = CreateFixtureProvider().GetSnapshot("unknown-device");

        GreeCloudStateMappingResult result = CreateMapper().Map(snapshot);

        Assert.Equal("unknown-device", result.State.DeviceId);
        Assert.False(result.State.IsKnownDevice);
        Assert.False(result.State.IsOnline);
        Assert.Contains(result.Issues, issue => issue.Code == "unknown-device");
        Assert.Equal("offline-masked-fixture", result.RuntimeMode);
    }

    [Fact]
    public void ExistingYandexQueryAndActionContractsRemainStable()
    {
        GreeAliceYandexSmartHomeOfflineMappingTests compatibility = new();

        compatibility.QueryResponseContainsOfflineFixtureState();
        compatibility.ActionResponseFailsClosedAndSendsNothing();
    }

    private static IGreeCloudMaskedStateFixtureProvider CreateFixtureProvider()
    {
        return new OfflineGreeCloudMaskedStateFixtureProvider();
    }

    private static IGreeCloudStateMapper CreateMapper()
    {
        return new OfflineGreeCloudStateMapper();
    }
}
