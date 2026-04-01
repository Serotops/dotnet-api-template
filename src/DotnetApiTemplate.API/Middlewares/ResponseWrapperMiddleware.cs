using System.Text.Json;
using DotnetApiTemplate.Common;

namespace DotnetApiTemplate.Middlewares;

public class ResponseWrapperMiddleware
{
    private readonly RequestDelegate _next;

    public ResponseWrapperMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip wrapping for certain endpoints
        if (ShouldSkipWrapping(context))
        {
            await _next(context);
            return;
        }

        var originalBodyStream = context.Response.Body;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        // Register callback to remove Content-Length header before response starts
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Remove("Content-Length");
            return Task.CompletedTask;
        });

        await _next(context);

        // Only wrap successful responses (2xx status codes)
        // Skip 204 No Content and 304 Not Modified as they cannot have a body
        if (context.Response.StatusCode >= 200 &&
            context.Response.StatusCode < 300 &&
            context.Response.StatusCode != StatusCodes.Status204NoContent &&
            context.Response.StatusCode != StatusCodes.Status304NotModified &&
            !context.Response.HasStarted)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();

            // Parse the original response
            object? data = null;
            if (!string.IsNullOrEmpty(responseText))
            {
                try
                {
                    data = JsonSerializer.Deserialize<object>(responseText);
                }
                catch
                {
                    data = responseText;
                }
            }

            var wrappedResponse = ApiResponse<object>.SuccessResponse(data ?? new object());
            var wrappedJson = JsonSerializer.Serialize(wrappedResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            // Reset the response body stream
            context.Response.Body = originalBodyStream;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(wrappedJson);
        }
        else
        {
            // For non-2xx responses or if response has started, copy the original response
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }
    }

    private static bool ShouldSkipWrapping(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Skip wrapping for health checks, Swagger UI, and OpenAPI documentation only
        // All API endpoints (e.g., /api/v1/cars) will be wrapped for consistency
        return path.Contains("/health") ||
               path.Contains("/swagger") ||
               path.Contains("/openapi");
    }
}
