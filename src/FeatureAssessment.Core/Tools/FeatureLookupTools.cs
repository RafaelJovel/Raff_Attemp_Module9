using System.ComponentModel;
using System.Text.Json;
using FeatureAssessment.Core.Models;
using Microsoft.SemanticKernel;

namespace FeatureAssessment.Core.Tools;

/// <summary>
/// Implementation of feature lookup tools that reads from local filesystem
/// </summary>
public class FeatureLookupTools : IFeatureLookupTools
{
    private readonly string _dataDirectory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public FeatureLookupTools(string dataDirectory)
    {
        _dataDirectory = dataDirectory ?? throw new ArgumentNullException(nameof(dataDirectory));
    }

    [KernelFunction("list_all_features")]
    [Description("Lists all available features with their basic metadata (ID, JIRA key, summary, status)")]
    public async Task<IReadOnlyList<FeatureInfo>> ListAllFeaturesAsync()
    {
        var incomingDirectory = Path.Combine(_dataDirectory, "incoming");

        if (!Directory.Exists(incomingDirectory))
        {
            return Array.Empty<FeatureInfo>();
        }

        var features = new List<FeatureInfo>();
        var featureDirs = Directory.GetDirectories(incomingDirectory, "feature*");

        foreach (var featureDir in featureDirs)
        {
            var featureId = Path.GetFileName(featureDir);
            var jiraMetadataPath = Path.Combine(featureDir, "jira", "feature_issue.json");

            if (!File.Exists(jiraMetadataPath))
            {
                continue;
            }

            try
            {
                var jsonContent = await File.ReadAllTextAsync(jiraMetadataPath);
                using var doc = JsonDocument.Parse(jsonContent);
                var root = doc.RootElement;

                var key = root.GetProperty("key").GetString() ?? string.Empty;
                var summary = root.GetProperty("fields").GetProperty("summary").GetString() ?? string.Empty;
                var status = root.GetProperty("fields").GetProperty("status").GetProperty("name").GetString() ?? string.Empty;

                features.Add(new FeatureInfo(featureId, key, summary, status));
            }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException)
            {
                // Skip malformed or incomplete JSON files
                continue;
            }
        }

        return features;
    }

    [KernelFunction("get_feature_metadata")]
    [Description("Retrieves detailed metadata for a specific feature by JIRA key (e.g., PLAT-1523), feature ID (e.g., feature1), or feature name")]
    public async Task<FeatureMetadata> GetFeatureMetadataAsync(
        [Description("The feature identifier: JIRA key, feature ID, or feature name")] string featureIdentifier)
    {
        if (string.IsNullOrWhiteSpace(featureIdentifier))
        {
            throw new ArgumentException("Feature identifier cannot be empty", nameof(featureIdentifier));
        }

        var incomingDirectory = Path.Combine(_dataDirectory, "incoming");

        if (!Directory.Exists(incomingDirectory))
        {
            throw new FeatureNotFoundException(featureIdentifier);
        }

        // Try to find feature by different identifiers
        string? featureId = null;

        // Case 1: Direct feature ID (e.g., "feature1")
        var directPath = Path.Combine(incomingDirectory, featureIdentifier);
        if (Directory.Exists(directPath))
        {
            featureId = featureIdentifier;
        }
        else
        {
            // Case 2: JIRA key or fuzzy name match
            var allFeatures = await ListAllFeaturesAsync();

            var matchedFeature = allFeatures.FirstOrDefault(f =>
                f.JiraKey.Equals(featureIdentifier, StringComparison.OrdinalIgnoreCase) ||
                f.Summary.Contains(featureIdentifier, StringComparison.OrdinalIgnoreCase));

            featureId = matchedFeature?.FeatureId;
        }

        if (featureId == null)
        {
            throw new FeatureNotFoundException(featureIdentifier);
        }

        // Read and parse the full JIRA metadata
        var jiraMetadataPath = Path.Combine(incomingDirectory, featureId, "jira", "feature_issue.json");

        if (!File.Exists(jiraMetadataPath))
        {
            throw new FeatureNotFoundException(featureIdentifier);
        }

        try
        {
            var jsonContent = await File.ReadAllTextAsync(jiraMetadataPath);
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            var key = root.GetProperty("key").GetString() ?? string.Empty;
            var fields = ParseFields(root.GetProperty("fields"));

            return new FeatureMetadata(featureId, key, fields);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException)
        {
            throw new FeatureNotFoundException(featureIdentifier);
        }
    }

    private static JiraFields ParseFields(JsonElement fieldsElement)
    {
        var summary = fieldsElement.GetProperty("summary").GetString() ?? string.Empty;

        var issueTypeElement = fieldsElement.GetProperty("issuetype");
        var issueType = new JiraIssueType(
            issueTypeElement.GetProperty("id").GetString() ?? string.Empty,
            issueTypeElement.GetProperty("name").GetString() ?? string.Empty,
            issueTypeElement.GetProperty("subtask").GetBoolean());

        var projectElement = fieldsElement.GetProperty("project");
        var project = new JiraProject(
            projectElement.GetProperty("id").GetString() ?? string.Empty,
            projectElement.GetProperty("key").GetString() ?? string.Empty,
            projectElement.GetProperty("name").GetString() ?? string.Empty);

        var statusElement = fieldsElement.GetProperty("status");
        var statusCategoryElement = statusElement.GetProperty("statusCategory");
        var status = new JiraStatus(
            statusElement.GetProperty("id").GetString() ?? string.Empty,
            statusElement.GetProperty("name").GetString() ?? string.Empty,
            statusElement.TryGetProperty("description", out var descElement) ? descElement.GetString() : null,
            new JiraStatusCategory(
                statusCategoryElement.GetProperty("id").GetInt32(),
                statusCategoryElement.GetProperty("key").GetString() ?? string.Empty,
                statusCategoryElement.GetProperty("colorName").GetString() ?? string.Empty,
                statusCategoryElement.GetProperty("name").GetString() ?? string.Empty));

        JiraPriority? priority = null;
        if (fieldsElement.TryGetProperty("priority", out var priorityElement) && priorityElement.ValueKind != JsonValueKind.Null)
        {
            priority = new JiraPriority(
                priorityElement.GetProperty("id").GetString() ?? string.Empty,
                priorityElement.GetProperty("name").GetString() ?? string.Empty);
        }

        JiraUser? assignee = ParseUser(fieldsElement, "assignee");
        JiraUser? reporter = ParseUser(fieldsElement, "reporter");

        var created = fieldsElement.GetProperty("created").GetString() ?? string.Empty;
        var updated = fieldsElement.GetProperty("updated").GetString() ?? string.Empty;

        List<string>? labels = null;
        if (fieldsElement.TryGetProperty("labels", out var labelsElement) && labelsElement.ValueKind == JsonValueKind.Array)
        {
            labels = labelsElement.EnumerateArray()
                .Select(l => l.GetString() ?? string.Empty)
                .ToList();
        }

        List<JiraComponent>? components = null;
        if (fieldsElement.TryGetProperty("components", out var componentsElement) && componentsElement.ValueKind == JsonValueKind.Array)
        {
            components = componentsElement.EnumerateArray()
                .Select(c => new JiraComponent(
                    c.GetProperty("id").GetString() ?? string.Empty,
                    c.GetProperty("name").GetString() ?? string.Empty))
                .ToList();
        }

        return new JiraFields(
            summary,
            issueType,
            project,
            status,
            priority,
            assignee,
            reporter,
            created,
            updated,
            labels,
            components);
    }

    private static JiraUser? ParseUser(JsonElement fieldsElement, string propertyName)
    {
        if (!fieldsElement.TryGetProperty(propertyName, out var userElement) || userElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return new JiraUser(
            userElement.GetProperty("accountId").GetString() ?? string.Empty,
            userElement.GetProperty("displayName").GetString() ?? string.Empty,
            userElement.TryGetProperty("emailAddress", out var emailElement) ? emailElement.GetString() : null,
            userElement.GetProperty("active").GetBoolean());
    }
}
