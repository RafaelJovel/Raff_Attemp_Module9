namespace FeatureAssessment.Core.Configuration;

/// <summary>
/// Configuration settings for Anthropic LLM provider.
/// Supports IOptions pattern for dependency injection and validation.
/// </summary>
public class AnthropicConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Anthropic";

    /// <summary>
    /// The Anthropic API key for authentication.
    /// Load from:
    /// 1. Environment variable: ANTHROPIC_API_KEY (highest priority)
    /// 2. User Secrets: dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-api03-..."
    /// 3. Local config file: appsettings.Development.local.json (gitignored)
    /// Should start with "sk-ant-api03-" or similar prefix.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The Claude model name to use for chat completion.
    /// Default: claude-haiku-4-5 (fast, cost-effective, reliable tool calling)
    /// Other options: claude-sonnet-4-5, claude-opus-4-6
    /// </summary>
    public string ModelName { get; set; } = "claude-haiku-4-5";

    /// <summary>
    /// Temperature setting for LLM responses (0.0 to 1.0).
    /// Lower values = more deterministic, higher values = more creative.
    /// Default: 0.0 for consistent feature identification.
    /// </summary>
    public double Temperature { get; set; } = 0.0;

    /// <summary>
    /// Maximum tokens to generate in the response.
    /// Default: 4096 (Claude models support up to 200k context, but we need less for responses).
    /// </summary>
    public int MaxTokens { get; set; } = 4096;

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
