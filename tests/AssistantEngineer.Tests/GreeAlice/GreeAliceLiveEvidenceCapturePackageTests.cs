using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceLiveEvidenceCapturePackageTests
{
    [Fact]
    public void EvidenceCaptureDocsExistAndStateReadOnlyBoundary()
    {
        string capture = ReadRepoFile("docs", "integrations", "gree-alice", "gree-plus-live-evidence-capture.md");
        string template = ReadRepoFile("docs", "integrations", "gree-alice", "gree-plus-live-evidence-template.md");
        string readme = ReadRepoFile("docs", "integrations", "gree-alice", "README.md");

        Assert.Contains("adb logcat", capture, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not use mitmproxy", capture, StringComparison.Ordinal);
        Assert.Contains("Do not tap power", capture, StringComparison.Ordinal);
        Assert.Contains("redact-gree-plus-live-evidence.ps1", capture, StringComparison.Ordinal);
        Assert.Contains("extract-gree-plus-live-evidence.ps1", capture, StringComparison.Ordinal);
        Assert.Contains("extract-gree-plus-focused-live-evidence.ps1", capture, StringComparison.Ordinal);
        Assert.Contains("sendDataToDevice observed: no/yes", template, StringComparison.Ordinal);
        Assert.Contains("Contract status: unknown/partial/confirmed-read-only", template, StringComparison.Ordinal);
        Assert.Contains("Gree Plus live evidence capture package: exists", readme, StringComparison.Ordinal);
        Assert.Contains("Gree Plus live evidence raw artifacts in repository: forbidden", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void EvidenceRedactionHelperRedactsSensitiveValuesAndKeepsContractShape()
    {
        string script = Path.Combine(FindRepositoryRoot(), "scripts", "integrations", "gree-alice", "redact-gree-plus-live-evidence.ps1");
        string sample = string.Join(
            "; ",
            [
                "getInfo",
                "endpoint=/App/GetHomes",
                "method=POST",
                "Pow=1",
                "Mod=1",
                "SetTem=25",
                "AllErr=0",
                "operator@example.test",
                "uid=raw-user-1",
                "homeId=raw-home-1",
                "deviceId=raw-device-1",
                "mac=AA:BB:CC:DD:EE:FF",
                "access_token=raw-access-1",
                "refresh_token=raw-refresh-1",
                "Authorization: Bearer raw-auth-1",
                "Cookie: raw-cookie-1",
                "phone=+1 202 555 0101",
                "accountName=raw-account-1",
                "ip=192.168.1.12",
                "\\\"user_id\\\":\\\"escaped-user-1\\\"",
                "\\\"homeId\\\":\\\"escaped-home-1\\\"",
                "\\\"deviceId\\\":\\\"escaped-device-1\\\"",
                "\\\"mac\\\":\\\"AA-BB-CC-DD-EE-11\\\"",
                "path=/App/GetMsg?deviceid=raw-device-query-1&user_id=raw-user-query-1",
                "task token=raw-window-token-1",
                "pName=user_id pValue=raw-analytics-user-1",
                "pName=Appliance_name pValue=raw-appliance-name-1"
            ]);

        string redacted = RunPowerShell(script, sample);

        Assert.Contains("getInfo", redacted, StringComparison.Ordinal);
        Assert.Contains("endpoint=/App/GetHomes", redacted, StringComparison.Ordinal);
        Assert.Contains("method=POST", redacted, StringComparison.Ordinal);
        Assert.Contains("Pow=1", redacted, StringComparison.Ordinal);
        Assert.Contains("SetTem=25", redacted, StringComparison.Ordinal);
        Assert.Contains("AllErr=0", redacted, StringComparison.Ordinal);
        Assert.Contains("<EMAIL>", redacted, StringComparison.Ordinal);
        Assert.Contains("<UID>", redacted, StringComparison.Ordinal);
        Assert.Contains("<HOME_ID>", redacted, StringComparison.Ordinal);
        Assert.Contains("<DEVICE_ID>", redacted, StringComparison.Ordinal);
        Assert.Contains("<DEVICE_MAC>", redacted, StringComparison.Ordinal);
        Assert.Contains("<ACCESS_TOKEN>", redacted, StringComparison.Ordinal);
        Assert.Contains("<REFRESH_TOKEN>", redacted, StringComparison.Ordinal);
        Assert.Contains("<AUTHORIZATION>", redacted, StringComparison.Ordinal);
        Assert.Contains("<SESSION>", redacted, StringComparison.Ordinal);
        Assert.Contains("<PHONE>", redacted, StringComparison.Ordinal);
        Assert.Contains("<ACCOUNT>", redacted, StringComparison.Ordinal);
        Assert.Contains("<LOCAL_IP>", redacted, StringComparison.Ordinal);
        Assert.Contains("deviceid=<DEVICE_ID>", redacted, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<WINDOW_TOKEN>", redacted, StringComparison.Ordinal);
        Assert.Contains("pName=user_id pValue=<UID>", redacted, StringComparison.Ordinal);
        Assert.Contains("pName=Appliance_name pValue=<DEVICE_ALIAS_OR_MAC>", redacted, StringComparison.Ordinal);

        string[] forbidden =
        [
            "operator",
            "raw-user-1",
            "raw-home-1",
            "raw-device-1",
            "AA:BB:CC:DD:EE:FF",
            "raw-access-1",
            "raw-refresh-1",
            "raw-auth-1",
            "raw-cookie-1",
            "raw-account-1",
            "192.168.1.12",
            "escaped-user-1",
            "escaped-home-1",
            "escaped-device-1",
            "AA-BB-CC-DD-EE-11",
            "raw-device-query-1",
            "raw-user-query-1",
            "raw-window-token-1",
            "raw-analytics-user-1",
            "raw-appliance-name-1"
        ];

        foreach (string value in forbidden)
        {
            Assert.DoesNotContain(value, redacted, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void EvidenceRedactionHelperHasNoDefaultInputOrNetworkBehavior()
    {
        string scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "integrations", "gree-alice", "redact-gree-plus-live-evidence.ps1");
        string script = File.ReadAllText(scriptPath);

        Assert.Contains("This helper has no default input path", script, StringComparison.Ordinal);
        Assert.Contains("Get-Content -LiteralPath $InputPath -Raw", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Invoke-WebRequest", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Invoke-RestMethod", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Start-Process", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mqtt", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sendDataToDevice", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/GreeAccess/access/action", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EvidenceExtractorSplitsStatusRiskNegativeProofGapsAndLeakCheck()
    {
        string root = FindRepositoryRoot();
        string script = Path.Combine(root, "scripts", "integrations", "gree-alice", "extract-gree-plus-live-evidence.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), "gree-alice-evidence-tests", Guid.NewGuid().ToString("N"));
        string inputPath = Path.Combine(tempRoot, "redacted-input.txt");
        string outputPath = Path.Combine(tempRoot, "extract");
        Directory.CreateDirectory(tempRoot);

        try
        {
            File.WriteAllText(
                inputPath,
                string.Join(
                    Environment.NewLine,
                    [
                        "cordova.callbackFromNative fullstatueJson {\\\"t\\\":\\\"status\\\",\\\"Pow\\\":1,\\\"Mod\\\":1,\\\"SetTem\\\":25,\\\"AllErr\\\":0,\\\"deviceState\\\":4,\\\"status\\\":true}",
                        "analytics click funName=sendDataToDevice pName=user_id pValue=<UID>",
                        "BridgeWebView PluginInterface start funName=sendDataToDevice without command payload",
                        "region apiHost=hkgrih.gree.com host=hk.dis.gree.com"
                    ]));

            RunExtractor(script, inputPath, outputPath);

            string status = File.ReadAllText(Path.Combine(outputPath, "status-evidence.txt"));
            string risk = File.ReadAllText(Path.Combine(outputPath, "control-risk-evidence.txt"));
            string negative = File.ReadAllText(Path.Combine(outputPath, "negative-control-proof.txt"));
            string gaps = File.ReadAllText(Path.Combine(outputPath, "contract-gaps.txt"));
            string leak = File.ReadAllText(Path.Combine(outputPath, "leak-check.txt"));
            string summary = File.ReadAllText(Path.Combine(outputPath, "summary.md"));

            Assert.Contains("fullstatueJson", status, StringComparison.Ordinal);
            Assert.Contains("\\\"t\\\":\\\"status\\\"", status, StringComparison.Ordinal);
            Assert.Contains("Pow", status, StringComparison.Ordinal);
            Assert.Contains("SetTem", status, StringComparison.Ordinal);
            Assert.Contains("sendDataToDevice", risk, StringComparison.Ordinal);
            Assert.Contains("No strong command/control markers found", negative, StringComparison.Ordinal);
            Assert.Contains("risk candidate, not command proof", summary, StringComparison.Ordinal);
            Assert.Contains("direct HTTP live read contract", gaps, StringComparison.Ordinal);
            Assert.Contains("No obvious unredacted", leak, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void EvidenceExtractorReportsStrongCommandMarkerAsControlRisk()
    {
        string root = FindRepositoryRoot();
        string script = Path.Combine(root, "scripts", "integrations", "gree-alice", "extract-gree-plus-live-evidence.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), "gree-alice-evidence-tests", Guid.NewGuid().ToString("N"));
        string inputPath = Path.Combine(tempRoot, "redacted-input.txt");
        string outputPath = Path.Combine(tempRoot, "extract");
        Directory.CreateDirectory(tempRoot);

        try
        {
            File.WriteAllText(inputPath, "transport payload {\\\"t\\\":\\\"cmd\\\",\\\"opt\\\":[\\\"Pow\\\"],\\\"p\\\":[1]}");

            RunExtractor(script, inputPath, outputPath);

            string negative = File.ReadAllText(Path.Combine(outputPath, "negative-control-proof.txt"));
            string summary = File.ReadAllText(Path.Combine(outputPath, "summary.md"));

            Assert.Contains("Strong command/control markers require manual review", negative, StringComparison.Ordinal);
            Assert.Contains("\\\"t\\\":\\\"cmd\\\"", negative, StringComparison.Ordinal);
            Assert.Contains("Strong command/control marker lines: 1", summary, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void EvidenceExtractorHasNoDefaultInputOrNetworkBehavior()
    {
        string scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "integrations", "gree-alice", "extract-gree-plus-live-evidence.ps1");
        string script = File.ReadAllText(scriptPath);

        Assert.Contains("has no default paths", script, StringComparison.Ordinal);
        Assert.Contains("Get-Content -LiteralPath $inputFullPath -Raw", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Invoke-WebRequest", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Invoke-RestMethod", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Start-Process", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FocusedEvidenceExtractorIncludesGreeStatusAndRejectsAndroidNoise()
    {
        string root = FindRepositoryRoot();
        string script = Path.Combine(root, "scripts", "integrations", "gree-alice", "extract-gree-plus-focused-live-evidence.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), "gree-alice-focused-evidence-tests", Guid.NewGuid().ToString("N"));
        string inputPath = Path.Combine(tempRoot, "redacted-input.txt");
        string outputPath = Path.Combine(tempRoot, "extract");
        Directory.CreateDirectory(tempRoot);

        try
        {
            File.WriteAllText(
                inputPath,
                string.Join(
                    Environment.NewLine,
                    [
                        "I/com.gree.greeplus PluginInterface start funName=getInfo",
                        "D/GREE+ getEnvApiAddress ApiAddress=https://hkgrih.gree.com",
                        "D/GREE+ GreeAccess /GreeAccess/access/action actionBytes=<REDACTED_SHAPE>",
                        "I/chromium cordova.callbackFromNative fullstatueJson {\\\"t\\\":\\\"status\\\",\\\"Pow\\\":1,\\\"Mod\\\":1,\\\"SetTem\\\":25,\\\"AllErr\\\":0,\\\"deviceState\\\":4,\\\"status\\\":true,\\\"host\\\":\\\"hk.dis.gree.com\\\"}",
                        "I/PluginInterface funName=sendDataToDevice analytics click trace",
                        "W/PowerManager SourcePower TvStatus POWER_ON",
                        "I/StatusBar WindowManager generic notification",
                        "W/ServiceManager No service published for: wifirtt",
                        "D/Samsung oneconnect TV BLE ScanController line"
                    ]));

            RunExtractor(script, inputPath, outputPath);

            string status = File.ReadAllText(Path.Combine(outputPath, "focused-status-evidence.txt"));
            string api = File.ReadAllText(Path.Combine(outputPath, "focused-api-evidence.txt"));
            string risk = File.ReadAllText(Path.Combine(outputPath, "focused-control-risk-evidence.txt"));
            string negative = File.ReadAllText(Path.Combine(outputPath, "focused-negative-control-proof.txt"));
            string rejected = File.ReadAllText(Path.Combine(outputPath, "focused-noise-rejected.txt"));
            string summary = File.ReadAllText(Path.Combine(outputPath, "focused-summary.md"));

            Assert.Contains("fullstatueJson", status, StringComparison.Ordinal);
            Assert.Contains("\\\"t\\\":\\\"status\\\"", status, StringComparison.Ordinal);
            Assert.Contains("Pow", status, StringComparison.Ordinal);
            Assert.Contains("SetTem", status, StringComparison.Ordinal);
            Assert.Contains("ApiAddress", api, StringComparison.Ordinal);
            Assert.Contains("GreeAccess", api, StringComparison.Ordinal);
            Assert.Contains("actionBytes", api, StringComparison.Ordinal);
            Assert.Contains("sendDataToDevice", risk, StringComparison.Ordinal);
            Assert.Contains("No strong command/control markers found", negative, StringComparison.Ordinal);
            Assert.Contains("sendDataToDevice appeared as a focused risk candidate", negative, StringComparison.Ordinal);
            Assert.Contains("PowerManager", rejected, StringComparison.Ordinal);
            Assert.Contains("wifirtt", rejected, StringComparison.Ordinal);
            Assert.Contains("oneconnect", rejected, StringComparison.Ordinal);
            Assert.DoesNotContain("PowerManager", status, StringComparison.Ordinal);
            Assert.DoesNotContain("SourcePower", risk, StringComparison.Ordinal);
            Assert.Contains("Rejected noise lines: 4", summary, StringComparison.Ordinal);
            Assert.Contains("Strong command/control marker lines: 0", summary, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void FocusedEvidenceExtractorReportsExactCommandMarkersOnlyAsStrongControl()
    {
        string root = FindRepositoryRoot();
        string script = Path.Combine(root, "scripts", "integrations", "gree-alice", "extract-gree-plus-focused-live-evidence.ps1");
        string tempRoot = Path.Combine(Path.GetTempPath(), "gree-alice-focused-evidence-tests", Guid.NewGuid().ToString("N"));
        string inputPath = Path.Combine(tempRoot, "redacted-input.txt");
        string outputPath = Path.Combine(tempRoot, "extract");
        Directory.CreateDirectory(tempRoot);

        try
        {
            File.WriteAllText(
                inputPath,
                string.Join(
                    Environment.NewLine,
                    [
                        "I/com.gree.greeplus transport payload {\"t\":\"cmd\",\"opt\":[\"Pow\"],\"p\":[1]}",
                        "I/com.gree.greeplus transport payload {\\\"t\\\":\\\"cmd\\\",\\\"opt\\\":[\\\"SetTem\\\"],\\\"p\\\":[25]}",
                        "D/GREE+ dev_control control_order control_Agtype",
                        "I/MQTT Gree topic publish payload marker",
                        "W/ServiceManager No service published for: wifirtt",
                        "W/TvStatus POWER_ON SourcePower"
                    ]));

            RunExtractor(script, inputPath, outputPath);

            string negative = File.ReadAllText(Path.Combine(outputPath, "focused-negative-control-proof.txt"));
            string rejected = File.ReadAllText(Path.Combine(outputPath, "focused-noise-rejected.txt"));
            string summary = File.ReadAllText(Path.Combine(outputPath, "focused-summary.md"));

            Assert.Contains("Strong command/control markers require manual review", negative, StringComparison.Ordinal);
            Assert.Contains("\"t\":\"cmd\"", negative, StringComparison.Ordinal);
            Assert.Contains("\\\"t\\\":\\\"cmd\\\"", negative, StringComparison.Ordinal);
            Assert.Contains("control_order", negative, StringComparison.Ordinal);
            Assert.Contains("Gree topic publish", negative, StringComparison.Ordinal);
            Assert.Contains("wifirtt", rejected, StringComparison.Ordinal);
            Assert.DoesNotContain("No service published for: wifirtt", negative, StringComparison.Ordinal);
            Assert.DoesNotContain("TvStatus POWER_ON", negative, StringComparison.Ordinal);
            Assert.Contains("Strong command/control marker lines: 4", summary, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void FocusedEvidenceExtractorHasNoDefaultInputOrNetworkBehavior()
    {
        string scriptPath = Path.Combine(FindRepositoryRoot(), "scripts", "integrations", "gree-alice", "extract-gree-plus-focused-live-evidence.ps1");
        string script = File.ReadAllText(scriptPath);

        Assert.True(File.Exists(scriptPath));
        Assert.Contains("has no default paths", script, StringComparison.Ordinal);
        Assert.Contains("Get-Content -LiteralPath $inputFullPath -Raw", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Invoke-WebRequest", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Invoke-RestMethod", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Start-Process", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectAsync", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SubscribeAsync", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PublishAsync", script, StringComparison.OrdinalIgnoreCase);
    }

    private static string RunPowerShell(string scriptPath, string text)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "powershell",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add("-Text");
        startInfo.ArgumentList.Add(text);

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start PowerShell.");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(process.ExitCode == 0, error);

        return output;
    }

    private static void RunExtractor(string scriptPath, string inputPath, string outputPath)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "powershell",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add("-InputPath");
        startInfo.ArgumentList.Add(inputPath);
        startInfo.ArgumentList.Add("-OutputDirectory");
        startInfo.ArgumentList.Add(outputPath);

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start PowerShell.");
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(process.ExitCode == 0, error);
    }

    private static string ReadRepoFile(params string[] relativeParts)
    {
        string root = FindRepositoryRoot();
        string path = Path.Combine(new[] { root }.Concat(relativeParts).ToArray());

        Assert.True(File.Exists(path), "Expected repository file to exist: " + path);

        return File.ReadAllText(path);
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
