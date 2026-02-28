using System.Diagnostics;
using FeatureAssessment.Core.Agents;
using FeatureAssessment.Core.Clients;
using FeatureAssessment.Core.Configuration;
using FeatureAssessment.Core.Observability;
using FeatureAssessment.Core.Tools;
using FeatureAssessment.Core.Workflow;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FeatureAssessment.Core.Tests.Integration;

/// <summary>
/// End-to-end integration tests for the full AssessmentWorkflow pipeline
/// (Feature Lookup → Coordinator) using the real Anthropic LLM.
/// Tests are skipped (Inconclusive) if API key is not available.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public class WorkflowIntegrationTests
{
    private IAssessmentWorkflow _workflow = null!;
    private List<Activity> _capturedActivities = null!;
    private ActivityListener _activityListener = null!;

    private static string? ResolveApiKey()
    {
        var envKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (!string.IsNullOrEmpty(envKey))
            return envKey;

        var localConfigPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..",
            "src", "FeatureAssessment.Core", "appsettings.Development.local.json");

        var fullPath = Path.GetFullPath(localConfigPath);
        if (!File.Exists(fullPath))
            return null;

        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(fullPath));
        if (doc.RootElement.TryGetProperty("Anthropic", out var section) &&
            section.TryGetProperty("ApiKey", out var key))
            return key.GetString();

        return null;
    }

    [TestInitialize]
    public void Setup()
    {
        var apiKey = ResolveApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Inconclusive(
                "Anthropic API key not found. Set ANTHROPIC_API_KEY environment variable " +
                "or add it to src/FeatureAssessment.Core/appsettings.Development.local.json.");
        }

        var dataDirectory = Path.Combine(
            Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "data");

        var tools = new FeatureLookupTools(dataDirectory);

        var anthropicConfig = Options.Create(new AnthropicConfiguration
        {
            ApiKey = apiKey,
            ModelName = "claude-haiku-4-5-20251001",
            Temperature = 0.0,
            MaxTokens = 1024
        });
        var ollamaConfig = Options.Create(new OllamaConfiguration
        {
            Endpoint = "http://localhost:11434",
            ModelName = "llama3.1:8b"
        });
        var providerConfig = Options.Create(
            new LlmProviderConfiguration { Provider = LlmProvider.Anthropic });

        var mockKFLogger = new Mock<ILogger<KernelFactory>>();

        // Lookup agent — uses kernel with FeatureLookup tools
        var lookupKernelFactory = new KernelFactory(
            providerConfig, ollamaConfig, anthropicConfig, tools, mockKFLogger.Object);
        var lookupAgent = new FeatureLookupAgent(
            lookupKernelFactory, new Mock<ILogger<FeatureLookupAgent>>().Object);

        // Coordinator agent — uses kernel without tools
        var coordinatorKernelFactory = new KernelFactory(
            providerConfig, ollamaConfig, anthropicConfig, null, mockKFLogger.Object);
        var coordinatorAgent = new CoordinatorAgent(
            coordinatorKernelFactory, new Mock<ILogger<CoordinatorAgent>>().Object);

        _workflow = new AssessmentWorkflow(
            lookupAgent, coordinatorAgent,
            new Mock<ILogger<AssessmentWorkflow>>().Object);

        // Capture all FeatureAssessment activities
        _capturedActivities = new List<Activity>();
        _activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
                options.Source.Name.StartsWith(ActivitySources.ServiceName)
                    ? ActivitySamplingResult.AllDataAndRecorded
                    : ActivitySamplingResult.None,
            ActivityStarted = a => { lock (_capturedActivities) { _capturedActivities.Add(a); } }
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _activityListener?.Dispose();
        lock (_capturedActivities) { _capturedActivities?.Clear(); }
    }

    [TestMethod]
    public async Task Workflow_WithKnownFeature_PopulatesStateAndRunsCoordinator()
    {
        // Act
        var result = await _workflow.RunAsync("Is PLAT-1523 ready for production?");

        // Assert — feature lookup succeeded
        Console.WriteLine($"Final Stage: {result.CurrentStage}");
        Console.WriteLine($"Feature Key: {result.FeatureKey}");
        Console.WriteLine($"Target Env:  {result.TargetEnvironment}");
        Console.WriteLine($"Coordinator Response:\n{result.CoordinatorResponse}");

        result.Should().NotBeNull();
        result.IsFeatureIdentified.Should().BeTrue("lookup should identify PLAT-1523");
        result.FeatureKey.Should().Be("PLAT-1523");
        result.TargetEnvironment.Should().Be("Production");

        // Coordinator ran
        result.CurrentStage.Should().Be("coordinator_completed");
        result.CoordinatorResponse.Should().NotBeNullOrEmpty();
        result.CoordinatorResponse!.Length.Should().BeGreaterThan(50);
    }

    [TestMethod]
    public async Task Workflow_WithUnknownFeature_ReturnsErrorState_CoordinatorNotInvoked()
    {
        // Act
        var result = await _workflow.RunAsync("Is feature XYZ-9999 ready?");

        // Assert — feature lookup failed, workflow stopped there
        Console.WriteLine($"Final Stage:   {result.CurrentStage}");
        Console.WriteLine($"Error Message: {result.ErrorMessage}");

        result.Should().NotBeNull();
        result.IsFeatureIdentified.Should().BeFalse();
        result.CurrentStage.Should().Be("error");
        result.CoordinatorResponse.Should().BeNullOrEmpty(
            "coordinator should not have been invoked");
    }

    [TestMethod]
    public async Task Workflow_RootActivity_AppearsInTrace_WithBothAgentActivities()
    {
        // Act
        await _workflow.RunAsync("Is PLAT-1523 ready for production?");

        // Assert — root workflow span present
        var rootActivity = _capturedActivities.FirstOrDefault(a =>
            a.OperationName == "AssessmentWorkflow.Run");
        Assert.IsNotNull(rootActivity, "AssessmentWorkflow.Run root span should be captured");

        // Feature lookup span present
        var lookupActivity = _capturedActivities.FirstOrDefault(a =>
            a.OperationName == "FeatureLookupAgent.LookupFeature");
        Assert.IsNotNull(lookupActivity, "FeatureLookupAgent span should be captured");

        // Coordinator span present
        var coordinatorActivity = _capturedActivities.FirstOrDefault(a =>
            a.OperationName == "CoordinatorAgent.Assess");
        Assert.IsNotNull(coordinatorActivity, "CoordinatorAgent span should be captured");

        Console.WriteLine($"Activities captured: {_capturedActivities.Count}");
        foreach (var a in _capturedActivities)
            Console.WriteLine($"  [{a.Source.Name}] {a.OperationName}");
    }
}
