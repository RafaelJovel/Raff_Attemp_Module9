namespace FeatureAssessment.Core.Models;

/// <summary>
/// Result of a feature lookup operation by the Feature Lookup Agent.
/// </summary>
public record FeatureLookupResult
{
    /// <summary>
    /// The JIRA key of the identified feature (e.g., "PLAT-1523").
    /// </summary>
    public string? FeatureKey { get; init; }

    /// <summary>
    /// The feature identifier (e.g., "feature1").
    /// </summary>
    public string? FeatureId { get; init; }

    /// <summary>
    /// The target environment extracted from the query ("UAT" or "Production").
    /// Defaults to "UAT" if not specified in the query.
    /// </summary>
    public string TargetEnvironment { get; init; } = "UAT";

    /// <summary>
    /// Indicates whether the lookup was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Error message if the lookup failed (e.g., "Feature not found").
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Additional context or information from the agent's response.
    /// </summary>
    public string? AdditionalContext { get; init; }
}
