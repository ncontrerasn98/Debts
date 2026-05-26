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
        // Solo aplica a métodos que modifican estado
        if (context.Request.Method is not ("POST" or "PATCH" or "PUT"))
        {
            await _next(context);
            return;
        }

        // Si no viene el header, dejamos pasar sin validar
        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var key))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = key.ToString();

        // Buscar en Redis
        var cachedResponse = await idempotencyService.GetAsync(idempotencyKey);

        if (cachedResponse is not null)
        {
            _logger.LogInformation(
                "Idempotent request detected for key {Key} — returning cached response",
                idempotencyKey);

            // 204 no tiene body
            if (context.Response.StatusCode == 204 || cachedResponse == string.Empty)
            {
                context.Response.StatusCode = 204;
                return;
            }

            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(cachedResponse);
            return;
        }

        // Capturar la respuesta para guardarla en Redis
        var originalBody = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        await _next(context);

        // Solo guardar si fue exitoso
        if (context.Response.StatusCode is >= 200 and < 300)
        {
            memoryStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();

            await idempotencyService.SetAsync(idempotencyKey, responseBody);

            _logger.LogInformation(
                "Idempotency key {Key} stored in Redis",
                idempotencyKey);
        }

        memoryStream.Seek(0, SeekOrigin.Begin);
        await memoryStream.CopyToAsync(originalBody);
        context.Response.Body = originalBody;
    }
}