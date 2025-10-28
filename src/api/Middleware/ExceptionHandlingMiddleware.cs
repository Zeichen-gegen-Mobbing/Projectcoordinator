using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using System.Net;
using System.Security.Authentication;
using System.Text.Json;

namespace api.Middleware;

public class ExceptionHandlingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(FunctionContext context, Exception exception)
    {
        var httpRequestData = await context.GetHttpRequestDataAsync();
        if (httpRequestData == null)
        {
            return;
        }

        var (statusCode, problemDetails) = MapExceptionToProblemDetails(exception);

        var response = httpRequestData.CreateResponse();
        response.StatusCode = statusCode;
        
        // WriteAsJsonAsync sets Content-Type, so don't set it manually
        await response.WriteAsJsonAsync(problemDetails);

        context.GetInvocationResult().Value = response;
    }

    private (HttpStatusCode StatusCode, ProblemDetails ProblemDetails) MapExceptionToProblemDetails(Exception exception)
    {
        return exception switch
        {
            AuthenticationException authenticationException => (
                HttpStatusCode.Unauthorized,
                new ProblemDetails
                {
                    Title = "Authentication Failed",
                    Detail = authenticationException.Message,
                    Status = StatusCodes.Status401Unauthorized
                }),

            UnauthorizedAccessException unauthorizedAccessException => (
                HttpStatusCode.Forbidden,
                new ProblemDetails
                {
                    Title = "Forbidden",
                    Detail = unauthorizedAccessException.Message,
                    Status = StatusCodes.Status403Forbidden
                }),
            _ => (
                HttpStatusCode.InternalServerError,
                new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred. Please try again later.",
                    Status = StatusCodes.Status500InternalServerError
                })
        };
    }
}
