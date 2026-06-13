namespace AssistantEngineer.Api.Services.OperationalDiagnostics;

public static class OperationalCorrelationIdPolicy
{
    public static string GetSafeRequestPath(HttpRequest request) =>
        request.Path.Value ?? "/";

    public static bool IsValid(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength)
        {
            return false;
        }

        return value.All(character =>
            char.IsAsciiLetterOrDigit(character) ||
            character is '_' or '-' or '.');
    }

    public static string Create() => Guid.NewGuid().ToString("N");
}
