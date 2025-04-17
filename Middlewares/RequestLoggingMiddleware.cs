namespace middleware_demo.Middlewares;

using Microsoft.AspNetCore.Http;
using Serilog;
using System.Diagnostics;
using System.Threading.Tasks;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Log.Information("➡️ Incoming Request: {Method} {Path} from {IP}",
                context.Request.Method, 
                context.Request.Path, 
                context.Connection.RemoteIpAddress);

            await _next(context);

            stopwatch.Stop();
            Log.Information(" Response Status: {StatusCode}, Time Taken: {ElapsedMilliseconds} ms",
                context.Response.StatusCode, 
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            Log.Error(ex, " Exception caught in RequestLoggingMiddleware: {Message}", ex.Message);
            throw;
        }
    }
}

