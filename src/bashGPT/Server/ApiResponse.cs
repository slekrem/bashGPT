using System.Net;
using System.Reflection;
using System.Text.Json;
using BashGPT.Providers;

namespace BashGPT.Server;

internal static class ApiResponse
{
    internal static async Task WriteJsonAsync(HttpListenerResponse response, object payload, int statusCode = 200)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json; charset=utf-8";
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonDefaults.Options);
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
        response.Close();
    }

    internal static async Task WriteResourceAsync(HttpListenerResponse response, string resourceName, string contentType)
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            response.StatusCode = 404;
            response.Close();
            return;
        }
        response.StatusCode = 200;
        response.ContentType = contentType;
        response.ContentLength64 = stream.Length;
        await stream.CopyToAsync(response.OutputStream);
        response.Close();
    }
}
