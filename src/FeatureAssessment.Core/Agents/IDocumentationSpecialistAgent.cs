using System.Threading.Tasks;

namespace FeatureAssessment.Core.Agents
{
    public interface IDocumentationSpecialistAgent
    {
        Task<string> AssessAsync(string query, string featureId);
    }
}
