using System.Diagnostics;
using FeatureAssessment.Core.Agents;
using FeatureAssessment.Core.Clients;
using FeatureAssessment.Core.Configuration;
using FeatureAssessment.Core.Observability;
using FeatureAssessment.Core.Tools;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FeatureAssessment.Core.Tests.Integration;

/// <summary>
/// End-to-end integration tests validating the Feature Lookup Agent with Anthropic as the LLM provider.
/// Requires ANTHROPIC_API_KEY environment variable. Tests are skipped (Inconclusive) if not set.
/// </summary>
[TestClass]
[TestCategory("Integration")]
public class AnthropicEndToEndTests
{
    private string _apiKey = null!;
    private IKernelFactory _kernelFactory = null!;
    private FeatureLookupTools _tools = null!;
    private Mock<ILogger<FeatureLookupAgent>> _mockAgentLogger = null!;
    private List<Activity> _capturedActivities = null!;
    private ActivityListener _activityListener = null!;

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

        _apiKey = apiKey;

        var dataDirectory = Path.Combine(
            Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "data");

        _tools = new FeatureLookupTools(dataDirectory);

        var anthropicConfig = Options.Create(new AnthropicConfiguration
        {
            ApiKey = _apiKey,
            ModelName = "claude-haiku-4-5-20251001",  // Haiku 4.5 — fast and cost-effective
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

        _mockAgentLogger = new Mock<ILogger<FeatureLookupAgent>>();
        var mockKernelFactoryLogger = new Mock<ILogger<KernelFactory>>();

        _kernelFactory = new KernelFactory(
            providerConfig,
            ollamaConfig,
            anthropicConfig,
            _tools,
            null,
            mockKernelFactoryLogger.Object);

        _capturedActivities = new List<Activity>();
        _activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
                options.Source.Name.StartsWith(ActivitySources.ServiceName)
                    ? ActivitySamplingResult.AllDataAndRecorded
                    : ActivitySamplingResult.None,
            ActivityStarted = a =>
            {
                lock (_capturedActivities)
                {
                    _capturedActivities.Add(a);
                }
            }
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    /// <summary>
    /// Resolves the Anthropic API key from environment variable (CI/CD) or local config file (dev).
    /// </summary>
    private static string? ResolveApiKey()
    {
        // 1. Environment variable takes priority (CI/CD, production)
        var envKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (!string.IsNullOrEmpty(envKey))
            return envKey;

        // 2. Fall back to local config file (gitignored developer config)
        var localConfigPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..",
            "src", "FeatureAssessment.Core", "appsettings.Development.local.json");

        var fullPath = Path.GetFullPath(localConfigPath);
        if (!File.Exists(fullPath))
            return null;

        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(fullPath));
        if (doc.RootElement.TryGetProperty("Anthropic", out var anthropicSection) &&
            anthropicSection.TryGetProperty("ApiKey", out var apiKeyElement))
        {
            return apiKeyElement.GetString();
        }

        return null;
    }

    [TestCleanup]
    public void Cleanup()
    {
        _activityListener?.Dispose();
        lock (_capturedActivities)
        {
            _capturedActivities?.Clear();
        }
    }

    [TestMethod]
    public void AnthropicApiKey_IsAvailable()
    {
        // API key presence is checked in TestInitialize — reaching here means it is set.
        _apiKey.Should().NotBeNullOrEmpty("ANTHROPIC_API_KEY must be set");
    }

    [TestMethod]
    public async Task FeatureLookupAgent_WithAnthropic_IdentifiesFeatureByJiraKey()
    {
        // Arrange
        var agent = new FeatureLookupAgent(_kernelFactory, _mockAgentLogger.Object);

        // Act
        var result = await agent.LookupFeatureAsync("Is PLAT-1523 ready for production?");

        // Assert
        result.IsSuccess.Should().BeTrue("Claude should identify feature PLAT-1523");
        result.FeatureKey.Should().Be("PLAT-1523");
        result.FeatureId.Should().Be("feature1");
        result.TargetEnvironment.Should().Be("Production");
    }

    [TestMethod]
    public async Task FeatureLookupAgent_WithAnthropic_IdentifiesFeatureByName()
    {
        // Arrange
        var agent = new FeatureLookupAgent(_kernelFactory, _mockAgentLogger.Object);

        // Act
        var result = await agent.LookupFeatureAsync("Check maintenance scheduling for UAT");

        // Assert
        result.IsSuccess.Should().BeTrue("Claude should match 'maintenance scheduling' to PLAT-1523");
        result.FeatureKey.Should().Be("PLAT-1523");
        result.TargetEnvironment.Should().Be("UAT");
    }

    [TestMethod]
    public async Task FeatureLookupAgent_WithAnthropic_DefaultsToUAT_WhenNoEnvironmentSpecified()
    {
        // Arrange
        var agent = new FeatureLookupAgent(_kernelFactory, _mockAgentLogger.Object);

        // Act
        var result = await agent.LookupFeatureAsync("Tell me about PLAT-1523");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TargetEnvironment.Should().Be("UAT", "default environment should be UAT when not specified");
    }

    [TestMethod]
    public async Task FeatureLookupAgent_WithAnthropic_ReturnsFailure_ForNonExistentFeature()
    {
        // Arrange
        var agent = new FeatureLookupAgent(_kernelFactory, _mockAgentLogger.Object);

        // Act
        var result = await agent.LookupFeatureAsync("Is feature XYZ-9999 ready for production?");

        // Assert
        result.IsSuccess.Should().BeFalse("XYZ-9999 does not exist in the feature data");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task FeatureLookupAgent_WithAnthropic_GeneratesTraceWithCorrectTags()
    {
        // Arrange
        var agent = new FeatureLookupAgent(_kernelFactory, _mockAgentLogger.Object);
        var query = "Is PLAT-1523 ready for production?";

        // Act
        var result = await agent.LookupFeatureAsync(query);

        // Assert — result
        result.IsSuccess.Should().BeTrue();

        // Assert — trace
        Activity? mainActivity;
        lock (_capturedActivities)
        {
            mainActivity = _capturedActivities
                .FirstOrDefault(a => a.OperationName == "FeatureLookupAgent.LookupFeature");
        }

        Assert.IsNotNull(mainActivity, "FeatureLookupAgent.LookupFeature activity should be present");

        var queryTag = mainActivity.Tags.FirstOrDefault(t => t.Key == "query");
        Assert.AreEqual("query", queryTag.Key, "Query tag should be set");
        Assert.AreEqual(query, queryTag.Value);

        var isSuccessTag = mainActivity.Tags.FirstOrDefault(t => t.Key == "is_success");
        Assert.AreEqual("is_success", isSuccessTag.Key, "Success tag should be set");
        Assert.AreEqual("True", isSuccessTag.Value, "Success tag should be true");

        Assert.AreNotEqual(ActivityStatusCode.Error, mainActivity.Status,
            "Activity should not have error status on success");
    }
}
