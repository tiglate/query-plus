using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QueryPlus.Application.Common;
using QueryPlus.Domain.Exceptions;
using AppValidationException = QueryPlus.Application.Common.ValidationException;

namespace QueryPlus.Api.ProblemDetails;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception on {Path} ({ExceptionType})", context.Request.Path,
            exception.GetType().FullName);
        var (status, title, errors) = exception switch
        {
            AppValidationException validation => (StatusCodes.Status400BadRequest, "Validation failed",
                validation.Errors),
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Resource not found", null),
            ForbiddenOperationException => (StatusCodes.Status403Forbidden, "Operation forbidden", null),
            DomainException => (StatusCodes.Status400BadRequest, "Invalid request", null),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", null)
        };
        context.Response.StatusCode = status;
        var details = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = status, Title = title, Detail = status == 500 ? null : exception.Message,
            Instance = context.Request.Path
        };
        if (errors is not null)
        {
            details.Extensions["errors"] = errors;
        }

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
            { HttpContext = context, ProblemDetails = details });
    }
}
