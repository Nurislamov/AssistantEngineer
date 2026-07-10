using System.Reflection;
using System.Text.Json;
using AssistantEngineer.Tools.GreeCloudProbe.GreePlusCommands;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceGreePlusCommandBuilderTests
{
    [Theory]
    [MemberData(nameof(CommandCases))]
    public void BuilderReturnsExactCommandPayloads(string caseId, GreePlusCommandPayload payload, string[] expectedOpt, object[] expectedP)
    {
        Assert.Equal("cmd", payload.T);
        Assert.Equal(expectedOpt, payload.Opt);
        Assert.Equal(expectedP, payload.P);
        Assert.Equal(payload.Opt.Count, payload.P.Count);

        AssertPayloadMatchesDocumentedMap(caseId, payload);
    }

    [Fact]
    public void PayloadModelDoesNotExposeDeviceIdentifierProperties()
    {
        string[] propertyNames = typeof(GreePlusCommandPayload)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(static property => property.Name)
            .ToArray();

        Assert.Equal(["T", "Opt", "P"], propertyNames);
        Assert.DoesNotContain(propertyNames, static name => name.Contains("Mac", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, static name => name.Contains("Device", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PayloadSerializesToGreePlusShapeOnly()
    {
        string json = JsonSerializer.Serialize(GreePlusCommandBuilder.PowerOn());
        using JsonDocument document = JsonDocument.Parse(json);
        string[] names = document.RootElement.EnumerateObject().Select(static property => property.Name).ToArray();

        Assert.Equal(["t", "opt", "p"], names);
        Assert.Equal("cmd", document.RootElement.GetProperty("t").GetString());
    }

    [Fact]
    public void TemperatureRangeIsConservativeAndInclusive()
    {
        Assert.Equal(16, GreePlusCommandBuilder.SetTemperature(16).P[0]);
        Assert.Equal(30, GreePlusCommandBuilder.SetTemperature(30).P[0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => GreePlusCommandBuilder.SetTemperature(15));
        Assert.Throws<ArgumentOutOfRangeException>(() => GreePlusCommandBuilder.SetTemperature(31));
        Assert.Throws<ArgumentOutOfRangeException>(() => GreePlusCommandBuilder.PowerOff(15));
        Assert.Throws<ArgumentOutOfRangeException>(() => GreePlusCommandBuilder.SetMode(GreePlusMode.Cool, 31));
    }

    [Fact]
    public void UnknownEnumValuesAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GreePlusCommandBuilder.SetMode((GreePlusMode)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => GreePlusCommandBuilder.SetFan((GreePlusFanSpeed)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => GreePlusCommandBuilder.SetFeature((GreePlusFeature)999, enabled: true));
        Assert.Throws<ArgumentOutOfRangeException>(() => GreePlusCommandBuilder.SetVerticalSwing((GreePlusSwingPosition)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => GreePlusCommandBuilder.SetHorizontalSwing((GreePlusSwingPosition)999));
    }

    [Fact]
    public void CommandBuilderFilesStayOfflineAndLocalOnly()
    {
        string root = FindRepositoryRoot();
        string[] relativePaths =
        [
            Path.Combine("tools", "AssistantEngineer.Tools.GreeCloudProbe", "GreePlusCommands", "GreePlusCommandBuilder.cs"),
            Path.Combine("tools", "AssistantEngineer.Tools.GreeCloudProbe", "GreePlusCommands", "GreePlusCommandPayload.cs"),
            Path.Combine("tools", "AssistantEngineer.Tools.GreeCloudProbe", "GreePlusCommands", "GreePlusFanSpeed.cs"),
            Path.Combine("tools", "AssistantEngineer.Tools.GreeCloudProbe", "GreePlusCommands", "GreePlusFeature.cs"),
            Path.Combine("tools", "AssistantEngineer.Tools.GreeCloudProbe", "GreePlusCommands", "GreePlusMode.cs"),
            Path.Combine("tools", "AssistantEngineer.Tools.GreeCloudProbe", "GreePlusCommands", "GreePlusSwingPosition.cs"),
            Path.Combine("tests", "AssistantEngineer.Tests", "GreeAlice", "GreeAliceGreePlusCommandBuilderTests.cs")
        ];
        string combined = string.Join(
            Environment.NewLine,
            relativePaths.Select(path => File.ReadAllText(Path.Combine(root, path))));

        string[] forbidden =
        [
            "CON" + "NECT",
            "PUB" + "LISH",
            "SUB" + "SCRIBE",
            "access" + "_token",
            "home" + "Id",
            "u" + "id",
            "." + "local",
            "m" + "qtt"
        ];

        foreach (string value in forbidden)
        {
            Assert.DoesNotContain(value, combined, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static IEnumerable<object[]> CommandCases()
    {
        yield return Case("PowerOn", GreePlusCommandBuilder.PowerOn(), ["Pow", "WdSpd", "Quiet", "Tur"], [1, 0, 0, 0]);
        yield return Case("PowerOff", GreePlusCommandBuilder.PowerOff(), ["TemUn", "SetTem", "TemRec", "Pow", "SwhSlp", "SlpMod", "Air"], [0, 25, 0, 0, 0, 0, 0]);
        yield return Case("SetTemperature25", GreePlusCommandBuilder.SetTemperature(25), ["SetTem", "TemUn", "TemRec"], [25, 0, 0]);
        yield return Case("ModeAuto", GreePlusCommandBuilder.SetMode(GreePlusMode.Auto), ["Dmod", "Dwet", "Mod"], [15, 0, 0]);
        yield return Case("ModeCool25", GreePlusCommandBuilder.SetMode(GreePlusMode.Cool), ["Dmod", "Dwet", "Mod", "SetTem", "TemRec"], [15, 0, 1, 25, 0]);
        yield return Case("ModeDry", GreePlusCommandBuilder.SetMode(GreePlusMode.Dry), ["AssHt", "Dmod", "Dwet", "Mod", "WdSpd", "Quiet", "Tur"], [0, 15, 0, 2, 1, 0, 0]);
        yield return Case("ModeFan", GreePlusCommandBuilder.SetMode(GreePlusMode.Fan), ["Dmod", "Dwet", "Mod"], [15, 0, 3]);
        yield return Case("ModeHeat", GreePlusCommandBuilder.SetMode(GreePlusMode.Heat), ["Dmod", "Dwet", "Mod"], [15, 0, 4]);
        yield return Case("FanAuto", GreePlusCommandBuilder.SetFan(GreePlusFanSpeed.Auto), ["WdSpd", "Quiet", "Tur"], [0, 0, 0]);
        yield return Case("FanLow", GreePlusCommandBuilder.SetFan(GreePlusFanSpeed.Low), ["WdSpd", "Quiet", "Tur"], [1, 0, 0]);
        yield return Case("FanMediumLow", GreePlusCommandBuilder.SetFan(GreePlusFanSpeed.MediumLow), ["WdSpd", "Quiet", "Tur"], [2, 0, 0]);
        yield return Case("FanMedium", GreePlusCommandBuilder.SetFan(GreePlusFanSpeed.Medium), ["WdSpd", "Quiet", "Tur"], [3, 0, 0]);
        yield return Case("FanMediumHigh", GreePlusCommandBuilder.SetFan(GreePlusFanSpeed.MediumHigh), ["WdSpd", "Quiet", "Tur"], [4, 0, 0]);
        yield return Case("FanHigh", GreePlusCommandBuilder.SetFan(GreePlusFanSpeed.High), ["WdSpd", "Quiet", "Tur"], [5, 0, 0]);
        yield return Case("FanQuiet", GreePlusCommandBuilder.SetFan(GreePlusFanSpeed.Quiet), ["Quiet", "Tur"], [2, 0]);
        yield return Case("FanTurbo", GreePlusCommandBuilder.SetFan(GreePlusFanSpeed.Turbo), ["Quiet", "Tur"], [0, 1]);
        yield return Case("FeatureLightOn", GreePlusCommandBuilder.SetFeature(GreePlusFeature.Light, enabled: true), ["Lig"], [1]);
        yield return Case("FeatureLightOff", GreePlusCommandBuilder.SetFeature(GreePlusFeature.Light, enabled: false), ["Lig"], [0]);
        yield return Case("FeatureBlowOn", GreePlusCommandBuilder.SetFeature(GreePlusFeature.Blow, enabled: true), ["Blo"], [1]);
        yield return Case("FeatureBlowOff", GreePlusCommandBuilder.SetFeature(GreePlusFeature.Blow, enabled: false), ["Blo"], [0]);
        yield return Case("FeatureHealthOn", GreePlusCommandBuilder.SetFeature(GreePlusFeature.Health, enabled: true), ["Health"], [1]);
        yield return Case("FeatureHealthOff", GreePlusCommandBuilder.SetFeature(GreePlusFeature.Health, enabled: false), ["Health"], [0]);
        yield return Case("FeatureEnergySaveOn", GreePlusCommandBuilder.SetFeature(GreePlusFeature.EnergySave, enabled: true), ["WdSpd", "Quiet", "Tur", "SvSt"], [0, 0, 0, 1]);
        yield return Case("FeatureEnergySaveOff", GreePlusCommandBuilder.SetFeature(GreePlusFeature.EnergySave, enabled: false), ["SvSt"], [0]);
        yield return Case("FeatureSleepOn", GreePlusCommandBuilder.SetFeature(GreePlusFeature.Sleep, enabled: true), ["SvSt", "Dmod", "SlpMod", "SwhSlp"], [0, 15, 1, 1]);
        yield return Case("FeatureSleepOff", GreePlusCommandBuilder.SetFeature(GreePlusFeature.Sleep, enabled: false), ["Dmod", "SlpMod", "SwhSlp"], [15, 0, 0]);
        yield return Case("VerticalSwing", GreePlusCommandBuilder.SetVerticalSwing(GreePlusSwingPosition.Swing), ["SwUpDn"], [1]);
        yield return Case("VerticalAngle1", GreePlusCommandBuilder.SetVerticalSwing(GreePlusSwingPosition.Angle1), ["SwUpDn"], [2]);
        yield return Case("VerticalAngle2", GreePlusCommandBuilder.SetVerticalSwing(GreePlusSwingPosition.Angle2), ["SwUpDn"], [3]);
        yield return Case("VerticalAngle3", GreePlusCommandBuilder.SetVerticalSwing(GreePlusSwingPosition.Angle3), ["SwUpDn"], [4]);
        yield return Case("VerticalAngle4", GreePlusCommandBuilder.SetVerticalSwing(GreePlusSwingPosition.Angle4), ["SwUpDn"], [5]);
        yield return Case("VerticalAngle5", GreePlusCommandBuilder.SetVerticalSwing(GreePlusSwingPosition.Angle5), ["SwUpDn"], [6]);
        yield return Case("HorizontalSwing", GreePlusCommandBuilder.SetHorizontalSwing(GreePlusSwingPosition.Swing), ["SwingLfRig"], [1]);
        yield return Case("HorizontalAngle1", GreePlusCommandBuilder.SetHorizontalSwing(GreePlusSwingPosition.Angle1), ["SwingLfRig"], [2]);
        yield return Case("HorizontalAngle2", GreePlusCommandBuilder.SetHorizontalSwing(GreePlusSwingPosition.Angle2), ["SwingLfRig"], [3]);
        yield return Case("HorizontalAngle3", GreePlusCommandBuilder.SetHorizontalSwing(GreePlusSwingPosition.Angle3), ["SwingLfRig"], [4]);
        yield return Case("HorizontalAngle4", GreePlusCommandBuilder.SetHorizontalSwing(GreePlusSwingPosition.Angle4), ["SwingLfRig"], [5]);
        yield return Case("HorizontalAngle5", GreePlusCommandBuilder.SetHorizontalSwing(GreePlusSwingPosition.Angle5), ["SwingLfRig"], [6]);
    }

    private static object[] Case(string id, GreePlusCommandPayload payload, string[] opt, object[] p)
    {
        return [id, payload, opt, p];
    }

    private static void AssertPayloadMatchesDocumentedMap(string caseId, GreePlusCommandPayload payload)
    {
        using JsonDocument document = JsonDocument.Parse(ReadRepoFile("docs", "integrations", "gree-alice", "gree-plus-plugin-10001-command-map.json"));
        JsonElement command = document.RootElement.GetProperty("commands").GetProperty(caseId);
        string[] expectedOpt = command.GetProperty("opt").EnumerateArray().Select(static item => item.GetString()!).ToArray();
        object[] expectedP = command.GetProperty("p").EnumerateArray().Select(static item => (object)item.GetInt32()).ToArray();

        Assert.Equal(expectedOpt, payload.Opt);
        Assert.Equal(expectedP, payload.P);
    }

    private static string ReadRepoFile(params string[] relativeParts)
    {
        return File.ReadAllText(Path.Combine(new[] { FindRepositoryRoot() }.Concat(relativeParts).ToArray()));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AssistantEngineer.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate AssistantEngineer.sln from " + AppContext.BaseDirectory);
    }
}
