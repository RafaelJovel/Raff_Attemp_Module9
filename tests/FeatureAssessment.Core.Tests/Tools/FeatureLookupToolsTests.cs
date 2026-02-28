using FeatureAssessment.Core.Models;
using FeatureAssessment.Core.Tools;
using FluentAssertions;

namespace FeatureAssessment.Core.Tests.Tools;

[TestClass]
public class FeatureLookupToolsTests
{
    private const string TestDataDirectory = "../../../../../data";
    private IFeatureLookupTools _tools = null!;

    [TestInitialize]
    public void Setup()
    {
        _tools = new FeatureLookupTools(TestDataDirectory);
    }

    #region ListAllFeaturesAsync Tests

    [TestMethod]
    public async Task ListAllFeaturesAsync_ReturnsAllFeatures_WhenDataDirectoryExists()
    {
        // Act
        var features = await _tools.ListAllFeaturesAsync();

        // Assert
        features.Should().NotBeEmpty();
        features.Should().HaveCountGreaterThanOrEqualTo(4); // We have feature1-4 in sample data
    }

    [TestMethod]
    public async Task ListAllFeaturesAsync_ReturnsCorrectFeatureInfo_ForFeature1()
    {
        // Act
        var features = await _tools.ListAllFeaturesAsync();

        // Assert
        var feature1 = features.FirstOrDefault(f => f.FeatureId == "feature1");
        feature1.Should().NotBeNull();
        feature1!.JiraKey.Should().Be("PLAT-1523");
        feature1.Summary.Should().Be("Maintenance Scheduling & Alert System");
        feature1.CurrentStage.Should().Be("UAT");
    }

    [TestMethod]
    public async Task ListAllFeaturesAsync_ExtractsCurrentStageFromStatus()
    {
        // Act
        var features = await _tools.ListAllFeaturesAsync();

        // Assert
        features.Should().AllSatisfy(f => f.CurrentStage.Should().NotBeNullOrEmpty());
    }

    [TestMethod]
    public async Task ListAllFeaturesAsync_ReturnsEmptyList_WhenDataDirectoryDoesNotExist()
    {
        // Arrange
        var tools = new FeatureLookupTools("nonexistent-path");

        // Act
        var features = await tools.ListAllFeaturesAsync();

        // Assert
        features.Should().BeEmpty();
    }

    #endregion

    #region GetFeatureMetadataAsync Tests

    [TestMethod]
    public async Task GetFeatureMetadataAsync_ReturnsMetadata_WhenGivenJiraKey()
    {
        // Act
        var metadata = await _tools.GetFeatureMetadataAsync("PLAT-1523");

        // Assert
        metadata.Should().NotBeNull();
        metadata.FeatureId.Should().Be("feature1");
        metadata.Key.Should().Be("PLAT-1523");
        metadata.Fields.Summary.Should().Be("Maintenance Scheduling & Alert System");
    }

    [TestMethod]
    public async Task GetFeatureMetadataAsync_ReturnsMetadata_WhenGivenFeatureId()
    {
        // Act
        var metadata = await _tools.GetFeatureMetadataAsync("feature1");

        // Assert
        metadata.Should().NotBeNull();
        metadata.FeatureId.Should().Be("feature1");
        metadata.Key.Should().Be("PLAT-1523");
    }

    [TestMethod]
    public async Task GetFeatureMetadataAsync_ReturnsMetadata_WhenGivenFeatureNameFuzzyMatch()
    {
        // Act
        var metadata = await _tools.GetFeatureMetadataAsync("Maintenance");

        // Assert
        metadata.Should().NotBeNull();
        metadata.FeatureId.Should().Be("feature1");
        metadata.Fields.Summary.Should().Contain("Maintenance");
    }

    [TestMethod]
    public async Task GetFeatureMetadataAsync_ThrowsFeatureNotFoundException_WhenFeatureDoesNotExist()
    {
        // Act
        var act = async () => await _tools.GetFeatureMetadataAsync("nonexistent-feature");

        // Assert
        await act.Should().ThrowAsync<FeatureNotFoundException>()
            .WithMessage("Feature not found: nonexistent-feature");
    }

    [TestMethod]
    public async Task GetFeatureMetadataAsync_ThrowsArgumentException_WhenIdentifierIsEmpty()
    {
        // Act
        var act = async () => await _tools.GetFeatureMetadataAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Feature identifier cannot be empty*");
    }

    [TestMethod]
    public async Task GetFeatureMetadataAsync_ParsesCompleteJiraFields()
    {
        // Act
        var metadata = await _tools.GetFeatureMetadataAsync("PLAT-1523");

        // Assert
        metadata.Fields.Should().NotBeNull();
        metadata.Fields.Summary.Should().NotBeNullOrEmpty();
        metadata.Fields.IssueType.Should().NotBeNull();
        metadata.Fields.IssueType.Name.Should().Be("Epic");
        metadata.Fields.Project.Should().NotBeNull();
        metadata.Fields.Project.Key.Should().Be("PLAT");
        metadata.Fields.Status.Should().NotBeNull();
        metadata.Fields.Status.Name.Should().Be("UAT");
        metadata.Fields.Priority.Should().NotBeNull();
        metadata.Fields.Priority!.Name.Should().Be("High");
    }

    [TestMethod]
    public async Task GetFeatureMetadataAsync_ParsesAssigneeAndReporter()
    {
        // Act
        var metadata = await _tools.GetFeatureMetadataAsync("PLAT-1523");

        // Assert
        metadata.Fields.Assignee.Should().NotBeNull();
        metadata.Fields.Assignee!.DisplayName.Should().Be("Emma Rodriguez");
        metadata.Fields.Assignee.Active.Should().BeTrue();
        metadata.Fields.Reporter.Should().NotBeNull();
        metadata.Fields.Reporter!.DisplayName.Should().Be("Emma Rodriguez");
    }

    [TestMethod]
    public async Task GetFeatureMetadataAsync_ParsesLabelsAndComponents()
    {
        // Act
        var metadata = await _tools.GetFeatureMetadataAsync("PLAT-1523");

        // Assert
        metadata.Fields.Labels.Should().NotBeNull();
        metadata.Fields.Labels.Should().Contain("feature");
        metadata.Fields.Labels.Should().Contain("UAT");
        metadata.Fields.Components.Should().NotBeNull();
        metadata.Fields.Components.Should().HaveCountGreaterThan(0);
        metadata.Fields.Components.Should().Contain(c => c.Name == "Backend API");
    }

    [TestMethod]
    public async Task GetFeatureMetadataAsync_ParsesTimestamps()
    {
        // Act
        var metadata = await _tools.GetFeatureMetadataAsync("PLAT-1523");

        // Assert
        metadata.Fields.Created.Should().NotBeNullOrEmpty();
        metadata.Fields.Updated.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public async Task GetFeatureMetadataAsync_IsCaseInsensitive_ForJiraKey()
    {
        // Act
        var metadata = await _tools.GetFeatureMetadataAsync("plat-1523");

        // Assert
        metadata.Should().NotBeNull();
        metadata.Key.Should().Be("PLAT-1523");
    }

    #endregion
}
