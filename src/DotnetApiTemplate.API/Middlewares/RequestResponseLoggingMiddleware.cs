using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace DotnetApiTemplate.Middlewares;

public class RequestResponseLoggingMiddleware
{
    private const int MaxLoggedBodyBytes = 4096;

    private static readonly string[] SensitiveFieldNames =
    {
        "password", "passwd", "pwd",
        "token", "access_token", "refresh_token", "id_token",
        "authorization", "auth",
        "secret", "client_secret",
        "apikey", "api_key",
        "ssn", "creditcard", "credit_card", "card_number", "cvv"
    };

    private static readonly Regex JsonFieldRegex = BuildJsonFieldRegex();
    private static readonly Regex FormFieldRegex = BuildFormFieldRegex();

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.TraceIdentifier;
        context.Response.Headers.Append("X-Correlation-ID", correlationId);

        await LogRequest(context, correlationId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
            stopwatch.Stop();

            LogResponse(context, correlationId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

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
    }

    private async Task LogRequest(HttpContext context, string correlationId)
    {
        var request = context.Request;
        var requestBody = string.Empty;

        if (ShouldLogBody(request))
        {
            request.EnableBuffering();
            request.Body.Position = 0;
            using var reader = new StreamReader(
                request.Body,
                Encoding.UTF8,
                leaveOpen: true);

            var buffer = new char[MaxLoggedBodyBytes];
            var read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
            requestBody = new string(buffer, 0, read);
            if (request.ContentLength > MaxLoggedBodyBytes)
            {
                requestBody += "...[truncated]";
            }
            request.Body.Position = 0;

            requestBody = Redact(requestBody);
        }

        _logger.LogInformation(
            "HTTP {Method} {Path} - CorrelationId: {CorrelationId}, Body: {Body}",
            request.Method,
            request.Path,
            correlationId,
            requestBody);
    }

    private void LogResponse(HttpContext context, string correlationId, long duration)
    {
        _logger.LogInformation(
            "HTTP {Method} {Path} - Status: {StatusCode}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            duration,
            correlationId);
    }

    private static bool ShouldLogBody(HttpRequest request)
    {
        if (request.ContentLength is null or 0)
        {
            return false;
        }

        var contentType = request.ContentType ?? string.Empty;
        if (contentType.Contains("multipart/", StringComparison.OrdinalIgnoreCase) ||
            contentType.Contains("application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static string Redact(string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return body;
        }

        body = JsonFieldRegex.Replace(body, m => $"{m.Groups[1].Value}\"***REDACTED***\"");
        body = FormFieldRegex.Replace(body, m => $"{m.Groups[1].Value}***REDACTED***");
        return body;
    }

    private static Regex BuildJsonFieldRegex()
    {
        var fields = string.Join("|", SensitiveFieldNames);
        return new Regex(
            $"(\"(?:{fields})\"\\s*:\\s*)\"[^\"]*\"",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    private static Regex BuildFormFieldRegex()
    {
        var fields = string.Join("|", SensitiveFieldNames);
        return new Regex(
            $@"(\b(?:{fields})=)[^&\s]*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }
}
