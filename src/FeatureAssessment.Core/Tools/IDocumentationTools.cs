namespace FeatureAssessment.Core.Tools;

/// <summary>
/// Interface for accessing and reading feature planning documentation.
/// </summary>
public interface IDocumentationTools
{
    /// <summary>
    /// Lists all available planning documents for a feature.
    /// </summary>
    /// <param name="featureId">The feature identifier (e.g., "feature1")</param>
    /// <returns>List of markdown file names found in the planning directory, ordered alphabetically. Empty list if directory doesn't exist.</returns>
    Task<List<string>> ListPlanningDocsAsync(string featureId);

    /// <summary>
    /// Reads the content of a specific planning document.
    /// </summary>
    /// <param name="featureId">The feature identifier (e.g., "feature1")</param>
    /// <param name="docName">The document name with or without .md extension (e.g., "USER_STORY" or "USER_STORY.md")</param>
    /// <returns>Full markdown content of the document, or an error message if file not found</returns>
    Task<string> ReadPlanningDocAsync(string featureId, string docName);
}
