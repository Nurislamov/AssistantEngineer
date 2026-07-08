using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssistantEngineer.Tools.GreeCloudProbe;

internal static class MqttConnectInputContractCommand
{
    private const string StageName = "GREE-ALICE-11";
    private const string ModeName = "mqtt-connect-input-contract-draft";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static int Run(string[] args)
    {
        var options = Options.Parse(args);
        Directory.CreateDirectory(options.OutputDirectory);

        var timestampUtc = DateTimeOffset.UtcNow;
        var report = options.ConfigurationOnly
            ? Report.ConfigurationOnly(options, timestampUtc)
            : BuildReport(options, timestampUtc);

        var outputPath = Path.Combine(
            options.OutputDirectory,
            $"gree-mqtt-connect-input-contract-{timestampUtc:yyyyMMdd-HHmmss}.json");

        File.WriteAllText(outputPath, JsonSerializer.Serialize(report, JsonOptions));
        PrintSummary(report, outputPath);

        return 0;
    }

    private static Report BuildReport(Options options, DateTimeOffset timestampUtc)
    {
        var inputContract = BuildInputContract();
        var blockers = new[]
        {
            "GREE-ALICE-10 decision is not-ready.",
            "MQTT client id format is still unknown.",
            "MQTT username format is still unknown.",
            "MQTT password/token/auth mode format is still unknown.",
            "CONNECT-only packet options are not confirmed.",
            "CONNACK handling and immediate DISCONNECT behavior are not implemented.",
            "This stage defines a contract only and intentionally does not implement MQTT CONNECT."
        };

        var summary = new Summary(
            ContractStatus: "draft-connect-still-blocked",
            InputCount: inputContract.Count,
            RequiredFutureInputCount: inputContract.Count(static input => input.RequiredForFutureConnectOnlyTest),
            SecretInputCount: inputContract.Count(static input => input.Secret),
            BlockerCount: blockers.Length,
            MqttConnectImplementationIncluded: false,
            MqttConnectSent: false,
            MqttSubscribeSent: false,
            MqttPublishSent: false,
            DeviceControlSent: false,
            RawCredentialsStored: false);

        return new Report(
            StageName,
            ModeName,
            timestampUtc,
            new OptionsSnapshot(
                RepositoryRoot: MaskPath(options.RepositoryRoot, options.RepositoryRoot),
                OutputDirectory: MaskPath(options.OutputDirectory, options.RepositoryRoot)),
            summary,
            inputContract,
            BuildValidationRules(),
            BuildMaskingRules(),
            BuildArtifactShape(),
            blockers,
            Safety.Empty,
            new[]
            {
                "This report defines the future input contract only.",
                "It does not open a TCP/TLS/MQTT connection.",
                "It does not send MQTT CONNECT, SUBSCRIBE, PUBLISH, or any device command.",
                "Raw secret values must never be written to artifacts or committed to Git."
            });
    }

    private static IReadOnlyList<InputSpec> BuildInputContract() =>
        new[]
        {
            new InputSpec("GREE_ALICE_MQTT_HOST", false, true, "mqtt-hk.gree.com", false, "hostname", "plain non-secret host value", "MQTT broker host candidate validated by GREE-ALICE-08."),
            new InputSpec("GREE_ALICE_MQTT_PORT", false, true, "1994", false, "integer 1..65535", "plain non-secret port value", "MQTT broker port candidate validated by GREE-ALICE-08."),
            new InputSpec("GREE_ALICE_MQTT_CLIENT_ID", true, false, null, true, "non-empty string, exact format still unknown", "mask all except prefix/suffix length metadata", "Future MQTT CONNECT client id. Format is not confirmed yet."),
            new InputSpec("GREE_ALICE_MQTT_USERNAME", true, false, null, true, "non-empty string, exact format still unknown", "mask all except prefix/suffix length metadata", "Future MQTT CONNECT username. Format is not confirmed yet."),
            new InputSpec("GREE_ALICE_MQTT_PASSWORD", false, false, null, true, "non-empty string when password auth is confirmed", "never print raw; report only presence and length bucket", "Candidate MQTT password input. Do not use until auth mode is confirmed."),
            new InputSpec("GREE_ALICE_MQTT_TOKEN", false, false, null, true, "non-empty string when token auth is confirmed", "never print raw; report only presence and length bucket", "Candidate MQTT token input. Do not use until auth mode is confirmed."),
            new InputSpec("GREE_ALICE_MQTT_AUTH_MODE", true, false, null, false, "one of: password, token, signed, unknown", "plain enum value", "Future auth-mode selector. Must remain unknown until protocol evidence is confirmed."),
            new InputSpec("GREE_ALICE_MQTT_KEEP_ALIVE_SECONDS", false, true, "30", false, "integer 5..300", "plain non-secret numeric value", "Future CONNECT keep-alive value."),
            new InputSpec("GREE_ALICE_MQTT_CONNECT_TIMEOUT_SECONDS", false, true, "10", false, "integer 1..60", "plain non-secret numeric value", "Future CONNECT-only timeout."),
            new InputSpec("GREE_ALICE_MQTT_DISCONNECT_AFTER_CONNACK", false, true, "true", false, "boolean true only for first safe implementation", "plain non-secret boolean value", "Future guard rail requiring immediate DISCONNECT after CONNACK."),
            new InputSpec("GREE_ALICE_MQTT_ALLOW_SUBSCRIBE", false, true, "false", false, "boolean false only", "plain non-secret boolean value", "Guard rail. SUBSCRIBE must remain blocked."),
            new InputSpec("GREE_ALICE_MQTT_ALLOW_PUBLISH", false, true, "false", false, "boolean false only", "plain non-secret boolean value", "Guard rail. PUBLISH must remain blocked.")
        };

    private static IReadOnlyList<string> BuildValidationRules() =>
        new[]
        {
            "Future CONNECT-only implementation must fail closed when client id is missing.",
            "Future CONNECT-only implementation must fail closed when username is missing.",
            "Future CONNECT-only implementation must fail closed when auth mode is unknown.",
            "Future CONNECT-only implementation must fail closed when both password and token are supplied unless a signed mode explicitly allows it.",
            "Future CONNECT-only implementation must fail closed when neither password nor token nor signed credentials are supplied.",
            "Future CONNECT-only implementation must reject GREE_ALICE_MQTT_ALLOW_SUBSCRIBE=true.",
            "Future CONNECT-only implementation must reject GREE_ALICE_MQTT_ALLOW_PUBLISH=true.",
            "Future CONNECT-only implementation must require GREE_ALICE_MQTT_DISCONNECT_AFTER_CONNACK=true for the first live test.",
            "Future CONNECT-only implementation must not accept wildcard topic inputs.",
            "Future CONNECT-only implementation must not accept power/mode/setpoint/fan/swing command inputs."
        };

    private static IReadOnlyList<string> BuildMaskingRules() =>
        new[]
        {
            "Never write raw Gree+ password to artifacts.",
            "Never write raw MQTT password/token to artifacts.",
            "Never write raw device key to artifacts.",
            "Never write raw MAC-like identifiers to committed files.",
            "Never write SSID, barcode, latitude, or longitude to committed files.",
            "Mask client id and username unless proven non-sensitive.",
            "For secrets, report only provided/not-provided and length bucket.",
            "For identifiers, report only stable masked form.",
            "Keep all reports under artifacts/gree-alice/ and out of Git."
        };

    private static ArtifactShape BuildArtifactShape() =>
        new(
            OutputDirectory: "artifacts/gree-alice/mqtt-connect-input-contract/",
            FileNamePattern: "gree-mqtt-connect-input-contract-YYYYMMDD-HHMMSS.json",
            RequiredTopLevelSections: new[]
            {
                "Stage",
                "Mode",
                "TimestampUtc",
                "Summary",
                "InputContract",
                "ValidationRules",
                "MaskingRules",
                "ArtifactShape",
                "Blockers",
                "Safety",
                "Notes"
            },
            ForbiddenFields: new[]
            {
                "rawPassword",
                "rawToken",
                "rawDeviceKey",
                "rawMac",
                "ssid",
                "barcode",
                "latitude",
                "longitude",
                "controlPayload",
                "publishPayload"
            });

    private static string? MaskPath(string? path, string repoRoot)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var fullPath = Path.GetFullPath(path);
        var fullRepo = Path.GetFullPath(repoRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (fullPath.StartsWith(fullRepo, StringComparison.OrdinalIgnoreCase))
            return "." + fullPath[fullRepo.Length..].Replace(Path.DirectorySeparatorChar, '/');

        return "<external-path>";
    }

    private static void PrintSummary(Report report, string outputPath)
    {
        Console.WriteLine("AssistantEngineer Gree MQTT CONNECT input contract");
        Console.WriteLine($"Stage: {StageName}");
        Console.WriteLine($"Mode: {report.Mode}");
        Console.WriteLine($"Output: {outputPath}");
        Console.WriteLine();

        Console.WriteLine("Contract:");
        Console.WriteLine($"  Status: {report.Summary.ContractStatus}");
        Console.WriteLine($"  Inputs: {report.Summary.InputCount}");
        Console.WriteLine($"  Required future inputs: {report.Summary.RequiredFutureInputCount}");
        Console.WriteLine($"  Secret inputs: {report.Summary.SecretInputCount}");
        Console.WriteLine($"  Blockers: {report.Summary.BlockerCount}");
        Console.WriteLine();

        Console.WriteLine("Safety:");
        Console.WriteLine($"  MQTT CONNECT implementation included: {ToYesNo(report.Summary.MqttConnectImplementationIncluded)}");
        Console.WriteLine($"  MQTT CONNECT sent: {ToYesNo(report.Summary.MqttConnectSent)}");
        Console.WriteLine($"  MQTT SUBSCRIBE sent: {ToYesNo(report.Summary.MqttSubscribeSent)}");
        Console.WriteLine($"  MQTT PUBLISH sent: {ToYesNo(report.Summary.MqttPublishSent)}");
        Console.WriteLine($"  Device control sent: {ToYesNo(report.Summary.DeviceControlSent)}");
        Console.WriteLine($"  Raw credentials stored: {ToYesNo(report.Summary.RawCredentialsStored)}");
        Console.WriteLine();

        Console.WriteLine("Next step: keep CONNECT implementation blocked until auth/client-id evidence is known.");
    }

    private static string ToYesNo(bool value) => value ? "yes" : "no";

    private sealed record Options(string RepositoryRoot, string OutputDirectory, bool ConfigurationOnly)
    {
        public static Options Parse(string[] args)
        {
            var values = ReadArgs(args);

            var repoRoot = GetValue(values, "repo-root", null) ?? ResolveRepositoryRoot();
            repoRoot = Path.GetFullPath(repoRoot);

            var outputDir = GetValue(values, "output-dir", "GREE_ALICE_OUTPUT_DIR");
            if (string.IsNullOrWhiteSpace(outputDir))
                outputDir = Path.Combine(repoRoot, "artifacts", "gree-alice", "mqtt-connect-input-contract");

            return new Options(
                repoRoot,
                Path.GetFullPath(outputDir),
                values.ContainsKey("configuration-only"));
        }

        private static Dictionary<string, string?> ReadArgs(string[] args)
        {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];

                if (arg.Equals("--draft-mqtt-connect-input-contract", StringComparison.OrdinalIgnoreCase))
                {
                    result["draft-mqtt-connect-input-contract"] = null;
                    continue;
                }

                if (!arg.StartsWith("--", StringComparison.Ordinal))
                    throw new ArgumentException($"Unexpected argument: {arg}");

                var keyValue = arg[2..];
                var equalsIndex = keyValue.IndexOf('=');
                if (equalsIndex >= 0)
                {
                    result[keyValue[..equalsIndex]] = keyValue[(equalsIndex + 1)..];
                    continue;
                }

                if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    result[keyValue] = args[index + 1];
                    index++;
                    continue;
                }

                result[keyValue] = null;
            }

            return result;
        }

        private static string? GetValue(
            IReadOnlyDictionary<string, string?> values,
            string argumentName,
            string? environmentVariableName)
        {
            if (values.TryGetValue(argumentName, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;

            return environmentVariableName is null
                ? null
                : Environment.GetEnvironmentVariable(environmentVariableName);
        }

        private static string ResolveRepositoryRoot()
        {
            var current = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "AssistantEngineer.sln")))
                    return current.FullName;

                current = current.Parent;
            }

            throw new InvalidOperationException("Repository root with AssistantEngineer.sln was not found.");
        }
    }

    private sealed record Report(
        string Stage,
        string Mode,
        DateTimeOffset TimestampUtc,
        OptionsSnapshot Options,
        Summary Summary,
        IReadOnlyList<InputSpec> InputContract,
        IReadOnlyList<string> ValidationRules,
        IReadOnlyList<string> MaskingRules,
        ArtifactShape ArtifactShape,
        IReadOnlyList<string> Blockers,
        Safety Safety,
        IReadOnlyList<string> Notes)
    {
        public static Report ConfigurationOnly(Options options, DateTimeOffset timestampUtc)
        {
            return new Report(
                StageName,
                "configuration-only",
                timestampUtc,
                new OptionsSnapshot(
                    RepositoryRoot: MaskPath(options.RepositoryRoot, options.RepositoryRoot),
                    OutputDirectory: MaskPath(options.OutputDirectory, options.RepositoryRoot)),
                new Summary(
                    ContractStatus: "configuration-only",
                    InputCount: 0,
                    RequiredFutureInputCount: 0,
                    SecretInputCount: 0,
                    BlockerCount: 0,
                    MqttConnectImplementationIncluded: false,
                    MqttConnectSent: false,
                    MqttSubscribeSent: false,
                    MqttPublishSent: false,
                    DeviceControlSent: false,
                    RawCredentialsStored: false),
                Array.Empty<InputSpec>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                BuildArtifactShape(),
                Array.Empty<string>(),
                Safety.Empty,
                new[]
                {
                    "Configuration-only mode did not build the full input contract.",
                    "Run without --configuration-only to generate the offline contract draft."
                });
        }
    }

    private sealed record OptionsSnapshot(string? RepositoryRoot, string? OutputDirectory);

    private sealed record Summary(
        string ContractStatus,
        int InputCount,
        int RequiredFutureInputCount,
        int SecretInputCount,
        int BlockerCount,
        bool MqttConnectImplementationIncluded,
        bool MqttConnectSent,
        bool MqttSubscribeSent,
        bool MqttPublishSent,
        bool DeviceControlSent,
        bool RawCredentialsStored);

    private sealed record InputSpec(
        string Name,
        bool RequiredForFutureConnectOnlyTest,
        bool HasDefault,
        string? DefaultValue,
        bool Secret,
        string AllowedShape,
        string MaskingRule,
        string Purpose);

    private sealed record ArtifactShape(
        string OutputDirectory,
        string FileNamePattern,
        IReadOnlyList<string> RequiredTopLevelSections,
        IReadOnlyList<string> ForbiddenFields);

    private sealed record Safety(
        bool MqttConnectSent,
        bool MqttSubscribeSent,
        bool MqttPublishSent,
        bool DeviceControlSent,
        bool RawCredentialsStored,
        bool RawDeviceKeysStored,
        bool RawMacsStored,
        bool ProductionBridgeChanged)
    {
        public static Safety Empty { get; } =
            new(false, false, false, false, false, false, false, false);
    }
}
