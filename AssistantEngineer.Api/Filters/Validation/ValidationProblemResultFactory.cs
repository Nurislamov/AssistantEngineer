using AssistantEngineer.Api.Extensions.Http;
using Microsoft.AspNetCore.Mvc;

namespace AssistantEngineer.Api.Filters.Validation;

internal static class ValidationProblemResultFactory
{
    public static BadRequestObjectResult Create(
        ActionContext context) =>
        ApiProblemDetailsFactory.CreateValidationResult(context);
}