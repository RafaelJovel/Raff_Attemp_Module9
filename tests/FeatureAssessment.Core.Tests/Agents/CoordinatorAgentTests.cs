using System.Diagnostics;
using FeatureAssessment.Core.Agents;
using FeatureAssessment.Core.Clients;
using FeatureAssessment.Core.Models;
using FeatureAssessment.Core.Observability;
using FeatureAssessment.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FeatureAssessment.Core.Tests.Agents;

[TestClass]
[DoNotParallelize]
public class CoordinatorAgentTests
{
    private Mock<IKernelFactory> _mockKernelFactory = null!;
    private Mock<ILogger<CoordinatorAgent>> _mockLogger = null!;
    private List<Activity> _capturedActivities = null!;
    private ActivityListener _activityListener = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<CoordinatorAgent>>();
        _mockKernelFactory = MockKernelFactoryHelper.CreateBasicMockFactory();

        _capturedActivities = new List<Activity>();
        var expectedSourceName = $"{ActivitySources.ServiceName}.Coordinator";
        _activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
                options.Source.Name == expectedSourceName
                    ? ActivitySamplingResult.AllDataAndRecorded
                    : ActivitySamplingResult.None,
            ActivityStarted = activity =>
            {
                lock (_capturedActivities)
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
        Activity.Current = null;
    }

    [TestMethod]
    public void Constructor_WithNullKernelFactory_ThrowsArgumentNullException()
    {
        var act = () => new CoordinatorAgent(null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("kernelFactory");
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var act = () => new CoordinatorAgent(_mockKernelFactory.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [TestMethod]
    public async Task AssessAsync_WithNullState_ThrowsArgumentNullException()
    {
        var agent = new CoordinatorAgent(_mockKernelFactory.Object, _mockLogger.Object);

        var act = async () => await agent.AssessAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("state");
    }

    [TestMethod]
    public async Task AssessAsync_WhenFeatureNotIdentified_ReturnsErrorState_WithoutLlmCall()
    {
        // Arrange
        var state = new AssessmentState
        {
            IsFeatureIdentified = false,
            ErrorMessage = "Feature not found",
            CurrentStage = "error"
        };
        var agent = new CoordinatorAgent(_mockKernelFactory.Object, _mockLogger.Object);

        // Act
        var result = await agent.AssessAsync(state);

        // Assert — no LLM call was made (CreateKernel not invoked)
        _mockKernelFactory.Verify(f => f.CreateKernel(), Times.Never);
        Assert.AreEqual("error", result.CurrentStage);
        Assert.IsFalse(string.IsNullOrEmpty(result.CoordinatorResponse));
        Assert.IsFalse(result.IsFeatureIdentified);
    }

    [TestMethod]
    public async Task AssessAsync_WhenFeatureNotIdentified_CreatesActivityWithErrorStatus()
    {
        // Arrange
        var state = new AssessmentState { IsFeatureIdentified = false };
        var agent = new CoordinatorAgent(_mockKernelFactory.Object, _mockLogger.Object);

        // Act
        await agent.AssessAsync(state);

        // Assert — activity was created
        var activity = _capturedActivities.FirstOrDefault(a =>
            a.OperationName == "CoordinatorAgent.Assess");
        Assert.IsNotNull(activity, "CoordinatorAgent.Assess activity should be created");
        Assert.AreEqual(ActivityStatusCode.Error, activity.Status);
    }

    [TestMethod]
    public async Task AssessAsync_WhenFeatureIdentified_CreatesActivityWithCorrectTags()
    {
        // Arrange
        var state = new AssessmentState
        {
            IsFeatureIdentified = true,
            FeatureKey = "PLAT-1523",
            FeatureId = "feature1",
            TargetEnvironment = "Production",
            CurrentStage = "feature_lookup_completed"
        };
        var agent = new CoordinatorAgent(_mockKernelFactory.Object, _mockLogger.Object);

        // Act — LLM call will fail (no IChatCompletionService in kernel), but activity is created first
        try
        {
            await agent.AssessAsync(state);
        }
        catch
        {
            // Expected — no real LLM configured
        }

        // Assert — activity created with correct tags
        var activity = _capturedActivities.FirstOrDefault(a =>
            a.OperationName == "CoordinatorAgent.Assess" &&
            a.Tags.Any(t => t.Key == "feature_key" && t.Value == "PLAT-1523"));
        Assert.IsNotNull(activity, "CoordinatorAgent.Assess activity with feature_key tag should be created");

        var featureKeyTag = activity.Tags.FirstOrDefault(t => t.Key == "feature_key");
        Assert.AreEqual("feature_key", featureKeyTag.Key);
        Assert.AreEqual("PLAT-1523", featureKeyTag.Value);

        var envTag = activity.Tags.FirstOrDefault(t => t.Key == "target_environment");
        Assert.AreEqual("target_environment", envTag.Key);
        Assert.AreEqual("Production", envTag.Value);

        var serviceTag = activity.Tags.FirstOrDefault(t => t.Key == "service.name");
        Assert.AreEqual("service.name", serviceTag.Key);
        Assert.AreEqual(ActivitySources.ServiceName, serviceTag.Value);
    }

    [TestMethod]
    public async Task AssessAsync_WhenLlmFails_ReturnsErrorState_AndSetsActivityErrorStatus()
    {
        // Arrange
        var state = new AssessmentState
        {
            IsFeatureIdentified = true,
            FeatureKey = "PLAT-1523",
            TargetEnvironment = "Production",
            CurrentStage = "feature_lookup_completed"
        };
        var agent = new CoordinatorAgent(_mockKernelFactory.Object, _mockLogger.Object);

        // Act — kernel has no IChatCompletionService, so it will throw
        var result = await agent.AssessAsync(state);

        // Assert
        Assert.AreEqual("error", result.CurrentStage);
        Assert.IsFalse(string.IsNullOrEmpty(result.CoordinatorResponse));
        result.CoordinatorResponse.Should().Contain("Assessment failed");

        var activity = _capturedActivities.FirstOrDefault(a =>
            a.OperationName == "CoordinatorAgent.Assess");
        Assert.IsNotNull(activity);
        Assert.AreEqual(ActivityStatusCode.Error, activity.Status);
    }

    [TestMethod]
    public void ActivitySource_HasCorrectName()
    {
        Assert.AreEqual(
            $"{ActivitySources.ServiceName}.Coordinator",
            ActivitySources.Coordinator.Name);
        Assert.AreEqual(ActivitySources.Version, ActivitySources.Coordinator.Version);
    }
}
