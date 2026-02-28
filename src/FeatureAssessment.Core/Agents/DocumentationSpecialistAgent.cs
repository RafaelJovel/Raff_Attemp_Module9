using System;
using System.Text;
using System.Threading.Tasks;
using FeatureAssessment.Core.Tools;
using Microsoft.Extensions.Logging;

namespace FeatureAssessment.Core.Agents
{
    public class DocumentationSpecialistAgent : IDocumentationSpecialistAgent
    {
        private readonly IDocumentationTools _docTools;
        private readonly ILogger<DocumentationSpecialistAgent> _logger;

        public DocumentationSpecialistAgent(IDocumentationTools docTools, ILogger<DocumentationSpecialistAgent> logger)
        {
            _docTools = docTools ?? throw new ArgumentNullException(nameof(docTools));
            _logger = logger;
        }

        public async Task<string> AssessAsync(string query, string featureId)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Assessment for feature: {featureId}");
            sb.AppendLine($"Query: {query}");

            try
            {
                var docs = await _docTools.ListPlanningDocsAsync(featureId).ConfigureAwait(false);
                if (docs == null || docs.Count == 0)
                {
                    sb.AppendLine("Planning documents: none found.");
                    return sb.ToString();
                }

                sb.AppendLine("Planning documents:");
                foreach (var d in docs)
                {
                    sb.AppendLine(" - " + d);
                }

                sb.AppendLine();
                sb.AppendLine("Document excerpts / presence:");
                foreach (var d in docs)
                {
                    var name = d.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ? d[..^3] : d;
                    var content = await _docTools.ReadPlanningDocAsync(featureId, name).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        sb.AppendLine($" - {d}: empty or not found");
                    }
                    else
                    {
                        // Report presence of top-level headings as factual indicators
                        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        var headings = 0;
                        foreach (var l in lines)
                        {
                            if (l.StartsWith("#")) headings++;
                        }
                        sb.AppendLine($" - {d}: {Math.Min(headings, 10)} headings found");
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during documentation assessment for {FeatureId}", featureId);
                sb.AppendLine("Error: " + ex.Message);
                return sb.ToString();
            }
        }
    }
}
