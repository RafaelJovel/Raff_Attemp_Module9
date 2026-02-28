using FeatureAssessment.Core.Agents;
using FeatureAssessment.Core.Tools;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FeatureAssessment.Core.Tests.Integration;

[TestClass]
public class ConsultDocumentationSpecialistToolIntegrationTests
{
    [TestMethod]
    public async Task ConsultDocumentationSpecialistTool_ReturnsFullAssessment_ForFeature1()
    {
        // Arrange - use real tools/agent so we hit sample data
        var docTools = new DocumentationTools(new NullLogger<DocumentationTools>());
        var docAgent = new DocumentationSpecialistAgent(docTools, new NullLogger<DocumentationSpecialistAgent>());
        var tool = new ConsultDocumentationSpecialistTool(docAgent);

        // Act
        var result = await tool.ConsultDocumentationSpecialistAsync("Assess all documentation", "feature1");

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("Assessment for feature: feature1");
        result.Should().Contain("Planning documents:");
        result.Should().Contain("Document excerpts / presence:");
        // at least one of the known planning docs for feature1 should be mentioned
        result.Should().Contain("USER_STORY.md");
    }
}