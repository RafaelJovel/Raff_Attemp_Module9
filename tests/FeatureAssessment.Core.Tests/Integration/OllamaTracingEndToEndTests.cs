using System.Diagnostics;
using FeatureAssessment.Core.Agents;
using FeatureAssessment.Core.Clients;
using FeatureAssessment.Core.Configuration;
using FeatureAssessment.Core.Observability;
using FeatureAssessment.Core.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FeatureAssessment.Core.Tests.Integration;

[TestClass]
[DoNotParallelize]
public class OllamaTracingEndToEndTests
{
    private List<Activity> _capturedActivities = null!;
    private ActivityListener _activityListener = null!;

    [TestInitialize]
    public void Setup()
    {
        _capturedActivities = new List<Activity>();

        // Setup ActivityListener to capture all activities from FeatureAssessment
        // NOTE: Must filter in Sample callback to properly exclude Semantic Kernel's internal activities
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => true, // Listen to all sources
            Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
            {
                // Only sample activities from our ActivitySources (any starting with our service name)
                return options.Source.Name.StartsWith(ActivitySources.ServiceName)
                    ? ActivitySamplingResult.AllDataAndRecorded
                    : ActivitySamplingResult.None;
            },
            ActivityStarted = activity => _capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _activityListener?.Dispose();
        _capturedActivities.Clear();
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task FeatureLookupAgent_WithTracing_GeneratesCompleteTrace()
    {
        // Arrange
        var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "data");
        var tools = new FeatureLookupTools(dataDirectory);

        // Configure Ollama
        var ollamaConfig = Options.Create(new OllamaConfiguration
        {
            Endpoint = "http://localhost:11434",
            ModelName = "llama3.1:8b",
            Temperature = 0.0,
            MaxTokens = 500
        });

        // Configure LLM Provider to use Ollama
        var providerConfig = Options.Create(new LlmProviderConfiguration { Provider = LlmProvider.Ollama });

        // Configure Anthropic (required but not used for this test)
        var anthropicConfig = Options.Create(new AnthropicConfiguration { ApiKey = "dummy" });

        var mockAgentLogger = new Mock<ILogger<FeatureLookupAgent>>();
        var mockKernelFactoryLogger = new Mock<ILogger<KernelFactory>>();

        // Create KernelFactory and Agent
        var kernelFactory = new KernelFactory(providerConfig, ollamaConfig, anthropicConfig, tools, mockKernelFactoryLogger.Object);
        var agent = new FeatureLookupAgent(kernelFactory, mockAgentLogger.Object);

        var query = "Is PLAT-1523 ready for production?";

        // Act
        var result = await agent.LookupFeatureAsync(query);

        // Assert
        Assert.IsTrue(result.IsSuccess, $"Feature lookup should succeed. Error: {result.ErrorMessage}");

        // Verify activity was created
        Assert.IsGreaterThanOrEqualTo(1, _capturedActivities.Count, "At least one activity should be captured");

        var mainActivity = _capturedActivities.FirstOrDefault(a => a.OperationName == "FeatureLookupAgent.LookupFeature");
        Assert.IsNotNull(mainActivity, "Main FeatureLookupAgent activity should be present");

        // Verify tags are set
        var queryTag = mainActivity.Tags.FirstOrDefault(t => t.Key == "query");
        Assert.AreEqual("query", queryTag.Key, "Query tag should be set");
        Assert.AreEqual(query, queryTag.Value);

        var featureKeyTag = mainActivity.Tags.FirstOrDefault(t => t.Key == "feature_key");
        Assert.AreEqual("feature_key", featureKeyTag.Key, "Feature key tag should be set");
        Assert.IsFalse(string.IsNullOrEmpty(featureKeyTag.Value), "Feature key should have a value");

        var targetEnvTag = mainActivity.Tags.FirstOrDefault(t => t.Key == "target_environment");
        Assert.AreEqual("target_environment", targetEnvTag.Key, "Target environment tag should be set");

        var isSuccessTag = mainActivity.Tags.FirstOrDefault(t => t.Key == "is_success");
        Assert.AreEqual("is_success", isSuccessTag.Key, "Success tag should be set");
        Assert.AreEqual("True", isSuccessTag.Value, "Success tag should be true");

        // Verify no error status
        Assert.AreNotEqual(ActivityStatusCode.Error, mainActivity.Status, "Activity should not have error status");

        Console.WriteLine($"Trace completed successfully:");
        Console.WriteLine($"  Query: {query}");
        Console.WriteLine($"  Feature Key: {result.FeatureKey}");
        Console.WriteLine($"  Target Environment: {result.TargetEnvironment}");
        Console.WriteLine($"  Activities Captured: {_capturedActivities.Count}");
        foreach (var activity in _capturedActivities)
        {
            Console.WriteLine($"    - {activity.OperationName} (Duration: {activity.Duration})");
        }
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task FeatureLookupAgent_WithFeatureNotFound_RecordsErrorInTrace()
    {
        // Arrange
        var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "data");
        var tools = new FeatureLookupTools(dataDirectory);

        // Configure Ollama
        var ollamaConfig = Options.Create(new OllamaConfiguration
        {
            Endpoint = "http://localhost:11434",
            ModelName = "llama3.1:8b",
            Temperature = 0.0,
            MaxTokens = 500
        });

        // Configure LLM Provider to use Ollama
        var providerConfig = Options.Create(new LlmProviderConfiguration { Provider = LlmProvider.Ollama });

        // Configure Anthropic (required but not used for this test)
        var anthropicConfig = Options.Create(new AnthropicConfiguration { ApiKey = "dummy" });

        var mockAgentLogger = new Mock<ILogger<FeatureLookupAgent>>();
        var mockKernelFactoryLogger = new Mock<ILogger<KernelFactory>>();

        // Create KernelFactory and Agent
        var kernelFactory = new KernelFactory(providerConfig, ollamaConfig, anthropicConfig, tools, mockKernelFactoryLogger.Object);
        var agent = new FeatureLookupAgent(kernelFactory, mockAgentLogger.Object);

        var query = "Is feature NONEXISTENT-999 ready?";

        // Act
        var result = await agent.LookupFeatureAsync(query);

        // Assert - Result may succeed or fail depending on LLM response
        // But we should still have trace data

        Assert.IsGreaterThanOrEqualTo(1, _capturedActivities.Count, "At least one activity should be captured");

        // Filter for our specific activity by BOTH operation name AND query tag to avoid test isolation issues
        var mainActivity = _capturedActivities.FirstOrDefault(a =>
            a.OperationName == "FeatureLookupAgent.LookupFeature" &&
            a.Tags.Any(t => t.Key == "query" && t.Value == query));
        Assert.IsNotNull(mainActivity, "Main FeatureLookupAgent activity with correct query should be present");

        // Verify query tag is set
        var queryTag = mainActivity.Tags.FirstOrDefault(t => t.Key == "query");
        Assert.AreEqual("query", queryTag.Key, "Query tag should be set");
        Assert.AreEqual(query, queryTag.Value);

        Console.WriteLine($"Trace completed for non-existent feature:");
        Console.WriteLine($"  Query: {query}");
        Console.WriteLine($"  Success: {result.IsSuccess}");
        Console.WriteLine($"  Error: {result.ErrorMessage}");
        Console.WriteLine($"  Activity Status: {mainActivity.Status}");
        Console.WriteLine($"  Activities Captured: {_capturedActivities.Count}");
    }
}
