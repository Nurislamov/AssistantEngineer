using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AssistantEngineer.Tools.GreeCloudProbe;

internal static class MqttEvidenceInventoryCommand
{
    private const string StageName = "GREE-ALICE-15";
    private const string ModeName = "masked-mqtt-evidence-inventory";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly Regex MacRegex = new(
        @"\b(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}\b",
        RegexOptions.Compiled);

    private static readonly Regex EmailRegex = new(
        @"\b[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TokenLikeRegex = new(
        @"^[A-Za-z0-9_\-+/=]{32,}$",
        RegexOptions.Compiled);

    private static readonly Regex PrivateIpRegex = new(
        @"\b(?:10\.\d{1,3}\.\d{1,3}\.\d{1,3}|192\.168\.\d{1,3}\.\d{1,3}|172\.(?:1[6-9]|2\d|3[0-1])\.\d{1,3}\.\d{1,3})\b",
        RegexOptions.Compiled);

    private static readonly string[] SensitiveNameFragments =
    {
        "password",
        "passwd",
        "token",
        "secret",
        "key",
        "mac",
        "imei",
        "serial",
        "barcode",
        "ssid",
        "bssid",
        "phone",
        "email",
        "lat",
        "lng",
        "longitude",
        "latitude",
        "clientid",
        "client_id",
        "username",
        "userid",
        "user_id",
        "uid",
        "deviceid",
        "device_id"
    };

    private static readonly string[] MqttSignalFragments =
    {
        "mqtt",
        "topic",
        "client",
        "clientid",
        "client_id",
        "username",
        "password",
        "token",
        "auth",
        "subscribe",
        "publish",
        "qos",
        "connack",
        "connect",
        "broker"
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
            $"gree-mqtt-evidence-inventory-{timestampUtc:yyyyMMdd-HHmmss}.json");

        File.WriteAllText(outputPath, JsonSerializer.Serialize(report, JsonOptions));
        PrintSummary(report, outputPath);

        return 0;
    }

    private static Report BuildReport(Options options, DateTimeOffset timestampUtc)
    {
        var warnings = new List<string>();
        var rootExists = Directory.Exists(options.ArtifactsRoot);
        var files = new List<FileInventory>();
        var aggregate = new AggregateInventory();

        if (!rootExists)
        {
            warnings.Add("Artifacts root does not exist.");
        }
        else
        {
            var jsonFiles = Directory
                .EnumerateFiles(options.ArtifactsRoot, "*.json", SearchOption.AllDirectories)
                .Where(path => !IsOutputInventory(path, options.OutputDirectory))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Take(options.MaxFiles)
                .ToArray();

            foreach (var path in jsonFiles)
                files.Add(InspectFile(path, options, aggregate));
        }

        var distinctFieldNames = aggregate.FieldNameCounts
            .OrderByDescending(static pair => pair.Value)
            .ThenBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Take(options.MaxFieldNames)
            .Select(static pair => new FieldNameSummary(pair.Key, pair.Value, ClassifyFieldName(pair.Key)))
            .ToArray();

        var mqttSignalFieldNames = distinctFieldNames
            .Where(static item => item.Classification.Equals("mqtt-signal", StringComparison.OrdinalIgnoreCase) ||
                                  item.Classification.Equals("sensitive-or-identity", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var summary = new Summary(
            ArtifactsRootExists: rootExists,
            FilesScanned: files.Count,
            JsonFilesParsed: files.Count(static file => file.JsonParsed),
            JsonFilesFailed: files.Count(static file => !file.JsonParsed),
            DistinctFieldNames: aggregate.FieldNameCounts.Count,
            SensitiveOrIdentityFieldNameHits: aggregate.SensitiveFieldNameHits,
            MqttSignalFieldNameHits: aggregate.MqttSignalFieldNameHits,
            RawLeakCandidateHits: aggregate.RawLeakCandidateHits,
            StringLengthBucketCount: aggregate.StringLengthBuckets.Count,
            OutputContainsRawValues: false,
            MqttConnectImplementationIncluded: false,
            MqttConnectSent: false,
            MqttSubscribeSent: false,
            MqttPublishSent: false,
            DeviceControlSent: false,
            NetworkConnectionOpened: false,
            PrivateCaptureCommitted: false);

        return new Report(
            StageName,
            ModeName,
            timestampUtc,
            new Inputs(
                ArtifactsRoot: MaskPath(options.ArtifactsRoot, options.RepositoryRoot),
                OutputDirectory: MaskPath(options.OutputDirectory, options.RepositoryRoot),
                MaxFiles: options.MaxFiles,
                MaxFieldNames: options.MaxFieldNames),
            summary,
            files,
            distinctFieldNames,
            mqttSignalFieldNames,
            aggregate.StringLengthBuckets
                .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(static pair => new StringLengthBucketSummary(pair.Key, pair.Value))
                .ToArray(),
            new[]
            {
                "Only field names, classifications, counts, and length buckets are reported.",
                "Raw JSON primitive values are not written to the report.",
                "Potential raw leak candidates are counted but never printed.",
                "The command does not open network connections and does not send MQTT packets."
            },
            warnings);
    }

    private static FileInventory InspectFile(string path, Options options, AggregateInventory aggregate)
    {
        var info = new FileInfo(path);
        var relativePath = MaskPath(path, options.RepositoryRoot);
        var file = new MutableFileInventory(relativePath ?? "<external-path>", info.Length);

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            TraverseElement(document.RootElement, "$", file, aggregate);

            return new FileInventory(
                Path: file.Path,
                SizeBytes: file.SizeBytes,
                JsonParsed: true,
                FieldNameCount: file.FieldNameCount,
                SensitiveOrIdentityFieldNameHits: file.SensitiveFieldNameHits,
                MqttSignalFieldNameHits: file.MqttSignalFieldNameHits,
                RawLeakCandidateHits: file.RawLeakCandidateHits,
                StringValueCount: file.StringValueCount,
                MaxDepth: file.MaxDepth,
                ParseError: null);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return new FileInventory(
                Path: file.Path,
                SizeBytes: file.SizeBytes,
                JsonParsed: false,
                FieldNameCount: 0,
                SensitiveOrIdentityFieldNameHits: 0,
                MqttSignalFieldNameHits: 0,
                RawLeakCandidateHits: 0,
                StringValueCount: 0,
                MaxDepth: 0,
                ParseError: ex.GetType().Name);
        }
    }

    private static void TraverseElement(
        JsonElement element,
        string path,
        MutableFileInventory file,
        AggregateInventory aggregate)
    {
        file.MaxDepth = Math.Max(file.MaxDepth, CountDepth(path));

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var name = property.Name;
                    file.FieldNameCount++;
                    aggregate.IncrementFieldName(name);

                    var classification = ClassifyFieldName(name);
                    if (classification.Equals("sensitive-or-identity", StringComparison.OrdinalIgnoreCase))
                    {
                        file.SensitiveFieldNameHits++;
                        aggregate.SensitiveFieldNameHits++;
                    }

                    if (classification.Equals("mqtt-signal", StringComparison.OrdinalIgnoreCase))
                    {
                        file.MqttSignalFieldNameHits++;
                        aggregate.MqttSignalFieldNameHits++;
                    }

                    TraverseElement(property.Value, path + "." + NormalizePathName(name), file, aggregate);
                }

                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    TraverseElement(item, path + "[]", file, aggregate);
                    index++;

                    if (index > 5000)
                        break;
                }

                break;

            case JsonValueKind.String:
                file.StringValueCount++;
                var value = element.GetString() ?? string.Empty;
                var bucket = GetLengthBucket(value.Length);
                aggregate.IncrementLengthBucket(bucket);

                if (LooksLikeRawLeak(value))
                {
                    file.RawLeakCandidateHits++;
                    aggregate.RawLeakCandidateHits++;
                }

                break;
        }
    }

    private static bool LooksLikeRawLeak(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();

        return MacRegex.IsMatch(trimmed) ||
               EmailRegex.IsMatch(trimmed) ||
               PrivateIpRegex.IsMatch(trimmed) ||
               TokenLikeRegex.IsMatch(trimmed);
    }

    private static string ClassifyFieldName(string fieldName)
    {
        var compact = fieldName.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

        if (MqttSignalFragments.Any(fragment => compact.Contains(fragment.Replace("_", string.Empty, StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase)))
            return "mqtt-signal";

        if (SensitiveNameFragments.Any(fragment => compact.Contains(fragment.Replace("_", string.Empty, StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase)))
            return "sensitive-or-identity";

        return "general";
    }

    private static string NormalizePathName(string name)
    {
        if (name.Length <= 64)
            return name;

        return name[..64] + "<truncated>";
    }

    private static int CountDepth(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return 0;

        return path.Count(static ch => ch == '.' || ch == '[');
    }

    private static string GetLengthBucket(int length) =>
        length switch
        {
            <= 0 => "0",
            <= 4 => "1-4",
            <= 8 => "5-8",
            <= 16 => "9-16",
            <= 32 => "17-32",
            <= 64 => "33-64",
            <= 128 => "65-128",
            <= 256 => "129-256",
            _ => "257+"
        };

    private static bool IsOutputInventory(string path, string outputDirectory)
    {
        var fullPath = Path.GetFullPath(path);
        var fullOutput = Path.GetFullPath(outputDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return fullPath.StartsWith(fullOutput, StringComparison.OrdinalIgnoreCase);
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
        Console.WriteLine("AssistantEngineer Gree MQTT evidence inventory");
        Console.WriteLine($"Stage: {StageName}");
        Console.WriteLine($"Mode: {report.Mode}");
        Console.WriteLine($"Output: {outputPath}");
        Console.WriteLine();

        Console.WriteLine("Inventory:");
        Console.WriteLine($"  Artifacts root exists: {ToYesNo(report.Summary.ArtifactsRootExists)}");
        Console.WriteLine($"  Files scanned: {report.Summary.FilesScanned}");
        Console.WriteLine($"  JSON files parsed: {report.Summary.JsonFilesParsed}");
        Console.WriteLine($"  JSON files failed: {report.Summary.JsonFilesFailed}");
        Console.WriteLine($"  Distinct field names: {report.Summary.DistinctFieldNames}");
        Console.WriteLine($"  Sensitive/identity field name hits: {report.Summary.SensitiveOrIdentityFieldNameHits}");
        Console.WriteLine($"  MQTT signal field name hits: {report.Summary.MqttSignalFieldNameHits}");
        Console.WriteLine($"  Raw leak candidate hits: {report.Summary.RawLeakCandidateHits}");
        Console.WriteLine();

        Console.WriteLine("Safety:");
        Console.WriteLine($"  Output contains raw values: {ToYesNo(report.Summary.OutputContainsRawValues)}");
        Console.WriteLine($"  Network connection opened: {ToYesNo(report.Summary.NetworkConnectionOpened)}");
        Console.WriteLine($"  MQTT CONNECT implementation included: {ToYesNo(report.Summary.MqttConnectImplementationIncluded)}");
        Console.WriteLine($"  MQTT CONNECT sent: {ToYesNo(report.Summary.MqttConnectSent)}");
        Console.WriteLine($"  MQTT SUBSCRIBE sent: {ToYesNo(report.Summary.MqttSubscribeSent)}");
        Console.WriteLine($"  MQTT PUBLISH sent: {ToYesNo(report.Summary.MqttPublishSent)}");
        Console.WriteLine($"  Device control sent: {ToYesNo(report.Summary.DeviceControlSent)}");

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
        Console.WriteLine("Next step: review masked inventory only; do not use raw secrets or private captures.");
    }

    private static string ToYesNo(bool value) => value ? "yes" : "no";

    private sealed record Options(
        string RepositoryRoot,
        string ArtifactsRoot,
        string OutputDirectory,
        int MaxFiles,
        int MaxFieldNames,
        bool ConfigurationOnly)
    {
        public static Options Parse(string[] args)
        {
            var values = ReadArgs(args);

            var repoRoot = GetValue(values, "repo-root", null) ?? ResolveRepositoryRoot();
            repoRoot = Path.GetFullPath(repoRoot);

            var artifactsRoot = GetValue(values, "artifacts-root", "GREE_ALICE_ARTIFACTS_ROOT");
            if (string.IsNullOrWhiteSpace(artifactsRoot))
                artifactsRoot = Path.Combine(repoRoot, "artifacts", "gree-alice");

            var outputDir = GetValue(values, "output-dir", "GREE_ALICE_OUTPUT_DIR");
            if (string.IsNullOrWhiteSpace(outputDir))
                outputDir = Path.Combine(repoRoot, "artifacts", "gree-alice", "mqtt-evidence-inventory");

            return new Options(
                repoRoot,
                Path.GetFullPath(artifactsRoot),
                Path.GetFullPath(outputDir),
                ParsePositiveInt(GetValue(values, "max-files", null), 1000),
                ParsePositiveInt(GetValue(values, "max-field-names", null), 250),
                values.ContainsKey("configuration-only"));
        }

        private static Dictionary<string, string?> ReadArgs(string[] args)
        {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];

                if (arg.Equals("--inventory-mqtt-evidence", StringComparison.OrdinalIgnoreCase))
                {
                    result["inventory-mqtt-evidence"] = null;
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

        private static int ParsePositiveInt(string? value, int fallback)
        {
            return int.TryParse(value, out var parsed) && parsed > 0
                ? parsed
                : fallback;
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

    private sealed class AggregateInventory
    {
        public Dictionary<string, int> FieldNameCounts { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, int> StringLengthBuckets { get; } = new(StringComparer.OrdinalIgnoreCase);

        public int SensitiveFieldNameHits { get; set; }

        public int MqttSignalFieldNameHits { get; set; }

        public int RawLeakCandidateHits { get; set; }

        public void IncrementFieldName(string name)
        {
            FieldNameCounts[name] = FieldNameCounts.TryGetValue(name, out var count)
                ? count + 1
                : 1;
        }

        public void IncrementLengthBucket(string bucket)
        {
            StringLengthBuckets[bucket] = StringLengthBuckets.TryGetValue(bucket, out var count)
                ? count + 1
                : 1;
        }
    }

    private sealed class MutableFileInventory(string path, long sizeBytes)
    {
        public string Path { get; } = path;

        public long SizeBytes { get; } = sizeBytes;

        public int FieldNameCount { get; set; }

        public int SensitiveFieldNameHits { get; set; }

        public int MqttSignalFieldNameHits { get; set; }

        public int RawLeakCandidateHits { get; set; }

        public int StringValueCount { get; set; }

        public int MaxDepth { get; set; }
    }

    private sealed record Report(
        string Stage,
        string Mode,
        DateTimeOffset TimestampUtc,
        Inputs Inputs,
        Summary Summary,
        IReadOnlyList<FileInventory> Files,
        IReadOnlyList<FieldNameSummary> FieldNames,
        IReadOnlyList<FieldNameSummary> MqttSignalFieldNames,
        IReadOnlyList<StringLengthBucketSummary> StringLengthBuckets,
        IReadOnlyList<string> Notes,
        IReadOnlyList<string> Warnings)
    {
        public static Report ConfigurationOnly(Options options, DateTimeOffset timestampUtc)
        {
            return new Report(
                StageName,
                "configuration-only",
                timestampUtc,
                new Inputs(
                    ArtifactsRoot: MaskPath(options.ArtifactsRoot, options.RepositoryRoot),
                    OutputDirectory: MaskPath(options.OutputDirectory, options.RepositoryRoot),
                    MaxFiles: options.MaxFiles,
                    MaxFieldNames: options.MaxFieldNames),
                new Summary(
                    ArtifactsRootExists: Directory.Exists(options.ArtifactsRoot),
                    FilesScanned: 0,
                    JsonFilesParsed: 0,
                    JsonFilesFailed: 0,
                    DistinctFieldNames: 0,
                    SensitiveOrIdentityFieldNameHits: 0,
                    MqttSignalFieldNameHits: 0,
                    RawLeakCandidateHits: 0,
                    StringLengthBucketCount: 0,
                    OutputContainsRawValues: false,
                    MqttConnectImplementationIncluded: false,
                    MqttConnectSent: false,
                    MqttSubscribeSent: false,
                    MqttPublishSent: false,
                    DeviceControlSent: false,
                    NetworkConnectionOpened: false,
                    PrivateCaptureCommitted: false),
                Array.Empty<FileInventory>(),
                Array.Empty<FieldNameSummary>(),
                Array.Empty<FieldNameSummary>(),
                Array.Empty<StringLengthBucketSummary>(),
                new[]
                {
                    "Configuration-only mode did not inspect artifact contents.",
                    "Run without --configuration-only to generate a masked local inventory."
                },
                Array.Empty<string>());
        }
    }

    private sealed record Inputs(
        string? ArtifactsRoot,
        string? OutputDirectory,
        int MaxFiles,
        int MaxFieldNames);

    private sealed record Summary(
        bool ArtifactsRootExists,
        int FilesScanned,
        int JsonFilesParsed,
        int JsonFilesFailed,
        int DistinctFieldNames,
        int SensitiveOrIdentityFieldNameHits,
        int MqttSignalFieldNameHits,
        int RawLeakCandidateHits,
        int StringLengthBucketCount,
        bool OutputContainsRawValues,
        bool MqttConnectImplementationIncluded,
        bool MqttConnectSent,
        bool MqttSubscribeSent,
        bool MqttPublishSent,
        bool DeviceControlSent,
        bool NetworkConnectionOpened,
        bool PrivateCaptureCommitted);

    private sealed record FileInventory(
        string Path,
        long SizeBytes,
        bool JsonParsed,
        int FieldNameCount,
        int SensitiveOrIdentityFieldNameHits,
        int MqttSignalFieldNameHits,
        int RawLeakCandidateHits,
        int StringValueCount,
        int MaxDepth,
        string? ParseError);

    private sealed record FieldNameSummary(
        string Name,
        int Count,
        string Classification);

    private sealed record StringLengthBucketSummary(
        string Bucket,
        int Count);
}
