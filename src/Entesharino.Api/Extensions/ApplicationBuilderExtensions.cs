using Entesharino.Api.Middlewares;

namespace Entesharino.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseApiMiddlewares(
        this IApplicationBuilder app)
    {
        app.UseMiddleware<AuthorizationResponseMiddleware>();

        return app;
    }
}
