namespace bashGPT.Core.Providers.Abstractions;

public class LlmProviderException(string message, Exception? inner = null)
    : Exception(message, inner);
