using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace DotnetApiTemplate.API.Middlewares;

/// <summary>
/// Logs each request/response with a correlation id and duration.
/// Request bodies are only captured for JSON content types, and any field whose
/// name matches a credential/PII denylist is redacted before logging.
/// Response bodies are intentionally NOT logged to avoid buffering large payloads in memory.
/// </summary>
public class RequestResponseLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestResponseLoggingMiddleware> logger)
{
    private const int MaxLoggedBodyLength = 4096;

    // Normalized (lowercase, with '_' and '-' stripped) names whose values must never be logged.
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.Ordinal)
    {
        "password", "passwd", "pwd", "newpassword", "currentpassword", "oldpassword",
        "token", "accesstoken", "refreshtoken", "idtoken", "bearer", "authorization",
        "apikey", "clientsecret", "privatekey", "secret",
        "creditcard", "cardnumber", "cvv", "cvc", "ssn", "pin", "otp", "mfacode", "sessionid",
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.TraceIdentifier;
        context.Response.Headers.Append("X-Correlation-ID", correlationId);

        await LogRequest(context, correlationId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);
            stopwatch.Stop();

            logger.LogInformation(
                "HTTP {Method} {Path} - Status: {StatusCode}, Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            if (ex is not TaskCanceledException and not OperationCanceledException)
            {
                logger.LogError(ex, "Request failed. CorrelationId: {CorrelationId}, Duration: {Duration}ms",
                    correlationId, stopwatch.ElapsedMilliseconds);
            }

            throw;
        }
    }

    private async Task LogRequest(HttpContext context, string correlationId)
    {
        var request = context.Request;
        var body = string.Empty;

        // Only capture JSON bodies; other content types (form posts, file uploads,
        // binary) are skipped entirely to avoid logging credentials or large payloads.
        if (request.ContentLength is > 0 && IsJsonContentType(request.ContentType))
        {
            request.EnableBuffering();
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var raw = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            body = Sanitize(raw);
            if (body.Length > MaxLoggedBodyLength)
            {
                body = body[..MaxLoggedBodyLength] + "...(truncated)";
            }
        }

        logger.LogInformation(
            "HTTP {Method} {Path}{QueryString} - CorrelationId: {CorrelationId}, Body: {Body}",
            request.Method,
            request.Path,
            request.QueryString,
            correlationId,
            body);
    }

    private static bool IsJsonContentType(string? contentType) =>
        contentType is not null
        && contentType.Contains("json", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Parses the body as JSON and rewrites it with sensitive values redacted.
    /// If the body is not valid JSON, the entire body is redacted to be safe.
    /// </summary>
    private static string Sanitize(string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return body;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                WriteRedacted(document.RootElement, writer);
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (JsonException)
        {
            return "***(non-JSON body redacted)***";
        }
    }

    private static void WriteRedacted(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    if (IsSensitive(property.Name))
                    {
                        writer.WriteString(property.Name, "***");
                    }
                    else
                    {
                        writer.WritePropertyName(property.Name);
                        WriteRedacted(property.Value, writer);
                    }
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteRedacted(item, writer);
                }
                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static bool IsSensitive(string propertyName)
    {
        var normalized = propertyName
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .ToLowerInvariant();

        return SensitiveKeys.Contains(normalized);
    }
}
