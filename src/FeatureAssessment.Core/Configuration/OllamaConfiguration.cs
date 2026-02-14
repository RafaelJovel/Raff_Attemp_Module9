namespace FeatureAssessment.Core.Configuration;

/// <summary>
/// Configuration settings for Ollama LLM provider.
/// </summary>
public class OllamaConfiguration
{
    /// <summary>
    /// The Ollama API endpoint URL.
    /// Default: http://localhost:11434
    /// </summary>
    public string Endpoint { get; init; } = "http://localhost:11434";

    /// <summary>
    /// The model name to use for chat completion.
    /// Default: qwen2.5:latest
    /// </summary>
    public string ModelName { get; init; } = "qwen2.5:latest";

    /// <summary>
    /// Temperature setting for LLM responses (0.0 to 1.0).
    /// Lower values = more deterministic, higher values = more creative.
    /// Default: 0.0 for consistent feature identification.
    /// </summary>
    public double Temperature { get; init; } = 0.0;

    /// <summary>
    /// Maximum tokens to generate in the response.
    /// Default: 500 (sufficient for feature lookup responses).
    /// </summary>
    public int MaxTokens { get; init; } = 500;
}
