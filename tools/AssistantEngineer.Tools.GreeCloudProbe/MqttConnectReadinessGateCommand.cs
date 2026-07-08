using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssistantEngineer.Tools.GreeCloudProbe;

internal static class MqttConnectReadinessGateCommand
{
    private const string StageName = "GREE-ALICE-22";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly string[] ForbiddenArgumentNames =
    {
        "topic",
        "payload",
        "command",
        "cmd",
        "power",
        "pow",
        "setpoint",
        "set-tem",
        "settem",
        "mode",
        "fan",
        "swing"
    };

    public static int Run(string[] args)
    {
        var options = Options.Parse(args);
        Directory.CreateDirectory(options.OutputDirectory);

        var timestamp = DateTimeOffset.UtcNow;
        var report = options.ConfigurationOnly
            ? ReadinessReport.ConfigurationOnly(options, timestamp)
            : BuildReport(options, timestamp);

        var outputPath = Path.Combine(
            options.OutputDirectory,
            $"gree-mqtt-connect-readiness-gate-{timestamp:yyyyMMdd-HHmmss}.json");

        File.WriteAllText(outputPath, JsonSerializer.Serialize(report, JsonOptions));
        Print(report, outputPath);

        return 0;
    }

    private static ReadinessReport BuildReport(Options options, DateTimeOffset timestamp)
    {
        var violations = new List<string>();
        var sourceReport = ResolveDryRunReportPath(options, violations);

        DryRunEvidence? evidence = null;
        if (!string.IsNullOrWhiteSpace(sourceReport) && File.Exists(sourceReport))
            evidence = ReadDryRunEvidence(sourceReport, violations);

        foreach (var forbidden in options.ForbiddenArguments)
            violations.Add($"Forbidden argument was provided to readiness gate: --{forbidden}");

        if (evidence is null)
        {
            violations.Add("Dry-run evidence report is missing or unreadable.");
        }
        else
        {
            if (!evidence.Status.Equals("dry-run-ready-for-separate-live-safety-stage", StringComparison.Ordinal))
                violations.Add($"Dry-run status is not ready: {evidence.Status}");

            if (!evidence.ConnectGate.Equals("blocked", StringComparison.OrdinalIgnoreCase))
                violations.Add("Dry-run CONNECT gate must remain blocked.");

            if (!evidence.SubscribeGate.Equals("blocked", StringComparison.OrdinalIgnoreCase))
                violations.Add("Dry-run SUBSCRIBE gate must remain blocked.");

            if (!evidence.PublishGate.Equals("blocked", StringComparison.OrdinalIgnoreCase))
                violations.Add("Dry-run PUBLISH gate must remain blocked.");

            if (!evidence.DeviceControlGate.Equals("blocked", StringComparison.OrdinalIgnoreCase))
                violations.Add("Dry-run device-control gate must remain blocked.");

            if (evidence.OutputContainsRawValues)
                violations.Add("Dry-run report says output contains raw values.");

            if (evidence.NetworkConnectionOpened)
                violations.Add("Dry-run report says a network connection was opened.");

            if (evidence.MqttConnectImplementationIncluded)
                violations.Add("Dry-run report says MQTT CONNECT implementation is included.");

            if (evidence.MqttConnectSent)
                violations.Add("Dry-run report says MQTT CONNECT was sent.");

            if (evidence.MqttSubscribeSent)
                violations.Add("Dry-run report says MQTT SUBSCRIBE was sent.");

            if (evidence.MqttPublishSent)
                violations.Add("Dry-run report says MQTT PUBLISH was sent.");

            if (evidence.DeviceControlSent)
                violations.Add("Dry-run report says device control was sent.");

            if (evidence.RawCredentialsStored)
                violations.Add("Dry-run report says raw credentials were stored.");
        }

        var readyForHumanReview = violations.Count == 0;
        var decision = readyForHumanReview
            ? "ready-for-human-live-safety-review"
            : "blocked-readiness-incomplete";

        var summary = new ReadinessSummary(
            Status: decision,
            DryRunReportFound: evidence is not null,
            DryRunReportPath: MaskPath(sourceReport, options.RepositoryRoot),
            Violations: violations.Count,
            ReadyForHumanLiveSafetyReview: readyForHumanReview,
            LiveConnectGate: readyForHumanReview
                ? "blocked-pending-explicit-human-approval"
                : "blocked",
            SubscribeGate: "blocked",
            PublishGate: "blocked",
            DeviceControlGate: "blocked",
            OutputContainsRawValues: false,
            NetworkConnectionOpened: false,
            MqttConnectImplementationIncluded: false,
            MqttConnectSent: false,
            MqttSubscribeSent: false,
            MqttPublishSent: false,
            DeviceControlSent: false,
            RawCredentialsStored: false);

        return new ReadinessReport(
            StageName,
            "mqtt-connect-readiness-gate",
            timestamp,
            new ReadinessInputs(
                OutputDirectory: MaskPath(options.OutputDirectory, options.RepositoryRoot),
                DryRunReportPath: MaskPath(sourceReport, options.RepositoryRoot),
                ConfigurationOnly: false),
            summary,
            evidence,
            violations,
            new[]
            {
                "This is an offline readiness gate only.",
                "A ready result does not approve live MQTT CONNECT.",
                "Live MQTT CONNECT remains blocked until a separate explicit safety stage is approved.",
                "No DNS, TCP, TLS, MQTT, SUBSCRIBE, PUBLISH, or device-control operation is performed."
            });
    }

    private static string? ResolveDryRunReportPath(Options options, List<string> violations)
    {
        if (!string.IsNullOrWhiteSpace(options.DryRunReportPath))
            return Path.GetFullPath(options.DryRunReportPath);

        var dryRunDir = Path.Combine(
            options.RepositoryRoot,
            "artifacts",
            "gree-alice",
            "mqtt-connect-dry-run");

        if (!Directory.Exists(dryRunDir))
        {
            violations.Add("Dry-run output directory was not found.");
            return null;
        }

        var latest = Directory
            .EnumerateFiles(dryRunDir, "gree-mqtt-connect-dry-run-*.json", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .FirstOrDefault();

        if (latest is null)
        {
            violations.Add("No dry-run report was found.");
            return null;
        }

        return latest.FullName;
    }

    private static DryRunEvidence? ReadDryRunEvidence(string path, List<string> violations)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;

            var stage = GetString(root, "Stage");
            var mode = GetString(root, "Mode");
            var summary = root.TryGetProperty("Summary", out var summaryElement)
                ? summaryElement
                : default;

            if (summary.ValueKind != JsonValueKind.Object)
            {
                violations.Add("Dry-run report does not contain a Summary object.");
                return null;
            }

            if (!string.Equals(stage, "GREE-ALICE-19", StringComparison.Ordinal))
                violations.Add($"Dry-run report stage is unexpected: {stage ?? "<missing>"}");

            if (!string.Equals(mode, "mqtt-connect-dry-run-contract", StringComparison.Ordinal))
                violations.Add($"Dry-run report mode is unexpected: {mode ?? "<missing>"}");

            return new DryRunEvidence(
                Stage: stage ?? "<missing>",
                Mode: mode ?? "<missing>",
                Status: GetString(summary, "Status") ?? "<missing>",
                ConnectGate: GetString(summary, "ConnectGate") ?? "<missing>",
                SubscribeGate: GetString(summary, "SubscribeGate") ?? "<missing>",
                PublishGate: GetString(summary, "PublishGate") ?? "<missing>",
                DeviceControlGate: GetString(summary, "DeviceControlGate") ?? "<missing>",
                OutputContainsRawValues: GetBool(summary, "OutputContainsRawValues", violations),
                NetworkConnectionOpened: GetBool(summary, "NetworkConnectionOpened", violations),
                MqttConnectImplementationIncluded: GetBool(summary, "MqttConnectImplementationIncluded", violations),
                MqttConnectSent: GetBool(summary, "MqttConnectSent", violations),
                MqttSubscribeSent: GetBool(summary, "MqttSubscribeSent", violations),
                MqttPublishSent: GetBool(summary, "MqttPublishSent", violations),
                DeviceControlSent: GetBool(summary, "DeviceControlSent", violations),
                RawCredentialsStored: GetBool(summary, "RawCredentialsStored", violations));
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            violations.Add($"Dry-run report could not be read: {ex.GetType().Name}");
            return null;
        }
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static bool GetBool(JsonElement element, string propertyName, List<string> violations)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            violations.Add($"Dry-run report is missing Summary.{propertyName}.");
            return true;
        }

        if (value.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            violations.Add($"Dry-run report Summary.{propertyName} is not a boolean.");
            return true;
        }

        return value.GetBoolean();
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

    private static void Print(ReadinessReport report, string outputPath)
    {
        Console.WriteLine("AssistantEngineer Gree MQTT CONNECT readiness gate");
        Console.WriteLine($"Stage: {report.Stage}");
        Console.WriteLine($"Mode: {report.Mode}");
        Console.WriteLine($"Output: {outputPath}");
        Console.WriteLine();

        Console.WriteLine("Decision:");
        Console.WriteLine($"  Status: {report.Summary.Status}");
        Console.WriteLine($"  Dry-run report found: {YesNo(report.Summary.DryRunReportFound)}");
        Console.WriteLine($"  Violations: {report.Summary.Violations}");
        Console.WriteLine($"  Ready for human live-safety review: {YesNo(report.Summary.ReadyForHumanLiveSafetyReview)}");
        Console.WriteLine();

        Console.WriteLine("Gates:");
        Console.WriteLine($"  Live CONNECT gate: {report.Summary.LiveConnectGate}");
        Console.WriteLine($"  SUBSCRIBE gate: {report.Summary.SubscribeGate}");
        Console.WriteLine($"  PUBLISH gate: {report.Summary.PublishGate}");
        Console.WriteLine($"  Device control gate: {report.Summary.DeviceControlGate}");
        Console.WriteLine();

        Console.WriteLine("Safety:");
        Console.WriteLine($"  Output contains raw values: {YesNo(report.Summary.OutputContainsRawValues)}");
        Console.WriteLine($"  Network connection opened: {YesNo(report.Summary.NetworkConnectionOpened)}");
        Console.WriteLine($"  MQTT CONNECT implementation included: {YesNo(report.Summary.MqttConnectImplementationIncluded)}");
        Console.WriteLine($"  MQTT CONNECT sent: {YesNo(report.Summary.MqttConnectSent)}");
        Console.WriteLine($"  MQTT SUBSCRIBE sent: {YesNo(report.Summary.MqttSubscribeSent)}");
        Console.WriteLine($"  MQTT PUBLISH sent: {YesNo(report.Summary.MqttPublishSent)}");
        Console.WriteLine($"  Device control sent: {YesNo(report.Summary.DeviceControlSent)}");
        Console.WriteLine($"  Raw credentials stored: {YesNo(report.Summary.RawCredentialsStored)}");

        if (report.Violations.Count > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Violations:");
            Console.ResetColor();

            foreach (var violation in report.Violations)
                Console.WriteLine($"  - {violation}");
        }

        Console.WriteLine();
        Console.WriteLine("Next step: live CONNECT remains blocked unless a separate explicit safety stage is approved.");
    }

    private static string YesNo(bool value) => value ? "yes" : "no";

    private sealed record Options(
        string RepositoryRoot,
        string OutputDirectory,
        string? DryRunReportPath,
        bool ConfigurationOnly,
        IReadOnlyList<string> ForbiddenArguments)
    {
        public static Options Parse(string[] args)
        {
            var values = ReadArgs(args);
            var repoRoot = Get(values, "repo-root") ?? ResolveRepositoryRoot();
            repoRoot = Path.GetFullPath(repoRoot);

            var outputDir = Get(values, "output-dir") ??
                Environment.GetEnvironmentVariable("GREE_ALICE_OUTPUT_DIR") ??
                Path.Combine(repoRoot, "artifacts", "gree-alice", "mqtt-connect-readiness-gate");

            var forbiddenArgs = values.Keys
                .Where(key => ForbiddenArgumentNames.Contains(key, StringComparer.OrdinalIgnoreCase))
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return new Options(
                repoRoot,
                Path.GetFullPath(outputDir),
                Get(values, "dry-run-report"),
                values.ContainsKey("configuration-only"),
                forbiddenArgs);
        }

        private static Dictionary<string, string?> ReadArgs(string[] args)
        {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg.Equals("--mqtt-connect-readiness-gate", StringComparison.OrdinalIgnoreCase))
                {
                    result["mqtt-connect-readiness-gate"] = null;
                    continue;
                }

                if (!arg.StartsWith("--", StringComparison.Ordinal))
                    throw new ArgumentException($"Unexpected argument: {arg}");

                var key = arg[2..];

                if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    result[key] = args[i + 1];
                    i++;
                }
                else
                {
                    result[key] = null;
                }
            }

            return result;
        }

        private static string? Get(IReadOnlyDictionary<string, string?> values, string key) =>
            values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : null;

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

    private sealed record ReadinessReport(
        string Stage,
        string Mode,
        DateTimeOffset TimestampUtc,
        ReadinessInputs Inputs,
        ReadinessSummary Summary,
        DryRunEvidence? DryRunEvidence,
        IReadOnlyList<string> Violations,
        IReadOnlyList<string> Notes)
    {
        public static ReadinessReport ConfigurationOnly(Options options, DateTimeOffset timestamp)
        {
            return new ReadinessReport(
                StageName,
                "configuration-only",
                timestamp,
                new ReadinessInputs(
                    OutputDirectory: MaskPath(options.OutputDirectory, options.RepositoryRoot),
                    DryRunReportPath: MaskPath(options.DryRunReportPath, options.RepositoryRoot),
                    ConfigurationOnly: true),
                new ReadinessSummary(
                    Status: "configuration-only",
                    DryRunReportFound: false,
                    DryRunReportPath: MaskPath(options.DryRunReportPath, options.RepositoryRoot),
                    Violations: 0,
                    ReadyForHumanLiveSafetyReview: false,
                    LiveConnectGate: "not-evaluated",
                    SubscribeGate: "not-evaluated",
                    PublishGate: "not-evaluated",
                    DeviceControlGate: "not-evaluated",
                    OutputContainsRawValues: false,
                    NetworkConnectionOpened: false,
                    MqttConnectImplementationIncluded: false,
                    MqttConnectSent: false,
                    MqttSubscribeSent: false,
                    MqttPublishSent: false,
                    DeviceControlSent: false,
                    RawCredentialsStored: false),
                null,
                Array.Empty<string>(),
                new[] { "Configuration-only mode did not inspect dry-run reports." });
        }
    }

    private sealed record ReadinessInputs(string? OutputDirectory, string? DryRunReportPath, bool ConfigurationOnly);

    private sealed record ReadinessSummary(
        string Status,
        bool DryRunReportFound,
        string? DryRunReportPath,
        int Violations,
        bool ReadyForHumanLiveSafetyReview,
        string LiveConnectGate,
        string SubscribeGate,
        string PublishGate,
        string DeviceControlGate,
        bool OutputContainsRawValues,
        bool NetworkConnectionOpened,
        bool MqttConnectImplementationIncluded,
        bool MqttConnectSent,
        bool MqttSubscribeSent,
        bool MqttPublishSent,
        bool DeviceControlSent,
        bool RawCredentialsStored);

    private sealed record DryRunEvidence(
        string Stage,
        string Mode,
        string Status,
        string ConnectGate,
        string SubscribeGate,
        string PublishGate,
        string DeviceControlGate,
        bool OutputContainsRawValues,
        bool NetworkConnectionOpened,
        bool MqttConnectImplementationIncluded,
        bool MqttConnectSent,
        bool MqttSubscribeSent,
        bool MqttPublishSent,
        bool DeviceControlSent,
        bool RawCredentialsStored);
}
