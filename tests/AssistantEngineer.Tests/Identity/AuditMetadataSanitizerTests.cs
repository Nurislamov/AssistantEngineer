using AssistantEngineer.Modules.Identity.Application.Services.Audit;

namespace AssistantEngineer.Tests.Identity;

public sealed class AuditMetadataSanitizerTests
{
    private readonly AuditMetadataSanitizer _sanitizer = new();

    [Fact]
    public void Sanitize_RemovesForbiddenKeysCaseInsensitively()
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["apiKey"] = "secret-key",
            ["Authorization"] = "Bearer token",
            ["TOKEN"] = "abc",
            ["password"] = "p@ss",
            ["safe"] = "value"
        };

        var sanitized = _sanitizer.Sanitize(metadata, maxValueLength: 512);

        Assert.NotNull(sanitized);
        Assert.False(sanitized!.ContainsKey("apiKey"));
        Assert.False(sanitized.ContainsKey("Authorization"));
        Assert.False(sanitized.ContainsKey("TOKEN"));
        Assert.False(sanitized.ContainsKey("password"));
        Assert.Equal("value", sanitized["safe"]);
    }

    [Fact]
    public void Sanitize_TruncatesLongValues()
    {
        var longValue = new string('x', 900);
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["safe"] = longValue
        };

        var sanitized = _sanitizer.Sanitize(metadata, maxValueLength: 128);

        Assert.NotNull(sanitized);
        Assert.Equal(128, sanitized!["safe"].Length);
    }

    [Fact]
    public void Sanitize_PreservesSafeMetadata()
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["eventCode"] = "AUD-WF-001",
            ["resultStatus"] = "Succeeded"
        };

        var sanitized = _sanitizer.Sanitize(metadata, maxValueLength: 512);

        Assert.NotNull(sanitized);
        Assert.Equal(2, sanitized!.Count);
        Assert.Equal("AUD-WF-001", sanitized["eventCode"]);
        Assert.Equal("Succeeded", sanitized["resultStatus"]);
    }
}
