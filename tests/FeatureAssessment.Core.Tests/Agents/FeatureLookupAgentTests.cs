using FeatureAssessment.Core.Agents;
using FeatureAssessment.Core.Configuration;
using FeatureAssessment.Core.Tools;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FeatureAssessment.Core.Tests.Agents;

[TestClass]
public class FeatureLookupAgentTests
{
    private Mock<IFeatureLookupTools> _mockTools = null!;
    private Mock<ILogger<FeatureLookupAgent>> _mockLogger = null!;
    private Mock<IOptions<OllamaConfiguration>> _mockConfigOptions = null!;
    private OllamaConfiguration _config = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockTools = new Mock<IFeatureLookupTools>();
        _mockLogger = new Mock<ILogger<FeatureLookupAgent>>();
        _config = new OllamaConfiguration
        {
            Endpoint = "http://localhost:11434",
            ModelName = "qwen2.5:latest",
            Temperature = 0.0,
            MaxTokens = 500,
            TimeoutSeconds = 30,
            MaxRetries = 3
        };
        _mockConfigOptions = new Mock<IOptions<OllamaConfiguration>>();
        _mockConfigOptions.Setup(x => x.Value).Returns(_config);
    }

    [TestMethod]
    public void Constructor_WithNullTools_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new FeatureLookupAgent(null!, _mockConfigOptions.Object, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tools");
    }

    [TestMethod]
    public void Constructor_WithNullConfigOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new FeatureLookupAgent(_mockTools.Object, null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("configOptions");
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new FeatureLookupAgent(_mockTools.Object, _mockConfigOptions.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [TestMethod]
    public async Task LookupFeatureAsync_WithNullQuery_ThrowsArgumentException()
    {
        // Arrange
        var agent = new FeatureLookupAgent(_mockTools.Object, _mockConfigOptions.Object, _mockLogger.Object);

        // Act & Assert
        var act = async () => await agent.LookupFeatureAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("query");
    }

    [TestMethod]
    public async Task LookupFeatureAsync_WithEmptyQuery_ThrowsArgumentException()
    {
        // Arrange
        var agent = new FeatureLookupAgent(_mockTools.Object, _mockConfigOptions.Object, _mockLogger.Object);

        // Act & Assert
        var act = async () => await agent.LookupFeatureAsync("");
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("query");
    }

    [TestMethod]
    public async Task LookupFeatureAsync_WithWhitespaceQuery_ThrowsArgumentException()
    {
        // Arrange
        var agent = new FeatureLookupAgent(_mockTools.Object, _mockConfigOptions.Object, _mockLogger.Object);

        // Act & Assert
        var act = async () => await agent.LookupFeatureAsync("   ");
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("query");
    }

    // NOTE: The following tests require integration with a real LLM (Ollama).
    // They are marked as [Ignore] because we cannot easily mock Semantic Kernel's
    // internal LLM interactions in unit tests. These scenarios are validated
    // in integration tests and manual testing.

    [TestMethod]
    [Ignore("Requires real Ollama LLM - validated in integration tests")]
    public async Task LookupFeatureAsync_WithJiraKeyAndProduction_ReturnsCorrectResult()
    {
        // This test validates:
        // - Query: "Is PLAT-1523 ready for production?"
        // - Expected: feature_key="PLAT-1523", target_environment="Production"
        // - Agent should call GetFeatureMetadataAsync("PLAT-1523")

        // Implementation verified in OllamaConnectivityTests integration tests
        await Task.CompletedTask;
    }

    [TestMethod]
    [Ignore("Requires real Ollama LLM - validated in integration tests")]
    public async Task LookupFeatureAsync_WithFeatureNameAndUAT_ReturnsCorrectResult()
    {
        // This test validates:
        // - Query: "Check maintenance scheduling for UAT"
        // - Expected: Feature matched by name (fuzzy match), target_environment="UAT"
        // - Agent should call ListAllFeaturesAsync(), then GetFeatureMetadataAsync()

        // Implementation verified in OllamaConnectivityTests integration tests
        await Task.CompletedTask;
    }

    [TestMethod]
    [Ignore("Requires real Ollama LLM - validated in integration tests")]
    public async Task LookupFeatureAsync_WithNonExistentFeature_ReturnsError()
    {
        // This test validates:
        // - Query: "Is feature XYZ ready for production?"
        // - Expected: IsSuccess=false, ErrorMessage contains "FEATURE_NOT_FOUND"

        // Implementation verified in OllamaConnectivityTests integration tests
        await Task.CompletedTask;
    }

    [TestMethod]
    [Ignore("Requires real Ollama LLM - validated in integration tests")]
    public async Task LookupFeatureAsync_WithoutEnvironmentSpecified_DefaultsToUAT()
    {
        // This test validates:
        // - Query: "Tell me about PLAT-1523"
        // - Expected: target_environment="UAT" (default when not specified)

        // Implementation verified in OllamaConnectivityTests integration tests
        await Task.CompletedTask;
    }
}
