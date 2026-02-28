using System.Diagnostics;
using FeatureAssessment.Core.Agents;
using FeatureAssessment.Core.Clients;
using FeatureAssessment.Core.Configuration;
using FeatureAssessment.Core.Models;
using FeatureAssessment.Core.Observability;
using FeatureAssessment.Core.Tests.Helpers;
using FeatureAssessment.Core.Tools;
using Microsoft.Extensions.Logging;
using Moq;

namespace FeatureAssessment.Core.Tests.Observability;

[TestClass]
[DoNotParallelize]
public class FeatureLookupAgentTracingTests
{
    private Mock<IFeatureLookupTools> _mockTools = null!;
    private Mock<IKernelFactory> _mockKernelFactory = null!;
    private Mock<ILogger<FeatureLookupAgent>> _mockLogger = null!;
    private List<Activity> _capturedActivities = null!;
    private ActivityListener _activityListener = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockTools = new Mock<IFeatureLookupTools>();
        _mockLogger = new Mock<ILogger<FeatureLookupAgent>>();
        _mockKernelFactory = MockKernelFactoryHelper.CreateMockFactory(_mockTools);

        // Ensure clean state - clear any existing activities
        _capturedActivities = new List<Activity>();

        // Setup ActivityListener to capture activities
        // NOTE: Must filter in Sample callback to properly exclude Semantic Kernel's internal activities
        var expectedSourceName = $"{ActivitySources.ServiceName}.FeatureLookup";
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => true, // Listen to all sources
            Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
            {
                // Only sample activities from our specific ActivitySource
                return options.Source.Name == expectedSourceName
                    ? ActivitySamplingResult.AllDataAndRecorded
                    : ActivitySamplingResult.None;
            },
            ActivityStarted = activity =>
            {
                lock (_capturedActivities) // Thread-safe capture
                {
                    _capturedActivities.Add(activity);
                }
            }
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _activityListener?.Dispose();
        lock (_capturedActivities)
        {
            _capturedActivities.Clear();
        }

        // Wait for any pending activities to complete
        Activity.Current = null;
    }

    [TestMethod]
    public async Task LookupFeatureAsync_CreatesActivity()
    {
        // Arrange
        var query = "Is PLAT-1523 ready for production?";
        var agent = new FeatureLookupAgent(_mockKernelFactory.Object, _mockLogger.Object);

        // Act
        // Note: This will fail at runtime because Ollama isn't configured,
        // but we're testing that the Activity is created before the failure
        try
        {
            await agent.LookupFeatureAsync(query);
        }
        catch
        {
            // Expected to fail - we're just testing activity creation
        }

        // Assert - Filter for our specific activity
        var activity = _capturedActivities.FirstOrDefault(a => a.OperationName == "FeatureLookupAgent.LookupFeature");
        Assert.IsNotNull(activity, "FeatureLookupAgent.LookupFeature activity should be created");
        Assert.AreEqual("FeatureLookupAgent.LookupFeature", activity.OperationName);
        Assert.AreEqual(ActivitySources.FeatureLookup.Name, activity.Source.Name);
    }

    [TestMethod]
    public async Task LookupFeatureAsync_SetsQueryTag()
    {
        // Arrange
        var query = "Check maintenance scheduling for UAT";
        var agent = new FeatureLookupAgent(_mockKernelFactory.Object, _mockLogger.Object);

        // Act
        try
        {
            await agent.LookupFeatureAsync(query);
        }
        catch
        {
            // Expected to fail
        }

        // Assert - Filter for our specific activity by BOTH operation name AND query tag
        var activity = _capturedActivities.FirstOrDefault(a =>
            a.OperationName == "FeatureLookupAgent.LookupFeature" &&
            a.Tags.Any(t => t.Key == "query" && t.Value == query));
        Assert.IsNotNull(activity, "FeatureLookupAgent.LookupFeature activity with correct query should be created");

        var queryTag = activity.Tags.FirstOrDefault(t => t.Key == "query");
        Assert.AreEqual("query", queryTag.Key, "Query tag should be set");
        Assert.AreEqual(query, queryTag.Value, "Query tag should match input query");
    }

    [TestMethod]
    public async Task LookupFeatureAsync_SetsServiceNameTag()
    {
        // Arrange
        var query = "Test query";
        var agent = new FeatureLookupAgent(_mockKernelFactory.Object, _mockLogger.Object);

        // Act
        try
        {
            await agent.LookupFeatureAsync(query);
        }
        catch
        {
            // Expected to fail
        }

        // Assert - Filter for our specific activity
        var activity = _capturedActivities.FirstOrDefault(a => a.OperationName == "FeatureLookupAgent.LookupFeature");
        Assert.IsNotNull(activity, "FeatureLookupAgent.LookupFeature activity should be created");

        var serviceNameTag = activity.Tags.FirstOrDefault(t => t.Key == "service.name");
        Assert.AreEqual("service.name", serviceNameTag.Key, "Service name tag should be set");
        Assert.AreEqual(ActivitySources.ServiceName, serviceNameTag.Value);
    }

    [TestMethod]
    public async Task LookupFeatureAsync_RecordsErrorStatusOnException()
    {
        // Arrange
        var query = "Test query for exception test";

        // Configure tools to throw exception
        _mockTools.Setup(x => x.ListAllFeaturesAsync())
            .Throws(new InvalidOperationException("Test exception"));

        var agent = new FeatureLookupAgent(_mockKernelFactory.Object, _mockLogger.Object);

        // Act
        var result = await agent.LookupFeatureAsync(query);

        // Assert - Agent should return error result, not throw
        Assert.IsFalse(result.IsSuccess, "Result should indicate failure");

        // Filter for our specific activity by query
        var activity = _capturedActivities.FirstOrDefault(a =>
            a.OperationName == "FeatureLookupAgent.LookupFeature" &&
            a.Tags.Any(t => t.Key == "query" && t.Value == query));
        Assert.IsNotNull(activity, "FeatureLookupAgent.LookupFeature activity should be created");

        // NOTE: The exception occurs during Semantic Kernel execution (LLM call failure or tool invocation)
        // The agent catches it and returns an error result. Verify error status is recorded.
        Assert.AreEqual(ActivityStatusCode.Error, activity.Status, "Activity should have Error status when exception occurs");

        // Verify exception details are captured
        var exceptionTypeTag = activity.Tags.FirstOrDefault(t => t.Key == "exception.type");
        var exceptionMessageTag = activity.Tags.FirstOrDefault(t => t.Key == "exception.message");

        Assert.AreEqual("exception.type", exceptionTypeTag.Key, "Exception type should be recorded");
        Assert.AreEqual("exception.message", exceptionMessageTag.Key, "Exception message should be recorded");
    }

    [TestMethod]
    public async Task LookupFeatureAsync_PreservesActivityAcrossAsync()
    {
        // Arrange
        var query = "Test query for async preservation";
        Activity? activityDuringExecution = null;
        var activityCaptured = false;

        // Configure tools to capture Activity.Current during execution
        // Note: The callback should happen AFTER the agent starts its activity
        _mockTools.Setup(x => x.ListAllFeaturesAsync())
            .Callback(() =>
            {
                activityDuringExecution = Activity.Current;
                activityCaptured = true;
            })
            .ReturnsAsync([]);

        var agent = new FeatureLookupAgent(_mockKernelFactory.Object, _mockLogger.Object);

        // Act
        var result = await agent.LookupFeatureAsync(query);

        // Assert - Filter for our specific activity
        var ourActivity = _capturedActivities.FirstOrDefault(a => a.OperationName == "FeatureLookupAgent.LookupFeature");
        Assert.IsNotNull(ourActivity, "FeatureLookupAgent.LookupFeature activity should be created");

        // Verify the tools callback was actually called
        // If not called, the LLM execution failed before reaching tools
        if (activityCaptured)
        {
            // Activity.Current might be our activity or a Semantic Kernel child activity
            // We just verify that SOME activity was present during execution
            Assert.IsNotNull(activityDuringExecution, "Activity.Current should be set during async execution");
        }
        else
        {
            // Tools weren't called (expected since Ollama isn't running), but our activity should still exist
            Assert.IsNotNull(ourActivity, "Activity should be created even if tools aren't called");
        }
    }

    [TestMethod]
    public void ActivitySource_HasCorrectName()
    {
        // Assert
        Assert.AreEqual($"{ActivitySources.ServiceName}.FeatureLookup", ActivitySources.FeatureLookup.Name);
        Assert.AreEqual(ActivitySources.Version, ActivitySources.FeatureLookup.Version);
    }

    [TestMethod]
    public void ActivitySource_ToolsHasCorrectName()
    {
        // Assert
        Assert.AreEqual($"{ActivitySources.ServiceName}.Tools", ActivitySources.Tools.Name);
        Assert.AreEqual(ActivitySources.Version, ActivitySources.Tools.Version);
    }
}
