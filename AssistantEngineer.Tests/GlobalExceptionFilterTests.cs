using AssistantEngineer.Api.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Tests;

public class GlobalExceptionFilterTests
{
    [Fact]
    public void OnExceptionDoesNotExposeExceptionMessageInDevelopment()
    {
        var filter = new GlobalExceptionFilter(
            NullLogger<GlobalExceptionFilter>.Instance,
            new TestHostEnvironment(Environments.Development));
        var context = CreateExceptionContext(new InvalidOperationException("Password=secret"));

        filter.OnException(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.DoesNotContain("secret", problem.Detail);
        Assert.Equal("InvalidOperationException", problem.Extensions["exceptionType"]);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
        Assert.True(context.ExceptionHandled);
    }

    [Fact]
    public void OnExceptionDoesNotExposeExceptionTypeOutsideDevelopment()
    {
        var filter = new GlobalExceptionFilter(
            NullLogger<GlobalExceptionFilter>.Instance,
            new TestHostEnvironment(Environments.Production));
        var context = CreateExceptionContext(new InvalidOperationException("Password=secret"));

        filter.OnException(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.DoesNotContain("secret", problem.Detail);
        Assert.False(problem.Extensions.ContainsKey("exceptionType"));
        Assert.True(problem.Extensions.ContainsKey("traceId"));
    }

    private static ExceptionContext CreateExceptionContext(Exception exception)
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "trace-123"
        };
        httpContext.Request.Path = "/api/test";
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ExceptionContext(actionContext, [])
        {
            Exception = exception
        };
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "AssistantEngineer.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
