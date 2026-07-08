using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssistantEngineer.Tools.GreeCloudProbe;

internal static class MqttChannelProbeCommand
{
    private const string StageName = "GREE-ALICE-08";
    private const string ModeName = "mqtt-channel-handshake-probe";
    private const string DefaultHost = "mqtt-hk.gree.com";
    private const int DefaultPort = 1994;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task<int> RunAsync(string[] args)
    {
        var options = MqttChannelOptions.Parse(args);
        Directory.CreateDirectory(options.OutputDirectory);

        var timestampUtc = DateTimeOffset.UtcNow;
        var report = options.ConfigurationOnly
            ? MqttChannelProbeReport.ConfigurationOnly(options, timestampUtc)
            : await RunProbeAsync(options, timestampUtc);

        var outputPath = Path.Combine(
            options.OutputDirectory,
            $"gree-mqtt-channel-probe-{timestampUtc:yyyyMMdd-HHmmss}.json");

        File.WriteAllText(outputPath, JsonSerializer.Serialize(report, JsonOptions));
        PrintSummary(options, report, outputPath);

        return report.Errors.Count == 0 ? 0 : 2;
    }

    private static async Task<MqttChannelProbeReport> RunProbeAsync(
        MqttChannelOptions options,
        DateTimeOffset timestampUtc)
    {
        var errors = new List<MqttChannelError>();
        var dnsResult = await ProbeDnsAsync(options, errors);
        var tcpResult = MqttTcpProbeResult.NotAttempted;
        var tlsResult = MqttTlsProbeResult.NotAttempted;

        if (dnsResult.Resolved)
            tcpResult = await ProbeTcpAsync(options, errors);

        if (tcpResult.Connected)
            tlsResult = await ProbeTlsAsync(options, errors);

        var summary = new MqttChannelSummary(
            DnsResolved: dnsResult.Resolved,
            TcpConnected: tcpResult.Connected,
            TlsAuthenticated: tlsResult.Authenticated,
            MqttApplicationDataSent: false,
            MqttPublishSent: false,
            ControlCommandSent: false);

        return new MqttChannelProbeReport(
            StageName,
            ModeName,
            timestampUtc,
            MqttChannelInputs.FromOptions(options),
            summary,
            dnsResult,
            tcpResult,
            tlsResult,
            errors,
            new[]
            {
                "This probe validates only DNS, TCP, and TLS/SNI reachability.",
                "It does not send MQTT CONNECT, SUBSCRIBE, PUBLISH, or any device control command.",
                "It does not use or store Gree+ credentials.",
                "Reports are local artifacts under artifacts/gree-alice/ and should not be committed."
            });
    }

    private static async Task<MqttDnsProbeResult> ProbeDnsAsync(
        MqttChannelOptions options,
        List<MqttChannelError> errors)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(options.Host);
            stopwatch.Stop();

            return new MqttDnsProbeResult(
                Resolved: addresses.Length > 0,
                Host: options.Host,
                AddressCount: addresses.Length,
                Addresses: addresses.Select(static address => address.ToString()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                DurationMs: stopwatch.ElapsedMilliseconds,
                Error: null);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            errors.Add(new MqttChannelError("MQTT_DNS_FAILED", exception.Message));

            return new MqttDnsProbeResult(
                Resolved: false,
                Host: options.Host,
                AddressCount: 0,
                Addresses: Array.Empty<string>(),
                DurationMs: stopwatch.ElapsedMilliseconds,
                Error: exception.GetType().Name);
        }
    }

    private static async Task<MqttTcpProbeResult> ProbeTcpAsync(
        MqttChannelOptions options,
        List<MqttChannelError> errors)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(options.Host, options.Port)
                .WaitAsync(TimeSpan.FromSeconds(options.TimeoutSeconds));

            stopwatch.Stop();

            return new MqttTcpProbeResult(
                Connected: true,
                Host: options.Host,
                Port: options.Port,
                DurationMs: stopwatch.ElapsedMilliseconds,
                Error: null);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            errors.Add(new MqttChannelError("MQTT_TCP_FAILED", exception.Message));

            return new MqttTcpProbeResult(
                Connected: false,
                Host: options.Host,
                Port: options.Port,
                DurationMs: stopwatch.ElapsedMilliseconds,
                Error: exception.GetType().Name);
        }
    }

    private static async Task<MqttTlsProbeResult> ProbeTlsAsync(
        MqttChannelOptions options,
        List<MqttChannelError> errors)
    {
        var stopwatch = Stopwatch.StartNew();
        var policyErrorsText = "NotEvaluated";
        var certificateSubject = default(string);
        var certificateIssuer = default(string);
        var certificateNotBefore = default(string);
        var certificateNotAfter = default(string);
        var certificateThumbprint = default(string);

        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(options.Host, options.Port)
                .WaitAsync(TimeSpan.FromSeconds(options.TimeoutSeconds));

            await using var networkStream = tcpClient.GetStream();
            using var sslStream = new SslStream(
                networkStream,
                leaveInnerStreamOpen: false,
                userCertificateValidationCallback: (_, certificate, _, sslPolicyErrors) =>
                {
                    policyErrorsText = sslPolicyErrors.ToString();

                    if (certificate is not null)
                    {
                        certificateSubject = certificate.Subject;
                        certificateIssuer = certificate.Issuer;
                        certificateNotBefore = certificate.GetEffectiveDateString();
                        certificateNotAfter = certificate.GetExpirationDateString();
                        certificateThumbprint = certificate.GetCertHashString();
                    }

                    return sslPolicyErrors == SslPolicyErrors.None;
                });

            var sslOptions = new SslClientAuthenticationOptions
            {
                TargetHost = options.Host,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck
            };

            await sslStream.AuthenticateAsClientAsync(sslOptions)
                .WaitAsync(TimeSpan.FromSeconds(options.TimeoutSeconds));

            stopwatch.Stop();

            return new MqttTlsProbeResult(
                Attempted: true,
                Authenticated: sslStream.IsAuthenticated,
                SniHost: options.Host,
                SslProtocol: sslStream.SslProtocol.ToString(),
                PolicyErrors: policyErrorsText,
                CertificateSubject: certificateSubject,
                CertificateIssuer: certificateIssuer,
                CertificateNotBefore: certificateNotBefore,
                CertificateNotAfter: certificateNotAfter,
                CertificateThumbprint: certificateThumbprint,
                DurationMs: stopwatch.ElapsedMilliseconds,
                Error: null);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            errors.Add(new MqttChannelError("MQTT_TLS_FAILED", exception.Message));

            return new MqttTlsProbeResult(
                Attempted: true,
                Authenticated: false,
                SniHost: options.Host,
                SslProtocol: null,
                PolicyErrors: policyErrorsText,
                CertificateSubject: certificateSubject,
                CertificateIssuer: certificateIssuer,
                CertificateNotBefore: certificateNotBefore,
                CertificateNotAfter: certificateNotAfter,
                CertificateThumbprint: certificateThumbprint,
                DurationMs: stopwatch.ElapsedMilliseconds,
                Error: exception.GetType().Name);
        }
    }

    private static void PrintSummary(
        MqttChannelOptions options,
        MqttChannelProbeReport report,
        string outputPath)
    {
        Console.WriteLine("AssistantEngineer Gree MQTT channel probe");
        Console.WriteLine($"Stage: {StageName}");
        Console.WriteLine($"Mode: {report.Mode}");
        Console.WriteLine($"Host: {options.Host}");
        Console.WriteLine($"Port: {options.Port}");
        Console.WriteLine($"Timeout seconds: {options.TimeoutSeconds}");
        Console.WriteLine($"Output: {outputPath}");
        Console.WriteLine();

        Console.WriteLine("Safety:");
        Console.WriteLine($"  MQTT application data sent: {ToYesNo(report.Summary.MqttApplicationDataSent)}");
        Console.WriteLine($"  MQTT publish sent: {ToYesNo(report.Summary.MqttPublishSent)}");
        Console.WriteLine($"  Control command sent: {ToYesNo(report.Summary.ControlCommandSent)}");
        Console.WriteLine();

        Console.WriteLine("Channel:");
        Console.WriteLine($"  DNS resolved: {ToYesNo(report.Summary.DnsResolved)}");
        Console.WriteLine($"  TCP connected: {ToYesNo(report.Summary.TcpConnected)}");
        Console.WriteLine($"  TLS authenticated: {ToYesNo(report.Summary.TlsAuthenticated)}");

        if (report.Dns.Resolved)
            Console.WriteLine($"  Resolved addresses: {string.Join(", ", report.Dns.Addresses)}");

        if (report.Tls.Attempted)
        {
            Console.WriteLine($"  TLS protocol: {DisplayValue(report.Tls.SslProtocol)}");
            Console.WriteLine($"  TLS policy errors: {DisplayValue(report.Tls.PolicyErrors)}");
            Console.WriteLine($"  Certificate subject: {DisplayValue(report.Tls.CertificateSubject)}");
            Console.WriteLine($"  Certificate issuer: {DisplayValue(report.Tls.CertificateIssuer)}");
            Console.WriteLine($"  Certificate not after: {DisplayValue(report.Tls.CertificateNotAfter)}");
        }

        if (report.Errors.Count > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Errors:");
            Console.ResetColor();

            foreach (var error in report.Errors)
                Console.WriteLine($"  - {error.Code}: {error.Message}");
        }

        Console.WriteLine();

        if (report.Mode == "configuration-only")
            Console.WriteLine("Next step: run with --probe-mqtt-channel without --configuration-only to test DNS/TCP/TLS.");
        else if (report.Summary.TlsAuthenticated)
            Console.WriteLine("Next step: keep MQTT read-only and investigate authentication/topic model before any subscribe/publish work.");
        else
            Console.WriteLine("Next step: inspect the local JSON report and network access before attempting MQTT protocol work.");
    }

    private static string DisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "<not set>" : value;

    private static string ToYesNo(bool value) => value ? "yes" : "no";

    private sealed record MqttChannelOptions(
        string RepositoryRoot,
        string Host,
        int Port,
        string OutputDirectory,
        int TimeoutSeconds,
        bool ConfigurationOnly)
    {
        public static MqttChannelOptions Parse(string[] args)
        {
            var values = ReadArgs(args);

            var repoRoot = GetValue(values, "repo-root", null) ?? ResolveRepositoryRoot();
            repoRoot = Path.GetFullPath(repoRoot);

            var host = GetValue(values, "mqtt-host", "GREE_ALICE_MQTT_HOST") ?? DefaultHost;
            var portRaw = GetValue(values, "mqtt-port", "GREE_ALICE_MQTT_PORT");
            var port = ParseInt(portRaw, DefaultPort, 1, 65535, "mqtt-port");

            var outputDir = GetValue(values, "output-dir", "GREE_ALICE_OUTPUT_DIR");
            if (string.IsNullOrWhiteSpace(outputDir))
                outputDir = Path.Combine(repoRoot, "artifacts", "gree-alice", "mqtt-channel");

            var timeoutRaw = GetValue(values, "timeout-seconds", "GREE_ALICE_TIMEOUT_SECONDS");
            var timeoutSeconds = ParseInt(timeoutRaw, 15, 1, 300, "timeout-seconds");

            var configurationOnly = values.ContainsKey("configuration-only");

            return new MqttChannelOptions(
                repoRoot,
                host.Trim(),
                port,
                Path.GetFullPath(outputDir),
                timeoutSeconds,
                configurationOnly);
        }

        private static Dictionary<string, string?> ReadArgs(string[] args)
        {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];

                if (arg.Equals("--probe-mqtt-channel", StringComparison.OrdinalIgnoreCase))
                {
                    result["probe-mqtt-channel"] = null;
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

        private static int ParseInt(string? raw, int defaultValue, int min, int max, string name)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return defaultValue;

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

    private sealed record MqttChannelProbeReport(
        string Stage,
        string Mode,
        DateTimeOffset TimestampUtc,
        MqttChannelInputs Inputs,
        MqttChannelSummary Summary,
        MqttDnsProbeResult Dns,
        MqttTcpProbeResult Tcp,
        MqttTlsProbeResult Tls,
        IReadOnlyList<MqttChannelError> Errors,
        IReadOnlyList<string> Notes)
    {
        public static MqttChannelProbeReport ConfigurationOnly(
            MqttChannelOptions options,
            DateTimeOffset timestampUtc)
        {
            return new MqttChannelProbeReport(
                StageName,
                "configuration-only",
                timestampUtc,
                MqttChannelInputs.FromOptions(options),
                new MqttChannelSummary(
                    DnsResolved: false,
                    TcpConnected: false,
                    TlsAuthenticated: false,
                    MqttApplicationDataSent: false,
                    MqttPublishSent: false,
                    ControlCommandSent: false),
                new MqttDnsProbeResult(
                    Resolved: false,
                    Host: options.Host,
                    AddressCount: 0,
                    Addresses: Array.Empty<string>(),
                    DurationMs: 0,
                    Error: null),
                MqttTcpProbeResult.NotAttempted,
                MqttTlsProbeResult.NotAttempted,
                Array.Empty<MqttChannelError>(),
                new[]
                {
                    "MQTT channel probe was not executed.",
                    "Run without --configuration-only to test only DNS/TCP/TLS reachability."
                });
        }
    }

    private sealed record MqttChannelInputs(
        string Host,
        int Port,
        int TimeoutSeconds)
    {
        public static MqttChannelInputs FromOptions(MqttChannelOptions options) =>
            new(options.Host, options.Port, options.TimeoutSeconds);
    }

    private sealed record MqttChannelSummary(
        bool DnsResolved,
        bool TcpConnected,
        bool TlsAuthenticated,
        bool MqttApplicationDataSent,
        bool MqttPublishSent,
        bool ControlCommandSent);

    private sealed record MqttDnsProbeResult(
        bool Resolved,
        string Host,
        int AddressCount,
        IReadOnlyList<string> Addresses,
        long DurationMs,
        string? Error);

    private sealed record MqttTcpProbeResult(
        bool Connected,
        string? Host,
        int? Port,
        long DurationMs,
        string? Error)
    {
        public static MqttTcpProbeResult NotAttempted { get; } =
            new(Connected: false, Host: null, Port: null, DurationMs: 0, Error: "not-attempted");
    }

    private sealed record MqttTlsProbeResult(
        bool Attempted,
        bool Authenticated,
        string? SniHost,
        string? SslProtocol,
        string? PolicyErrors,
        string? CertificateSubject,
        string? CertificateIssuer,
        string? CertificateNotBefore,
        string? CertificateNotAfter,
        string? CertificateThumbprint,
        long DurationMs,
        string? Error)
    {
        public static MqttTlsProbeResult NotAttempted { get; } =
            new(
                Attempted: false,
                Authenticated: false,
                SniHost: null,
                SslProtocol: null,
                PolicyErrors: null,
                CertificateSubject: null,
                CertificateIssuer: null,
                CertificateNotBefore: null,
                CertificateNotAfter: null,
                CertificateThumbprint: null,
                DurationMs: 0,
                Error: "not-attempted");
    }

    private sealed record MqttChannelError(
        string Code,
        string Message);
}
