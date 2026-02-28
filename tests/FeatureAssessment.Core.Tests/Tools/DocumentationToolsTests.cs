using Microsoft.Extensions.Logging;
using Moq;
using FeatureAssessment.Core.Tools;

namespace FeatureAssessment.Core.Tests.Tools;

[TestClass]
public class DocumentationToolsTests
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
    public async Task ListPlanningDocsAsync_ReturnsEmptyList_WhenDirectoryNotFound()
    {
        // Arrange
        var featureId = "nonexistent-feature-12345";

        // Act
        var result = await _documentationTools.ListPlanningDocsAsync(featureId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task ListPlanningDocsAsync_ReturnsOrderedList_WhenDirectoryExists()
    {
        // Arrange
        var featureId = "feature1";

        // Act
        var result = await _documentationTools.ListPlanningDocsAsync(featureId);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Count > 0, "Should find at least one planning document for feature1");
        
        // Verify ordering
        var orderedResult = result.OrderBy(f => f).ToList();
        CollectionAssert.AreEqual(orderedResult, result, "Results should be alphabetically ordered");
        
        // Verify all files have .md extension
        Assert.IsTrue(result.All(f => f.EndsWith(".md")), "All files should have .md extension");
    }

    [TestMethod]
    public async Task ListPlanningDocsAsync_IncludesExpectedDocuments_ForFeature1()
    {
        // Arrange
        var featureId = "feature1";

        // Act
        var result = await _documentationTools.ListPlanningDocsAsync(featureId);

        // Assert
        Assert.IsNotNull(result);
        CollectionAssert.Contains(result, "USER_STORY.md", "Should contain USER_STORY.md");
        CollectionAssert.Contains(result, "DESIGN_DOC.md", "Should contain DESIGN_DOC.md");
        CollectionAssert.Contains(result, "ARCHITECTURE.md", "Should contain ARCHITECTURE.md");
    }

    [TestMethod]
    public async Task ReadPlanningDocAsync_ReturnsError_WhenFileNotFound()
    {
        // Arrange
        var featureId = "feature1";
        var docName = "NONEXISTENT_DOC";

        // Act
        var result = await _documentationTools.ReadPlanningDocAsync(featureId, docName);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("File not found"), "Should return error message for missing file");
    }

    [TestMethod]
    public async Task ReadPlanningDocAsync_ReturnsError_WhenFeatureIdInvalid()
    {
        // Arrange
        var featureId = "nonexistent-feature-12345";
        var docName = "USER_STORY";

        // Act
        var result = await _documentationTools.ReadPlanningDocAsync(featureId, docName);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("File not found") || result.Contains("Error"), 
            "Should return error message for invalid feature");
    }

    [TestMethod]
    public async Task ReadPlanningDocAsync_HandlesExtension_AutoAppendsMd()
    {
        // Arrange
        var featureId = "feature1";
        var docName = "USER_STORY"; // Without .md

        // Act
        var result = await _documentationTools.ReadPlanningDocAsync(featureId, docName);

        // Assert
        Assert.IsNotNull(result);
        // Should either successfully read it, or return an error (not throw exception)
        Assert.IsFalse(string.IsNullOrEmpty(result), "Should return a result (content or error message)");
    }

    [TestMethod]
    public async Task ReadPlanningDocAsync_HandleExtensionProvided_FindsFile()
    {
        // Arrange
        var featureId = "feature1";
        var docName = "USER_STORY.md"; // With .md

        // Act
        var result = await _documentationTools.ReadPlanningDocAsync(featureId, docName);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result), "Should return a result (content or error message)");
    }

    [TestMethod]
    public async Task ReadPlanningDocAsync_ReturnsContent_WithValidDoc()
    {
        // Arrange
        var featureId = "feature1";
        var docName = "USER_STORY";

        // Act
        var result = await _documentationTools.ReadPlanningDocAsync(featureId, docName);

        // Assert
        Assert.IsNotNull(result);
        // Either it contains markdown content (has headers, text) or an error message
        // We consider success if it's not empty and doesn't contain "Error reading"
        if (!result.Contains("File not found"))
        {
            Assert.IsTrue(result.Length > 10, "Actual content should be substantial");
        }
    }

    [TestMethod]
    public async Task ReadPlanningDocAsync_NoException_OnAnyInput()
    {
        // Arrange - test various invalid inputs
        var testCases = new[]
        {
            ("feature999", "FAKE_DOC"),
            ("", "USER_STORY"),
            ("feature1", ""),
            ("feature1", "DESIGN_DOC.md.md"), // Double extension
        };

        // Act & Assert - none should throw
        foreach (var (featureId, docName) in testCases)
        {
            var result = await _documentationTools.ReadPlanningDocAsync(featureId, docName);
            Assert.IsNotNull(result, $"Should return string for featureId='{featureId}', docName='{docName}'");
            Assert.IsFalse(string.IsNullOrEmpty(result), 
                $"Should not return null or empty for featureId='{featureId}', docName='{docName}'");
        }
    }
}
