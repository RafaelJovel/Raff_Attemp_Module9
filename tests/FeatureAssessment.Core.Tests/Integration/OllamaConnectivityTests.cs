using FeatureAssessment.Core.Agents;
using FeatureAssessment.Core.Configuration;
using FeatureAssessment.Core.Models;
using FeatureAssessment.Core.Tools;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FeatureAssessment.Core.Tests.Integration;

[TestClass]
[TestCategory("Integration")]
public class OllamaConnectivityTests
{
    private const string OllamaEndpoint = "http://localhost:11434";
    private const string ModelName = "qwen2.5:0.5b";

    [TestMethod]
    public async Task OllamaEndpoint_IsReachable()
    {
        // Arrange
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(5);

        // Act
        try
        {
            var response = await httpClient.GetAsync($"{OllamaEndpoint}/api/tags");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue(
                "Ollama should be running at {0}. Start Ollama with 'docker run -d -p 11434:11434 ollama/ollama'",
                OllamaEndpoint);
        }
        catch (HttpRequestException ex)
        {
            Assert.Fail($"Cannot connect to Ollama at {OllamaEndpoint}. Ensure Ollama is running. Error: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            Assert.Fail($"Connection to Ollama at {OllamaEndpoint} timed out. Ensure Ollama is running.");
        }
    }

    [TestMethod]
    public async Task OllamaModel_IsAvailable()
    {
        // Arrange
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(5);

        // Act
        try
        {
            var response = await httpClient.GetAsync($"{OllamaEndpoint}/api/tags");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            // Assert
            content.Should().Contain("qwen2.5",
                "Model qwen2.5 should be available in Ollama. Pull it with 'docker exec <container> ollama pull qwen2.5'");
        }
        catch (HttpRequestException ex)
        {
            Assert.Fail($"Cannot connect to Ollama at {OllamaEndpoint}. Error: {ex.Message}");
        }
    }

    [TestMethod]
    [Ignore("Configuration fix required - see Task 4 in workitem002.md")]
    public async Task FeatureLookupAgent_CanConnectToOllama()
    {
        // Arrange
        var mockTools = new Mock<IFeatureLookupTools>();
        var mockLogger = new Mock<ILogger<FeatureLookupAgent>>();
        var config = new OllamaConfiguration
        {
            Endpoint = OllamaEndpoint,
            ModelName = ModelName,
            Temperature = 0.0,
            MaxTokens = 100,
            TimeoutSeconds = 30,
            MaxRetries = 3
        };
        var configOptions = Microsoft.Extensions.Options.Options.Create(config);

        // Setup mock tools to return empty list (agent should handle this gracefully)
        mockTools
            .Setup(t => t.ListAllFeaturesAsync())
            .ReturnsAsync(new List<FeatureInfo>());

        var agent = new FeatureLookupAgent(mockTools.Object, configOptions, mockLogger.Object);

        // Act
        var result = await agent.LookupFeatureAsync("Is feature XYZ ready?");

        // Assert
        // We don't assert on exact result content (depends on LLM behavior)
        // We just verify the agent can execute without throwing exceptions
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse("because feature XYZ doesn't exist");

        // Verify tools were called (agent should call ListAllFeaturesAsync to check available features)
        mockTools.Verify(t => t.ListAllFeaturesAsync(), Times.AtLeastOnce);
    }

    [TestMethod]
    [Ignore("Configuration fix required - see Task 4 in workitem002.md")]
    public async Task FeatureLookupAgent_WithRealTools_CanIdentifyFeature()
    {
        // Arrange
        var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "data", "incoming");
        var tools = new FeatureLookupTools(dataDirectory);
        var mockLogger = new Mock<ILogger<FeatureLookupAgent>>();
        var config = new OllamaConfiguration
        {
            Endpoint = OllamaEndpoint,
            ModelName = ModelName,
            Temperature = 0.0,
            MaxTokens = 500,
            TimeoutSeconds = 30,
            MaxRetries = 3
        };
        var configOptions = Microsoft.Extensions.Options.Options.Create(config);

        var agent = new FeatureLookupAgent(tools, configOptions, mockLogger.Object);

        // Act
        var result = await agent.LookupFeatureAsync("Is PLAT-1523 ready for production?");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.FeatureKey.Should().Be("PLAT-1523");
        result.FeatureId.Should().Be("feature1");
        result.TargetEnvironment.Should().Be("Production");
    }
}
