using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssistantEngineer.Tools.GreeCloudProbe;

internal static class MqttConnectSafetyReviewCommand
{
    private const string StageName = "GREE-ALICE-10";
    private const string ModeName = "mqtt-connect-only-safety-review";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static int Run(string[] args)
    {
        var options = MqttConnectSafetyReviewOptions.Parse(args);
        Directory.CreateDirectory(options.OutputDirectory);

        var timestampUtc = DateTimeOffset.UtcNow;
        var report = options.ConfigurationOnly
            ? MqttConnectSafetyReviewReport.ConfigurationOnly(options, timestampUtc)
            : BuildReport(options, timestampUtc);

        var outputPath = Path.Combine(
            options.OutputDirectory,
            $"gree-mqtt-connect-safety-review-{timestampUtc:yyyyMMdd-HHmmss}.json");

        File.WriteAllText(outputPath, JsonSerializer.Serialize(report, JsonOptions));
        PrintSummary(report, outputPath);

        return 0;
    }

    private static MqttConnectSafetyReviewReport BuildReport(
        MqttConnectSafetyReviewOptions options,
        DateTimeOffset timestampUtc)
    {
        var warnings = new List<string>();

        var modelReportPath = options.ModelReportPath ?? FindLatestUsefulModelReport(
            Path.Combine(options.RepositoryRoot, "artifacts", "gree-alice", "mqtt-model"));

        var evidence = modelReportPath is null
            ? MqttConnectSafetyEvidence.Empty
            : ReadEvidence(modelReportPath, warnings);

        if (modelReportPath is null)
            warnings.Add("No local MQTT auth/topic model draft report was found.");

        var blockers = new List<string>();

        if (!evidence.ModelReportFound)
            blockers.Add("Missing GREE-ALICE-09 MQTT auth/topic model draft report.");

        if (evidence.TransportTlsAuthenticated != true)
            blockers.Add("MQTT transport TLS/SNI evidence is missing or not authenticated.");

        if (evidence.DeviceSignalsCount <= 0)
            blockers.Add("No device signal was extracted from the model draft.");

        if (evidence.DeviceKeyPresenceCount <= 0)
            blockers.Add("No device key presence signal was extracted from the model draft.");

        if (!string.Equals(evidence.AuthModelStatus, "known-connect-candidate", StringComparison.OrdinalIgnoreCase))
            blockers.Add("MQTT auth model is not known. Current status must remain unknown-read-only.");

        if (!string.Equals(evidence.TopicModelStatus, "known-read-candidate", StringComparison.OrdinalIgnoreCase))
            blockers.Add("MQTT topic model is not known. This blocks subscribe/publish/control work.");

        blockers.Add("MQTT client id format is not confirmed.");
        blockers.Add("MQTT username format is not confirmed.");
        blockers.Add("MQTT password/token format is not confirmed.");
        blockers.Add("CONNECT-only packet shape is not specified.");
        blockers.Add("CONNACK handling and immediate DISCONNECT behavior are not implemented.");

        var requiredBeforeConnect = new[]
        {
            "Confirm MQTT client id format.",
            "Confirm MQTT username format.",
            "Confirm MQTT password/token format.",
            "Confirm whether auth uses cloud uid/token, device mac/key, app token, region secret, signed payload, or another value.",
            "Define exact CONNECT-only packet options.",
            "Define timeout and immediate disconnect behavior after CONNACK.",
            "Define masked logging for client id, username, token, device key, and device identifiers.",
            "Confirm that the future test sends no SUBSCRIBE, no PUBLISH, no retained payload, no will message, and no device command."
        };

        var guardRails = new[]
        {
            "Future test must be opt-in with a separate flag, not part of normal probe flow.",
            "Future test must never run in configuration-only mode.",
            "Future test must not subscribe to any topic.",
            "Future test must not publish any MQTT message.",
            "Future test must not send power/mode/setpoint/fan/swing payloads.",
            "Future test must not write raw credentials or raw device keys to artifacts.",
            "Future test must mask client id, username, token, MAC-like identifiers, device keys, SSID, barcode, latitude, and longitude.",
            "Future test must disconnect immediately after CONNECT result is known.",
            "Future test must keep all artifacts under artifacts/gree-alice/ and out of Git.",
            "Future test must remain isolated in tools/AssistantEngineer.Tools.GreeCloudProbe."
        };

        var safety = new MqttConnectSafetyFlags(
            MqttConnectSent: false,
            MqttSubscribeSent: false,
            MqttPublishSent: false,
            DeviceControlSent: false,
            RawCredentialsStored: false,
            RawDeviceKeysStored: false,
            RawMacsStored: false,
            ProductionBridgeChanged: false);

        var decision = blockers.Count == 0
            ? "ready-for-separate-connect-only-implementation"
            : "not-ready";

        var summary = new MqttConnectSafetySummary(
            ModelReportFound: evidence.ModelReportFound,
            TransportTlsAuthenticated: evidence.TransportTlsAuthenticated == true,
            DeviceSignalsCount: evidence.DeviceSignalsCount,
            DeviceKeyPresenceCount: evidence.DeviceKeyPresenceCount,
            MaskedMacSignalCount: evidence.MaskedMacSignalCount,
            AuthModelStatus: evidence.AuthModelStatus,
            TopicModelStatus: evidence.TopicModelStatus,
            Decision: decision,
            BlockerCount: blockers.Count);

        return new MqttConnectSafetyReviewReport(
            StageName,
            ModeName,
            timestampUtc,
            new MqttConnectSafetyInputs(
                ModelReportPath: MaskPath(modelReportPath, options.RepositoryRoot),
                OutputDirectory: MaskPath(options.OutputDirectory, options.RepositoryRoot)),
            summary,
            evidence,
            blockers,
            requiredBeforeConnect,
            guardRails,
            safety,
            warnings,
            new[]
            {
                "This is an offline safety review.",
                "It does not connect to MQTT.",
                "It does not send MQTT CONNECT, SUBSCRIBE, PUBLISH, or any device command.",
                "At current evidence level, CONNECT-only implementation must remain blocked until auth/client inputs are known."
            });
    }

    private static MqttConnectSafetyEvidence ReadEvidence(
        string modelReportPath,
        List<string> warnings)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(modelReportPath));
            var root = document.RootElement;

            return new MqttConnectSafetyEvidence(
                ModelReportFound: true,
                ModelReportMode: TryGetString(root, "Mode"),
                TransportHost: TryGetNestedString(root, new[] { "Transport", "Host" }),
                TransportPort: TryGetNestedInt(root, new[] { "Transport", "Port" }),
                TransportTlsAuthenticated: TryGetNestedBool(root, new[] { "Transport", "TlsAuthenticated" }),
                TransportSslProtocol: TryGetNestedString(root, new[] { "Transport", "SslProtocol" }),
                DeviceSignalsCount: TryGetNestedInt(root, new[] { "Summary", "DeviceSignalsCount" }) ?? 0,
                DeviceKeyPresenceCount: TryGetNestedInt(root, new[] { "Summary", "DeviceKeyPresenceCount" }) ?? 0,
                MaskedMacSignalCount: TryGetNestedInt(root, new[] { "Summary", "MaskedMacSignalCount" }) ?? 0,
                AuthModelStatus: TryGetNestedString(root, new[] { "AuthModel", "Status" }) ?? "unknown",
                TopicModelStatus: TryGetNestedString(root, new[] { "TopicModel", "Status" }) ?? "unknown");
        }
        catch (Exception exception)
        {
            warnings.Add($"Failed to read MQTT model draft report: {exception.GetType().Name}");
            return MqttConnectSafetyEvidence.Empty;
        }
    }

    private static string? FindLatestUsefulModelReport(string directory)
    {
        if (!Directory.Exists(directory))
            return null;

        var files = Directory
            .GetFiles(directory, "gree-mqtt-auth-topic-model-*.json")
            .Select(static path => new FileInfo(path))
            .OrderByDescending(static file => file.LastWriteTimeUtc)
            .ToArray();

        foreach (var file in files)
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file.FullName));
                var mode = TryGetString(document.RootElement, "Mode");
                var tlsAuthenticated = TryGetNestedBool(document.RootElement, new[] { "Transport", "TlsAuthenticated" }) == true;
                var deviceSignals = TryGetNestedInt(document.RootElement, new[] { "Summary", "DeviceSignalsCount" }) ?? 0;

                if (mode?.Contains("mqtt-auth-topic-model-draft", StringComparison.OrdinalIgnoreCase) == true &&
                    tlsAuthenticated &&
                    deviceSignals > 0)
                {
                    return file.FullName;
                }
            }
            catch
            {
                // Ignore malformed local artifacts and keep searching older reports.
            }
        }

        return files.FirstOrDefault()?.FullName;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static string? TryGetString(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static string? TryGetNestedString(JsonElement root, IReadOnlyList<string> path)
    {
        return TryGetNestedElement(root, path, out var element)
            ? element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            }
            : null;
    }

    private static int? TryGetNestedInt(JsonElement root, IReadOnlyList<string> path)
    {
        if (!TryGetNestedElement(root, path, out var element))
            return null;

        if (element.ValueKind == JsonValueKind.Number &&
            element.TryGetInt32(out var number))
        {
            return number;
        }

        if (element.ValueKind == JsonValueKind.String &&
            int.TryParse(element.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static bool? TryGetNestedBool(JsonElement root, IReadOnlyList<string> path)
    {
        if (!TryGetNestedElement(root, path, out var element))
            return null;

        if (element.ValueKind == JsonValueKind.True)
            return true;

        if (element.ValueKind == JsonValueKind.False)
            return false;

        if (element.ValueKind == JsonValueKind.String &&
            bool.TryParse(element.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static bool TryGetNestedElement(JsonElement root, IReadOnlyList<string> path, out JsonElement element)
    {
        element = root;
        foreach (var segment in path)
        {
            if (!TryGetProperty(element, segment, out var next))
                return false;

            element = next;
        }

        return true;
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

    private static void PrintSummary(MqttConnectSafetyReviewReport report, string outputPath)
    {
        Console.WriteLine("AssistantEngineer Gree MQTT CONNECT-only safety review");
        Console.WriteLine($"Stage: {StageName}");
        Console.WriteLine($"Mode: {report.Mode}");
        Console.WriteLine($"Output: {outputPath}");
        Console.WriteLine();

        Console.WriteLine("Evidence:");
        Console.WriteLine($"  Model report found: {ToYesNo(report.Summary.ModelReportFound)}");
        Console.WriteLine($"  TLS authenticated: {ToYesNo(report.Summary.TransportTlsAuthenticated)}");
        Console.WriteLine($"  Device signals: {report.Summary.DeviceSignalsCount}");
        Console.WriteLine($"  Device key presence count: {report.Summary.DeviceKeyPresenceCount}");
        Console.WriteLine($"  Masked MAC signal count: {report.Summary.MaskedMacSignalCount}");
        Console.WriteLine($"  Auth model status: {report.Summary.AuthModelStatus}");
        Console.WriteLine($"  Topic model status: {report.Summary.TopicModelStatus}");
        Console.WriteLine();

        Console.WriteLine("Decision:");
        Console.WriteLine($"  {report.Summary.Decision}");
        Console.WriteLine($"  Blockers: {report.Summary.BlockerCount}");
        Console.WriteLine();

        Console.WriteLine("Safety:");
        Console.WriteLine($"  MQTT CONNECT sent: {ToYesNo(report.Safety.MqttConnectSent)}");
        Console.WriteLine($"  MQTT SUBSCRIBE sent: {ToYesNo(report.Safety.MqttSubscribeSent)}");
        Console.WriteLine($"  MQTT PUBLISH sent: {ToYesNo(report.Safety.MqttPublishSent)}");
        Console.WriteLine($"  Device control sent: {ToYesNo(report.Safety.DeviceControlSent)}");
        Console.WriteLine($"  Raw credentials stored: {ToYesNo(report.Safety.RawCredentialsStored)}");
        Console.WriteLine($"  Production bridge changed: {ToYesNo(report.Safety.ProductionBridgeChanged)}");

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
        Console.WriteLine("Next step: keep CONNECT implementation blocked until client id/auth inputs are known.");
    }

    private static string ToYesNo(bool value) => value ? "yes" : "no";

    private sealed record MqttConnectSafetyReviewOptions(
        string RepositoryRoot,
        string? ModelReportPath,
        string OutputDirectory,
        bool ConfigurationOnly)
    {
        public static MqttConnectSafetyReviewOptions Parse(string[] args)
        {
            var values = ReadArgs(args);

            var repoRoot = GetValue(values, "repo-root", null) ?? ResolveRepositoryRoot();
            repoRoot = Path.GetFullPath(repoRoot);

            var outputDir = GetValue(values, "output-dir", "GREE_ALICE_OUTPUT_DIR");
            if (string.IsNullOrWhiteSpace(outputDir))
                outputDir = Path.Combine(repoRoot, "artifacts", "gree-alice", "mqtt-connect-safety");

            var modelReportPath = GetValue(values, "mqtt-model-report", null);
            if (!string.IsNullOrWhiteSpace(modelReportPath))
                modelReportPath = Path.GetFullPath(modelReportPath);

            return new MqttConnectSafetyReviewOptions(
                repoRoot,
                modelReportPath,
                Path.GetFullPath(outputDir),
                values.ContainsKey("configuration-only"));
        }

        private static Dictionary<string, string?> ReadArgs(string[] args)
        {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];

                if (arg.Equals("--review-mqtt-connect-safety", StringComparison.OrdinalIgnoreCase))
                {
                    result["review-mqtt-connect-safety"] = null;
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

    private sealed record MqttConnectSafetyReviewReport(
        string Stage,
        string Mode,
        DateTimeOffset TimestampUtc,
        MqttConnectSafetyInputs Inputs,
        MqttConnectSafetySummary Summary,
        MqttConnectSafetyEvidence Evidence,
        IReadOnlyList<string> Blockers,
        IReadOnlyList<string> RequiredBeforeConnect,
        IReadOnlyList<string> GuardRails,
        MqttConnectSafetyFlags Safety,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Notes)
    {
        public static MqttConnectSafetyReviewReport ConfigurationOnly(
            MqttConnectSafetyReviewOptions options,
            DateTimeOffset timestampUtc)
        {
            return new MqttConnectSafetyReviewReport(
                StageName,
                "configuration-only",
                timestampUtc,
                new MqttConnectSafetyInputs(null, MaskPath(options.OutputDirectory, options.RepositoryRoot)),
                new MqttConnectSafetySummary(false, false, 0, 0, 0, "unknown", "unknown", "not-ready", 0),
                MqttConnectSafetyEvidence.Empty,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                new MqttConnectSafetyFlags(false, false, false, false, false, false, false, false),
                Array.Empty<string>(),
                new[]
                {
                    "Configuration-only mode did not read local reports.",
                    "Run without --configuration-only to review local MQTT model draft evidence."
                });
        }
    }

    private sealed record MqttConnectSafetyInputs(
        string? ModelReportPath,
        string? OutputDirectory);

    private sealed record MqttConnectSafetySummary(
        bool ModelReportFound,
        bool TransportTlsAuthenticated,
        int DeviceSignalsCount,
        int DeviceKeyPresenceCount,
        int MaskedMacSignalCount,
        string AuthModelStatus,
        string TopicModelStatus,
        string Decision,
        int BlockerCount);

    private sealed record MqttConnectSafetyEvidence(
        bool ModelReportFound,
        string? ModelReportMode,
        string? TransportHost,
        int? TransportPort,
        bool? TransportTlsAuthenticated,
        string? TransportSslProtocol,
        int DeviceSignalsCount,
        int DeviceKeyPresenceCount,
        int MaskedMacSignalCount,
        string AuthModelStatus,
        string TopicModelStatus)
    {
        public static MqttConnectSafetyEvidence Empty { get; } =
            new(false, null, null, null, null, null, 0, 0, 0, "unknown", "unknown");
    }

    private sealed record MqttConnectSafetyFlags(
        bool MqttConnectSent,
        bool MqttSubscribeSent,
        bool MqttPublishSent,
        bool DeviceControlSent,
        bool RawCredentialsStored,
        bool RawDeviceKeysStored,
        bool RawMacsStored,
        bool ProductionBridgeChanged);
}
