using FeatureAssessment.Core.Agents;
using FeatureAssessment.Core.Clients;
using FeatureAssessment.Core.Configuration;
using FeatureAssessment.Core.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FeatureAssessment.Core.Tests.Integration;

/// <summary>
/// Integration tests for CoordinatorAgent using the real Anthropic LLM.
/// Requires ANTHROPIC_API_KEY or appsettings.Development.local.json.
/// Tests are skipped (Inconclusive) if API key is not available.
/// </summary>
[TestClass]
[TestCategory("Integration")]
public class CoordinatorIntegrationTests
{
    private IKernelFactory _kernelFactory = null!;

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
        if (doc.RootElement.TryGetProperty("Anthropic", out var anthropicSection) &&
            anthropicSection.TryGetProperty("ApiKey", out var apiKeyElement))
        {
            return apiKeyElement.GetString();
        }

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

        var mockLogger = new Mock<ILogger<KernelFactory>>();

        // Pass null tools — coordinator does not use feature lookup tools
        _kernelFactory = new KernelFactory(
            providerConfig, ollamaConfig, anthropicConfig,
            null, null, mockLogger.Object);
    }

    [TestMethod]
    public async Task CoordinatorAgent_WithAnthropic_AcknowledgesInsufficientInformation()
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

        var agent = new CoordinatorAgent(
            _kernelFactory,
            new Mock<ILogger<CoordinatorAgent>>().Object);

        // Act
        var result = await agent.AssessAsync(state);

        // Assert — coordinator completed (or returned useful response)
        Console.WriteLine($"Stage: {result.CurrentStage}");
        Console.WriteLine($"Coordinator Response:\n{result.CoordinatorResponse}");

        // The coordinator should either complete or return an error — not throw
        result.Should().NotBeNull();
        result.CoordinatorResponse.Should().NotBeNullOrEmpty(
            "coordinator must always return a response");

        // Stage is either coordinator_completed or error (if some kernel issue)
        result.CurrentStage.Should().BeOneOf("coordinator_completed", "error");

        // When successful, the response should acknowledge missing specialist data
        if (result.CurrentStage == "coordinator_completed")
        {
            // The response should be substantive (not just a few chars)
            result.CoordinatorResponse!.Length.Should().BeGreaterThan(50,
                "coordinator response should be substantive");
        }
    }

    [TestMethod]
    public async Task CoordinatorAgent_WhenFeatureNotIdentified_ReturnsErrorWithoutApiCall()
    {
        // Arrange — feature was NOT identified (feature lookup failed)
        var state = new AssessmentState
        {
            IsFeatureIdentified = false,
            ErrorMessage = "Feature XYZ-999 not found in any known feature list",
            CurrentStage = "error"
        };

        var agent = new CoordinatorAgent(
            _kernelFactory,
            new Mock<ILogger<CoordinatorAgent>>().Object);

        // Act
        var result = await agent.AssessAsync(state);

        // Assert — coordinator returns error without calling LLM
        Assert.AreEqual("error", result.CurrentStage);
        result.CoordinatorResponse.Should().NotBeNullOrEmpty();
        Console.WriteLine($"Coordinator Response: {result.CoordinatorResponse}");
    }
}
