using FeatureAssessment.Core.Models;

namespace FeatureAssessment.Core.Tools;

/// <summary>
/// Feature lookup tools for discovering and retrieving feature metadata
/// </summary>
public interface IFeatureLookupTools
{
    /// <summary>
    /// Lists all features found in the data/incoming directory
    /// </summary>
    /// <returns>List of feature information</returns>
    Task<IReadOnlyList<FeatureInfo>> ListAllFeaturesAsync();

    /// <summary>
    /// Retrieves full feature metadata by feature identifier
    /// </summary>
    /// <param name="featureIdentifier">JIRA key (e.g. "PLAT-1523"), feature ID (e.g. "feature1"), or feature name</param>
    /// <returns>Feature metadata</returns>
    /// <exception cref="FeatureNotFoundException">When feature is not found</exception>
    Task<FeatureMetadata> GetFeatureMetadataAsync(string featureIdentifier);
}

/// <summary>
/// Exception thrown when a feature cannot be found
/// </summary>
public class FeatureNotFoundException : Exception
{
    public FeatureNotFoundException(string featureIdentifier)
        : base($"Feature not found: {featureIdentifier}")
    {
        FeatureIdentifier = featureIdentifier;
    }

    public string FeatureIdentifier { get; }
}
