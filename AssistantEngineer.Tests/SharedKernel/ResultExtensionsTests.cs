using AssistantEngineer.Api.Extensions;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Tests;

public class ResultExtensionsTests
{
    [Fact]
    public void ToActionResultMapsValidationFailureToBadRequestProblemDetails()
    {
        var result = Result<object>.Validation("Invalid request.");

        var actionResult = result.ToActionResult(CreateController());

        var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Validation failed", problem.Title);
        Assert.Equal("Invalid request.", problem.Detail);
        Assert.Equal("validation_failed", problem.Extensions[ApiProblemDetailsFactory.CodeExtensionName]);
        Assert.Equal("trace-123", problem.Extensions[ApiProblemDetailsFactory.CorrelationIdExtensionName]);
    }

    [Fact]
    public void ToActionResultMapsNotFoundFailureToNotFoundProblemDetails()
    {
        var result = Result<object>.NotFound("Missing.");

        var actionResult = result.ToActionResult(CreateController());

        var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("Not found", problem.Title);
        Assert.Equal("Missing.", problem.Detail);
        Assert.Equal("resource_not_found", problem.Extensions[ApiProblemDetailsFactory.CodeExtensionName]);
        Assert.Equal("trace-123", problem.Extensions[ApiProblemDetailsFactory.CorrelationIdExtensionName]);
    }

    [Fact]
    public void ToActionResultMapsConflictFailureToConflictProblemDetails()
    {
        var result = Result<object>.Conflict("Duplicate.");

        var actionResult = result.ToActionResult(CreateController());

        var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Equal("Conflict", problem.Title);
        Assert.Equal("Duplicate.", problem.Detail);
        Assert.Equal("conflict", problem.Extensions[ApiProblemDetailsFactory.CodeExtensionName]);
        Assert.Equal("trace-123", problem.Extensions[ApiProblemDetailsFactory.CorrelationIdExtensionName]);
    }

    private static ControllerBase CreateController()
    {
        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    TraceIdentifier = "trace-123"
                }
            }
        };

        return controller;
    }

    private sealed class TestController : ControllerBase;
}
