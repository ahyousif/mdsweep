namespace Mdsweep.Api.Common.Authentication;

public static class AntiforgeryMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthenticatedApiAntiforgery(this IApplicationBuilder app) =>
        app.Use(
            async (httpContext, next) =>
            {
                if (
                    httpContext.User.Identity?.IsAuthenticated != true
                    || !httpContext.Request.Path.StartsWithSegments("/api")
                    || !IsUnsafeMethod(httpContext.Request.Method)
                )
                {
                    await next(httpContext);
                    return;
                }

                try
                {
                    await httpContext
                        .RequestServices.GetRequiredService<IAntiforgery>()
                        .ValidateRequestAsync(httpContext);
                }
                catch (AntiforgeryValidationException)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                await next(httpContext);
            }
        );

    private static bool IsUnsafeMethod(string method) =>
        HttpMethods.IsPost(method)
        || HttpMethods.IsPut(method)
        || HttpMethods.IsPatch(method)
        || HttpMethods.IsDelete(method);
}
