using System.Security.Claims;
using AssistantEngineer.Api.Options.Security;
using AssistantEngineer.Api.Security.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace AssistantEngineer.Tests.Api;

public sealed class ApiAuthenticationBoundaryMiddlewareTests
{
    [Fact]
    public async Task AuthDisabled_AllowsRequestAndSetsAnonymousPrincipal()
    {
        var principalContext = new AuthenticatedPrincipalContext();
        var logger = new CapturingLogger<ApiAuthenticationBoundaryMiddleware>();
        var middleware = CreateMiddleware(
            logger,
            new ApiAuthenticationOptions { Enabled = false },
            environmentName: "Production",
            _ => Task.CompletedTask);

        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext, principalContext);

        Assert.False(principalContext.Principal.IsAuthenticated);
    }

    [Fact]
    public async Task AuthEnabled_AuthenticatedUserSetsPrincipal()
    {
        var principalContext = new AuthenticatedPrincipalContext();
        var logger = new CapturingLogger<ApiAuthenticationBoundaryMiddleware>();
        var middleware = CreateMiddleware(
            logger,
            new ApiAuthenticationOptions { Enabled = true, AllowAnonymousInDevelopment = false },
            environmentName: "Production",
            _ => Task.CompletedTask);

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "42"),
                new Claim("assistant_engineer_organization_id", "100"),
                new Claim("assistant_engineer_permission", "ProjectsRead"),
                new Claim("assistant_engineer_auth_method", "api_key")
            }, "ApiKey"))
        };

        await middleware.InvokeAsync(httpContext, principalContext);

        Assert.True(principalContext.Principal.IsAuthenticated);
        Assert.Equal(42, principalContext.Principal.UserId);
        Assert.Equal(100, principalContext.Principal.OrganizationId);
    }

    [Fact]
    public async Task DevelopmentAnonymousCompatibility_AllowsAnonymousPrincipalWhenConfigured()
    {
        var principalContext = new AuthenticatedPrincipalContext();
        var logger = new CapturingLogger<ApiAuthenticationBoundaryMiddleware>();
        var middleware = CreateMiddleware(
            logger,
            new ApiAuthenticationOptions { Enabled = true, AllowAnonymousInDevelopment = true },
            environmentName: "Development",
            _ => Task.CompletedTask);

        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext, principalContext);

        Assert.False(principalContext.Principal.IsAuthenticated);
    }

    [Fact]
    public async Task MiddlewareLogs_DoNotContainRawApiKey()
    {
        var principalContext = new AuthenticatedPrincipalContext();
        var logger = new CapturingLogger<ApiAuthenticationBoundaryMiddleware>();
        var middleware = CreateMiddleware(
            logger,
            new ApiAuthenticationOptions { Enabled = true, AllowAnonymousInDevelopment = true },
            environmentName: "Development",
            _ => Task.CompletedTask);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-AssistantEngineer-Api-Key"] = "super-secret-api-key";

        await middleware.InvokeAsync(httpContext, principalContext);

        Assert.DoesNotContain(
            logger.Messages,
            message => message.Contains("super-secret-api-key", StringComparison.Ordinal));
    }

    private static ApiAuthenticationBoundaryMiddleware CreateMiddleware(
        ILogger<ApiAuthenticationBoundaryMiddleware> logger,
        ApiAuthenticationOptions options,
        string environmentName,
        RequestDelegate next)
    {
        return new ApiAuthenticationBoundaryMiddleware(
            next,
            logger,
            new StaticOptionsMonitor<ApiAuthenticationOptions>(options),
            new FakeWebHostEnvironment(environmentName));
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public FakeWebHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
            ApplicationName = "AssistantEngineer.Api.Tests";
            ContentRootPath = AppContext.BaseDirectory;
            ContentRootFileProvider = new NullFileProvider();
            WebRootPath = AppContext.BaseDirectory;
            WebRootFileProvider = new NullFileProvider();
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; }
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }

    private sealed class NullFileProvider : IFileProvider
    {
        public IDirectoryContents GetDirectoryContents(string subpath) => NotFoundDirectoryContents.Singleton;
        public IFileInfo GetFileInfo(string subpath) => new NotFoundFileInfo(subpath);
        public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
    }

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T> where T : class
    {
        public StaticOptionsMonitor(T value)
        {
            CurrentValue = value;
        }

        public T CurrentValue { get; }
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
