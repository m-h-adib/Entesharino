using System.Text.Json;
using Entesharino.Application.Common.Models;

namespace Entesharino.Api.Middlewares;

public sealed class AuthorizationResponseMiddleware
{
    private readonly RequestDelegate _next;

    public AuthorizationResponseMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.StatusCode != StatusCodes.Status403Forbidden ||
            context.Response.HasStarted)
            return;

        context.Response.ContentType = "application/json; charset=utf-8";

        var result = ResultDto.Fail(
            "شما مجوز دسترسی به این بخش را ندارید.",
            StatusCodes.Status403Forbidden);

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(result));
    }
}
