namespace BashGPT.Providers;

public class LlmProviderException(string message, Exception? inner = null)
    : Exception(message, inner);
