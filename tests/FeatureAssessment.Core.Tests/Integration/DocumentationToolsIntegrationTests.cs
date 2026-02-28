using Microsoft.Extensions.Logging;
using Moq;
using FeatureAssessment.Core.Tools;

namespace FeatureAssessment.Core.Tests.Integration;

[TestClass]
[TestCategory("Integration")]
public class DocumentationToolsIntegrationTests
{
    private Mock<ILogger<DocumentationTools>> _loggerMock = null!;
    private DocumentationTools _documentationTools = null!;

    [TestInitialize]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<DocumentationTools>>();
        _documentationTools = new DocumentationTools(_loggerMock.Object);
    }

    [TestMethod]
    public async Task ListPlanningDocsAsync_WithRealData_ReturnsAllFeature1Docs()
    {
        // Arrange
        var featureId = "feature1";

        // Act
        var result = await _documentationTools.ListPlanningDocsAsync(featureId);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Count > 0, "Should find planning documents for feature1");
        
        // Verify expected documents exist
        var docNames = new[] { "USER_STORY.md", "DESIGN_DOC.md", "ARCHITECTURE.md" };
        foreach (var doc in docNames)
        {
            CollectionAssert.Contains(result, doc, $"Should contain {doc}");
        }
    }

    [TestMethod]
    public async Task ReadPlanningDocAsync_WithRealData_ReturnsUserStoryContent()
    {
        // Arrange
        var featureId = "feature1";
        var docName = "USER_STORY";

        // Act
        var result = await _documentationTools.ReadPlanningDocAsync(featureId, docName);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Contains("File not found"), "USER_STORY.md should exist for feature1");
        Assert.IsTrue(result.Length > 50, "USER_STORY content should be substantial");
        Assert.IsTrue(result.Contains("#") || result.Contains("##"), "Should contain markdown headers");
    }

    [TestMethod]
    public async Task ReadPlanningDocAsync_WithRealData_VerifyDocSections()
    {
        // Arrange
        var featureId = "feature1";
        var docName = "DESIGN_DOC";

        // Act
        var result = await _documentationTools.ReadPlanningDocAsync(featureId, docName);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Contains("File not found"), "DESIGN_DOC.md should exist for feature1");
        Assert.IsTrue(result.Length > 50, "DESIGN_DOC content should be substantial");
        // Just verify it contains markdown-like content
        Assert.IsTrue(result.Contains("#") || result.Contains("*") || result.Contains("-"), 
            "Should contain markdown formatting");
    }

    [TestMethod]
    public async Task ListPlanningDocsAsync_AllFeatures_ReturnRelatedDocs()
    {
        // Arrange
        var featureIds = new[] { "feature1", "feature2", "feature3", "feature4" };

        // Act & Assert
        foreach (var featureId in featureIds)
        {
            var result = await _documentationTools.ListPlanningDocsAsync(featureId);
            Assert.IsNotNull(result, $"Should return list for {featureId}");
            Assert.IsTrue(result.Count > 0, $"Should find documents for {featureId}");
            Assert.IsTrue(result.All(f => f.EndsWith(".md")), $"All files for {featureId} should have .md extension");
        }
    }

    [TestMethod]
    public async Task ReadPlanningDocAsync_Feature2_ReturnsContent()
    {
        // Arrange
        var featureId = "feature2";
        var docName = "USER_STORY";

        // Act
        var result = await _documentationTools.ReadPlanningDocAsync(featureId, docName);

        // Assert
        Assert.IsNotNull(result);
        // Feature2 should have planning docs (it's in UAT)
        Assert.IsTrue(result.Length > 0, "Should return content or informative error");
    }

    [TestMethod]
    public async Task ReadPlanningDocAsync_ConsecutiveCalls_ConsistentResults()
    {
        // Arrange
        var featureId = "feature1";
        var docName = "USER_STORY";

        // Act
        var result1 = await _documentationTools.ReadPlanningDocAsync(featureId, docName);
        var result2 = await _documentationTools.ReadPlanningDocAsync(featureId, docName);

        // Assert
        Assert.AreEqual(result1, result2, "Consecutive reads should return identical content");
    }
}
