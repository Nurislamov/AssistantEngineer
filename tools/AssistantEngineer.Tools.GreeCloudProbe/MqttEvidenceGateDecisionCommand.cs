using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssistantEngineer.Tools.GreeCloudProbe;

internal static class MqttEvidenceGateDecisionCommand
{
    private const string StageName = "GREE-ALICE-16";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static int Run(string[] args)
    {
        var options = Options.Parse(args);
        Directory.CreateDirectory(options.OutputDirectory);

        var timestamp = DateTimeOffset.UtcNow;
        var report = options.ConfigurationOnly
            ? GateReport.ConfigurationOnly(options, timestamp)
            : BuildReport(options, timestamp);

        var outputPath = Path.Combine(
            options.OutputDirectory,
            $"gree-mqtt-evidence-gate-decision-{timestamp:yyyyMMdd-HHmmss}.json");

        File.WriteAllText(outputPath, JsonSerializer.Serialize(report, JsonOptions));
        Print(report, outputPath);

        return 0;
    }

    private static GateReport BuildReport(Options options, DateTimeOffset timestamp)
    {
        var warnings = new List<string>();
        var inventoryPath = ResolveInventoryPath(options, warnings);

        var inventory = inventoryPath is null
            ? Inventory.Empty
            : ReadInventory(inventoryPath, warnings);

        var fieldNames = inventory.FieldNames;
        var signals = new GateSignals(
            HasClientIdName: HasAny(fieldNames, "clientid", "client_id", "client"),
            HasUsernameName: HasAny(fieldNames, "username", "user_name"),
            HasAuthName: HasAny(fieldNames, "auth", "password", "token", "sign", "signature"),
            HasTopicName: HasAny(fieldNames, "topic"),
            HasQosName: HasAny(fieldNames, "qos"),
            HasSubscribeName: HasAny(fieldNames, "subscribe", "sub"),
            HasPublishName: HasAny(fieldNames, "publish", "pub"),
            HasConnectName: HasAny(fieldNames, "connect", "connack"),
            HasBrokerName: HasAny(fieldNames, "broker", "host", "server", "port"));

        var blockers = new List<string>();

        if (inventoryPath is null)
        {
            blockers.Add("No masked inventory report was found.");
        }
        else
        {
            if (!signals.HasClientIdName)
                blockers.Add("No client id field-name signal was found.");

            if (!signals.HasUsernameName)
                blockers.Add("No username field-name signal was found.");

            if (!signals.HasAuthName)
                blockers.Add("No auth/password/token/signature field-name signal was found.");

            if (!signals.HasTopicName)
                blockers.Add("No topic field-name signal was found.");

            blockers.Add("Field-name signals are not enough for MQTT CONNECT.");
            blockers.Add("Raw client id, username, auth secret, and topic values remain unknown.");
            blockers.Add("SUBSCRIBE, PUBLISH, and device control remain blocked even if CONNECT-only is later approved.");
        }

        var safetyViolations = new List<string>();
        if (inventory.OutputContainsRawValues) safetyViolations.Add("Inventory output contains raw values.");
        if (inventory.NetworkConnectionOpened) safetyViolations.Add("Inventory opened a network connection.");
        if (inventory.MqttConnectSent) safetyViolations.Add("Inventory sent MQTT CONNECT.");
        if (inventory.MqttSubscribeSent) safetyViolations.Add("Inventory sent MQTT SUBSCRIBE.");
        if (inventory.MqttPublishSent) safetyViolations.Add("Inventory sent MQTT PUBLISH.");
        if (inventory.DeviceControlSent) safetyViolations.Add("Inventory sent device control.");

        var decision = safetyViolations.Count > 0
            ? "blocked-safety-violation"
            : "blocked-evidence-incomplete";

        var maskedInventoryPath = inventoryPath is null ? null : MaskPath(inventoryPath, options.RepositoryRoot);

        return new GateReport(
            StageName,
            "mqtt-evidence-gate-decision",
            timestamp,
            new GateInputs(
                InventoryReport: maskedInventoryPath,
                InventoryDirectory: MaskPath(options.InventoryDirectory, options.RepositoryRoot),
                OutputDirectory: MaskPath(options.OutputDirectory, options.RepositoryRoot)),
            new GateSummary(
                InventoryReportFound: inventoryPath is not null,
                InventoryReportPath: maskedInventoryPath,
                FilesScanned: inventory.FilesScanned,
                JsonFilesParsed: inventory.JsonFilesParsed,
                DistinctFieldNames: inventory.DistinctFieldNames,
                SensitiveOrIdentityFieldNameHits: inventory.SensitiveOrIdentityFieldNameHits,
                MqttSignalFieldNameHits: inventory.MqttSignalFieldNameHits,
                RawLeakCandidateHits: inventory.RawLeakCandidateHits,
                Decision: decision,
                ConnectGate: "blocked",
                SubscribeGate: "blocked",
                PublishGate: "blocked",
                DeviceControlGate: "blocked",
                BlockerCount: blockers.Count,
                SafetyViolationCount: safetyViolations.Count,
                OutputContainsRawValues: false,
                NetworkConnectionOpened: false,
                MqttConnectImplementationIncluded: false,
                MqttConnectSent: false,
                MqttSubscribeSent: false,
                MqttPublishSent: false,
                DeviceControlSent: false),
            signals,
            fieldNames.Take(options.MaxSignals).ToArray(),
            blockers,
            safetyViolations,
            warnings,
            new[]
            {
                "Decision is based on masked field-name inventory only.",
                "No raw primitive values are printed.",
                "No network connection is opened.",
                "MQTT CONNECT remains blocked until a separate explicit safety stage is approved."
            });
    }

    private static string? ResolveInventoryPath(Options options, List<string> warnings)
    {
        if (!string.IsNullOrWhiteSpace(options.InventoryReportPath))
        {
            if (File.Exists(options.InventoryReportPath))
                return options.InventoryReportPath;

            warnings.Add("Provided inventory report path does not exist.");
            return null;
        }

        if (!Directory.Exists(options.InventoryDirectory))
        {
            warnings.Add("Inventory directory does not exist.");
            return null;
        }

        var latest = Directory
            .EnumerateFiles(options.InventoryDirectory, "gree-mqtt-evidence-inventory-*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        if (latest is null)
            warnings.Add("No inventory report was found.");

        return latest;
    }

    private static Inventory ReadInventory(string path, List<string> warnings)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            var summary = root.TryGetProperty("Summary", out var s) ? s : default;

            return new Inventory(
                FilesScanned: ReadInt(summary, "FilesScanned"),
                JsonFilesParsed: ReadInt(summary, "JsonFilesParsed"),
                DistinctFieldNames: ReadInt(summary, "DistinctFieldNames"),
                SensitiveOrIdentityFieldNameHits: ReadInt(summary, "SensitiveOrIdentityFieldNameHits"),
                MqttSignalFieldNameHits: ReadInt(summary, "MqttSignalFieldNameHits"),
                RawLeakCandidateHits: ReadInt(summary, "RawLeakCandidateHits"),
                OutputContainsRawValues: ReadBool(summary, "OutputContainsRawValues"),
                NetworkConnectionOpened: ReadBool(summary, "NetworkConnectionOpened"),
                MqttConnectSent: ReadBool(summary, "MqttConnectSent"),
                MqttSubscribeSent: ReadBool(summary, "MqttSubscribeSent"),
                MqttPublishSent: ReadBool(summary, "MqttPublishSent"),
                DeviceControlSent: ReadBool(summary, "DeviceControlSent"),
                FieldNames: ReadFieldNames(root));
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            warnings.Add($"Failed to parse inventory report: {ex.GetType().Name}");
            return Inventory.Empty;
        }
    }

    private static IReadOnlyList<FieldNameSignal> ReadFieldNames(JsonElement root)
    {
        if (!root.TryGetProperty("FieldNames", out var array) || array.ValueKind != JsonValueKind.Array)
            return Array.Empty<FieldNameSignal>();

        var result = new List<FieldNameSignal>();

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var name = item.TryGetProperty("Name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                continue;

            result.Add(new FieldNameSignal(
                name,
                item.TryGetProperty("Count", out var c) && c.TryGetInt32(out var count) ? count : 0,
                item.TryGetProperty("Classification", out var cl) ? cl.GetString() ?? "unknown" : "unknown"));
        }

        return result;
    }

    private static bool HasAny(IReadOnlyList<FieldNameSignal> fields, params string[] terms)
    {
        foreach (var field in fields)
        {
            var compact = Normalize(field.Name);
            foreach (var term in terms)
            {
                if (compact.Contains(Normalize(term), StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    private static string Normalize(string value) =>
        value.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

    private static int ReadInt(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(name, out var property) &&
        property.TryGetInt32(out var value)
            ? value
            : 0;

    private static bool ReadBool(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(name, out var property) &&
        property.ValueKind == JsonValueKind.True;

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

    private static void Print(GateReport report, string outputPath)
    {
        Console.WriteLine("AssistantEngineer Gree MQTT evidence gate decision");
        Console.WriteLine($"Stage: {report.Stage}");
        Console.WriteLine($"Mode: {report.Mode}");
        Console.WriteLine($"Output: {outputPath}");
        Console.WriteLine();

        Console.WriteLine("Inventory:");
        Console.WriteLine($"  Inventory report found: {YesNo(report.Summary.InventoryReportFound)}");
        Console.WriteLine($"  Files scanned: {report.Summary.FilesScanned}");
        Console.WriteLine($"  JSON files parsed: {report.Summary.JsonFilesParsed}");
        Console.WriteLine($"  Distinct field names: {report.Summary.DistinctFieldNames}");
        Console.WriteLine($"  Sensitive/identity field name hits: {report.Summary.SensitiveOrIdentityFieldNameHits}");
        Console.WriteLine($"  MQTT signal field name hits: {report.Summary.MqttSignalFieldNameHits}");
        Console.WriteLine($"  Raw leak candidate hits: {report.Summary.RawLeakCandidateHits}");
        Console.WriteLine();

        Console.WriteLine("Signals:");
        Console.WriteLine($"  Client id field-name signal: {YesNo(report.Signals.HasClientIdName)}");
        Console.WriteLine($"  Username field-name signal: {YesNo(report.Signals.HasUsernameName)}");
        Console.WriteLine($"  Auth field-name signal: {YesNo(report.Signals.HasAuthName)}");
        Console.WriteLine($"  Topic field-name signal: {YesNo(report.Signals.HasTopicName)}");
        Console.WriteLine();

        Console.WriteLine("Decision:");
        Console.WriteLine($"  Decision: {report.Summary.Decision}");
        Console.WriteLine($"  CONNECT gate: {report.Summary.ConnectGate}");
        Console.WriteLine($"  SUBSCRIBE gate: {report.Summary.SubscribeGate}");
        Console.WriteLine($"  PUBLISH gate: {report.Summary.PublishGate}");
        Console.WriteLine($"  Device control gate: {report.Summary.DeviceControlGate}");
        Console.WriteLine($"  Blockers: {report.Summary.BlockerCount}");
        Console.WriteLine();

        Console.WriteLine("Safety:");
        Console.WriteLine($"  Output contains raw values: {YesNo(report.Summary.OutputContainsRawValues)}");
        Console.WriteLine($"  Network connection opened: {YesNo(report.Summary.NetworkConnectionOpened)}");
        Console.WriteLine($"  MQTT CONNECT implementation included: {YesNo(report.Summary.MqttConnectImplementationIncluded)}");
        Console.WriteLine($"  MQTT CONNECT sent: {YesNo(report.Summary.MqttConnectSent)}");
        Console.WriteLine($"  MQTT SUBSCRIBE sent: {YesNo(report.Summary.MqttSubscribeSent)}");
        Console.WriteLine($"  MQTT PUBLISH sent: {YesNo(report.Summary.MqttPublishSent)}");
        Console.WriteLine($"  Device control sent: {YesNo(report.Summary.DeviceControlSent)}");

        if (report.Blockers.Count > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Blockers:");
            Console.ResetColor();

            foreach (var blocker in report.Blockers)
                Console.WriteLine($"  - {blocker}");
        }

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
        Console.WriteLine("Next step: keep CONNECT blocked unless a separate explicit safety stage is approved.");
    }

    private static string YesNo(bool value) => value ? "yes" : "no";

    private sealed record Options(
        string RepositoryRoot,
        string InventoryDirectory,
        string? InventoryReportPath,
        string OutputDirectory,
        int MaxSignals,
        bool ConfigurationOnly)
    {
        public static Options Parse(string[] args)
        {
            var values = ReadArgs(args);

            var repoRoot = Get(values, "repo-root") ?? ResolveRepositoryRoot();
            repoRoot = Path.GetFullPath(repoRoot);

            var inventoryDirectory = Get(values, "inventory-dir") ??
                Environment.GetEnvironmentVariable("GREE_ALICE_MQTT_INVENTORY_DIR") ??
                Path.Combine(repoRoot, "artifacts", "gree-alice", "mqtt-evidence-inventory");

            var inventoryReport = Get(values, "inventory-report") ??
                Environment.GetEnvironmentVariable("GREE_ALICE_MQTT_INVENTORY_REPORT");

            if (!string.IsNullOrWhiteSpace(inventoryReport))
                inventoryReport = Path.GetFullPath(inventoryReport);

            var outputDir = Get(values, "output-dir") ??
                Environment.GetEnvironmentVariable("GREE_ALICE_OUTPUT_DIR") ??
                Path.Combine(repoRoot, "artifacts", "gree-alice", "mqtt-evidence-gate-decision");

            return new Options(
                repoRoot,
                Path.GetFullPath(inventoryDirectory),
                inventoryReport,
                Path.GetFullPath(outputDir),
                int.TryParse(Get(values, "max-signals"), out var max) && max > 0 ? max : 50,
                values.ContainsKey("configuration-only"));
        }

        private static Dictionary<string, string?> ReadArgs(string[] args)
        {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg.Equals("--decide-mqtt-evidence-gate", StringComparison.OrdinalIgnoreCase))
                {
                    result["decide-mqtt-evidence-gate"] = null;
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

    private sealed record GateReport(
        string Stage,
        string Mode,
        DateTimeOffset TimestampUtc,
        GateInputs Inputs,
        GateSummary Summary,
        GateSignals Signals,
        IReadOnlyList<FieldNameSignal> FieldNameSignals,
        IReadOnlyList<string> Blockers,
        IReadOnlyList<string> SafetyViolations,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Notes)
    {
        public static GateReport ConfigurationOnly(Options options, DateTimeOffset timestamp)
        {
            return new GateReport(
                StageName,
                "configuration-only",
                timestamp,
                new GateInputs(
                    InventoryReport: options.InventoryReportPath is null ? null : MaskPath(options.InventoryReportPath, options.RepositoryRoot),
                    InventoryDirectory: MaskPath(options.InventoryDirectory, options.RepositoryRoot),
                    OutputDirectory: MaskPath(options.OutputDirectory, options.RepositoryRoot)),
                new GateSummary(
                    InventoryReportFound: false,
                    InventoryReportPath: null,
                    FilesScanned: 0,
                    JsonFilesParsed: 0,
                    DistinctFieldNames: 0,
                    SensitiveOrIdentityFieldNameHits: 0,
                    MqttSignalFieldNameHits: 0,
                    RawLeakCandidateHits: 0,
                    Decision: "configuration-only",
                    ConnectGate: "not-evaluated",
                    SubscribeGate: "not-evaluated",
                    PublishGate: "not-evaluated",
                    DeviceControlGate: "not-evaluated",
                    BlockerCount: 0,
                    SafetyViolationCount: 0,
                    OutputContainsRawValues: false,
                    NetworkConnectionOpened: false,
                    MqttConnectImplementationIncluded: false,
                    MqttConnectSent: false,
                    MqttSubscribeSent: false,
                    MqttPublishSent: false,
                    DeviceControlSent: false),
                new GateSignals(false, false, false, false, false, false, false, false, false),
                Array.Empty<FieldNameSignal>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[] { "Configuration-only mode did not read inventory reports." });
        }
    }

    private sealed record GateInputs(string? InventoryReport, string? InventoryDirectory, string? OutputDirectory);

    private sealed record GateSummary(
        bool InventoryReportFound,
        string? InventoryReportPath,
        int FilesScanned,
        int JsonFilesParsed,
        int DistinctFieldNames,
        int SensitiveOrIdentityFieldNameHits,
        int MqttSignalFieldNameHits,
        int RawLeakCandidateHits,
        string Decision,
        string ConnectGate,
        string SubscribeGate,
        string PublishGate,
        string DeviceControlGate,
        int BlockerCount,
        int SafetyViolationCount,
        bool OutputContainsRawValues,
        bool NetworkConnectionOpened,
        bool MqttConnectImplementationIncluded,
        bool MqttConnectSent,
        bool MqttSubscribeSent,
        bool MqttPublishSent,
        bool DeviceControlSent);

    private sealed record GateSignals(
        bool HasClientIdName,
        bool HasUsernameName,
        bool HasAuthName,
        bool HasTopicName,
        bool HasQosName,
        bool HasSubscribeName,
        bool HasPublishName,
        bool HasConnectName,
        bool HasBrokerName);

    private sealed record FieldNameSignal(string Name, int Count, string Classification);

    private sealed record Inventory(
        int FilesScanned,
        int JsonFilesParsed,
        int DistinctFieldNames,
        int SensitiveOrIdentityFieldNameHits,
        int MqttSignalFieldNameHits,
        int RawLeakCandidateHits,
        bool OutputContainsRawValues,
        bool NetworkConnectionOpened,
        bool MqttConnectSent,
        bool MqttSubscribeSent,
        bool MqttPublishSent,
        bool DeviceControlSent,
        IReadOnlyList<FieldNameSignal> FieldNames)
    {
        public static Inventory Empty { get; } =
            new(0, 0, 0, 0, 0, 0, false, false, false, false, false, false, Array.Empty<FieldNameSignal>());
    }
}
