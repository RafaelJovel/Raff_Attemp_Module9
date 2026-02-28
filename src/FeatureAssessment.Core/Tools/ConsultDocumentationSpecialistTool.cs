using FeatureAssessment.Core.Agents;
using FeatureAssessment.Core.Observability;
using Microsoft.SemanticKernel;
using System.Diagnostics;

namespace FeatureAssessment.Core.Tools;

/// <summary>
/// Semantic Kernel plugin that allows the coordinator (or other agents) to reach
/// the Documentation Specialist Agent and retrieve an assessment.
/// </summary>
public class ConsultDocumentationSpecialistTool : IConsultDocumentationSpecialistTool
{
    private readonly IDocumentationSpecialistAgent _specialist;

    public ConsultDocumentationSpecialistTool(IDocumentationSpecialistAgent specialist)
    {
        _specialist = specialist ?? throw new ArgumentNullException(nameof(specialist));
    }

    /// <inheritdoc />
    [KernelFunction]
    public async Task<string> ConsultDocumentationSpecialistAsync(string query, string featureId)
    {
        // simple wrapper that delegates to the specialist agent
        using var activity = ActivitySources.DocumentationSpecialist.StartActivity("ConsultDocumentationSpecialistTool.Invoke");
        if (activity is not null)
        {
            activity.SetTag("feature_id", featureId);
            activity.SetTag("query", query);
        }

        return await _specialist.AssessAsync(query, featureId).ConfigureAwait(false);
    }
}