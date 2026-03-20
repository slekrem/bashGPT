namespace bashGPT.Tools.Fetch;

public sealed class DefaultFetchPolicy : IFetchPolicy
{
    private static readonly string[] AllowedMethods = ["GET", "HEAD"];

    public bool Allow(FetchInput input)
    {
        if (!Uri.TryCreate(input.Url, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != "http" && uri.Scheme != "https")
            return false;

        if (!AllowedMethods.Contains(input.Method.ToUpperInvariant()))
            return false;

        return true;
    }
}
