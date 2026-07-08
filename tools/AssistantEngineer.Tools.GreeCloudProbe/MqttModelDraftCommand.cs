using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssistantEngineer.Tools.GreeCloudProbe;

internal static class MqttModelDraftCommand
{
    private const string StageName = "GREE-ALICE-09";
    private const string ModeName = "mqtt-auth-topic-model-draft";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static int Run(string[] args)
    {
        var options = MqttModelDraftOptions.Parse(args);
        Directory.CreateDirectory(options.OutputDirectory);

        var timestampUtc = DateTimeOffset.UtcNow;
        var report = options.ConfigurationOnly
            ? MqttModelDraftReport.ConfigurationOnly(options, timestampUtc)
            : BuildReport(options, timestampUtc);

        var outputPath = Path.Combine(
            options.OutputDirectory,
            $"gree-mqtt-auth-topic-model-{timestampUtc:yyyyMMdd-HHmmss}.json");

        File.WriteAllText(outputPath, JsonSerializer.Serialize(report, JsonOptions));
        PrintSummary(report, outputPath);

        return 0;
    }

    private static MqttModelDraftReport BuildReport(
        MqttModelDraftOptions options,
        DateTimeOffset timestampUtc)
    {
        var warnings = new List<string>();

        var discoveryReportPath = options.DiscoveryReportPath ?? FindLatestUsefulDiscoveryReport(
            Path.Combine(options.RepositoryRoot, "artifacts", "gree-alice", "probe"));

        var mqttChannelReportPath = options.MqttChannelReportPath ?? FindLatestUsefulMqttChannelReport(
            Path.Combine(options.RepositoryRoot, "artifacts", "gree-alice", "mqtt-channel"));

        var devices = discoveryReportPath is null
            ? Array.Empty<MqttDeviceSignal>()
            : ReadDeviceSignals(discoveryReportPath, warnings);

        if (discoveryReportPath is null)
            warnings.Add("No local masked cloud discovery report was found. Device signals are empty.");

        var transport = mqttChannelReportPath is null
            ? MqttTransportModel.Unknown
            : ReadTransportModel(mqttChannelReportPath, warnings);

        if (mqttChannelReportPath is null)
            warnings.Add("No local MQTT channel probe report was found. Transport model is unknown.");

        if (discoveryReportPath is not null && devices.Count == 0)
            warnings.Add("A cloud discovery report was found, but no device signals were extracted. Check report schema or pass --input-report explicitly.");

        var keyProvidedCount = devices.Count(static device => device.KeyProvided == true);
        var macSignalCount = devices.Count(static device => !string.IsNullOrWhiteSpace(device.MacMasked));

        var authKnownInputs = new List<string>
        {
            "MQTT broker host/port candidate from traffic and GREE-ALICE-08 transport probe.",
            "Gree+ Cloud REST login is validated separately and provides uid/token during runtime only.",
            "Cloud device discovery reports whether each device has a local device key; raw keys must remain secret.",
            "Masked MAC-like identifiers can be used for shape analysis only, not as raw credentials."
        };

        if (keyProvidedCount > 0)
            authKnownInputs.Add($"Local device key presence was observed for {keyProvidedCount} device(s), but raw key values are not stored in this model.");

        if (macSignalCount > 0)
            authKnownInputs.Add($"Masked MAC-like identifier presence was observed for {macSignalCount} device(s).");

        var authModel = new MqttAuthModel(
            Status: "unknown-read-only",
            KnownInputs: authKnownInputs,
            Unknowns: new[]
            {
                "MQTT client id format",
                "MQTT username format",
                "MQTT password/token format",
                "whether MQTT auth uses cloud uid/token, device mac/key, app token, region secret, or another signed payload",
                "whether CONNECT can be safely tested without subscribing to topics"
            },
            NotAllowedYet: new[]
            {
                "do not store Gree+ password in files",
                "do not write raw token, raw key, raw MAC, SSID, barcode, latitude, or longitude into artifacts",
                "do not send MQTT CONNECT until auth inputs and safety boundaries are understood",
                "do not send SUBSCRIBE or PUBLISH",
                "do not send device control commands"
            });

        var topicModel = new MqttTopicModel(
            Status: "unknown-read-only",
            KnownTopicHints: Array.Empty<string>(),
            Unknowns: new[]
            {
                "status topic naming",
                "command topic naming",
                "account or home topic prefix",
                "device topic prefix",
                "QoS level",
                "payload encryption/signature shape",
                "whether cloud status events are pushed only after subscription"
            },
            NotAllowedYet: new[]
            {
                "do not subscribe to wildcard topics",
                "do not publish probe messages",
                "do not send power/mode/setpoint/fan/swing payloads",
                "do not connect this to Yandex Smart Home yet"
            });

        var safety = new MqttModelSafety(
            MqttConnectSent: false,
            MqttSubscribeSent: false,
            MqttPublishSent: false,
            DeviceControlSent: false,
            RawCredentialsStored: false,
            RawDeviceKeysStored: false,
            RawMacsStored: false);

        var summary = new MqttModelSummary(
            DiscoveryReportFound: discoveryReportPath is not null,
            MqttChannelReportFound: mqttChannelReportPath is not null,
            DeviceSignalsCount: devices.Count,
            DeviceKeyPresenceCount: keyProvidedCount,
            MaskedMacSignalCount: macSignalCount,
            TransportTlsAuthenticated: transport.TlsAuthenticated == true,
            AuthModelStatus: authModel.Status,
            TopicModelStatus: topicModel.Status);

        return new MqttModelDraftReport(
            StageName,
            ModeName,
            timestampUtc,
            new MqttModelInputs(
                DiscoveryReportPath: MaskPath(discoveryReportPath, options.RepositoryRoot),
                MqttChannelReportPath: MaskPath(mqttChannelReportPath, options.RepositoryRoot),
                OutputDirectory: MaskPath(options.OutputDirectory, options.RepositoryRoot)),
            summary,
            transport,
            devices,
            authModel,
            topicModel,
            safety,
            warnings,
            new[]
            {
                "This report is an offline draft from local masked artifacts.",
                "It does not connect to MQTT.",
                "It does not subscribe or publish.",
                "It does not send device control commands.",
                "Use it to decide what evidence is still missing before any MQTT protocol test."
            });
    }

    private static IReadOnlyList<MqttDeviceSignal> ReadDeviceSignals(
        string path,
        List<string> warnings)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (!TryGetProperty(document.RootElement, "Devices", out var devicesElement) ||
                devicesElement.ValueKind != JsonValueKind.Array)
            {
                warnings.Add("Discovery report does not contain a Devices array.");
                return Array.Empty<MqttDeviceSignal>();
            }

            var devices = new List<MqttDeviceSignal>();
            foreach (var device in devicesElement.EnumerateArray())
            {
                var safeRawPropertyNames = TryGetProperty(device, "SafeRawProperties", out var safeRaw) &&
                                           safeRaw.ValueKind == JsonValueKind.Object
                    ? safeRaw.EnumerateObject().Select(static property => property.Name).OrderBy(static name => name, StringComparer.OrdinalIgnoreCase).ToArray()
                    : Array.Empty<string>();

                devices.Add(new MqttDeviceSignal(
                    DeviceName: TryGetString(device, "DeviceName", "Name", "name", "DeviceNameMasked"),
                    Classification: TryGetString(device, "Classification", "DeviceClassification", "NormalizedKind"),
                    Version: TryGetString(device, "Version", "ver", "DeviceVersion"),
                    MacMasked: TryGetString(device, "MacMasked", "Mac", "mac", "DeviceMacMasked"),
                    KeyProvided: TryGetBool(device, "KeyProvided", "HasKey", "keyProvided"),
                    KeyMasked: TryGetString(device, "KeyMasked", "Key", "key"),
                    RawFieldNames: TryGetStringArray(device, "RawFieldNames"),
                    SafeRawPropertyNames: safeRawPropertyNames));
            }

            return devices;
        }
        catch (Exception exception)
        {
            warnings.Add($"Failed to read discovery report: {exception.GetType().Name}");
            return Array.Empty<MqttDeviceSignal>();
        }
    }

    private static MqttTransportModel ReadTransportModel(
        string path,
        List<string> warnings)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;

            var host = TryGetNestedString(root, new[] { "Inputs", "Host" }) ?? "mqtt-hk.gree.com";
            var port = TryGetNestedInt(root, new[] { "Inputs", "Port" }) ?? 1994;

            return new MqttTransportModel(
                Host: host,
                Port: port,
                DnsResolved: TryGetNestedBool(root, new[] { "Summary", "DnsResolved" }),
                TcpConnected: TryGetNestedBool(root, new[] { "Summary", "TcpConnected" }),
                TlsAuthenticated: TryGetNestedBool(root, new[] { "Summary", "TlsAuthenticated" }),
                SslProtocol: TryGetNestedString(root, new[] { "Tls", "SslProtocol" }),
                CertificateSubject: TryGetNestedString(root, new[] { "Tls", "CertificateSubject" }),
                CertificateIssuer: TryGetNestedString(root, new[] { "Tls", "CertificateIssuer" }),
                CertificateNotAfter: TryGetNestedString(root, new[] { "Tls", "CertificateNotAfter" }));
        }
        catch (Exception exception)
        {
            warnings.Add($"Failed to read MQTT channel report: {exception.GetType().Name}");
            return MqttTransportModel.Unknown;
        }
    }

    private static string? FindLatestUsefulDiscoveryReport(string directory)
    {
        return FindLatestUsefulReport(
            directory,
            "gree-cloud-probe-*.json",
            static root =>
            {
                var mode = TryGetString(root, "Mode");
                var hasDevices = TryGetProperty(root, "Devices", out var devices) &&
                                 devices.ValueKind == JsonValueKind.Array &&
                                 devices.GetArrayLength() > 0;

                var authSucceeded = TryGetNestedBool(root, new[] { "Auth", "LoginSucceeded" }) == true;

                return hasDevices || (authSucceeded && mode?.Contains("cloud", StringComparison.OrdinalIgnoreCase) == true);
            });
    }

    private static string? FindLatestUsefulMqttChannelReport(string directory)
    {
        return FindLatestUsefulReport(
            directory,
            "gree-mqtt-channel-probe-*.json",
            static root =>
            {
                var tlsAuthenticated = TryGetNestedBool(root, new[] { "Summary", "TlsAuthenticated" }) == true;
                var tcpConnected = TryGetNestedBool(root, new[] { "Summary", "TcpConnected" }) == true;

                return tlsAuthenticated || tcpConnected;
            });
    }

    private static string? FindLatestUsefulReport(
        string directory,
        string pattern,
        Func<JsonElement, bool> isUseful)
    {
        if (!Directory.Exists(directory))
            return null;

        var files = Directory
            .GetFiles(directory, pattern)
            .Select(static path => new FileInfo(path))
            .OrderByDescending(static file => file.LastWriteTimeUtc)
            .ToArray();

        foreach (var file in files)
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file.FullName));
                if (isUseful(document.RootElement))
                    return file.FullName;
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

    private static string? TryGetString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(element, name, out var value))
                continue;

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };
        }

        return null;
    }

    private static bool? TryGetBool(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(element, name, out var value))
                continue;

            if (value.ValueKind == JsonValueKind.True)
                return true;

            if (value.ValueKind == JsonValueKind.False)
                return false;

            if (value.ValueKind == JsonValueKind.String &&
                bool.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static IReadOnlyList<string> TryGetStringArray(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var value) ||
            value.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return value
            .EnumerateArray()
            .Select(static item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText())
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Select(static item => item!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static item => item, StringComparer.OrdinalIgnoreCase)
            .ToArray();
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

    private static void PrintSummary(MqttModelDraftReport report, string outputPath)
    {
        Console.WriteLine("AssistantEngineer Gree MQTT auth/topic model draft");
        Console.WriteLine($"Stage: {StageName}");
        Console.WriteLine($"Mode: {report.Mode}");
        Console.WriteLine($"Output: {outputPath}");
        Console.WriteLine();

        Console.WriteLine("Inputs:");
        Console.WriteLine($"  Discovery report found: {ToYesNo(report.Summary.DiscoveryReportFound)}");
        Console.WriteLine($"  MQTT channel report found: {ToYesNo(report.Summary.MqttChannelReportFound)}");
        Console.WriteLine();

        Console.WriteLine("Transport:");
        Console.WriteLine($"  Host: {report.Transport.Host}");
        Console.WriteLine($"  Port: {report.Transport.Port}");
        Console.WriteLine($"  TLS authenticated: {DisplayBool(report.Transport.TlsAuthenticated)}");
        Console.WriteLine($"  TLS protocol: {DisplayValue(report.Transport.SslProtocol)}");
        Console.WriteLine();

        Console.WriteLine("Model:");
        Console.WriteLine($"  Device signals: {report.Summary.DeviceSignalsCount}");
        Console.WriteLine($"  Device key presence count: {report.Summary.DeviceKeyPresenceCount}");
        Console.WriteLine($"  Masked MAC signal count: {report.Summary.MaskedMacSignalCount}");
        Console.WriteLine($"  Auth model status: {report.AuthModel.Status}");
        Console.WriteLine($"  Topic model status: {report.TopicModel.Status}");
        Console.WriteLine();

        Console.WriteLine("Safety:");
        Console.WriteLine($"  MQTT CONNECT sent: {ToYesNo(report.Safety.MqttConnectSent)}");
        Console.WriteLine($"  MQTT SUBSCRIBE sent: {ToYesNo(report.Safety.MqttSubscribeSent)}");
        Console.WriteLine($"  MQTT PUBLISH sent: {ToYesNo(report.Safety.MqttPublishSent)}");
        Console.WriteLine($"  Device control sent: {ToYesNo(report.Safety.DeviceControlSent)}");
        Console.WriteLine($"  Raw credentials stored: {ToYesNo(report.Safety.RawCredentialsStored)}");

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
        Console.WriteLine("Next step: inspect the local model draft, then decide whether a safe MQTT CONNECT-only test is justified.");
    }

    private static string DisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "<not set>" : value;

    private static string DisplayBool(bool? value) =>
        value.HasValue ? ToYesNo(value.Value) : "<unknown>";

    private static string ToYesNo(bool value) => value ? "yes" : "no";

    private sealed record MqttModelDraftOptions(
        string RepositoryRoot,
        string? DiscoveryReportPath,
        string? MqttChannelReportPath,
        string OutputDirectory,
        bool ConfigurationOnly)
    {
        public static MqttModelDraftOptions Parse(string[] args)
        {
            var values = ReadArgs(args);

            var repoRoot = GetValue(values, "repo-root", null) ?? ResolveRepositoryRoot();
            repoRoot = Path.GetFullPath(repoRoot);

            var outputDir = GetValue(values, "output-dir", "GREE_ALICE_OUTPUT_DIR");
            if (string.IsNullOrWhiteSpace(outputDir))
                outputDir = Path.Combine(repoRoot, "artifacts", "gree-alice", "mqtt-model");

            var discoveryReportPath = GetValue(values, "input-report", null);
            if (!string.IsNullOrWhiteSpace(discoveryReportPath))
                discoveryReportPath = Path.GetFullPath(discoveryReportPath);

            var mqttReportPath = GetValue(values, "mqtt-report", null);
            if (!string.IsNullOrWhiteSpace(mqttReportPath))
                mqttReportPath = Path.GetFullPath(mqttReportPath);

            return new MqttModelDraftOptions(
                repoRoot,
                discoveryReportPath,
                mqttReportPath,
                Path.GetFullPath(outputDir),
                values.ContainsKey("configuration-only"));
        }

        private static Dictionary<string, string?> ReadArgs(string[] args)
        {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];

                if (arg.Equals("--draft-mqtt-model", StringComparison.OrdinalIgnoreCase))
                {
                    result["draft-mqtt-model"] = null;
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

    private sealed record MqttModelDraftReport(
        string Stage,
        string Mode,
        DateTimeOffset TimestampUtc,
        MqttModelInputs Inputs,
        MqttModelSummary Summary,
        MqttTransportModel Transport,
        IReadOnlyList<MqttDeviceSignal> DeviceSignals,
        MqttAuthModel AuthModel,
        MqttTopicModel TopicModel,
        MqttModelSafety Safety,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Notes)
    {
        public static MqttModelDraftReport ConfigurationOnly(
            MqttModelDraftOptions options,
            DateTimeOffset timestampUtc)
        {
            return new MqttModelDraftReport(
                StageName,
                "configuration-only",
                timestampUtc,
                new MqttModelInputs(null, null, MaskPath(options.OutputDirectory, options.RepositoryRoot)),
                new MqttModelSummary(false, false, 0, 0, 0, false, "unknown-read-only", "unknown-read-only"),
                MqttTransportModel.Unknown,
                Array.Empty<MqttDeviceSignal>(),
                new MqttAuthModel("unknown-read-only", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()),
                new MqttTopicModel("unknown-read-only", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()),
                new MqttModelSafety(false, false, false, false, false, false, false),
                Array.Empty<string>(),
                new[]
                {
                    "Configuration-only mode did not read local reports.",
                    "Run without --configuration-only to draft the model from local masked artifacts."
                });
        }
    }

    private sealed record MqttModelInputs(
        string? DiscoveryReportPath,
        string? MqttChannelReportPath,
        string? OutputDirectory);

    private sealed record MqttModelSummary(
        bool DiscoveryReportFound,
        bool MqttChannelReportFound,
        int DeviceSignalsCount,
        int DeviceKeyPresenceCount,
        int MaskedMacSignalCount,
        bool TransportTlsAuthenticated,
        string AuthModelStatus,
        string TopicModelStatus);

    private sealed record MqttTransportModel(
        string Host,
        int Port,
        bool? DnsResolved,
        bool? TcpConnected,
        bool? TlsAuthenticated,
        string? SslProtocol,
        string? CertificateSubject,
        string? CertificateIssuer,
        string? CertificateNotAfter)
    {
        public static MqttTransportModel Unknown { get; } =
            new("mqtt-hk.gree.com", 1994, null, null, null, null, null, null, null);
    }

    private sealed record MqttDeviceSignal(
        string? DeviceName,
        string? Classification,
        string? Version,
        string? MacMasked,
        bool? KeyProvided,
        string? KeyMasked,
        IReadOnlyList<string> RawFieldNames,
        IReadOnlyList<string> SafeRawPropertyNames);

    private sealed record MqttAuthModel(
        string Status,
        IReadOnlyList<string> KnownInputs,
        IReadOnlyList<string> Unknowns,
        IReadOnlyList<string> NotAllowedYet);

    private sealed record MqttTopicModel(
        string Status,
        IReadOnlyList<string> KnownTopicHints,
        IReadOnlyList<string> Unknowns,
        IReadOnlyList<string> NotAllowedYet);

    private sealed record MqttModelSafety(
        bool MqttConnectSent,
        bool MqttSubscribeSent,
        bool MqttPublishSent,
        bool DeviceControlSent,
        bool RawCredentialsStored,
        bool RawDeviceKeysStored,
        bool RawMacsStored);
}
