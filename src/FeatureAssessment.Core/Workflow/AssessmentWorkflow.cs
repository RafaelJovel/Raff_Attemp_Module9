using System.Diagnostics;
using FeatureAssessment.Core.Agents;
using FeatureAssessment.Core.Models;
using FeatureAssessment.Core.Observability;
using Microsoft.Extensions.Logging;

namespace FeatureAssessment.Core.Workflow;

/// <summary>
/// Orchestrates the feature readiness assessment pipeline:
/// Feature Lookup Agent → Coordinator Agent.
///
/// Creates a root span so both agent activities appear as children
/// of a single unified trace.
/// </summary>
public class AssessmentWorkflow : IAssessmentWorkflow
{
    private readonly IFeatureLookupAgent _featureLookupAgent;
    private readonly ICoordinatorAgent _coordinatorAgent;
    private readonly ILogger<AssessmentWorkflow> _logger;

    public AssessmentWorkflow(
        IFeatureLookupAgent featureLookupAgent,
        ICoordinatorAgent coordinatorAgent,
        ILogger<AssessmentWorkflow> logger)
    {
        _featureLookupAgent = featureLookupAgent ?? throw new ArgumentNullException(nameof(featureLookupAgent));
        _coordinatorAgent = coordinatorAgent ?? throw new ArgumentNullException(nameof(coordinatorAgent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AssessmentState> RunAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or whitespace.", nameof(query));

        // Root span — both child agents' activities will nest under this
        using var rootActivity = ActivitySources.Coordinator.StartActivity("AssessmentWorkflow.Run");
        rootActivity?.SetTag("query", query);
        rootActivity?.SetTag("service.name", ActivitySources.ServiceName);

        _logger.LogInformation("Starting assessment workflow for query: {Query}", query);

        try
        {
            // Stage 1: Feature Lookup
            _logger.LogInformation("Stage 1: Feature Lookup");
            var lookupResult = await _featureLookupAgent.LookupFeatureAsync(query, cancellationToken);
            var state = AssessmentState.FromFeatureLookupResult(lookupResult);

            rootActivity?.SetTag("feature_key", state.FeatureKey);
            rootActivity?.SetTag("target_environment", state.TargetEnvironment);

            if (!state.IsFeatureIdentified)
            {
                _logger.LogWarning(
                    "Feature lookup failed — skipping coordinator. Error: {Error}",
                    state.ErrorMessage);
                rootActivity?.SetStatus(ActivityStatusCode.Error, state.ErrorMessage);
                return state;
            }

            // Stage 2: Coordinator Assessment
            _logger.LogInformation(
                "Stage 2: Coordinator Assessment for {FeatureKey} → {TargetEnvironment}",
                state.FeatureKey,
                state.TargetEnvironment);

            var finalState = await _coordinatorAgent.AssessAsync(state, cancellationToken);

            if (finalState.CurrentStage == "error")
                rootActivity?.SetStatus(ActivityStatusCode.Error, finalState.CoordinatorResponse);

            _logger.LogInformation(
                "Assessment workflow completed. Stage: {Stage}",
                finalState.CurrentStage);

            return finalState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in assessment workflow for query: {Query}", query);
            rootActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            rootActivity?.SetTag("exception.type", ex.GetType().FullName);
            rootActivity?.SetTag("exception.message", ex.Message);

            return new AssessmentState
            {
                CurrentStage = "error",
                ErrorMessage = $"Workflow failed: {ex.Message}"
            };
        }
    }
}
