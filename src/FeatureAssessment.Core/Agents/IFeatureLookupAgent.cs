using FeatureAssessment.Core.Models;

namespace FeatureAssessment.Core.Agents;

/// <summary>
/// Agent that translates natural language queries into feature metadata.
/// </summary>
public interface IFeatureLookupAgent
{
    /// <summary>
    /// Processes a natural language query to identify a feature and target environment.
    /// </summary>
    /// <param name="query">The natural language query about feature readiness (e.g., "Is PLAT-1523 ready for production?").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the identified feature and target environment.</returns>
    Task<FeatureLookupResult> LookupFeatureAsync(string query, CancellationToken cancellationToken = default);
}
