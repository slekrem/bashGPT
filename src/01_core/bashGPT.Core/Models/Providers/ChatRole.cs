namespace bashGPT.Core.Models.Providers;

public enum ChatRole
{
    System,
    User,
    Assistant,
    Tool,
    /// <summary>
    /// Dynamic context injected by an agent before each LLM call.
    /// Serialized as "user" when sent to the provider.
    /// </summary>
    Context
}
