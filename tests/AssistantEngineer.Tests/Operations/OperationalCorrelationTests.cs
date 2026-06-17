using AssistantEngineer.Api.Services.OperationalDiagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Operations;

public sealed class OperationalCorrelationTests
{
    [Fact]
    public async Task MiddlewareMakesCorrelationAvailableAndLogsOnlySafeRequestMetadata()
    {
        const string correlationId = "operator-check-42";
        const string secret = "do-not-log-secret-value";
        const string messageBody = "do-not-log-telegram-message";
        var accessor = new OperationalCorrelationIdAccessor();
        var logger = new CapturingLogger<OperationalCorrelationIdMiddleware>();
        var observedCorrelationId = string.Empty;
        var middleware = new OperationalCorrelationIdMiddleware(
            context =>
            {
                observedCorrelationId = accessor.CorrelationId ?? string.Empty;
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            },
            logger,
            Options.Create(new OperationalCorrelationOptions()));
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v1/equipment-diagnostics/telegram/webhook";
        context.Request.QueryString = new QueryString($"?token={secret}");
        context.Request.Headers.Authorization = secret;
        context.Request.Headers["X-Telegram-Bot-Api-Secret-Token"] = secret;
        context.Request.Headers["X-Correlation-ID"] = correlationId;
        context.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(messageBody));

        await middleware.InvokeAsync(context, accessor);

        var captured = string.Join(Environment.NewLine, logger.Messages.Concat(logger.ScopeValues));
        Assert.Equal(correlationId, observedCorrelationId);
        Assert.Equal(correlationId, context.Response.Headers["X-Correlation-ID"]);
        Assert.Contains("/api/v1/equipment-diagnostics/telegram/webhook", captured, StringComparison.Ordinal);
        Assert.DoesNotContain("token=", captured, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(secret, captured, StringComparison.Ordinal);
        Assert.DoesNotContain(messageBody, captured, StringComparison.Ordinal);
        Assert.Null(accessor.CorrelationId);
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("field_check-2026.06")]
    public void PolicyAcceptsOnlyBoundedSafeCharacters(string value)
    {
        Assert.True(OperationalCorrelationIdPolicy.IsValid(value, 128));
        Assert.False(OperationalCorrelationIdPolicy.IsValid("contains space", 128));
        Assert.False(OperationalCorrelationIdPolicy.IsValid("contains/slash", 128));
        Assert.False(OperationalCorrelationIdPolicy.IsValid(new string('a', 129), 128));
    }

    [Fact]
    public void CorrelationAndLoggingSourceDoesNotReadBodiesOrSensitiveHeaders()
    {
        var source = File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot,
            "src", "Backend", "AssistantEngineer.Api", "Services", "OperationalDiagnostics",
            "OperationalCorrelationIdMiddleware.cs"));

        Assert.DoesNotContain("Request.QueryString", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Request.Body", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Response.Body", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Authorization", source, StringComparison.Ordinal);
        Assert.DoesNotContain("X-Telegram-Bot-Api-Secret-Token", source, StringComparison.Ordinal);
    }

    [Fact]
    public void TelegramOutboundDisablesDefaultHttpClientUriLogging()
    {
        var registration = File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot,
            "src", "Backend", "AssistantEngineer.Api", "Configuration",
            "ApplicationModulesRegistration.cs"));
        var outbound = File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot,
            "src", "Backend", "AssistantEngineer.Api", "Services", "EquipmentDiagnostics",
            "EquipmentDiagnosticTelegramOutboundClient.cs"));

        Assert.Contains(".RemoveAllLoggers()", registration, StringComparison.Ordinal);
        Assert.DoesNotContain("RequestUri", outbound, StringComparison.Ordinal);
        Assert.DoesNotContain("_options.BotToken", outbound[(outbound.IndexOf("LogInformation", StringComparison.Ordinal))..], StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionLoggingSuppressesEfAndNpgsqlSqlNoise()
    {
        var appsettings = File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot,
            "src", "Backend", "AssistantEngineer.Api",
            "appsettings.json"));

        Assert.Contains("\"Microsoft.EntityFrameworkCore\"", appsettings, StringComparison.Ordinal);
        Assert.Contains("\"Microsoft.EntityFrameworkCore.Database.Command\"", appsettings, StringComparison.Ordinal);
        Assert.Contains("\"Npgsql\"", appsettings, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Microsoft.EntityFrameworkCore.Database.Command\":  \"Information\"", appsettings, StringComparison.Ordinal);
    }

    [Fact]
    public void BackendDockerImageInstallsGssapiRuntimeDependency()
    {
        var dockerfile = File.ReadAllText(Path.Combine(
            TestPaths.RepoRoot,
            "deploy", "docker", "backend", "Dockerfile"));

        Assert.Contains("libgssapi-krb5-2", dockerfile, StringComparison.Ordinal);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public List<string> ScopeValues { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            if (state is IEnumerable<KeyValuePair<string, object>> values)
            {
                ScopeValues.AddRange(values.Select(value => $"{value.Key}={value.Value}"));
            }

            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();
            public void Dispose() { }
        }
    }
}
