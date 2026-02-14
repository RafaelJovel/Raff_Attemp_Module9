using System.Text.Json.Serialization;

namespace FeatureAssessment.Core.Models;

/// <summary>
/// Represents complete JIRA feature metadata from get_feature_metadata tool
/// </summary>
public record FeatureMetadata(
    string FeatureId,
    string Key,
    JiraFields Fields);

public record JiraFields(
    string Summary,
    [property: JsonPropertyName("issuetype")] JiraIssueType IssueType,
    JiraProject Project,
    JiraStatus Status,
    JiraPriority? Priority,
    JiraUser? Assignee,
    JiraUser? Reporter,
    string Created,
    string Updated,
    List<string>? Labels,
    List<JiraComponent>? Components);

public record JiraIssueType(
    string Id,
    string Name,
    bool Subtask);

public record JiraProject(
    string Id,
    string Key,
    string Name);

public record JiraStatus(
    string Id,
    string Name,
    string? Description,
    JiraStatusCategory StatusCategory);

public record JiraStatusCategory(
    int Id,
    string Key,
    string ColorName,
    string Name);

public record JiraPriority(
    string Id,
    string Name);

public record JiraUser(
    string AccountId,
    string DisplayName,
    string? EmailAddress,
    bool Active);

public record JiraComponent(
    string Id,
    string Name);
