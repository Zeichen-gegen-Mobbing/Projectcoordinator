using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using ZgM.ProjectCoordinator.Shared;

namespace api.Middleware;

public class AuthorizationHeaderMiddleware : IFunctionsWorkerMiddleware
{
    private const string AuthorizationHeader = "Authorization";

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpRequestData = await context.GetHttpRequestDataAsync();

        if (httpRequestData is not null)
        {
            ReplaceAuthorizationHeader(httpRequestData.Headers);
        }

        await next(context);
    }

    public static void ReplaceAuthorizationHeader(HttpHeadersCollection headers)
    {
        if (headers.TryGetValues(CustomHttpHeaders.SwaAuthorization, out var swaAuthValues))
        {
            var swaAuthValue = swaAuthValues?.FirstOrDefault();
            
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
