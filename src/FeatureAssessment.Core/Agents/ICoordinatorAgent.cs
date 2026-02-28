using FeatureAssessment.Core.Models;

namespace FeatureAssessment.Core.Agents;

/// <summary>
/// Agent that coordinates the feature readiness assessment.
/// Acts as a supervisor: analyzes feature context, delegates to specialist agents,
/// synthesizes findings, and makes final GO/NO_GO/GO_WITH_RISKS decisions.
/// </summary>
public interface ICoordinatorAgent
{
    /// <summary>
    /// Assesses a feature's deployment readiness based on the provided state.
    /// </summary>
    /// <param name="state">Current assessment state (must have feature identified).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated state with coordinator response and stage set to "coordinator_completed" or "error".</returns>
    Task<AssessmentState> AssessAsync(
        AssessmentState state,
        CancellationToken cancellationToken = default);
}
