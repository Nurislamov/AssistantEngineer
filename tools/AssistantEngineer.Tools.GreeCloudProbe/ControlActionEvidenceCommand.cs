using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssistantEngineer.Tools.GreeCloudProbe;

internal static class ControlActionEvidenceCommand
{
    private const string StageName = "GREE-ALICE-13";
    private const string ModeName = "control-action-capture-evidence";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly IReadOnlyList<string> InternalVocabulary = new[]
    {
        "Pow",
        "Mod",
        "SetTem",
        "TemUn",
        "WdSpd",
        "SwUpDn",
        "SwingLfRig",
        "Quiet",
        "Tur",
        "Lig",
        "t=status",
        "t=cmd",
        "opt",
        "p"
    };

    private static readonly IReadOnlyList<string> Unknowns = new[]
    {
        "Gree+ Cloud MQTT client id",
        "Gree+ Cloud MQTT username",
        "Gree+ Cloud MQTT password/token/signature",
        "Gree+ Cloud MQTT status topic",
        "Gree+ Cloud MQTT command topic",
        "Gree+ Cloud MQTT QoS",
        "Gree+ Cloud MQTT payload encryption/signature shape",
        "Whether cloud payload reuses LAN-style command vocabulary directly or wraps it in another envelope"
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
            $"gree-control-action-evidence-{timestampUtc:yyyyMMdd-HHmmss}.json");

        File.WriteAllText(outputPath, JsonSerializer.Serialize(report, JsonOptions));
        PrintSummary(report, outputPath);

        return 0;
    }

    private static Report BuildReport(Options options, DateTimeOffset timestampUtc)
    {
        var warnings = new List<string>();
        var rows = Array.Empty<CaptureRow>();

        if (!string.IsNullOrWhiteSpace(options.CaptureCsvPath) && File.Exists(options.CaptureCsvPath))
        {
            rows = ReadCaptureRows(options.CaptureCsvPath!, warnings)
                .Where(static row =>
                    row.App.Equals("GREE+", StringComparison.OrdinalIgnoreCase) ||
                    row.PackageName.Equals("com.gree.greeplus", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
        else
        {
            warnings.Add("No capture CSV was provided or the provided path does not exist.");
        }

        var endpoints = BuildEndpointEvidence(rows);
        var udp7000Observed = rows.Any(static row =>
            row.Transport.Equals("UDP", StringComparison.OrdinalIgnoreCase) &&
            row.DestinationPort == 7000);

        var summary = new Summary(
            CaptureProvided: rows.Length > 0,
            CaptureRows: rows.Length,
            WindowStart: rows.Select(static row => row.FirstSeen).Where(static value => value is not null).OrderBy(static value => value).FirstOrDefault(),
            WindowEnd: rows.Select(static row => row.LastSeen).Where(static value => value is not null).OrderByDescending(static value => value).FirstOrDefault(),
            MqttTlsControlCandidateObserved: endpoints.Any(static endpoint => endpoint.Host.Equals("mqtt-hk.gree.com", StringComparison.OrdinalIgnoreCase) && endpoint.Port == 1994),
            RestDiscoveryTrafficObserved: endpoints.Any(static endpoint => endpoint.Host.Equals("hkgrih.gree.com", StringComparison.OrdinalIgnoreCase) && endpoint.Port == 443),
            Udp7000LanActivityObserved: udp7000Observed,
            InternalVocabularyCount: InternalVocabulary.Count,
            MqttAuthTopicStatus: "unknown",
            MqttConnectImplementationIncluded: false,
            MqttConnectSent: false,
            MqttSubscribeSent: false,
            MqttPublishSent: false,
            DeviceControlSent: false,
            PrivateCaptureCommitted: false);

        return new Report(
            StageName,
            ModeName,
            timestampUtc,
            new Inputs(
                CaptureCsvPath: MaskPath(options.CaptureCsvPath, options.RepositoryRoot),
                OutputDirectory: MaskPath(options.OutputDirectory, options.RepositoryRoot),
                ActionSequence: options.ActionSequence),
            summary,
            endpoints,
            InternalVocabulary,
            Unknowns,
            Safety.Empty,
            warnings,
            new[]
            {
                "This report documents our own capture evidence only.",
                "No third-party source names or links are recorded.",
                "It does not open TCP, TLS, or MQTT connections.",
                "It does not send MQTT CONNECT, SUBSCRIBE, PUBLISH, or any device command.",
                "The private PCAPdroid CSV must not be committed."
            });
    }

    private static IReadOnlyList<CaptureRow> ReadCaptureRows(string path, List<string> warnings)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        if (lines.Length == 0)
            return Array.Empty<CaptureRow>();

        var header = SplitCsvLine(lines[0]);
        var index = header
            .Select((name, i) => new { name, i })
            .ToDictionary(static item => item.name.Trim(), static item => item.i, StringComparer.OrdinalIgnoreCase);

        string Get(IReadOnlyList<string> parts, string name) =>
            index.TryGetValue(name, out var i) && i >= 0 && i < parts.Count ? parts[i] : string.Empty;

        var rows = new List<CaptureRow>();

        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            if (string.IsNullOrWhiteSpace(lines[lineIndex]))
                continue;

            var parts = SplitCsvLine(lines[lineIndex]);
            rows.Add(new CaptureRow(
                Transport: ParseTransport(Get(parts, "IPProto")),
                DestinationKind: ClassifyDestination(Get(parts, "DstIp")),
                DestinationPort: ParseInt(Get(parts, "DstPort")),
                App: Get(parts, "App"),
                PackageName: Get(parts, "PackageName"),
                Protocol: Get(parts, "Proto"),
                Info: Get(parts, "Info"),
                BytesSent: ParseLong(Get(parts, "BytesSent")),
                BytesReceived: ParseLong(Get(parts, "BytesRcvd")),
                PacketsSent: ParseLong(Get(parts, "PktsSent")),
                PacketsReceived: ParseLong(Get(parts, "PktsRcvd")),
                FirstSeen: ParseDate(Get(parts, "FirstSeen")),
                LastSeen: ParseDate(Get(parts, "LastSeen"))));
        }

        return rows;
    }

    private static IReadOnlyList<EndpointEvidence> BuildEndpointEvidence(IReadOnlyList<CaptureRow> rows)
    {
        return rows
            .Where(static row => !string.IsNullOrWhiteSpace(row.Info) || !string.IsNullOrWhiteSpace(row.DestinationKind))
            .GroupBy(static row => new
            {
                Host = NormalizeEndpointName(row.Info, row.DestinationKind),
                Port = row.DestinationPort,
                row.Transport,
                row.Protocol
            })
            .Select(static group => new EndpointEvidence(
                Host: group.Key.Host,
                Port: group.Key.Port,
                Transport: group.Key.Transport,
                Protocol: group.Key.Protocol,
                Rows: group.Count(),
                BytesSent: group.Sum(static row => row.BytesSent),
                BytesReceived: group.Sum(static row => row.BytesReceived),
                PacketsSent: group.Sum(static row => row.PacketsSent),
                PacketsReceived: group.Sum(static row => row.PacketsReceived),
                FirstSeen: group.Select(static row => row.FirstSeen).Where(static value => value is not null).OrderBy(static value => value).FirstOrDefault(),
                LastSeen: group.Select(static row => row.LastSeen).Where(static value => value is not null).OrderByDescending(static value => value).FirstOrDefault(),
                Interpretation: InterpretEndpoint(group.Key.Host, group.Key.Port, group.Key.Transport, group.Key.Protocol)))
            .OrderByDescending(static endpoint => endpoint.BytesSent + endpoint.BytesReceived)
            .ThenBy(static endpoint => endpoint.Host, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string NormalizeEndpointName(string info, string destinationKind)
    {
        if (!string.IsNullOrWhiteSpace(info))
            return info.Trim();

        return string.IsNullOrWhiteSpace(destinationKind) ? "<unknown>" : destinationKind;
    }

    private static string InterpretEndpoint(string host, int port, string transport, string protocol)
    {
        if (host.Equals("mqtt-hk.gree.com", StringComparison.OrdinalIgnoreCase) && port == 1994)
            return "Gree+ Cloud MQTT/TLS live-control candidate observed during control action.";

        if (host.Equals("hkgrih.gree.com", StringComparison.OrdinalIgnoreCase) && port == 443)
            return "Gree+ Cloud HTTPS REST discovery/account/device metadata path.";

        if (port == 7000 && transport.Equals("UDP", StringComparison.OrdinalIgnoreCase))
            return "Gree LAN UDP/7000 local discovery/control fallback activity.";

        if (protocol.Equals("DNS", StringComparison.OrdinalIgnoreCase))
            return "DNS lookup.";

        return "supporting network flow";
    }

    private static string ParseTransport(string ipProto) =>
        ipProto.Trim() switch
        {
            "6" => "TCP",
            "17" => "UDP",
            _ => string.IsNullOrWhiteSpace(ipProto) ? "<unknown>" : $"IPProto:{ipProto.Trim()}"
        };

    private static int ParseInt(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;

    private static long ParseLong(string value) =>
        long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;

    private static DateTimeOffset? ParseDate(string value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;

    private static string ClassifyDestination(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var ip = value.Trim();

        if (ip.Equals("255.255.255.255", StringComparison.Ordinal))
            return "<broadcast>";

        if (ip.StartsWith("192.168.", StringComparison.Ordinal) ||
            ip.StartsWith("10.", StringComparison.Ordinal) ||
            Is172Private(ip))
            return "<lan-device>";

        return "<public-ip>";
    }

    private static bool Is172Private(string ip)
    {
        var parts = ip.Split('.');
        if (parts.Length != 4)
            return false;

        return parts[0] == "172" &&
               int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var second) &&
               second is >= 16 and <= 31;
    }

    private static IReadOnlyList<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var ch = line[index];

            if (ch == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        result.Add(current.ToString());
        return result;
    }

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
        Console.WriteLine("AssistantEngineer Gree control action evidence");
        Console.WriteLine($"Stage: {StageName}");
        Console.WriteLine($"Mode: {report.Mode}");
        Console.WriteLine($"Output: {outputPath}");
        Console.WriteLine();

        Console.WriteLine("Capture:");
        Console.WriteLine($"  Capture provided: {ToYesNo(report.Summary.CaptureProvided)}");
        Console.WriteLine($"  Capture rows: {report.Summary.CaptureRows}");
        Console.WriteLine($"  Window start: {report.Summary.WindowStart?.ToString("O") ?? "<unknown>"}");
        Console.WriteLine($"  Window end: {report.Summary.WindowEnd?.ToString("O") ?? "<unknown>"}");
        Console.WriteLine();

        Console.WriteLine("Evidence:");
        Console.WriteLine($"  MQTT/TLS control candidate observed: {ToYesNo(report.Summary.MqttTlsControlCandidateObserved)}");
        Console.WriteLine($"  REST discovery traffic observed: {ToYesNo(report.Summary.RestDiscoveryTrafficObserved)}");
        Console.WriteLine($"  UDP 7000 LAN activity observed: {ToYesNo(report.Summary.Udp7000LanActivityObserved)}");
        Console.WriteLine($"  Internal vocabulary items: {report.Summary.InternalVocabularyCount}");
        Console.WriteLine($"  MQTT auth/topic status: {report.Summary.MqttAuthTopicStatus}");
        Console.WriteLine();

        Console.WriteLine("Safety:");
        Console.WriteLine($"  MQTT CONNECT implementation included: {ToYesNo(report.Summary.MqttConnectImplementationIncluded)}");
        Console.WriteLine($"  MQTT CONNECT sent: {ToYesNo(report.Summary.MqttConnectSent)}");
        Console.WriteLine($"  MQTT SUBSCRIBE sent: {ToYesNo(report.Summary.MqttSubscribeSent)}");
        Console.WriteLine($"  MQTT PUBLISH sent: {ToYesNo(report.Summary.MqttPublishSent)}");
        Console.WriteLine($"  Device control sent: {ToYesNo(report.Summary.DeviceControlSent)}");
        Console.WriteLine($"  Private capture committed: {ToYesNo(report.Summary.PrivateCaptureCommitted)}");

        if (report.Warnings.Count > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warnings:");
            Console.ResetColor();

            foreach (var warning in report.Warnings)
                Console.WriteLine($"  - {warning}");
        }

        Console.WriteLine();
        Console.WriteLine("Next step: document evidence; do not implement CONNECT until auth/topics are known.");
    }

    private static string ToYesNo(bool value) => value ? "yes" : "no";

    private sealed record Options(
        string RepositoryRoot,
        string? CaptureCsvPath,
        string ActionSequence,
        string OutputDirectory,
        bool ConfigurationOnly)
    {
        public static Options Parse(string[] args)
        {
            var values = ReadArgs(args);

            var repoRoot = GetValue(values, "repo-root", null) ?? ResolveRepositoryRoot();
            repoRoot = Path.GetFullPath(repoRoot);

            var captureCsv = GetValue(values, "control-capture-csv", "GREE_ALICE_CONTROL_CAPTURE_CSV");
            if (!string.IsNullOrWhiteSpace(captureCsv))
                captureCsv = Path.GetFullPath(captureCsv);

            var actionSequence = GetValue(values, "action-sequence", "GREE_ALICE_ACTION_SEQUENCE");
            if (string.IsNullOrWhiteSpace(actionSequence))
                actionSequence = "off/on and setpoint 24 -> 23 -> 24";

            var outputDir = GetValue(values, "output-dir", "GREE_ALICE_OUTPUT_DIR");
            if (string.IsNullOrWhiteSpace(outputDir))
                outputDir = Path.Combine(repoRoot, "artifacts", "gree-alice", "control-action-evidence");

            return new Options(
                repoRoot,
                captureCsv,
                actionSequence,
                Path.GetFullPath(outputDir),
                values.ContainsKey("configuration-only"));
        }

        private static Dictionary<string, string?> ReadArgs(string[] args)
        {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];

                if (arg.Equals("--summarize-control-action-evidence", StringComparison.OrdinalIgnoreCase))
                {
                    result["summarize-control-action-evidence"] = null;
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
        Inputs Inputs,
        Summary Summary,
        IReadOnlyList<EndpointEvidence> Endpoints,
        IReadOnlyList<string> InternalVocabulary,
        IReadOnlyList<string> Unknowns,
        Safety Safety,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Notes)
    {
        public static Report ConfigurationOnly(Options options, DateTimeOffset timestampUtc)
        {
            return new Report(
                StageName,
                "configuration-only",
                timestampUtc,
                new Inputs(
                    CaptureCsvPath: MaskPath(options.CaptureCsvPath, options.RepositoryRoot),
                    OutputDirectory: MaskPath(options.OutputDirectory, options.RepositoryRoot),
                    ActionSequence: options.ActionSequence),
                new Summary(
                    CaptureProvided: false,
                    CaptureRows: 0,
                    WindowStart: null,
                    WindowEnd: null,
                    MqttTlsControlCandidateObserved: false,
                    RestDiscoveryTrafficObserved: false,
                    Udp7000LanActivityObserved: false,
                    InternalVocabularyCount: 0,
                    MqttAuthTopicStatus: "unknown",
                    MqttConnectImplementationIncluded: false,
                    MqttConnectSent: false,
                    MqttSubscribeSent: false,
                    MqttPublishSent: false,
                    DeviceControlSent: false,
                    PrivateCaptureCommitted: false),
                Array.Empty<EndpointEvidence>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                Safety.Empty,
                Array.Empty<string>(),
                new[]
                {
                    "Configuration-only mode did not read a capture file.",
                    "Run without --configuration-only and pass --control-capture-csv to summarize local evidence."
                });
        }
    }

    private sealed record Inputs(string? CaptureCsvPath, string? OutputDirectory, string ActionSequence);

    private sealed record Summary(
        bool CaptureProvided,
        int CaptureRows,
        DateTimeOffset? WindowStart,
        DateTimeOffset? WindowEnd,
        bool MqttTlsControlCandidateObserved,
        bool RestDiscoveryTrafficObserved,
        bool Udp7000LanActivityObserved,
        int InternalVocabularyCount,
        string MqttAuthTopicStatus,
        bool MqttConnectImplementationIncluded,
        bool MqttConnectSent,
        bool MqttSubscribeSent,
        bool MqttPublishSent,
        bool DeviceControlSent,
        bool PrivateCaptureCommitted);

    private sealed record CaptureRow(
        string Transport,
        string DestinationKind,
        int DestinationPort,
        string App,
        string PackageName,
        string Protocol,
        string Info,
        long BytesSent,
        long BytesReceived,
        long PacketsSent,
        long PacketsReceived,
        DateTimeOffset? FirstSeen,
        DateTimeOffset? LastSeen);

    private sealed record EndpointEvidence(
        string Host,
        int Port,
        string Transport,
        string Protocol,
        int Rows,
        long BytesSent,
        long BytesReceived,
        long PacketsSent,
        long PacketsReceived,
        DateTimeOffset? FirstSeen,
        DateTimeOffset? LastSeen,
        string Interpretation);

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
