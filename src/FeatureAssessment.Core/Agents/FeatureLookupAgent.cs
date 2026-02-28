using System.Diagnostics;
using System.Text.Json;
using FeatureAssessment.Core.Clients;
using FeatureAssessment.Core.Models;
using FeatureAssessment.Core.Observability;
using FeatureAssessment.Core.Prompts;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FeatureAssessment.Core.Agents;

/// <summary>
/// Agent that uses LLM to translate natural language queries into feature metadata.
/// Provider-agnostic implementation using IKernelFactory for LLM abstraction.
/// </summary>
public class FeatureLookupAgent : IFeatureLookupAgent
{
    private readonly IKernelFactory _kernelFactory;
    private readonly ILogger<FeatureLookupAgent> _logger;

    public FeatureLookupAgent(
        IKernelFactory kernelFactory,
        ILogger<FeatureLookupAgent> logger)
    {
        _kernelFactory = kernelFactory ?? throw new ArgumentNullException(nameof(kernelFactory));
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
            // Create kernel with configured LLM provider
            var kernel = _kernelFactory.CreateKernel();
            _logger.LogDebug("Using LLM provider: {Provider}", _kernelFactory.CurrentProvider);

            // Execute agent
            var response = await ExecuteAgentAsync(kernel, query, cancellationToken);

            // Parse response
            var result = ParseResponse(response);

            // Add result attributes to activity
            activity?.SetTag("feature_key", result.FeatureKey);
            activity?.SetTag("feature_id", result.FeatureId);
            activity?.SetTag("target_environment", result.TargetEnvironment);
            activity?.SetTag("is_success", result.IsSuccess.ToString());

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

    private async Task<string> ExecuteAgentAsync(Kernel kernel, string query, CancellationToken cancellationToken)
    {
        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

        // Create chat history with system prompt
        var chatHistory = new ChatHistory(FeatureLookupSystemPrompt.Prompt);
        chatHistory.AddUserMessage(query);

        // Execute with automatic function calling enabled
        // Use provider-agnostic PromptExecutionSettings
        var executionSettings = new PromptExecutionSettings
        {
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
        _logger.LogTrace("ParseResponse called with response length: {Length}", response?.Length ?? 0);
        _logger.LogTrace("Raw response: {Response}", response);

        if (string.IsNullOrWhiteSpace(response))
        {
            _logger.LogWarning("Response is null or empty");
            return new FeatureLookupResult
            {
                IsSuccess = false,
                ErrorMessage = "Agent returned empty response"
            };
        }

        try
        {
            // Clean up the response - remove markdown code blocks if present
            var cleanedResponse = response.Trim();
            if (cleanedResponse.StartsWith("```json"))
            {
                cleanedResponse = cleanedResponse.Substring(7);
            }
            if (cleanedResponse.StartsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(3);
            }
            if (cleanedResponse.EndsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
            }
            cleanedResponse = cleanedResponse.Trim();

            // Try to extract the FIRST JSON object from response
            var jsonStart = cleanedResponse.IndexOf('{');
            var jsonEnd = -1;

            if (jsonStart >= 0)
            {
                // Find the matching closing brace for the first opening brace
                int braceCount = 0;
                for (int i = jsonStart; i < cleanedResponse.Length; i++)
                {
                    if (cleanedResponse[i] == '{') braceCount++;
                    else if (cleanedResponse[i] == '}')
                    {
                        braceCount--;
                        if (braceCount == 0)
                        {
                            jsonEnd = i;
                            break;
                        }
                    }
                }
            }

            _logger.LogTrace("JSON extraction: jsonStart={JsonStart}, jsonEnd={JsonEnd}", jsonStart, jsonEnd);

            if (jsonStart >= 0 && jsonEnd >= jsonStart)
            {
                var jsonText = cleanedResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                _logger.LogTrace("Extracted JSON text: {JsonText}", jsonText);

                var jsonDoc = JsonDocument.Parse(jsonText);
                var root = jsonDoc.RootElement;

                var result = new FeatureLookupResult
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

                _logger.LogDebug(
                    "Parsed result: Success={Success}, FeatureKey={FeatureKey}, FeatureId={FeatureId}, TargetEnv={TargetEnv}",
                    result.IsSuccess, result.FeatureKey, result.FeatureId, result.TargetEnvironment);

                return result;
            }

            _logger.LogWarning("Could not find JSON object in response: {Response}", cleanedResponse);
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
