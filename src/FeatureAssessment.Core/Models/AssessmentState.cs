namespace FeatureAssessment.Core.Models;

/// <summary>
/// Represents the current state of a feature readiness assessment.
/// Immutable record for thread-safe state management.
/// </summary>
public record AssessmentState
{
    /// <summary>
    /// The feature identifier (e.g., "feature1").
    /// Null if feature has not been identified yet.
    /// </summary>
    public string? FeatureId { get; init; }

    /// <summary>
    /// The JIRA feature key (e.g., "PLAT-1523").
    /// Null if feature has not been identified yet.
    /// </summary>
    public string? FeatureKey { get; init; }

    /// <summary>
    /// The current stage of the assessment workflow.
    /// Possible values: "feature_lookup", "coordinator", "completed", "error"
    /// </summary>
    public string? CurrentStage { get; init; }

    /// <summary>
    /// The target deployment environment for the assessment.
    /// Values: "UAT" or "Production"
    /// </summary>
    public string? TargetEnvironment { get; init; }

    /// <summary>
    /// Indicates whether the feature has been successfully identified.
    /// </summary>
    public bool IsFeatureIdentified { get; init; }

    /// <summary>
    /// Error message if feature lookup or assessment failed.
    /// Null if no errors occurred.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The coordinator agent's response text.
    /// Contains the preliminary assessment or final decision reasoning.
    /// Null if coordinator has not yet run.
    /// </summary>
    public string? CoordinatorResponse { get; init; }

    /// <summary>
    /// Additional metadata for the assessment.
    /// Allows extension without breaking changes.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Creates initial state from a FeatureLookupResult.
    /// </summary>
    /// <param name="lookupResult">The result from feature lookup agent.</param>
    /// <returns>New AssessmentState initialized with lookup results.</returns>
    public static AssessmentState FromFeatureLookupResult(FeatureLookupResult lookupResult)
    {
        return new AssessmentState
        {
            FeatureId = lookupResult.FeatureId,
            FeatureKey = lookupResult.FeatureKey,
            CurrentStage = lookupResult.IsSuccess ? "feature_lookup_completed" : "error",
            TargetEnvironment = lookupResult.TargetEnvironment,
            IsFeatureIdentified = lookupResult.IsSuccess,
            ErrorMessage = lookupResult.ErrorMessage
        };
    }

    /// <summary>
    /// Updates the current stage of the assessment.
    /// </summary>
    /// <param name="newStage">The new stage name.</param>
    /// <returns>New AssessmentState with updated stage.</returns>
    public AssessmentState WithStage(string newStage)
    {
        return this with { CurrentStage = newStage };
    }

    /// <summary>
    /// Sets the coordinator agent's response.
    /// </summary>
    /// <param name="response">The coordinator's response text.</param>
    /// <returns>New AssessmentState with coordinator response set.</returns>
    public AssessmentState WithCoordinatorResponse(string response)
    {
        return this with { CoordinatorResponse = response };
    }

    /// <summary>
    /// Adds or updates metadata entry.
    /// </summary>
    /// <param name="key">Metadata key.</param>
    /// <param name="value">Metadata value.</param>
    /// <returns>New AssessmentState with updated metadata.</returns>
    public AssessmentState WithMetadata(string key, object value)
    {
        var newMetadata = new Dictionary<string, object>(Metadata)
        {
            [key] = value
        };
        return this with { Metadata = newMetadata };
    }
}
