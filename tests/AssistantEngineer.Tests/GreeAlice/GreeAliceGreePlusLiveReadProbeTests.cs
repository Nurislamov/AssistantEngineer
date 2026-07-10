using System.Reflection;
using AssistantEngineer.Tools.GreeCloudProbe.GreePlusCommands;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceGreePlusLiveReadProbeTests
{
    private const string ReadOnlyStatusJson = """
        {
          "Pow": 1,
          "Mod": 1,
          "SetTem": 25,
          "AllErr": 0,
          "deviceState": 4,
          "status": true,
          "mid": 10001,
          "host": "hk.dis.gree.com"
        }
        """;

    [Fact]
    public void SafetyGateBlocksByDefault()
    {
        GreePlusLiveReadSafetyGateResult result = GreePlusLiveReadSafetyGate.Evaluate(new GreePlusLiveReadOptions(
            ApproveReadOnly: false,
            LiveReadSwitchValue: null,
            DeviceAlias: null,
            ConfigSource: GreePlusLiveReadConfigSource.Unknown));

        Assert.False(result.IsAllowed);
        Assert.Contains(result.MissingRequirements, requirement => requirement.Contains("GREE_ALICE_ENABLE_LIVE_READ=true", StringComparison.Ordinal));
        Assert.Contains(result.MissingRequirements, requirement => requirement.Contains("approval", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.MissingRequirements, requirement => requirement.Contains("device alias", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.MissingRequirements, requirement => requirement.Contains("config source", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SafetyGateRequiresSwitchApprovalAliasAndOperatorFileSource()
    {
        GreePlusLiveReadSafetyGateResult result = GreePlusLiveReadSafetyGate.Evaluate(ApprovedOptions());

        Assert.True(result.IsAllowed);
        Assert.Empty(result.MissingRequirements);
    }

    [Fact]
    public void SafetyGateAllowsEnvironmentSource()
    {
        GreePlusLiveReadSafetyGateResult result = GreePlusLiveReadSafetyGate.Evaluate(ApprovedOptions() with
        {
            ConfigSource = GreePlusLiveReadConfigSource.Environment
        });

        Assert.True(result.IsAllowed);
        Assert.Empty(result.MissingRequirements);
    }

    [Fact]
    public void SafetyGateRejectsRepositoryStableConfigSource()
    {
        GreePlusLiveReadSafetyGateResult result = GreePlusLiveReadSafetyGate.Evaluate(ApprovedOptions() with
        {
            ConfigSource = GreePlusLiveReadConfigSource.Unknown
        });

        Assert.False(result.IsAllowed);
        Assert.Contains(result.MissingRequirements, requirement => requirement.Contains("config source", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ProbeFailsClosedWhenExactReadContractIsUnknown()
    {
        GreePlusLiveReadResult result = new GreePlusLiveReadProbe().Run(ApprovedOptions());

        Assert.Equal(GreePlusLiveReadStatus.NotReady, result.Status);
        Assert.Equal(GreePlusLiveReadResultReason.ContractUnknown, result.Reason);
        Assert.False(result.NetworkAttempted);
        Assert.Null(result.Snapshot);
        Assert.Contains(result.MissingRequirements, requirement => requirement.Contains("endpoint", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.MissingRequirements, requirement => requirement.Contains("contract", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.MissingRequirements, requirement => requirement.Contains("status read", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.Diagnostics, value => value.Contains("success", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ContractInspectorReportsPartialEvidenceAndGaps()
    {
        GreePlusLiveReadContractReport report = GreePlusLiveReadContractInspector.InspectKnownEvidence();

        Assert.Equal(GreePlusLiveReadContractStatus.EvidencePartial, report.Status);
        Assert.False(report.IsReadOnlyContractConfirmed);
        Assert.NotEmpty(report.KnownEvidence);
        Assert.Contains(report.Gaps, gap => gap.Area.Contains("region", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(report.Gaps, gap => gap.Area.Contains("session", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(report.Gaps, gap => gap.Area.Contains("status read", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ProbeParsesProvidedReadOnlyStatusPayloadOffline()
    {
        GreePlusLiveReadOptions options = ApprovedOptions() with
        {
            ExactReadContractKnown = true,
            ReadOnlyStatusJson = ReadOnlyStatusJson
        };

        GreePlusLiveReadResult result = new GreePlusLiveReadProbe().Run(options);

        Assert.Equal(GreePlusLiveReadStatus.Parsed, result.Status);
        Assert.Equal(GreePlusLiveReadResultReason.OfflineStatusParsed, result.Reason);
        Assert.False(result.NetworkAttempted);
        Assert.Empty(result.MissingRequirements);
        Assert.NotNull(result.Snapshot);
        Assert.True(result.Snapshot!.IsPowerOn);
        Assert.False(result.Snapshot.HasError);
        Assert.True(result.Snapshot.IsStatusOnline);
        Assert.Equal(25, result.Snapshot.SetTem);
    }

    [Fact]
    public void ProbeReportsNotReadyWhenProvidedStatusPayloadIsMissing()
    {
        GreePlusLiveReadResult result = new GreePlusLiveReadProbe().Run(ApprovedOptions() with
        {
            ExactReadContractKnown = true,
            ReadOnlyStatusJson = null
        });

        Assert.Equal(GreePlusLiveReadStatus.NotReady, result.Status);
        Assert.Equal(GreePlusLiveReadResultReason.MissingStatusPayload, result.Reason);
        Assert.False(result.NetworkAttempted);
        Assert.Contains(result.MissingRequirements, requirement => requirement.Contains("response payload", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RedactorRemovesSensitiveValues()
    {
        string raw = string.Join(
            " ",
            [
                ("to" + "ken") + "=aaa111",
                ("access" + "_token") + "=access111",
                ("refresh" + "_token") + "=refresh111",
                ("coo" + "kie") + "=bbb222",
                ("auth" + "orization") + "=ccc333",
                "operator" + "@" + "example.test",
                ("u" + "id") + "=ddd444",
                ("home" + "Id") + "=eee555",
                ("device" + "Id") + "=fff666",
                ("m" + "ac") + "=00AABBCCDDEE",
                ("se" + "cret") + "=ggg777",
                ("pass" + "word") + "=hhh888",
                ("cre" + "dential") + "=iii999"
            ]);

        string redacted = GreePlusLiveReadRedactor.Redact(raw);

        Assert.DoesNotContain("aaa111", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("access111", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("refresh111", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("bbb222", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("ccc333", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("operator", redacted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ddd444", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("eee555", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("fff666", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("00AABBCCDDEE", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("ggg777", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("hhh888", redacted, StringComparison.Ordinal);
        Assert.DoesNotContain("iii999", redacted, StringComparison.Ordinal);
        Assert.Contains("<redacted>", redacted, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicResultModelDoesNotExposeRawIdentifierProperties()
    {
        string[] resultProperties = typeof(GreePlusLiveReadResult)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(static property => property.Name)
            .ToArray();

        string[] forbidden =
        [
            "m" + "ac",
            "home" + "Id",
            "device" + "Id",
            "u" + "id",
            "to" + "ken",
            "access" + "_token",
            "refresh" + "_token",
            "coo" + "kie",
            "auth",
            "email",
            "se" + "cret",
            "pass" + "word",
            "cre" + "dential"
        ];

        foreach (string value in forbidden)
        {
            Assert.DoesNotContain(resultProperties, property => property.Contains(value, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void LiveReadFilesContainNoTransportOrWriteBehavior()
    {
        string root = FindRepositoryRoot();
        string commandRoot = Path.Combine(root, "tools", "AssistantEngineer.Tools.GreeCloudProbe", "GreePlusCommands");
        string testRoot = Path.Combine(root, "tests", "AssistantEngineer.Tests", "GreeAlice");
        string[] paths = Directory.EnumerateFiles(commandRoot, "*LiveRead*.cs", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(testRoot, "*GreePlusLiveRead*.cs", SearchOption.TopDirectoryOnly))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        string combined = string.Join(Environment.NewLine, paths.Select(File.ReadAllText));
        string[] forbidden =
        [
            "CON" + "NECT",
            "PUB" + "LISH",
            "SUB" + "SCRIBE",
            "Set" + "Tem " + "command",
            "Power " + "command",
            "write " + "endpoint",
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

    private static GreePlusLiveReadOptions ApprovedOptions()
    {
        return new GreePlusLiveReadOptions(
            ApproveReadOnly: true,
            LiveReadSwitchValue: "true",
            DeviceAlias: "living-room-split",
            ConfigSource: GreePlusLiveReadConfigSource.OperatorFile);
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
