using System.Reflection;
using AssistantEngineer.Tools.GreeCloudProbe.GreePlusCommands;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceGreePlusDeviceStatusParserTests
{
    private const string CoolingOnlineFixture = """
        {
          "Pow": 1,
          "Mod": 1,
          "TemUn": 0,
          "SetTem": 25,
          "TemRec": 0,
          "WdSpd": 0,
          "Quiet": 0,
          "Tur": 0,
          "SwUpDn": 1,
          "SwingLfRig": 1,
          "Air": 0,
          "Blo": 0,
          "Health": 0,
          "SvSt": 0,
          "Lig": 1,
          "SwhSlp": 0,
          "SlpMod": 0,
          "Dmod": 15,
          "Dwet": 0,
          "AllErr": 0,
          "deviceState": 4,
          "status": true,
          "mid": 10001,
          "host": "hk.dis.gree.com"
        }
        """;

    [Fact]
    public void ParsesRepresentativeCoolingOnlineStatus()
    {
        GreePlusDeviceStatusSnapshot snapshot = GreePlusDeviceStatusParser.Parse(CoolingOnlineFixture);

        Assert.Equal(1, snapshot.Pow);
        Assert.Equal(1, snapshot.Mod);
        Assert.Equal(25, snapshot.SetTem);
        Assert.Equal(0, snapshot.TemUn);
        Assert.Equal(0, snapshot.WdSpd);
        Assert.Equal(0, snapshot.Quiet);
        Assert.Equal(0, snapshot.Tur);
        Assert.Equal(0, snapshot.AllErr);
        Assert.Equal(4, snapshot.DeviceState);
        Assert.True(snapshot.Status);
        Assert.Equal(10001, snapshot.Mid);
        Assert.Equal("hk.dis.gree.com", snapshot.Host);
        Assert.True(snapshot.IsPowerOn);
        Assert.False(snapshot.HasError);
        Assert.True(snapshot.IsStatusOnline);
    }

    [Fact]
    public void ParsesNumericLookingStringsAndErrorOfflineStatusWithoutThrowing()
    {
        const string json = """
            {
              "Pow": "1",
              "AllErr": "32",
              "status": "false",
              "deviceState": "0",
              "unknownFutureField": "ignored"
            }
            """;

        Assert.True(GreePlusDeviceStatusParser.TryParse(json, out GreePlusDeviceStatusSnapshot? snapshot));
        Assert.NotNull(snapshot);
        Assert.Equal(1, snapshot!.Pow);
        Assert.Equal(32, snapshot.AllErr);
        Assert.False(snapshot.Status);
        Assert.Equal(0, snapshot.DeviceState);
        Assert.True(snapshot.IsPowerOn);
        Assert.True(snapshot.HasError);
        Assert.False(snapshot.IsStatusOnline);
    }

    [Fact]
    public void ToleratesUnknownFieldsAndMissingOptionalValues()
    {
        const string json = """
            {
              "Pow": 0,
              "extra": 123,
              "nested": { "safe": true }
            }
            """;

        GreePlusDeviceStatusSnapshot snapshot = GreePlusDeviceStatusParser.Parse(json);

        Assert.Equal(0, snapshot.Pow);
        Assert.False(snapshot.IsPowerOn);
        Assert.Null(snapshot.Mod);
        Assert.Null(snapshot.SetTem);
        Assert.Null(snapshot.AllErr);
        Assert.Null(snapshot.HasError);
        Assert.Null(snapshot.Status);
        Assert.Null(snapshot.IsStatusOnline);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("42")]
    [InlineData("true")]
    public void RejectsInvalidRoots(string? json)
    {
        Assert.Throws<GreePlusDeviceStatusParsingException>(() => GreePlusDeviceStatusParser.Parse(json));
        Assert.False(GreePlusDeviceStatusParser.TryParse(json, out GreePlusDeviceStatusSnapshot? snapshot));
        Assert.Null(snapshot);
    }

    [Fact]
    public void SnapshotModelDoesNotExposeTransportIdentifierOrSecretProperties()
    {
        string[] propertyNames = typeof(GreePlusDeviceStatusSnapshot)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(static property => property.Name)
            .ToArray();

        string[] forbiddenParts =
        [
            "m" + "ac",
            "home" + "Id",
            "device" + "Id",
            "u" + "id",
            "to" + "ken",
            "cre" + "dential"
        ];

        foreach (string forbidden in forbiddenParts)
        {
            Assert.DoesNotContain(propertyNames, name => name.Contains(forbidden, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void StatusParserFilesStayOfflineAndLocalOnly()
    {
        string root = FindRepositoryRoot();
        string commandRoot = Path.Combine(root, "tools", "AssistantEngineer.Tools.GreeCloudProbe", "GreePlusCommands");
        string testRoot = Path.Combine(root, "tests", "AssistantEngineer.Tests", "GreeAlice");
        string[] paths = Directory.EnumerateFiles(commandRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(testRoot, "*GreePlusDeviceStatus*.cs", SearchOption.TopDirectoryOnly))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string combined = string.Join(Environment.NewLine, paths.Select(File.ReadAllText));
        string[] forbidden =
        [
            "CON" + "NECT",
            "PUB" + "LISH",
            "SUB" + "SCRIBE",
            "access" + "_token",
            "home" + "Id",
            "u" + "id",
            "." + "local",
            "Http" + "Client",
            "So" + "cket",
            "Tcp" + "Client",
            "Udp" + "Client",
            "Web" + "So" + "cket",
            "System" + ".Net",
            "M" + "qtt"
        ];

        foreach (string value in forbidden)
        {
            Assert.DoesNotContain(value, combined, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void InlineFixturesDoNotContainRealIdentifiers()
    {
        string fixtures = CoolingOnlineFixture;
        string[] forbidden =
        [
            "@" ,
            "home" + "Id",
            "u" + "id",
            "device" + "Id",
            "access" + "_token",
            "to" + "ken",
            "cre" + "dential"
        ];

        foreach (string value in forbidden)
        {
            Assert.DoesNotContain(value, fixtures, StringComparison.OrdinalIgnoreCase);
        }
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
