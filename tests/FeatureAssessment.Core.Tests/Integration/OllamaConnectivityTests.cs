using FeatureAssessment.Core.Agents;
using FeatureAssessment.Core.Clients;
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
    private const string ModelName = "llama3.1:8b";

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
            content.Should().Contain("llama3.1",
                "Model llama3.1:8b should be available in Ollama. Pull it with 'ollama pull llama3.1:8b'");
        }
        catch (HttpRequestException ex)
        {
            Assert.Fail($"Cannot connect to Ollama at {OllamaEndpoint}. Error: {ex.Message}");
        }
    }

    [TestMethod]
    public async Task FeatureLookupAgent_CanConnectToOllama()
    {
        // Arrange
        var mockTools = new Mock<IFeatureLookupTools>();
        var mockAgentLogger = new Mock<ILogger<FeatureLookupAgent>>();
        var mockKernelFactoryLogger = new Mock<ILogger<KernelFactory>>();

        // Configure Ollama
        var ollamaConfig = new OllamaConfiguration
        {
            Endpoint = OllamaEndpoint, // Ollama connector doesn't need /v1 suffix
            ModelName = ModelName,
            Temperature = 0.0,
            MaxTokens = 100,
            TimeoutSeconds = 30,
            MaxRetries = 3
        };
        var ollamaConfigOptions = Microsoft.Extensions.Options.Options.Create(ollamaConfig);

        // Configure LLM Provider to use Ollama
        var providerConfig = new LlmProviderConfiguration { Provider = LlmProvider.Ollama };
        var providerConfigOptions = Microsoft.Extensions.Options.Options.Create(providerConfig);

        // Configure Anthropic (required but not used for this test)
        var anthropicConfig = new AnthropicConfiguration { ApiKey = "dummy" };
        var anthropicConfigOptions = Microsoft.Extensions.Options.Options.Create(anthropicConfig);

        // Setup mock tools to return empty list (agent should handle this gracefully)
        mockTools
            .Setup(t => t.ListAllFeaturesAsync())
            .ReturnsAsync(new List<FeatureInfo>());

        // Create KernelFactory and Agent
        var kernelFactory = new KernelFactory(
            providerConfigOptions,
            ollamaConfigOptions,
            anthropicConfigOptions,
            mockTools.Object,
            null,
            mockKernelFactoryLogger.Object);

        var agent = new FeatureLookupAgent(kernelFactory, mockAgentLogger.Object);

        // Act
        var result = await agent.LookupFeatureAsync("Is feature XYZ ready?");

        // Assert
        // We don't assert on exact result content (depends on LLM behavior)
        // We just verify the agent can execute without throwing exceptions
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse("because feature XYZ doesn't exist");

        // Note: We don't assert which tools were called — local LLMs (llama3.1:8b) are
        // non-deterministic and may respond without calling tools for clearly invalid feature IDs.
    }

    [TestMethod]
    [Ignore("llama3.1:8b produces non-deterministic results for strict field assertions. " +
            "Covered reliably by AnthropicEndToEndTests (Task 6).")]
    public async Task FeatureLookupAgent_WithRealTools_CanIdentifyFeature()
    {
        // Arrange
        var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "data", "incoming");
        var tools = new FeatureLookupTools(dataDirectory);
        var mockAgentLogger = new Mock<ILogger<FeatureLookupAgent>>();
        var mockKernelFactoryLogger = new Mock<ILogger<KernelFactory>>();

        // Configure Ollama
        var ollamaConfig = new OllamaConfiguration
        {
            Endpoint = OllamaEndpoint, // Ollama connector doesn't need /v1 suffix
            ModelName = ModelName,
            Temperature = 0.0,
            MaxTokens = 500,
            TimeoutSeconds = 30,
            MaxRetries = 3
        };
        var ollamaConfigOptions = Microsoft.Extensions.Options.Options.Create(ollamaConfig);

        // Configure LLM Provider to use Ollama
        var providerConfig = new LlmProviderConfiguration { Provider = LlmProvider.Ollama };
        var providerConfigOptions = Microsoft.Extensions.Options.Options.Create(providerConfig);

        // Configure Anthropic (required but not used for this test)
        var anthropicConfig = new AnthropicConfiguration { ApiKey = "dummy" };
        var anthropicConfigOptions = Microsoft.Extensions.Options.Options.Create(anthropicConfig);

        // Create KernelFactory and Agent
        var kernelFactory = new KernelFactory(
            providerConfigOptions,
            ollamaConfigOptions,
            anthropicConfigOptions,
            tools,
            null,
            mockKernelFactoryLogger.Object);

        var agent = new FeatureLookupAgent(kernelFactory, mockAgentLogger.Object);

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
