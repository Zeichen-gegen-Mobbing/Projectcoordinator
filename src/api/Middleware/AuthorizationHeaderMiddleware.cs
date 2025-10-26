using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using ZgM.ProjectCoordinator.Shared;

namespace api.Middleware;

/// <summary>
/// Replaces the Authorization header with the value of <see cref="CustomHttpHeaders.SwaAuthorization"/>
/// As static Web App replaces the Authorization header
/// we need to use a custom header. To avoid rewriting all code that uses the Authorization header,
/// we replace it in this middleware.
/// </summary>
public class AuthorizationHeaderMiddleware : IFunctionsWorkerMiddleware
{
    private const string AuthorizationHeader = "Authorization";

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpRequestData = await context.GetHttpRequestDataAsync();
        ReplaceAuthorizationHeader(httpRequestData?.Headers);
        await next(context);
    }

    public static void ReplaceAuthorizationHeader(HttpHeadersCollection? headers)
    {
        if (headers?.TryGetValues(CustomHttpHeaders.SwaAuthorization, out var swaAuthValues) == true)
        {
            var swaAuthValue = swaAuthValues.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(swaAuthValue))
            {
                if (headers.Contains(AuthorizationHeader))
                {
                    headers.Remove(AuthorizationHeader);
                }

                headers.Add(AuthorizationHeader, swaAuthValue);
            }
        }
    }
}
