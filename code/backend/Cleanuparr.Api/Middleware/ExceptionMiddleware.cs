using System.Net;
using System.Text.Json;
using Cleanuparr.Api.Models;
using Cleanuparr.Domain.Exceptions;

namespace Cleanuparr.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Generate a unique identifier for this error
        string traceId = Guid.NewGuid().ToString();
        
        // Default status code and message
        int statusCode = (int)HttpStatusCode.InternalServerError;
        string message = "An unexpected error occurred";

        switch (exception)
        {
            // Handle different exception types
            case ValidationException:
                statusCode = (int)HttpStatusCode.BadRequest;
                message = exception.Message; // Use the validation message directly
            
                _logger.LogWarning(exception, 
                    "Validation error {TraceId} occurred during request to {Path}", 
                    traceId, context.Request.Path);
                break;
            
            default:
                // Log other exceptions as errors with more details
                _logger.LogError(exception, 
                    "Error {TraceId} occurred during request to {Path}: {Message}", 
                    traceId, context.Request.Path, exception.Message);
                break;
        }
        
        // Create the error response
        ErrorResponse errorResponse = new()
        {
            TraceId = traceId,
            Error = message
        };
        
        // Set the response
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        
        // Write the response
        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
