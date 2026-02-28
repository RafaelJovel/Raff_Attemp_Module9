namespace FeatureAssessment.Core.Tools;

/// <summary>
/// Defines a Semantic Kernel tool for consulting the documentation specialist agent.
/// </summary>
public interface IConsultDocumentationSpecialistTool
{
    /// <summary>
    /// Ask the documentation specialist to perform an assessment.
    /// </summary>
    /// <param name="query">Natural language query describing what to evaluate.</param>
    /// <param name="featureId">Identifier of the feature to assess.</param>
    /// <returns>Textual assessment produced by the specialist.</returns>
    Task<string> ConsultDocumentationSpecialistAsync(string query, string featureId);
}