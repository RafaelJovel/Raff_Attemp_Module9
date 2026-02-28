namespace FeatureAssessment.Core.Models;

/// <summary>
/// Represents basic feature information from list_all_features tool
/// </summary>
public record FeatureInfo(
    string FeatureId,
    string JiraKey,
    string Summary,
    string CurrentStage);
