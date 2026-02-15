namespace FeatureAssessment.Core.Configuration;

/// <summary>
/// Enumeration of supported LLM providers.
/// </summary>
public enum LlmProvider
{
    /// <summary>
    /// Local Ollama LLM (for development and offline scenarios).
    /// </summary>
    Ollama,

    /// <summary>
    /// Anthropic Claude API (production-ready, reliable tool calling).
    /// </summary>
    Anthropic
}

/// <summary>
/// Configuration for selecting which LLM provider to use.
/// Supports IOptions pattern for dependency injection.
/// </summary>
public class LlmProviderConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "LlmProvider";

    /// <summary>
    /// The LLM provider to use.
    /// Default: Anthropic (production-ready, deterministic, reliable).
    /// Set to Ollama for local development without API costs.
    /// </summary>
    public LlmProvider Provider { get; set; } = LlmProvider.Anthropic;
}
