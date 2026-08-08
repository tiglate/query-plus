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

        // Detail text is explicit per case, never a bare pass-through of exception.Message -
        // EntityNotFoundException/ForbiddenOperationException are sealed types whose Message is
        // always built from a reviewed template, so their Message is safe to surface; anything
        // else (including any future DomainException subtype not special-cased here) gets a
        // fixed, generic detail so an unreviewed message can never reach the wire.
        var (status, title, detail, errors) = exception switch
        {
            AppValidationException validation => (StatusCodes.Status400BadRequest, "Validation failed",
                "One or more fields are invalid.", (object?)validation.Errors),
            EntityNotFoundException notFound => (StatusCodes.Status404NotFound, "Resource not found",
                notFound.Message, null),
            ForbiddenOperationException forbidden => (StatusCodes.Status403Forbidden, "Operation forbidden",
                forbidden.Message, null),
            DomainException => (StatusCodes.Status400BadRequest, "Invalid request",
                "The request could not be processed.", null),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", null, null)
        };
        context.Response.StatusCode = status;
        var details = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = status, Title = title, Detail = detail,
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
