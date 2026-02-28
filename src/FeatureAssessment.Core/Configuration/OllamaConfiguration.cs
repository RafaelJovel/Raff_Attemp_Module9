namespace FeatureAssessment.Core.Configuration;

/// <summary>
/// Configuration settings for Ollama LLM provider.
/// Supports IOptions pattern for dependency injection and validation.
/// </summary>
public class OllamaConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Ollama";

    /// <summary>
    /// The Ollama API endpoint URL.
    /// Default: http://localhost:11434
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:11434";

    /// <summary>
    /// The model name to use for chat completion.
    /// Default: llama3.1:8b (proven tool calling support with Semantic Kernel)
    /// </summary>
    public string ModelName { get; set; } = "llama3.1:8b";

    /// <summary>
    /// Temperature setting for LLM responses (0.0 to 1.0).
    /// Lower values = more deterministic, higher values = more creative.
    /// Default: 0.0 for consistent feature identification.
    /// </summary>
    public double Temperature { get; set; } = 0.0;

    /// <summary>
    /// Maximum tokens to generate in the response.
    /// Default: 500 (sufficient for feature lookup responses).
    /// </summary>
    public int MaxTokens { get; set; } = 500;

    /// <summary>
    /// Request timeout in seconds.
    /// Default: 30 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of retry attempts for transient failures.
    /// Default: 3 retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}
