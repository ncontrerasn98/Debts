using Debts.API.Attributes;
using Debts.Application.Abstractions.Idempotency;

namespace Debts.API.Middlewares;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IIdempotencyService idempotencyService)
    {
        if (context.Request.Method is not ("POST" or "PATCH" or "PUT"))
        {
            await _next(context);
            return;
        }

        // Verificar si el endpoint tiene el atributo [Idempotent]
        var endpoint = context.GetEndpoint();
        var hasIdempotentAttribute = endpoint?
            .Metadata
            .GetMetadata<IdempotentAttribute>() is not null;

        if (!hasIdempotentAttribute)
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var key))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = key.ToString();
        var cachedResponse = await idempotencyService.GetAsync(idempotencyKey);

        if (cachedResponse is not null)
        {
            _logger.LogInformation(
                "Idempotent request detected for key {Key} — returning cached response",
                idempotencyKey);

            context.Response.StatusCode = 204;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(cachedResponse);
            return;
        }

        var originalBody = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        try
        {
            await _next(context);

            if (context.Response.StatusCode is >= 200 and < 300)
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
                await idempotencyService.SetAsync(idempotencyKey, responseBody);

                _logger.LogInformation("Idempotency key {Key} stored", idempotencyKey);
            }
        }
        finally
        {
            // Siempre restaurar el body original
            memoryStream.Seek(0, SeekOrigin.Begin);
            await memoryStream.CopyToAsync(originalBody);
            context.Response.Body = originalBody;
        }
    }
}