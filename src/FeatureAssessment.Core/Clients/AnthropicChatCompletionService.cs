using System.Net.Http;
using System.Text;
using System.Text.Json;
using Anthropic;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FeatureAssessment.Core.Clients;

/// <summary>
/// Custom Semantic Kernel IChatCompletionService implementation for Anthropic Claude.
/// Adapts the Anthropic SDK to work with Semantic Kernel's chat completion interface.
/// </summary>
internal class AnthropicChatCompletionService : IChatCompletionService
{
    private readonly string _apiKey;
    private readonly string _modelId;
    private readonly int _maxTokens;
    private readonly double _temperature;
    private readonly ILogger _logger;

    public AnthropicChatCompletionService(
        string apiKey,
        string modelId,
        int maxTokens,
        double temperature,
        ILogger logger)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _modelId = modelId ?? throw new ArgumentNullException(nameof(modelId));
        _maxTokens = maxTokens;
        _temperature = temperature;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>
    {
        ["ModelId"] = _modelId,
        ["Provider"] = "Anthropic"
    };

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var contentBuilder = new StringBuilder();

        await foreach (var update in GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken))
        {
            contentBuilder.Append(update.Content);
        }

        // Return combined content as single message
        var combinedContent = contentBuilder.ToString();
        return new[] { new ChatMessageContent(AuthorRole.Assistant, combinedContent) };
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting chat completion from Anthropic API");

        // Convert SK ChatHistory to Anthropic Messages format
        var messages = ConvertChatHistory(chatHistory);
        var systemPrompt = ExtractSystemPrompt(chatHistory);

        // Extract tools/functions from kernel if function calling is enabled
        var tools = ExtractTools(kernel, executionSettings);

        // Build Anthropic API request
        var request = new
        {
            model = _modelId,
            max_tokens = _maxTokens,
            temperature = _temperature,
            system = systemPrompt,
            messages = messages,
            tools = tools.Count > 0 ? tools : null
        };

        _logger.LogDebug("Anthropic API request with {ToolCount} tools: {Request}",
            tools.Count, JsonSerializer.Serialize(request));

        // Call Anthropic API
        string response;
        try
        {
            response = await CallAnthropicAPIAsync(request, kernel, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Anthropic API");
            throw;
        }

        yield return new StreamingChatMessageContent(AuthorRole.Assistant, response);
    }

    private List<object> ConvertChatHistory(ChatHistory chatHistory)
    {
        var messages = new List<object>();

        foreach (var message in chatHistory.Where(m => m.Role != AuthorRole.System))
        {
            messages.Add(new
            {
                role = message.Role.Label.ToLowerInvariant(),
                content = message.Content
            });
        }

        return messages;
    }

    private string ExtractSystemPrompt(ChatHistory chatHistory)
    {
        var systemMessages = chatHistory.Where(m => m.Role == AuthorRole.System).ToList();
        return systemMessages.Any()
            ? string.Join("\n\n", systemMessages.Select(m => m.Content))
            : string.Empty;
    }

    private List<object> ExtractTools(Kernel? kernel, PromptExecutionSettings? executionSettings)
    {
        var tools = new List<object>();

        if (kernel == null || executionSettings?.FunctionChoiceBehavior == null)
        {
            return tools;
        }

        // Get all functions from kernel plugins
        var functions = kernel.Plugins.GetFunctionsMetadata();

        foreach (var function in functions)
        {
            var tool = new
            {
                name = $"{function.PluginName}_{function.Name}",
                description = function.Description ?? function.Name,
                input_schema = new
                {
                    type = "object",
                    properties = function.Parameters.ToDictionary(
                        p => p.Name,
                        p => new
                        {
                            type = ConvertToJsonType(p.ParameterType?.Name ?? "string"),
                            description = p.Description ?? p.Name
                        }),
                    required = function.Parameters
                        .Where(p => p.IsRequired)
                        .Select(p => p.Name)
                        .ToArray()
                }
            };

            tools.Add(tool);
        }

        _logger.LogDebug("Extracted {Count} tools from kernel plugins", tools.Count);
        return tools;
    }

    private string ConvertToJsonType(string dotNetType)
    {
        return dotNetType.ToLowerInvariant() switch
        {
            "int32" or "int64" or "int" or "long" => "integer",
            "double" or "float" or "decimal" => "number",
            "boolean" or "bool" => "boolean",
            "object" => "object",
            "array" or "list" => "array",
            _ => "string"
        };
    }

    private async Task<string> CallAnthropicAPIAsync(object request, Kernel? kernel, CancellationToken cancellationToken)
    {
        try
        {
            // Extract request properties
            var requestType = request.GetType();
            var modelProp = requestType.GetProperty("model")?.GetValue(request) as string ?? _modelId;
            var maxTokensProp = requestType.GetProperty("max_tokens")?.GetValue(request);
            var temperatureProp = requestType.GetProperty("temperature")?.GetValue(request);
            var systemProp = requestType.GetProperty("system")?.GetValue(request) as string ?? string.Empty;
            var messagesProp = requestType.GetProperty("messages")?.GetValue(request) as List<object> ?? new List<object>();
            var toolsProp = requestType.GetProperty("tools")?.GetValue(request);

            // Convert messages to mutable list for tool execution loop
            var messages = new List<object>(messagesProp);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            // Tool execution loop - continue until we get a final text response
            const int maxIterations = 10; // Prevent infinite loops
            int iteration = 0;

            while (iteration < maxIterations)
            {
                iteration++;
                _logger.LogDebug("Anthropic API call iteration {Iteration}", iteration);

                // Build API request
                var apiRequest = new
                {
                    model = modelProp,
                    max_tokens = maxTokensProp ?? _maxTokens,
                    temperature = temperatureProp ?? _temperature,
                    system = systemProp,
                    messages = messages,
                    tools = toolsProp
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(apiRequest),
                    Encoding.UTF8,
                    "application/json");

                _logger.LogDebug("Sending request to Anthropic API: {Request}",
                    JsonSerializer.Serialize(apiRequest, new JsonSerializerOptions { WriteIndented = false }));

                var response = await httpClient.PostAsync(
                    "https://api.anthropic.com/v1/messages",
                    content,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogDebug("Anthropic API response: {Response}", responseBody);

                var responseJson = JsonDocument.Parse(responseBody);
                var contentArray = responseJson.RootElement.GetProperty("content");

                // Check for tool calls
                var toolUses = new List<(string id, string name, string input)>();
                var textResponses = new StringBuilder();

                foreach (var contentItem in contentArray.EnumerateArray())
                {
                    var type = contentItem.GetProperty("type").GetString();

                    if (type == "text" && contentItem.TryGetProperty("text", out var textElement))
                    {
                        textResponses.Append(textElement.GetString());
                    }
                    else if (type == "tool_use")
                    {
                        var toolId = contentItem.GetProperty("id").GetString() ?? string.Empty;
                        var toolName = contentItem.GetProperty("name").GetString() ?? string.Empty;
                        var toolInput = contentItem.GetProperty("input").ToString();

                        toolUses.Add((toolId, toolName, toolInput));
                        _logger.LogInformation("Tool call detected: {ToolName} with input: {Input}", toolName, toolInput);
                    }
                }

                // If no tool calls, we have the final response
                if (toolUses.Count == 0)
                {
                    _logger.LogDebug("No tool calls in response, returning final text");
                    return textResponses.ToString();
                }

                // Execute tool calls
                if (kernel == null)
                {
                    _logger.LogWarning("Tool calls requested but no kernel provided");
                    return textResponses.ToString();
                }

                // Add assistant message with tool uses to conversation
                var assistantContent = new List<object>();
                foreach (var contentItem in contentArray.EnumerateArray())
                {
                    assistantContent.Add(JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(contentItem.GetRawText())!);
                }

                messages.Add(new
                {
                    role = "assistant",
                    content = assistantContent
                });

                // Execute tools and build tool result message
                var toolResults = new List<object>();

                foreach (var (toolId, toolName, toolInput) in toolUses)
                {
                    try
                    {
                        var toolResult = await ExecuteToolAsync(kernel, toolName, toolInput, cancellationToken);

                        toolResults.Add(new
                        {
                            type = "tool_result",
                            tool_use_id = toolId,
                            content = toolResult
                        });

                        _logger.LogInformation("Tool {ToolName} executed successfully with result: {Result}",
                            toolName, toolResult);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing tool {ToolName}", toolName);

                        toolResults.Add(new
                        {
                            type = "tool_result",
                            tool_use_id = toolId,
                            content = $"Error executing tool: {ex.Message}",
                            is_error = true
                        });
                    }
                }

                // Add tool results as user message
                messages.Add(new
                {
                    role = "user",
                    content = toolResults
                });
            }

            _logger.LogWarning("Maximum tool execution iterations ({MaxIterations}) reached", maxIterations);
            return "Maximum tool execution iterations reached. Please try again with a simpler query.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Anthropic API");
            throw new InvalidOperationException($"Failed to call Anthropic API: {ex.Message}", ex);
        }
    }

    private async Task<string> ExecuteToolAsync(Kernel kernel, string toolName, string toolInputJson, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Executing tool: {ToolName} with input: {Input}", toolName, toolInputJson);

        // Parse tool name (format: PluginName_FunctionName)
        var parts = toolName.Split('_', 2);
        if (parts.Length != 2)
        {
            throw new InvalidOperationException($"Invalid tool name format: {toolName}. Expected: PluginName_FunctionName");
        }

        var pluginName = parts[0];
        var functionName = parts[1];

        // Get the function from kernel
        var function = kernel.Plugins.GetFunction(pluginName, functionName);

        // Parse input JSON to kernel arguments
        var inputDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(toolInputJson);
        var arguments = new KernelArguments();

        if (inputDict != null)
        {
            foreach (var (key, value) in inputDict)
            {
                // Convert JsonElement to appropriate type
                var stringValue = value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString(),
                    JsonValueKind.Number => value.ToString(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => null,
                    _ => value.GetRawText()
                };

                arguments[key] = stringValue;
            }
        }

        // Invoke the function
        var result = await function.InvokeAsync(kernel, arguments, cancellationToken);

        // Serialize the result to JSON if it's a complex object, otherwise return as string
        var resultValue = result.GetValue<object>();
        if (resultValue == null)
        {
            return string.Empty;
        }

        // If it's already a string, return it directly
        if (resultValue is string strValue)
        {
            return strValue;
        }

        // Otherwise, serialize complex objects to JSON
        return System.Text.Json.JsonSerializer.Serialize(resultValue, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = false
        });
    }
}
