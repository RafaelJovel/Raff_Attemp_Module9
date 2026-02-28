using Microsoft.Extensions.Logging;

namespace FeatureAssessment.Core.Tools;

/// <summary>
/// Implementation of documentation access tools for feature planning documents.
/// </summary>
public class DocumentationTools : IDocumentationTools
{
    private const string PlanningSubdir = "planning";
    private const string MarkdownExtension = ".md";

    private readonly ILogger<DocumentationTools> _logger;
    private readonly string _baseDataPath;

    private static string GetDataPath()
    {
        // Navigate from AppContext.BaseDirectory (usually bin/Debug/net10.0/) up to solution root
        // Structure: bin/Debug/net10.0/ → need to go up and find data/incoming
        var baseDir = AppContext.BaseDirectory;
        var dataPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "data", "incoming"));

        // If not found at that level, try relative to current directory (for running from sln root)
        if (!Directory.Exists(dataPath))
        {
            dataPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "data", "incoming"));
        }

        return dataPath;
    }

    public DocumentationTools(ILogger<DocumentationTools> logger)
    {
        _logger = logger;
        _baseDataPath = GetDataPath();
    }

    /// <summary>
    /// Lists all available planning documents for a feature.
    /// </summary>
    public async Task<List<string>> ListPlanningDocsAsync(string featureId)
    {
        return await Task.Run(() =>
        {
            try
            {
                var planningDir = Path.Combine(_baseDataPath, featureId, PlanningSubdir);

                if (!Directory.Exists(planningDir))
                {
                    _logger.LogInformation("Planning directory not found for feature {FeatureId}: {Path}",
                        featureId, planningDir);
                    return [];
                }

                var files = Directory.GetFiles(planningDir, $"*{MarkdownExtension}")
                    .Select(f => Path.GetFileName(f))
                    .OrderBy(f => f)
                    .ToList();

                _logger.LogInformation("Found {Count} planning documents for feature {FeatureId}",
                    files.Count, featureId);

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing planning documents for feature {FeatureId}", featureId);
                return [];
            }
        });
    }

    /// <summary>
    /// Reads the content of a specific planning document.
    /// </summary>
    public async Task<string> ReadPlanningDocAsync(string featureId, string docName)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Ensure .md extension
                var fileName = docName.EndsWith(MarkdownExtension) ? docName : $"{docName}{MarkdownExtension}";
                var filePath = Path.Combine(_baseDataPath, featureId, PlanningSubdir, fileName);

                if (!File.Exists(filePath))
                {
                    var errorMessage = $"File not found: {filePath}";
                    _logger.LogWarning("Document not found for feature {FeatureId}: {DocName}",
                        featureId, docName);
                    return errorMessage;
                }

                var content = File.ReadAllText(filePath);
                _logger.LogInformation("Successfully read planning document {DocName} for feature {FeatureId}",
                    docName, featureId);

                return content;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error reading document {docName}: {ex.Message}";
                _logger.LogError(ex, "Error reading planning document {DocName} for feature {FeatureId}",
                    docName, featureId);
                return errorMessage;
            }
        });
    }
}
