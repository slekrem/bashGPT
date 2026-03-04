using System.Net;
using System.Reflection;
using System.Text;
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

    internal static async Task WriteTextAsync(HttpListenerResponse response, string content, string contentType, int statusCode = 200)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        response.StatusCode = statusCode;
        response.ContentType = contentType;
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
        response.Close();
    }

    internal static async Task<bool> TryWriteResourceAsync(HttpListenerResponse response, string resourceName, string contentType)
    {
        var assemblies = new[]
        {
            Assembly.GetExecutingAssembly(),
            Assembly.GetEntryAssembly()
        };

        Stream? stream = null;
        foreach (var assembly in assemblies)
        {
            if (assembly is null) continue;
            stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is not null) break;
        }

        if (stream is null)
        {
            return false;
        }

        using (stream)
        {
            response.StatusCode = 200;
            response.ContentType = contentType;
            response.ContentLength64 = stream.Length;
            await stream.CopyToAsync(response.OutputStream);
            response.Close();
        }
        return true;
    }

    internal static async Task WriteResourceAsync(HttpListenerResponse response, string resourceName, string contentType)
    {
        var found = await TryWriteResourceAsync(response, resourceName, contentType);
        if (found) return;

        response.StatusCode = 404;
        response.Close();
    }

    internal static void WriteSseEvent(Stream stream, string data)
    {
        var bytes = Encoding.UTF8.GetBytes($"data: {data}\n\n");
        stream.Write(bytes);
        stream.Flush();
    }
}
