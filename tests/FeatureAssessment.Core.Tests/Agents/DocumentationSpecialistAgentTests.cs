using System.Collections.Generic;
using System.Threading.Tasks;
using FeatureAssessment.Core.Agents;
using FeatureAssessment.Core.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FeatureAssessment.Core.Tests.Agents
{
    [TestClass]
    public class DocumentationSpecialistAgentTests
    {
        private class FakeDocs : IDocumentationTools
        {
            public Task<string> ReadPlanningDocAsync(string featureId, string docName)
            {
                return Task.FromResult("# Title\n\nContent\n");
            }

            public Task<List<string>> ListPlanningDocsAsync(string featureId)
            {
                return Task.FromResult(new List<string> { "USER_STORY.md", "DESIGN_DOC.md" });
            }
        }

        [TestMethod]
        public async Task AssessAsync_ReturnsListAndHeadings()
        {
            var docs = new FakeDocs();
            var agent = new DocumentationSpecialistAgent(docs, new NullLogger<DocumentationSpecialistAgent>());

            var result = await agent.AssessAsync("Assess USER_STORY completeness", "feature1");

            Assert.IsTrue(result.Contains("Planning documents:"));
            Assert.IsTrue(result.Contains("USER_STORY.md"));
            Assert.IsTrue(result.Contains("headings" ) || result.Contains("headings found"));
        }
    }
}
