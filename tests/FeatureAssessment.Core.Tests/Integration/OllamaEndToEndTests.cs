using FeatureAssessment.Core.Agents;
using FeatureAssessment.Core.Configuration;
using FeatureAssessment.Core.Models;
using FeatureAssessment.Core.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FeatureAssessment.Core.Tests.Integration;

/// <summary>
/// End-to-end integration tests that validate the complete flow:
/// Configuration → Agent → Real Ollama → State Management
/// Requires Ollama running locally with qwen2.5:latest model.
/// </summary>
[TestClass]
[TestCategory("Integration")]
public class OllamaEndToEndTests
{
    private IOptions<OllamaConfiguration> _configOptions = null!;
    private Mock<IFeatureLookupTools> _mockTools = null!;
    private Mock<ILogger<FeatureLookupAgent>> _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        var config = new OllamaConfiguration
        {
            Endpoint = "http://localhost:11434",
            ModelName = "qwen2.5:latest",
            Temperature = 0.0,
            MaxTokens = 500,
            TimeoutSeconds = 30,
            MaxRetries = 3
        };

        _configOptions = Options.Create(config);
        _mockTools = new Mock<IFeatureLookupTools>();
        _mockLogger = new Mock<ILogger<FeatureLookupAgent>>();

        // Note: Mock tool setup omitted - these tests focus on configuration and state management
        // Real tool integration is validated in OllamaConnectivityTests
    }

    [TestMethod]
    public async Task EndToEnd_WithValidConfiguration_CreatesAgentSuccessfully()
    {
        // This test validates that configuration, agent creation, and basic setup work correctly.
        // It doesn't invoke real Ollama (see other integration tests for that).

        // Arrange & Act
        var agent = new FeatureLookupAgent(_mockTools.Object, _configOptions, _mockLogger.Object);

        // Assert
        Assert.IsNotNull(agent);
    }

    [TestMethod]
    public async Task EndToEnd_StateManagement_FromLookupResultToAssessmentState()
    {
        // This test validates the state management flow:
        // 1. Agent returns FeatureLookupResult
        // 2. Result is converted to AssessmentState
        // 3. State can be updated and tracked

        // Arrange
        var lookupResult = new FeatureLookupResult
        {
            FeatureId = "feature1",
            FeatureKey = "PLAT-1523",
            TargetEnvironment = "Production",
            IsSuccess = true
        };

        // Act - Convert to state
        var state = AssessmentState.FromFeatureLookupResult(lookupResult);

        // Assert initial state
        Assert.AreEqual("feature1", state.FeatureId);
        Assert.AreEqual("PLAT-1523", state.FeatureKey);
        Assert.AreEqual("Production", state.TargetEnvironment);
        Assert.AreEqual("feature_lookup_completed", state.CurrentStage);
        Assert.IsTrue(state.IsFeatureIdentified);

        // Act - Update state to next stage
        var updatedState = state.WithStage("coordinator");

        // Assert state transition
        Assert.AreEqual("coordinator", updatedState.CurrentStage);
        Assert.AreEqual("feature1", updatedState.FeatureId);

        // Act - Add metadata
        var stateWithMetadata = updatedState.WithMetadata("start_time", DateTime.UtcNow);

        // Assert metadata
        Assert.IsTrue(stateWithMetadata.Metadata.ContainsKey("start_time"));
        Assert.AreEqual(1, stateWithMetadata.Metadata.Count);
    }

    [TestMethod]
    public async Task EndToEnd_ConfigurationValidation_WithValidConfig_Succeeds()
    {
        // Arrange
        var validator = new OllamaConfigurationValidator();
        var config = _configOptions.Value;

        // Act
        var result = validator.Validate(null, config);

        // Assert
        Assert.IsTrue(result.Succeeded, "Configuration validation should pass with valid settings");
    }

    [TestMethod]
    public async Task EndToEnd_ConfigurationValidation_WithInvalidConfig_Fails()
    {
        // Arrange
        var validator = new OllamaConfigurationValidator();
        var invalidConfig = new OllamaConfiguration
        {
            Endpoint = "",  // Invalid - empty
            ModelName = "",  // Invalid - empty
            TimeoutSeconds = -1  // Invalid - negative
        };

        // Act
        var result = validator.Validate(null, invalidConfig);

        // Assert
        Assert.IsFalse(result.Succeeded, "Configuration validation should fail with invalid settings");
        Assert.IsNotNull(result.FailureMessage);
    }

    [TestMethod]
    [Ignore("Requires real Ollama running - enable for manual integration testing")]
    public async Task EndToEnd_RealOllama_WithSimpleQuery_ReturnsResult()
    {
        // This test requires Ollama to be running locally with qwen2.5:latest model.
        // Enable this test for manual validation of the complete integration.

        // Arrange
        var agent = new FeatureLookupAgent(_mockTools.Object, _configOptions, _mockLogger.Object);
        var query = "Is PLAT-1523 ready for production?";

        // Act
        var result = await agent.LookupFeatureAsync(query);

        // Assert
        Assert.IsNotNull(result);
        // Note: Full validation depends on Ollama's response and model behavior
        // In manual testing, verify that:
        // - Result contains feature_key or feature_id
        // - Target environment is correctly extracted
        // - IsSuccess is true if feature found
    }
}
