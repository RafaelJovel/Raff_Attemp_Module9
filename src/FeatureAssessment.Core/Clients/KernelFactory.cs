using Anthropic;
using FeatureAssessment.Core.Configuration;
using FeatureAssessment.Core.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;

namespace FeatureAssessment.Core.Clients;

/// <summary>
/// Factory implementation that creates Semantic Kernel instances configured
/// with either Ollama or Anthropic LLM providers based on configuration.
/// </summary>
public class KernelFactory : IKernelFactory
{
    private readonly IOptions<LlmProviderConfiguration> _providerConfig;
    private readonly IOptions<OllamaConfiguration> _ollamaConfig;
    private readonly IOptions<AnthropicConfiguration> _anthropicConfig;
    private readonly IFeatureLookupTools? _tools;
    private readonly ILogger<KernelFactory> _logger;

    public KernelFactory(
        IOptions<LlmProviderConfiguration> providerConfig,
        IOptions<OllamaConfiguration> ollamaConfig,
        IOptions<AnthropicConfiguration> anthropicConfig,
        IFeatureLookupTools? tools,
        ILogger<KernelFactory> logger)
    {
        _providerConfig = providerConfig ?? throw new ArgumentNullException(nameof(providerConfig));
        _ollamaConfig = ollamaConfig ?? throw new ArgumentNullException(nameof(ollamaConfig));
        _anthropicConfig = anthropicConfig ?? throw new ArgumentNullException(nameof(anthropicConfig));
        _tools = tools; // null is valid — means no tools/plugins registered in the kernel
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public LlmProvider CurrentProvider => _providerConfig.Value.Provider;

    public Kernel CreateKernel()
    {
        _logger.LogInformation("Creating Semantic Kernel for provider: {Provider}", CurrentProvider);

        return CurrentProvider switch
        {
            LlmProvider.Ollama => CreateOllamaKernel(),
            LlmProvider.Anthropic => CreateAnthropicKernel(),
            _ => throw new NotSupportedException($"LLM provider '{CurrentProvider}' is not supported")
        };
    }

    private Kernel CreateOllamaKernel()
    {
        var config = _ollamaConfig.Value;
        var builder = Kernel.CreateBuilder();

        _logger.LogInformation(
            "Configuring Ollama: Endpoint={Endpoint}, Model={Model}",
            config.Endpoint,
            config.ModelName);

        // Configure Ollama using dedicated Ollama connector for function calling support
        builder.AddOllamaChatCompletion(
            modelId: config.ModelName,
            endpoint: new Uri(config.Endpoint.Replace("/v1", ""))); // Ollama connector doesn't need /v1 suffix

        if (_tools != null)
            builder.Plugins.AddFromObject(_tools, "FeatureLookup");

        return builder.Build();
    }

    private Kernel CreateAnthropicKernel()
    {
        var config = _anthropicConfig.Value;
        var builder = Kernel.CreateBuilder();

        _logger.LogInformation(
            "Configuring Anthropic: Model={Model}",
            config.ModelName);

        // Create custom Anthropic chat completion service wrapper
        // Pass API key directly - service will handle Anthropic SDK internally
        var anthropicChatCompletion = new AnthropicChatCompletionService(
            config.ApiKey,
            config.ModelName,
            config.MaxTokens,
            config.Temperature,
            _logger);

        // Add the Anthropic chat completion service to kernel
        builder.Services.AddSingleton<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>(anthropicChatCompletion);

        if (_tools != null)
            builder.Plugins.AddFromObject(_tools, "FeatureLookup");

        _logger.LogDebug("Anthropic kernel created successfully with IChatClient integration");

        return builder.Build();
    }
}
