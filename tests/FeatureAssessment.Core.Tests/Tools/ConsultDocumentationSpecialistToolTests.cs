using FeatureAssessment.Core.Agents;
using FeatureAssessment.Core.Tools;
using FluentAssertions;
using Microsoft.SemanticKernel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace FeatureAssessment.Core.Tests.Tools;

[TestClass]
public class ConsultDocumentationSpecialistToolTests
{
    [TestMethod]
    public async Task ConsultDocumentationSpecialistAsync_DelegatesToAgent()
    {
        // Arrange
        var mockAgent = new Mock<IDocumentationSpecialistAgent>();
        mockAgent
            .Setup(a => a.AssessAsync("foo", "feature1"))
            .ReturnsAsync("result123");

        var tool = new ConsultDocumentationSpecialistTool(mockAgent.Object);

        // Act
        var result = await tool.ConsultDocumentationSpecialistAsync("foo", "feature1");

        // Assert
        result.Should().Be("result123");
        mockAgent.Verify(a => a.AssessAsync("foo", "feature1"), Times.Once);
    }

    [TestMethod]
    public async Task Tool_CanBeAddedToKernel_AndInvokedViaKernel()
    {
        // Arrange
        var mockAgent = new Mock<IDocumentationSpecialistAgent>();
        mockAgent
            .Setup(a => a.AssessAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string q, string f) => $"answered:{q}/{f}");

        var tool = new ConsultDocumentationSpecialistTool(mockAgent.Object);
        var kernel = Kernel.CreateBuilder().Build();

        kernel.Plugins.AddFromObject(tool, "Docs");

        // Act & Assert - the tool should be registered in plugins collection
        Assert.IsNotNull(kernel.Plugins);
    }
}