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
                "ip=192.168.1.12"
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
            "192.168.1.12"
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
