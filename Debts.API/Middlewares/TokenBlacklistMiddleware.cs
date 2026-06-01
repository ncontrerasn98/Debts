using Debts.Application.Abstractions.Auth;

namespace Debts.API.Middlewares;

public class TokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public TokenBlacklistMiddleware(RequestDelegate next)
        => _next = next;

    public async Task InvokeAsync(HttpContext context, ITokenBlacklistService blacklistService)
    {
        var token = context.Request.Headers.Authorization
            .FirstOrDefault()?.Replace("Bearer ", "");

        if (!string.IsNullOrEmpty(token) && await blacklistService.IsBlacklistedAsync(token))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync("Token has been revoked");
            return;
        }

        await _next(context);
    }
}