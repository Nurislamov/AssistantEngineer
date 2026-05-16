using System.Collections.ObjectModel;

namespace AssistantEngineer.Modules.Identity.Application.Services.Audit;

public sealed class AuditMetadataSanitizer
{
    private static readonly HashSet<string> ForbiddenKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "apikey",
        "x-api-key",
        "token",
        "access_token",
        "refresh_token",
        "password",
        "secret",
        "authorization",
        "cookie",
        "set-cookie"
    };

    public IReadOnlyDictionary<string, string>? Sanitize(
        IReadOnlyDictionary<string, string>? metadata,
        int maxValueLength)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return null;
        }

        var effectiveLimit = maxValueLength > 0 ? maxValueLength : 512;
        var sanitized = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in metadata)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                continue;
            }

            var normalizedKey = pair.Key.Trim();
            if (ForbiddenKeys.Contains(normalizedKey))
            {
                continue;
            }

            var value = pair.Value ?? string.Empty;
            if (value.Length > effectiveLimit)
            {
                value = value[..effectiveLimit];
            }

            sanitized[normalizedKey] = value;
        }

        if (sanitized.Count == 0)
        {
            return null;
        }

        return new ReadOnlyDictionary<string, string>(sanitized);
    }
}
