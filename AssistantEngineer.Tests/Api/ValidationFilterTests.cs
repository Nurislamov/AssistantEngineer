using AssistantEngineer.Api.Filters;
using AssistantEngineer.Api.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests;

public class ValidationFilterTests
{
    [Fact]
    public async Task OnActionExecutionAsyncSkipsFluentValidationWhenModelStateAlreadyInvalid()
    {
        var validator = new CountingValidator();
        var services = new ServiceCollection()
            .AddSingleton<IValidator<TestRequest>>(validator)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services
        };
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());
        actionContext.ModelState.AddModelError("Name", "Model binding failed.");
        var executingContext = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?> { ["request"] = new TestRequest() },
            controller: new object());
        var nextCalled = false;

        await new ValidationFilter().OnActionExecutionAsync(
            executingContext,
            () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(actionContext, [], new object()));
            });

        Assert.False(nextCalled);
        Assert.Equal(0, validator.InvocationCount);
        var result = Assert.IsType<BadRequestObjectResult>(executingContext.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(result.Value);
        Assert.Equal("Validation failed", problem.Title);
        Assert.Equal("validation_failed", problem.Extensions[ApiProblemDetailsFactory.CodeExtensionName]);
        Assert.Equal(httpContext.TraceIdentifier, problem.Extensions[ApiProblemDetailsFactory.CorrelationIdExtensionName]);
        Assert.Contains("Name", problem.Errors.Keys);
    }

    [Fact]
    public async Task OnActionExecutionAsyncRunsFluentValidationWhenModelStateIsValid()
    {
        var validator = new CountingValidator();
        var services = new ServiceCollection()
            .AddSingleton<IValidator<TestRequest>>(validator)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = services };
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());
        var executingContext = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?> { ["request"] = new TestRequest() },
            controller: new object());

        await new ValidationFilter().OnActionExecutionAsync(
            executingContext,
            () => Task.FromResult(new ActionExecutedContext(actionContext, [], new object())));

        Assert.Equal(1, validator.InvocationCount);
        var result = Assert.IsType<BadRequestObjectResult>(executingContext.Result);
        var problem = Assert.IsType<ValidationProblemDetails>(result.Value);
        Assert.Equal("validation_failed", problem.Extensions[ApiProblemDetailsFactory.CodeExtensionName]);
        Assert.Equal(httpContext.TraceIdentifier, problem.Extensions[ApiProblemDetailsFactory.CorrelationIdExtensionName]);
        Assert.Contains("Name", actionContext.ModelState.Keys);
    }

    private sealed class TestRequest
    {
        public string Name { get; init; } = string.Empty;
    }

    private sealed class CountingValidator : AbstractValidator<TestRequest>
    {
        public int InvocationCount { get; private set; }

        public CountingValidator()
        {
            RuleFor(request => request.Name).NotEmpty();
        }

        public override Task<FluentValidation.Results.ValidationResult> ValidateAsync(
            ValidationContext<TestRequest> context,
            CancellationToken cancellation = default)
        {
            InvocationCount++;
            return base.ValidateAsync(context, cancellation);
        }
    }
}
