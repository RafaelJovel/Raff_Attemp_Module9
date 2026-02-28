using FeatureAssessment.Core.Configuration;
using Microsoft.Extensions.Options;

namespace FeatureAssessment.Core.Tests.Configuration;

[TestClass]
public class OllamaConfigurationValidatorTests
{
    private OllamaConfigurationValidator _validator = null!;

    [TestInitialize]
    public void Setup()
    {
        _validator = new OllamaConfigurationValidator();
    }

    [TestMethod]
    public void Validate_WithValidConfiguration_ReturnsSuccess()
    {
        // Arrange
        var config = new OllamaConfiguration
        {
            Endpoint = "http://localhost:11434",
            ModelName = "qwen2.5:latest",
            Temperature = 0.0,
            MaxTokens = 500,
            TimeoutSeconds = 30,
            MaxRetries = 3
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public void Validate_WithEmptyEndpoint_ReturnsFailed()
    {
        // Arrange
        var config = new OllamaConfiguration
        {
            Endpoint = "",
            ModelName = "qwen2.5:latest"
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        Assert.IsFalse(result.Succeeded);
        Assert.Contains("Endpoint cannot be empty", result.FailureMessage!);
    }

    [TestMethod]
    public void Validate_WithInvalidEndpointUri_ReturnsFailed()
    {
        // Arrange
        var config = new OllamaConfiguration
        {
            Endpoint = "not-a-valid-uri",
            ModelName = "qwen2.5:latest"
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        Assert.IsFalse(result.Succeeded);
        Assert.Contains("must be a valid HTTP or HTTPS URL", result.FailureMessage!);
    }

    [TestMethod]
    public void Validate_WithFtpScheme_ReturnsFailed()
    {
        // Arrange
        var config = new OllamaConfiguration
        {
            Endpoint = "ftp://localhost:11434",
            ModelName = "qwen2.5:latest"
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        Assert.IsFalse(result.Succeeded);
        Assert.Contains("must be a valid HTTP or HTTPS URL", result.FailureMessage!);
    }

    [TestMethod]
    public void Validate_WithEmptyModelName_ReturnsFailed()
    {
        // Arrange
        var config = new OllamaConfiguration
        {
            Endpoint = "http://localhost:11434",
            ModelName = ""
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        Assert.IsFalse(result.Succeeded);
        Assert.Contains("ModelName cannot be empty", result.FailureMessage!);
    }

    [TestMethod]
    public void Validate_WithTemperatureBelowZero_ReturnsFailed()
    {
        // Arrange
        var config = new OllamaConfiguration
        {
            Endpoint = "http://localhost:11434",
            ModelName = "qwen2.5:latest",
            Temperature = -0.1
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        Assert.IsFalse(result.Succeeded);
        Assert.Contains("Temperature must be between 0.0 and 1.0", result.FailureMessage!);
    }

    [TestMethod]
    public void Validate_WithTemperatureAboveOne_ReturnsFailed()
    {
        // Arrange
        var config = new OllamaConfiguration
        {
            Endpoint = "http://localhost:11434",
            ModelName = "qwen2.5:latest",
            Temperature = 1.1
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        Assert.IsFalse(result.Succeeded);
        Assert.Contains("Temperature must be between 0.0 and 1.0", result.FailureMessage!);
    }

    [TestMethod]
    public void Validate_WithZeroMaxTokens_ReturnsFailed()
    {
        // Arrange
        var config = new OllamaConfiguration
        {
            Endpoint = "http://localhost:11434",
            ModelName = "qwen2.5:latest",
            MaxTokens = 0
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        Assert.IsFalse(result.Succeeded);
        Assert.Contains("MaxTokens must be greater than 0", result.FailureMessage!);
    }

    [TestMethod]
    public void Validate_WithNegativeMaxTokens_ReturnsFailed()
    {
        // Arrange
        var config = new OllamaConfiguration
        {
            Endpoint = "http://localhost:11434",
            ModelName = "qwen2.5:latest",
            MaxTokens = -1
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        Assert.IsFalse(result.Succeeded);
        Assert.Contains("MaxTokens must be greater than 0", result.FailureMessage!);
    }

    [TestMethod]
    public void Validate_WithZeroTimeoutSeconds_ReturnsFailed()
    {
        // Arrange
        var config = new OllamaConfiguration
        {
            Endpoint = "http://localhost:11434",
            ModelName = "qwen2.5:latest",
            TimeoutSeconds = 0
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        Assert.IsFalse(result.Succeeded);
        Assert.Contains("TimeoutSeconds must be greater than 0", result.FailureMessage!);
    }

    [TestMethod]
    public void Validate_WithNegativeMaxRetries_ReturnsFailed()
    {
        // Arrange
        var config = new OllamaConfiguration
        {
            Endpoint = "http://localhost:11434",
            ModelName = "qwen2.5:latest",
            MaxRetries = -1
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        Assert.IsFalse(result.Succeeded);
        Assert.Contains("MaxRetries must be 0 or greater", result.FailureMessage!);
    }

    [TestMethod]
    public void Validate_WithZeroMaxRetries_ReturnsSuccess()
    {
        // Arrange
        var config = new OllamaConfiguration
        {
            Endpoint = "http://localhost:11434",
            ModelName = "qwen2.5:latest",
            MaxRetries = 0
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var config = new OllamaConfiguration
        {
            Endpoint = "",
            ModelName = "",
            Temperature = -0.5,
            MaxTokens = 0,
            TimeoutSeconds = -1,
            MaxRetries = -1
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        Assert.IsFalse(result.Succeeded);
        var failures = result.FailureMessage!;
        Assert.Contains("Endpoint", failures);
        Assert.Contains("ModelName", failures);
        Assert.Contains("Temperature", failures);
        Assert.Contains("MaxTokens", failures);
        Assert.Contains("TimeoutSeconds", failures);
        Assert.Contains("MaxRetries", failures);
    }

    [TestMethod]
    public void Validate_WithHttpsEndpoint_ReturnsSuccess()
    {
        // Arrange
        var config = new OllamaConfiguration
        {
            Endpoint = "https://ollama.example.com",
            ModelName = "qwen2.5:latest"
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        Assert.IsTrue(result.Succeeded);
    }
}
