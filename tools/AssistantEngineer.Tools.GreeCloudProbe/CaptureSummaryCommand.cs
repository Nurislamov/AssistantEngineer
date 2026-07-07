using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AssistantEngineer.Tools.GreeCloudProbe;

internal static class CaptureSummaryCommand
{
    private const string StageName = "GREE-ALICE-07";
    private const string ModeName = "capture-summary";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static int Run(string[] args)
    {
        var options = CaptureSummaryOptions.Parse(args);

        if (!File.Exists(options.CaptureInput))
            throw new FileNotFoundException("Capture input file was not found.", options.CaptureInput);

        Directory.CreateDirectory(options.OutputDirectory);

        var timestampUtc = DateTimeOffset.UtcNow;
        var raw = File.ReadAllText(options.CaptureInput, Encoding.UTF8);
        var sanitized = CaptureSanitizer.Sanitize(raw);

        var urls = ExtractUrlObservations(sanitized);
        var hosts = ExtractHostObservations(sanitized, urls);
        var ports = ExtractPorts(sanitized, urls);
        var protocolHints = ExtractProtocolHints(sanitized, urls, ports);
        var keywordHits = ExtractKeywordHits(sanitized);
        var pathObservations = ExtractPathObservations(urls);

        var report = new CaptureSummaryReport(
            Stage: StageName,
            Mode: ModeName,
            TimestampUtc: timestampUtc,
            Input: new CaptureSummaryInput(
                CaptureInput: options.MaskLocalPaths ? MaskPath(options.CaptureInput) : options.CaptureInput,
                InputLength: raw.Length,
                SanitizedPreviewLength: options.PreviewLength,
                MaskLocalPaths: options.MaskLocalPaths),
            Summary: new CaptureSummary(
                UrlCount: urls.Count,
                HostCount: hosts.Count,
                PathShapeCount: pathObservations.Count,
                PortCount: ports.Count,
                ProtocolHintCount: protocolHints.Count,
                KeywordHitCount: keywordHits.Count),
            Hosts: hosts,
            PathObservations: pathObservations,
            Ports: ports,
            ProtocolHints: protocolHints,
            KeywordHits: keywordHits,
            SanitizedPreview: Truncate(sanitized, options.PreviewLength),
            Notes: new[]
            {
                "This report is generated from a user-provided capture export.",
                "The tool does not capture traffic and does not send control commands.",
                "Sensitive values are masked before saving the report.",
                "Use this only with traffic from accounts/devices you own or are authorized to analyze."
            });

        var outputPath = Path.Combine(
            options.OutputDirectory,
            $"gree-plus-capture-summary-{timestampUtc:yyyyMMdd-HHmmss}.json");

        File.WriteAllText(outputPath, JsonSerializer.Serialize(report, JsonOptions), Encoding.UTF8);

        PrintSummary(report, outputPath);
        return 0;
    }

    private static void PrintSummary(CaptureSummaryReport report, string outputPath)
    {
        Console.WriteLine("AssistantEngineer Gree+ capture summary");
        Console.WriteLine($"Stage: {StageName}");
        Console.WriteLine($"Mode: {ModeName}");
        Console.WriteLine($"Output: {outputPath}");
        Console.WriteLine();

        Console.WriteLine("Summary:");
        Console.WriteLine($"  URLs: {report.Summary.UrlCount}");
        Console.WriteLine($"  Hosts: {report.Summary.HostCount}");
        Console.WriteLine($"  Path shapes: {report.Summary.PathShapeCount}");
        Console.WriteLine($"  Ports: {report.Summary.PortCount}");
        Console.WriteLine($"  Protocol hints: {report.Summary.ProtocolHintCount}");
        Console.WriteLine($"  Keyword hits: {report.Summary.KeywordHitCount}");

        if (report.Hosts.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Hosts:");
            foreach (var host in report.Hosts.Take(20))
                Console.WriteLine($"  - {host.Host} ({host.Source}, count={host.Count})");
        }

        if (report.ProtocolHints.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Protocol hints:");
            foreach (var hint in report.ProtocolHints)
                Console.WriteLine($"  - {hint.Hint}: {hint.Reason}");
        }

        if (report.PathObservations.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Path observations:");
            foreach (var path in report.PathObservations.Take(20))
                Console.WriteLine($"  - {path.Scheme}://{path.Host}{path.PathShape} (count={path.Count})");
        }

        Console.WriteLine();
        Console.WriteLine("Next step: inspect the JSON report and share only sanitized host/path/protocol hints.");
    }

    private static IReadOnlyList<UrlObservation> ExtractUrlObservations(string sanitized)
    {
        var regex = new Regex(
            @"(?ix)\b(?<scheme>https?|wss?|mqtts?)://(?<rest>[^\s""'<>\\\)\]]+)",
            RegexOptions.Compiled);

        var observations = new List<UrlObservation>();

        foreach (Match match in regex.Matches(sanitized))
        {
            var candidate = match.Value.TrimEnd('.', ',', ';', ':');

            if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
                continue;

            observations.Add(new UrlObservation(
                Scheme: uri.Scheme.ToLowerInvariant(),
                Host: uri.Host.ToLowerInvariant(),
                Port: uri.IsDefaultPort ? null : uri.Port,
                PathShape: BuildPathShape(uri),
                OriginalShape: BuildUrlShape(uri)));
        }

        return observations;
    }

    private static IReadOnlyList<HostObservation> ExtractHostObservations(
        string sanitized,
        IReadOnlyList<UrlObservation> urls)
    {
        var counts = new Dictionary<(string Host, string Source), int>();

        foreach (var url in urls)
            Add(counts, url.Host, "url");

        var fieldRegex = new Regex(
            @"(?ix)\b(?:host|hostname|server_name|sni|destination|dst|server|remote_address|remote)\s*[:=]\s*[""']?(?<host>[a-z0-9][a-z0-9.-]+\.[a-z]{2,}|(?:\d{1,3}\.){3}\d{1,3})",
            RegexOptions.Compiled);

        foreach (Match match in fieldRegex.Matches(sanitized))
            Add(counts, match.Groups["host"].Value.ToLowerInvariant(), "field");

        return counts
            .OrderBy(static item => item.Key.Host, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static item => item.Key.Source, StringComparer.OrdinalIgnoreCase)
            .Select(static item => new HostObservation(item.Key.Host, item.Key.Source, item.Value))
            .ToArray();

        static void Add(IDictionary<(string Host, string Source), int> counts, string host, string source)
        {
            if (string.IsNullOrWhiteSpace(host))
                return;

            var normalized = host.Trim().TrimEnd('.').ToLowerInvariant();
            if (normalized.Length == 0)
                return;

            var key = (normalized, source);
            counts[key] = counts.TryGetValue(key, out var value) ? value + 1 : 1;
        }
    }

    private static IReadOnlyList<PathObservation> ExtractPathObservations(IReadOnlyList<UrlObservation> urls)
    {
        return urls
            .GroupBy(static url => (url.Scheme, url.Host, url.PathShape))
            .OrderBy(static group => group.Key.Host, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static group => group.Key.PathShape, StringComparer.OrdinalIgnoreCase)
            .Select(static group => new PathObservation(
                group.Key.Scheme,
                group.Key.Host,
                group.Key.PathShape,
                group.Count()))
            .ToArray();
    }

    private static IReadOnlyList<PortObservation> ExtractPorts(
        string sanitized,
        IReadOnlyList<UrlObservation> urls)
    {
        var counts = new Dictionary<int, int>();

        foreach (var port in urls.Select(static url => url.Port).Where(static port => port.HasValue).Select(static port => port!.Value))
            Add(counts, port);

        var portRegex = new Regex(
            @"(?ix)\b(?:port|dstport|destination_port|remote_port)\s*[:=]\s*(?<port>\d{2,5})\b",
            RegexOptions.Compiled);

        foreach (Match match in portRegex.Matches(sanitized))
        {
            if (int.TryParse(match.Groups["port"].Value, out var port))
                Add(counts, port);
        }

        return counts
            .OrderBy(static item => item.Key)
            .Select(static item => new PortObservation(item.Key, item.Value, DescribePort(item.Key)))
            .ToArray();

        static void Add(IDictionary<int, int> counts, int port)
        {
            if (port <= 0 || port > 65535)
                return;

            counts[port] = counts.TryGetValue(port, out var value) ? value + 1 : 1;
        }
    }

    private static IReadOnlyList<ProtocolHint> ExtractProtocolHints(
        string sanitized,
        IReadOnlyList<UrlObservation> urls,
        IReadOnlyList<PortObservation> ports)
    {
        var lower = sanitized.ToLowerInvariant();
        var hints = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (urls.Any(static url => url.Scheme.Equals("wss", StringComparison.OrdinalIgnoreCase) ||
                                   url.Scheme.Equals("ws", StringComparison.OrdinalIgnoreCase)) ||
            lower.Contains("websocket", StringComparison.OrdinalIgnoreCase))
        {
            hints["websocket"] = "capture contains ws/wss URL or websocket keyword";
        }

        if (urls.Any(static url => url.Scheme.StartsWith("mqtt", StringComparison.OrdinalIgnoreCase)) ||
            lower.Contains("mqtt", StringComparison.OrdinalIgnoreCase) ||
            ports.Any(static port => port.Port is 1883 or 8883))
        {
            hints["mqtt"] = "capture contains mqtt keyword, mqtt URL, or MQTT-like port 1883/8883";
        }

        if (ports.Any(static port => port.Port == 443) ||
            urls.Any(static url => url.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)))
        {
            hints["https"] = "capture contains HTTPS URL or port 443";
        }

        if (lower.Contains("/app/", StringComparison.OrdinalIgnoreCase) ||
            urls.Any(static url => url.PathShape.StartsWith("/App/", StringComparison.OrdinalIgnoreCase)))
        {
            hints["gree-app-rest"] = "capture contains /App/ path shape";
        }

        if (lower.Contains("grih", StringComparison.OrdinalIgnoreCase) ||
            urls.Any(static url => url.Host.Contains("grih", StringComparison.OrdinalIgnoreCase)))
        {
            hints["grih-host"] = "capture contains grih host marker";
        }

        if (lower.Contains("iot", StringComparison.OrdinalIgnoreCase))
            hints["iot"] = "capture contains iot keyword";

        return hints
            .Select(static item => new ProtocolHint(item.Key, item.Value))
            .ToArray();
    }

    private static IReadOnlyList<KeywordHit> ExtractKeywordHits(string sanitized)
    {
        var keywords = new[]
        {
            "gree",
            "grih",
            "mqtt",
            "wss",
            "websocket",
            "iot",
            "AC3167",
            "Pow",
            "Mod",
            "SetTem",
            "WdSpd",
            "SwUpDn",
            "SwLfRig",
            "TemSen",
            "device",
            "status",
            "control",
            "bind",
            "home",
            "room"
        };

        return keywords
            .Select(keyword => new KeywordHit(keyword, CountOccurrences(sanitized, keyword)))
            .Where(static hit => hit.Count > 0)
            .OrderByDescending(static hit => hit.Count)
            .ThenBy(static hit => hit.Keyword, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string BuildPathShape(Uri uri)
    {
        var path = string.IsNullOrWhiteSpace(uri.AbsolutePath) ? "/" : uri.AbsolutePath;

        if (string.IsNullOrWhiteSpace(uri.Query))
            return path;

        var keys = uri.Query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static part => part.Split('=', 2)[0])
            .Where(static key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static key => key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return keys.Length == 0
            ? path
            : $"{path}?<{string.Join(",", keys)}>";
    }

    private static string BuildUrlShape(Uri uri)
    {
        var port = uri.IsDefaultPort ? string.Empty : $":{uri.Port}";
        return $"{uri.Scheme.ToLowerInvariant()}://{uri.Host.ToLowerInvariant()}{port}{BuildPathShape(uri)}";
    }

    private static string DescribePort(int port)
    {
        return port switch
        {
            80 => "HTTP",
            443 => "HTTPS/WSS",
            1883 => "MQTT",
            8883 => "MQTT over TLS",
            8080 => "HTTP alternate",
            8443 => "HTTPS alternate",
            _ => "unknown"
        };
    }

    private static int CountOccurrences(string text, string keyword)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword))
            return 0;

        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(keyword, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            index += keyword.Length;
        }

        return count;
    }

    private static string MaskPath(string path)
    {
        try
        {
            return Path.GetFileName(path);
        }
        catch
        {
            return "<input>";
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        if (maxLength <= 0)
            return string.Empty;

        return value.Length <= maxLength
            ? value
            : value[..maxLength] + "...<truncated>";
    }

    private sealed record CaptureSummaryOptions(
        string RepositoryRoot,
        string CaptureInput,
        string OutputDirectory,
        int PreviewLength,
        bool MaskLocalPaths)
    {
        public static CaptureSummaryOptions Parse(string[] args)
        {
            var values = ReadArgs(args);

            var repoRoot = GetValue(values, "repo-root") ?? ResolveRepositoryRoot();
            repoRoot = Path.GetFullPath(repoRoot);

            var input = GetValue(values, "capture-input");
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("--capture-input is required for --summarize-capture.");

            input = Path.GetFullPath(input);

            var outputDir = GetValue(values, "output-dir");
            if (string.IsNullOrWhiteSpace(outputDir))
                outputDir = Path.Combine(repoRoot, "artifacts", "gree-alice", "channel-investigation");

            outputDir = Path.GetFullPath(outputDir);

            var previewLengthRaw = GetValue(values, "preview-length");
            var previewLength = string.IsNullOrWhiteSpace(previewLengthRaw)
                ? 4000
                : ParseInt(previewLengthRaw, 0, 20000, "preview-length");

            var maskLocalPaths = !values.ContainsKey("no-mask-local-paths");

            return new CaptureSummaryOptions(repoRoot, input, outputDir, previewLength, maskLocalPaths);
        }

        private static Dictionary<string, string?> ReadArgs(string[] args)
        {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];

                if (arg.Equals("--summarize-capture", StringComparison.OrdinalIgnoreCase))
                {
                    result["summarize-capture"] = null;
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

        private static string? GetValue(IReadOnlyDictionary<string, string?> values, string argumentName)
        {
            return values.TryGetValue(argumentName, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : null;
        }

        private static int ParseInt(string raw, int min, int max, string name)
        {
            if (!int.TryParse(raw, out var value))
                throw new ArgumentException($"{name} must be an integer.");

            if (value < min || value > max)
                throw new ArgumentOutOfRangeException(name, $"{name} must be between {min} and {max}.");

            return value;
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

    private static class CaptureSanitizer
    {
        private static readonly Regex EmailRegex = new(
            @"(?ix)\b[a-z0-9._%+\-]+@[a-z0-9.\-]+\.[a-z]{2,}\b",
            RegexOptions.Compiled);

        private static readonly Regex BearerRegex = new(
            @"(?ix)\bBearer\s+[a-z0-9._~+/=-]{10,}",
            RegexOptions.Compiled);

        private static readonly Regex KeyValueSecretRegex = new(
            @"(?ix)(?<name>\b(?:token|password|passwd|pwd|psw|secret|key|apikey|api_key|authorization|auth|session|sid|uid|userid|user)\b\s*[:=]\s*)[""']?(?<value>[^""'\s,&;}{\]]{3,})",
            RegexOptions.Compiled);

        private static readonly Regex QuerySecretRegex = new(
            @"(?ix)(?<prefix>[?&](?:token|password|passwd|pwd|psw|secret|key|apikey|api_key|authorization|auth|session|sid|uid|userid|user)=)(?<value>[^&#\s]+)",
            RegexOptions.Compiled);

        private static readonly Regex MacRegex = new(
            @"(?ix)\b(?:[0-9a-f]{2}[:-]){5}[0-9a-f]{2}\b",
            RegexOptions.Compiled);

        private static readonly Regex LongHexRegex = new(
            @"(?ix)\b[0-9a-f]{32,}\b",
            RegexOptions.Compiled);

        public static string Sanitize(string value)
        {
            var sanitized = value;
            sanitized = EmailRegex.Replace(sanitized, "<email>");
            sanitized = BearerRegex.Replace(sanitized, "Bearer <masked>");
            sanitized = KeyValueSecretRegex.Replace(sanitized, "${name}<masked>");
            sanitized = QuerySecretRegex.Replace(sanitized, "${prefix}<masked>");
            sanitized = MacRegex.Replace(sanitized, "<mac>");
            sanitized = LongHexRegex.Replace(sanitized, "<hex>");
            return sanitized;
        }
    }

    private sealed record CaptureSummaryReport(
        string Stage,
        string Mode,
        DateTimeOffset TimestampUtc,
        CaptureSummaryInput Input,
        CaptureSummary Summary,
        IReadOnlyList<HostObservation> Hosts,
        IReadOnlyList<PathObservation> PathObservations,
        IReadOnlyList<PortObservation> Ports,
        IReadOnlyList<ProtocolHint> ProtocolHints,
        IReadOnlyList<KeywordHit> KeywordHits,
        string SanitizedPreview,
        IReadOnlyList<string> Notes);

    private sealed record CaptureSummaryInput(
        string CaptureInput,
        int InputLength,
        int SanitizedPreviewLength,
        bool MaskLocalPaths);

    private sealed record CaptureSummary(
        int UrlCount,
        int HostCount,
        int PathShapeCount,
        int PortCount,
        int ProtocolHintCount,
        int KeywordHitCount);

    private sealed record UrlObservation(
        string Scheme,
        string Host,
        int? Port,
        string PathShape,
        string OriginalShape);

    private sealed record HostObservation(
        string Host,
        string Source,
        int Count);

    private sealed record PathObservation(
        string Scheme,
        string Host,
        string PathShape,
        int Count);

    private sealed record PortObservation(
        int Port,
        int Count,
        string Description);

    private sealed record ProtocolHint(
        string Hint,
        string Reason);

    private sealed record KeywordHit(
        string Keyword,
        int Count);
}
