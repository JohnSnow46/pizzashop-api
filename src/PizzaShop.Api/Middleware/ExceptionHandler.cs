using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PizzaShop.Application.Common.Exceptions;
using PizzaShop.Domain.Exceptions;
using ApplicationValidationException = PizzaShop.Application.Common.Exceptions.ValidationException;

namespace PizzaShop.Api.Middleware;

/// <summary>
/// Single global exception -> <see cref="ProblemDetails"/> mapping point (api-layer.md 4,
/// ADR-0027). Controllers never catch Application/Domain exceptions themselves. Maps by
/// concrete exception type (no shared Application exception base class — ADR-0017/0018,
/// deliberate YAGNI).
/// </summary>
public sealed class ExceptionHandler : IExceptionHandler
{
    // 409 = conflict with resource *state*, illegal for every caller regardless of role
    // (api-layer.md 4.2). Every other concrete DomainException defaults to 422: the request
    // is well-formed but the operation is not viable given the business rules/data.
    private static readonly HashSet<Type> ConflictDomainExceptionTypes = new()
    {
        typeof(InvalidOrderStatusTransitionException),
        typeof(InvalidPaymentStatusTransitionException),
        typeof(PromotionAlreadyAppliedException),
        typeof(LoyaltyPointsAlreadyRedeemedException),
    };

    private readonly ILogger<ExceptionHandler> _logger;

    public ExceptionHandler(ILogger<ExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = Map(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        if (exception is ApplicationValidationException validationException)
        {
            var validationProblem = new ValidationProblemDetails(ToErrorDictionary(validationException))
            {
                Status = statusCode,
                Title = title,
                Type = $"https://httpstatuses.io/{statusCode}",
                Instance = httpContext.Request.Path,
            };
            validationProblem.Extensions["traceId"] = httpContext.TraceIdentifier;

            await httpContext.Response.WriteAsJsonAsync(validationProblem, cancellationToken);
            return true;
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            // Application/Domain exception messages are safe to return (ADR-0017); a raw 500
            // never leaks the underlying exception's message.
            Detail = statusCode == StatusCodes.Status500InternalServerError ? null : exception.Message,
            Type = $"https://httpstatuses.io/{statusCode}",
            Instance = httpContext.Request.Path,
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static (int StatusCode, string Title) Map(Exception exception) => exception switch
    {
        ApplicationValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
        NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
        ForbiddenOperationException => (StatusCodes.Status403Forbidden, "Forbidden"),
        InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Invalid credentials"),
        ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
        InvalidPaymentNotificationException => (StatusCodes.Status400BadRequest, "Invalid payment notification"),
        NotSupportedException => (StatusCodes.Status501NotImplemented, "Not implemented"),
        // Must come before ArgumentException — ArgumentOutOfRangeException derives from it and
        // pattern order matters for the first-match switch below.
        ArgumentOutOfRangeException => (StatusCodes.Status400BadRequest, "Invalid argument"),
        ArgumentException => (StatusCodes.Status400BadRequest, "Invalid argument"),
        InvalidOperationException => (StatusCodes.Status500InternalServerError, "Internal server error"),
        DomainException domainException => (MapDomainException(domainException), "Business rule violation"),
        _ => (StatusCodes.Status500InternalServerError, "Internal server error"),
    };

    private static int MapDomainException(DomainException exception) =>
        ConflictDomainExceptionTypes.Contains(exception.GetType())
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status422UnprocessableEntity;

    private static Dictionary<string, string[]> ToErrorDictionary(ApplicationValidationException exception) =>
        exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
}
