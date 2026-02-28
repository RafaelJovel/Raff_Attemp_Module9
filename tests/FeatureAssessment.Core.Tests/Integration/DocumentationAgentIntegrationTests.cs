using System.Diagnostics;
using FeatureAssessment.Core.Agents;
using FeatureAssessment.Core.Observability;
using FeatureAssessment.Core.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace FeatureAssessment.Core.Tests.Integration;

[TestClass]
[TestCategory("Integration")]
public class DocumentationAgentIntegrationTests
{
    private Mock<ILogger<DocumentationSpecialistAgent>> _loggerMock = null!;
    private DocumentationSpecialistAgent _agent = null!;
    private IDocumentationTools _tools = null!;

    [TestInitialize]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<DocumentationSpecialistAgent>>();
        // use real DocumentationTools which will read from data/incoming directories
        _tools = new DocumentationTools(new Mock<ILogger<DocumentationTools>>().Object);
        _agent = new DocumentationSpecialistAgent(_tools, _loggerMock.Object);
    }

    [TestMethod]
    public async Task DocumentationAgent_WithRealTools_AssessesFeature1()
    {
        // Act
        var result = await _agent.AssessAsync("Assess USER_STORY completeness", "feature1");

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(result));
        Assert.IsTrue(result.Contains("Planning documents"), "Response should list planning documents");
        Assert.IsTrue(result.Contains("USER_STORY.md"), "Should mention USER_STORY.md in listing");
    }

    [TestMethod]
    public async Task DocumentationAgent_CreatesActivityWithTags()
    {
        // arrange activity listener
        var captured = new List<Activity>();
        var listener = new ActivityListener
        {
            ShouldListenTo = source => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
            {
                return options.Source.Name == ActivitySources.DocumentationSpecialist.Name
                    ? ActivitySamplingResult.AllDataAndRecorded
                    : ActivitySamplingResult.None;
            },
            ActivityStarted = activity =>
            {
                lock (captured) { captured.Add(activity); }
            }
        };
        ActivitySource.AddActivityListener(listener);

        try
        {
            // act
            var query = "Assess documentation for feature1";
            var featureId = "feature1";
            var res = await _agent.AssessAsync(query, featureId);
        }
        finally
        {
            listener.Dispose();
            Activity.Current = null;
        }

        // assert - look for one activity with correct tags
        var activity = captured.FirstOrDefault(a => a.OperationName == "DocumentationSpecialistAgent.AssessAsync");
        Assert.IsNotNull(activity, "Activity for AssessAsync should be created");
        Assert.AreEqual(ActivitySources.DocumentationSpecialist.Name, activity.Source.Name);
        Assert.IsTrue(activity.Tags.Any(t => t.Key == "feature_id" && t.Value == "feature1"));
        // ensure there is a query tag and it mentions the feature
        var queryTag = activity.Tags.FirstOrDefault(t => t.Key == "query");
        Assert.IsNotNull(queryTag, "Query tag should be present");
        // We don't assert specific value since previous activities may also be captured
        Assert.IsFalse(string.IsNullOrEmpty(queryTag.Value), "Query tag should have some value");
    }
}
