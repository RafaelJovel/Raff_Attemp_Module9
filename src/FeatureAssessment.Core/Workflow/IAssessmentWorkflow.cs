using FeatureAssessment.Core.Models;

namespace FeatureAssessment.Core.Workflow;

/// <summary>
/// Orchestrates the full feature readiness assessment pipeline:
/// Feature Lookup → Coordinator assessment.
/// </summary>
public interface IAssessmentWorkflow
{
    /// <summary>
    /// Runs the complete assessment pipeline for a natural language query.
    /// </summary>
    /// <param name="query">Natural language query, e.g. "Is PLAT-1523 ready for production?"</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Final <see cref="AssessmentState"/> after all pipeline stages have completed.
    /// <c>CurrentStage</c> will be "coordinator_completed" on success or "error" on failure.
    /// </returns>
    Task<AssessmentState> RunAsync(
        string query,
        CancellationToken cancellationToken = default);
}
