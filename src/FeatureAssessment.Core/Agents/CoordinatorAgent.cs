using System.Diagnostics;
using FeatureAssessment.Core.Clients;
using FeatureAssessment.Core.Models;
using FeatureAssessment.Core.Observability;
using FeatureAssessment.Core.Prompts;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FeatureAssessment.Core.Agents;

/// <summary>
/// Coordinator agent that orchestrates the feature readiness assessment.
/// Receives feature context from the Feature Lookup Agent, consults specialist agents
/// (when available), and produces GO/NO_GO/GO_WITH_RISKS decisions.
/// </summary>
public class CoordinatorAgent : ICoordinatorAgent
{
    private readonly IKernelFactory _kernelFactory;
    private readonly ILogger<CoordinatorAgent> _logger;

    public CoordinatorAgent(
        IKernelFactory kernelFactory,
        ILogger<CoordinatorAgent> logger)
    {
        _kernelFactory = kernelFactory ?? throw new ArgumentNullException(nameof(kernelFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AssessmentState> AssessAsync(
        AssessmentState state,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        using var activity = ActivitySources.Coordinator.StartActivity("CoordinatorAgent.Assess");
        activity?.SetTag("feature_key", state.FeatureKey);
        activity?.SetTag("target_environment", state.TargetEnvironment);
        activity?.SetTag("service.name", ActivitySources.ServiceName);

        if (!state.IsFeatureIdentified)
        {
            _logger.LogWarning("Cannot assess: feature was not identified. ErrorMessage: {Error}", state.ErrorMessage);
            activity?.SetStatus(ActivityStatusCode.Error, "Feature was not identified");
            return state
                .WithStage("error")
                .WithCoordinatorResponse("Cannot assess feature readiness: the feature was not identified. Please provide a valid feature reference.");
        }

        _logger.LogInformation(
            "Starting coordinator assessment for feature {FeatureKey} targeting {TargetEnvironment}",
            state.FeatureKey,
            state.TargetEnvironment);

        try
        {
            var kernel = _kernelFactory.CreateKernel();
            var userMessage = BuildUserMessage(state);
            var response = await ExecuteAgentAsync(kernel, userMessage, cancellationToken);

            activity?.SetTag("is_success", "True");

            _logger.LogInformation(
                "Coordinator assessment completed for feature {FeatureKey}",
                state.FeatureKey);

            return state
                .WithStage("coordinator_completed")
                .WithCoordinatorResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during coordinator assessment for feature: {FeatureKey}", state.FeatureKey);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);
            activity?.SetTag("exception.stacktrace", ex.StackTrace);

            return state
                .WithStage("error")
                .WithCoordinatorResponse($"Assessment failed: {ex.Message}");
        }
    }

    private static string BuildUserMessage(AssessmentState state)
    {
        return $"""
            Please assess the deployment readiness of the following feature:

            Feature Key: {state.FeatureKey ?? "Unknown"}
            Feature ID: {state.FeatureId ?? "Unknown"}
            Target Environment: {state.TargetEnvironment ?? "Unknown"}
            Current Stage: {state.CurrentStage ?? "Unknown"}

            Apply the {state.TargetEnvironment ?? "UAT"} deployment criteria from your decision framework.
            Gather evidence from specialist agents and make your assessment.
            """;
    }

    private async Task<string> ExecuteAgentAsync(
        Kernel kernel,
        string userMessage,
        CancellationToken cancellationToken)
    {
        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

        var chatHistory = new ChatHistory(CoordinatorSystemPrompt.Prompt);
        chatHistory.AddUserMessage(userMessage);

        // No FunctionChoiceBehavior.Auto() — coordinator has no tools in this implementation.
        // Specialist consultation tools will be added in a future work item.
        var executionSettings = new PromptExecutionSettings();

        _logger.LogDebug("Executing coordinator agent");

        var response = await chatCompletion.GetChatMessageContentAsync(
            chatHistory,
            executionSettings,
            kernel,
            cancellationToken);

        var responseText = response.Content ?? string.Empty;

        _logger.LogDebug("Coordinator response: {Response}", responseText);

        return responseText;
    }
}
