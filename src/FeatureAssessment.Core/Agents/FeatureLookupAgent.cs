using System.Diagnostics;
using System.Text.Json;
using FeatureAssessment.Core.Configuration;
using FeatureAssessment.Core.Models;
using FeatureAssessment.Core.Observability;
using FeatureAssessment.Core.Prompts;
using FeatureAssessment.Core.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;

namespace FeatureAssessment.Core.Agents;

/// <summary>
/// Agent that uses LLM to translate natural language queries into feature metadata.
/// Uses IOptions pattern for configuration and supports resilience policies.
/// </summary>
public class FeatureLookupAgent : IFeatureLookupAgent
{
    private readonly IFeatureLookupTools _tools;
    private readonly OllamaConfiguration _config;
    private readonly ILogger<FeatureLookupAgent> _logger;

    public FeatureLookupAgent(
        IFeatureLookupTools tools,
        IOptions<OllamaConfiguration> configOptions,
        ILogger<FeatureLookupAgent> logger)
    {
        _tools = tools ?? throw new ArgumentNullException(nameof(tools));
        ArgumentNullException.ThrowIfNull(configOptions);
        _config = configOptions.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FeatureLookupResult> LookupFeatureAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or whitespace.", nameof(query));
        }

        // Create activity for distributed tracing
        using var activity = ActivitySources.FeatureLookup.StartActivity("FeatureLookupAgent.LookupFeature");
        activity?.SetTag("query", query);
        activity?.SetTag("service.name", ActivitySources.ServiceName);

        _logger.LogInformation("Starting feature lookup for query: {Query}", query);

        try
        {
            // Create kernel with Ollama provider
            var kernel = CreateKernel();

            // Execute agent
            var response = await ExecuteAgentAsync(kernel, query, cancellationToken);

            // Parse response
            var result = ParseResponse(response);

            // Add result attributes to activity
            activity?.SetTag("feature_key", result.FeatureKey);
            activity?.SetTag("feature_id", result.FeatureId);
            activity?.SetTag("target_environment", result.TargetEnvironment);
            activity?.SetTag("is_success", result.IsSuccess);

            if (!result.IsSuccess && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                activity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
            }

            _logger.LogInformation(
                "Feature lookup completed. Success: {Success}, FeatureKey: {FeatureKey}",
                result.IsSuccess,
                result.FeatureKey);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during feature lookup for query: {Query}", query);

            // Record exception in activity
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);
            activity?.SetTag("exception.stacktrace", ex.StackTrace);

            return new FeatureLookupResult
            {
                IsSuccess = false,
                ErrorMessage = $"An error occurred during feature lookup: {ex.Message}"
            };
        }
    }

    private Kernel CreateKernel()
    {
        var builder = Kernel.CreateBuilder();

        // Configure Ollama using dedicated Ollama connector for function calling support
        builder.AddOllamaChatCompletion(
            modelId: _config.ModelName,
            endpoint: new Uri(_config.Endpoint.Replace("/v1", ""))); // Ollama connector doesn't need /v1 suffix

        // Register feature lookup tools as a plugin
        builder.Plugins.AddFromObject(_tools, "FeatureLookup");

        return builder.Build();
    }

    private async Task<string> ExecuteAgentAsync(Kernel kernel, string query, CancellationToken cancellationToken)
    {
        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

        // Create chat history with system prompt
        var chatHistory = new ChatHistory(FeatureLookupSystemPrompt.Prompt);
        chatHistory.AddUserMessage(query);

        // Execute with automatic function calling enabled
        var executionSettings = new OllamaPromptExecutionSettings
        {
            Temperature = (float)_config.Temperature,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        _logger.LogDebug("Executing agent with automatic tool calling enabled");

        var response = await chatCompletion.GetChatMessageContentAsync(
            chatHistory,
            executionSettings,
            kernel,
            cancellationToken);

        var responseText = response.Content ?? string.Empty;

        _logger.LogDebug("Agent response: {Response}", responseText);

        return responseText;
    }

    private FeatureLookupResult ParseResponse(string response)
    {
        try
        {
            // Try to extract JSON from response (in case there's extra text)
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd >= jsonStart)
            {
                var jsonText = response.Substring(jsonStart, jsonEnd - jsonStart + 1);

                var jsonDoc = JsonDocument.Parse(jsonText);
                var root = jsonDoc.RootElement;

                return new FeatureLookupResult
                {
                    FeatureKey = root.TryGetProperty("feature_key", out var fk) ? fk.GetString() : null,
                    FeatureId = root.TryGetProperty("feature_id", out var fi) ? fi.GetString() : null,
                    TargetEnvironment = root.TryGetProperty("target_environment", out var te)
                        ? te.GetString() ?? "UAT"
                        : "UAT",
                    IsSuccess = root.TryGetProperty("success", out var s) && s.GetBoolean(),
                    ErrorMessage = root.TryGetProperty("error_message", out var em) ? em.GetString() : null,
                    AdditionalContext = root.TryGetProperty("context", out var ctx) ? ctx.GetString() : null
                };
            }

            _logger.LogWarning("Could not find JSON in response: {Response}", response);
            return new FeatureLookupResult
            {
                IsSuccess = false,
                ErrorMessage = "Agent response was not in expected JSON format"
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse agent response as JSON: {Response}", response);
            return new FeatureLookupResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to parse agent response: {ex.Message}"
            };
        }
    }
}
