using System.Diagnostics;

namespace Debts.API.Middlewares;

public class CorrelationMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        // Si no existe Activity, crearla
        if (Activity.Current == null)
        {
            var activity = new Activity("HTTP Request");
            activity.Start();
        }

        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Trace-Id"] =
                Activity.Current?.TraceId.ToString();
            return Task.CompletedTask;
        });

        await _next(context);
    }
}