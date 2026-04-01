using System.Diagnostics;
using System.Text;

namespace DotnetApiTemplate.Middlewares;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate correlation ID
        var correlationId = context.TraceIdentifier;
        context.Response.Headers.Append("X-Correlation-ID", correlationId);

        // Log request
        await LogRequest(context, correlationId);

        // Capture response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
            stopwatch.Stop();

            // Log response
            await LogResponse(context, correlationId, stopwatch.ElapsedMilliseconds);

            // Copy response back to original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Don't log TaskCanceledException or OperationCanceledException as errors
            // These occur when the client disconnects or cancels the request, which is normal
            if (ex is not TaskCanceledException and not OperationCanceledException)
            {
                _logger.LogError(ex, "Request failed. CorrelationId: {CorrelationId}, Duration: {Duration}ms",
                    correlationId, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogDebug(ex, "Request canceled. CorrelationId: {CorrelationId}, Duration: {Duration}ms",
                    correlationId, stopwatch.ElapsedMilliseconds);
            }

            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task LogRequest(HttpContext context, string correlationId)
    {
        context.Request.EnableBuffering();

        var request = context.Request;
        var requestBody = string.Empty;

        if (request.ContentLength > 0)
        {
            request.Body.Position = 0;
            using var reader = new StreamReader(
                request.Body,
                Encoding.UTF8,
                leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            request.Body.Position = 0;
        }

        _logger.LogInformation(
            "HTTP {Method} {Path} - CorrelationId: {CorrelationId}, Body: {Body}",
            request.Method,
            request.Path,
            correlationId,
            requestBody);
    }

    private async Task LogResponse(HttpContext context, string correlationId, long duration)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        _logger.LogInformation(
            "HTTP {Method} {Path} - Status: {StatusCode}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            duration,
            correlationId);
    }
}
