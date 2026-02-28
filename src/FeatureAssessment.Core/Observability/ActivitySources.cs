using System.Diagnostics;

namespace FeatureAssessment.Core.Observability;

/// <summary>
/// Centralized ActivitySource definitions for distributed tracing.
/// Each component area has its own ActivitySource for better trace organization.
/// </summary>
public static class ActivitySources
{
    /// <summary>
    /// Service name used across all ActivitySources.
    /// </summary>
    public const string ServiceName = "FeatureAssessment";

    /// <summary>
    /// Version for all ActivitySources (matches assembly version).
    /// </summary>
    public const string Version = "1.0.0";

    /// <summary>
    /// ActivitySource for Feature Lookup Agent operations.
    /// Used for tracing feature identification and query processing.
    /// </summary>
    public static readonly ActivitySource FeatureLookup = new(
        $"{ServiceName}.FeatureLookup",
        Version
    );

    /// <summary>
    /// ActivitySource for tool invocations (file system access, data parsing).
    /// Used for tracing tool calls made by agents.
    /// </summary>
    public static readonly ActivitySource Tools = new(
        $"{ServiceName}.Tools",
        Version
    );

    /// <summary>
    /// ActivitySource for coordinator agent operations.
    /// Used for tracing the main assessment workflow and decision-making.
    /// </summary>
    public static readonly ActivitySource Coordinator = new(
        $"{ServiceName}.Coordinator",
        Version
    );

    /// <summary>
    /// ActivitySource for specialist agent operations (documentation, metrics, reviews).
    /// Used for tracing specialist agent consultations.
    /// </summary>
    public static readonly ActivitySource Specialists = new(
        $"{ServiceName}.Specialists",
        Version
    );

    /// <summary>
    /// ActivitySource specifically for the Documentation Specialist agent.
    /// This allows traces to filter on documentation assessments separately.
    /// </summary>
    public static readonly ActivitySource DocumentationSpecialist = new(
        $"{ServiceName}.Specialists.Documentation",
        Version
    );
}
