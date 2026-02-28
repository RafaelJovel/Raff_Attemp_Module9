using System.Diagnostics;
using FeatureAssessment.Core.Agents;
using FeatureAssessment.Core.Models;
using FeatureAssessment.Core.Observability;
using FeatureAssessment.Core.Workflow;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FeatureAssessment.Core.Tests.Workflow;

[TestClass]
[DoNotParallelize]
public class AssessmentWorkflowTests
{
    private Mock<IFeatureLookupAgent> _mockLookupAgent = null!;
    private Mock<ICoordinatorAgent> _mockCoordinator = null!;
    private Mock<ILogger<AssessmentWorkflow>> _mockLogger = null!;
    private List<Activity> _capturedActivities = null!;
    private ActivityListener _activityListener = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLookupAgent = new Mock<IFeatureLookupAgent>();
        _mockCoordinator = new Mock<ICoordinatorAgent>();
        _mockLogger = new Mock<ILogger<AssessmentWorkflow>>();

        _capturedActivities = new List<Activity>();
        _activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
                options.Source.Name.StartsWith(ActivitySources.ServiceName)
                    ? ActivitySamplingResult.AllDataAndRecorded
                    : ActivitySamplingResult.None,
            ActivityStarted = activity =>
            {
                lock (_capturedActivities) { _capturedActivities.Add(activity); }
            }
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _activityListener?.Dispose();
        lock (_capturedActivities) { _capturedActivities.Clear(); }
        Activity.Current = null;
    }

    // ── Constructor guards ─────────────────────────────────────────────────

    [TestMethod]
    public void Constructor_WithNullLookupAgent_ThrowsArgumentNullException()
    {
        var act = () => new AssessmentWorkflow(null!, _mockCoordinator.Object, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("featureLookupAgent");
    }

    [TestMethod]
    public void Constructor_WithNullCoordinator_ThrowsArgumentNullException()
    {
        var act = () => new AssessmentWorkflow(_mockLookupAgent.Object, null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("coordinatorAgent");
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var act = () => new AssessmentWorkflow(_mockLookupAgent.Object, _mockCoordinator.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [TestMethod]
    public async Task RunAsync_WithNullQuery_ThrowsArgumentException()
    {
        var workflow = new AssessmentWorkflow(_mockLookupAgent.Object, _mockCoordinator.Object, _mockLogger.Object);
        var act = async () => await workflow.RunAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("query");
    }

    [TestMethod]
    public async Task RunAsync_WithWhitespaceQuery_ThrowsArgumentException()
    {
        var workflow = new AssessmentWorkflow(_mockLookupAgent.Object, _mockCoordinator.Object, _mockLogger.Object);
        var act = async () => await workflow.RunAsync("   ");
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("query");
    }

    // ── Happy path ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task RunAsync_WhenLookupSucceeds_CallsCoordinatorWithPopulatedState()
    {
        // Arrange
        var lookupResult = new FeatureLookupResult
        {
            FeatureKey = "PLAT-1523",
            FeatureId = "feature1",
            TargetEnvironment = "Production",
            IsSuccess = true
        };
        _mockLookupAgent
            .Setup(a => a.LookupFeatureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lookupResult);

        var expectedFinalState = AssessmentState.FromFeatureLookupResult(lookupResult)
            .WithStage("coordinator_completed")
            .WithCoordinatorResponse("Preliminary assessment complete.");
        _mockCoordinator
            .Setup(c => c.AssessAsync(It.IsAny<AssessmentState>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedFinalState);

        var workflow = new AssessmentWorkflow(_mockLookupAgent.Object, _mockCoordinator.Object, _mockLogger.Object);

        // Act
        var result = await workflow.RunAsync("Is PLAT-1523 ready for production?");

        // Assert — both agents called, state flowed correctly
        _mockLookupAgent.Verify(a => a.LookupFeatureAsync("Is PLAT-1523 ready for production?", It.IsAny<CancellationToken>()), Times.Once);
        _mockCoordinator.Verify(c => c.AssessAsync(
            It.Is<AssessmentState>(s => s.FeatureKey == "PLAT-1523" && s.IsFeatureIdentified),
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.AreEqual("coordinator_completed", result.CurrentStage);
        Assert.AreEqual("PLAT-1523", result.FeatureKey);
        Assert.AreEqual("Preliminary assessment complete.", result.CoordinatorResponse);
    }

    // ── Feature not found — coordinator skipped ────────────────────────────

    [TestMethod]
    public async Task RunAsync_WhenLookupFails_SkipsCoordinator_ReturnsErrorState()
    {
        // Arrange
        _mockLookupAgent
            .Setup(a => a.LookupFeatureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeatureLookupResult
            {
                IsSuccess = false,
                ErrorMessage = "Feature XYZ-999 not found"
            });

        var workflow = new AssessmentWorkflow(_mockLookupAgent.Object, _mockCoordinator.Object, _mockLogger.Object);

        // Act
        var result = await workflow.RunAsync("Is XYZ-999 ready?");

        // Assert — coordinator never invoked
        _mockCoordinator.Verify(c => c.AssessAsync(It.IsAny<AssessmentState>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.AreEqual("error", result.CurrentStage);
        Assert.IsFalse(result.IsFeatureIdentified);
    }

    // ── Exception handling ──────────────────────────────────────────────────

    [TestMethod]
    public async Task RunAsync_WhenLookupThrows_ReturnsErrorState_DoesNotPropagate()
    {
        // Arrange
        _mockLookupAgent
            .Setup(a => a.LookupFeatureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Lookup exploded"));

        var workflow = new AssessmentWorkflow(_mockLookupAgent.Object, _mockCoordinator.Object, _mockLogger.Object);

        // Act — should NOT throw
        var result = await workflow.RunAsync("Is PLAT-1523 ready?");

        // Assert
        Assert.AreEqual("error", result.CurrentStage);
        result.ErrorMessage.Should().Contain("Lookup exploded");
        _mockCoordinator.Verify(c => c.AssessAsync(It.IsAny<AssessmentState>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Tracing ────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task RunAsync_CreatesRootActivity_WithQueryTag()
    {
        // Arrange
        _mockLookupAgent
            .Setup(a => a.LookupFeatureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeatureLookupResult { IsSuccess = false, ErrorMessage = "not found" });

        var workflow = new AssessmentWorkflow(_mockLookupAgent.Object, _mockCoordinator.Object, _mockLogger.Object);
        var query = "Is PLAT-1523 ready for production?";

        // Act
        await workflow.RunAsync(query);

        // Assert — root workflow activity created
        var rootActivity = _capturedActivities.FirstOrDefault(a =>
            a.OperationName == "AssessmentWorkflow.Run");
        Assert.IsNotNull(rootActivity, "AssessmentWorkflow.Run activity should be created");

        var queryTag = rootActivity.Tags.FirstOrDefault(t => t.Key == "query");
        Assert.AreEqual("query", queryTag.Key);
        Assert.AreEqual(query, queryTag.Value);
    }
}
