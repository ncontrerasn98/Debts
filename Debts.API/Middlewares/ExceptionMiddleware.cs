using System.Net;
using Debts.Domain.Exceptions;
using ValidationException = FluentValidation.ValidationException;

namespace Debts.API.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            
            _logger.LogWarning(ex, "Validation exception occurred");

            await HandleException(
                context,
                HttpStatusCode.BadRequest,
                ex.Errors.Select(e => e.ErrorMessage));
        }
        catch (NotFoundException ex)
        {
            _logger.LogError(ex, "Not found exception occurred");
            
            await HandleException(
                context,
                HttpStatusCode.NotFound,
                ex.Message);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogError(ex, "unauthorized exception occurred");
            
            await HandleException(
                context,
                HttpStatusCode.Unauthorized,
                ex.Message);
        }
        catch (ServiceUnavailableException ex)
        {
            _logger.LogWarning(ex, "External service unavailable");

            await HandleException(
                context,
                HttpStatusCode.ServiceUnavailable,
                ex.Message);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain exception occurred");

            await HandleException(
                context,
                HttpStatusCode.BadRequest,
                ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            
            await HandleException(
                context,
                HttpStatusCode.InternalServerError,
                "Unexpected error");
        }
    }

    private static async Task HandleException(
        HttpContext context,
        HttpStatusCode statusCode,
        object response)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsJsonAsync(response);
    }
}