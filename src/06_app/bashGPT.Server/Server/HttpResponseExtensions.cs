using bashGPT.Core.Serialization;

namespace bashGPT.Server;

internal static class HttpResponseExtensions
{
    internal static async Task WriteJsonAsync(this HttpResponse response, object payload, int statusCode = 200)
    {
        response.StatusCode = statusCode;
        await response.WriteAsJsonAsync(payload, JsonDefaults.Options);
    }
}
