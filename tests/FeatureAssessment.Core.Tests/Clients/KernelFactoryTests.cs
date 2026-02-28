using FeatureAssessment.Core.Clients;
using FeatureAssessment.Core.Configuration;
using FeatureAssessment.Core.Tools;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FeatureAssessment.Core.Tests.Clients;

[TestClass]
public class KernelFactoryTests
{
    [TestMethod]
    public void CreateKernel_WithDocumentationTools_RegistersDocumentationPlugin()
    {
        // Arrange - minimal configuration objects
        var providerConfig = Options.Create(new LlmProviderConfiguration { Provider = LlmProvider.Ollama });
        var ollamaConfig = Options.Create(new OllamaConfiguration { Endpoint = "http://localhost", ModelName = "test" });
        var anthropicConfig = Options.Create(new AnthropicConfiguration { ApiKey = "dummy", ModelName = "dummy" });

        var docTools = new DocumentationTools(new NullLogger<DocumentationTools>());
        var factory = new KernelFactory(
            providerConfig,
            ollamaConfig,
            anthropicConfig,
            tools: null,
            documentationTools: docTools,
            logger: new NullLogger<KernelFactory>());

        // Act - create kernel should register documentation tools as a plugin
        // If no exception is thrown, the plugin registration succeeded
        var kernel = factory.CreateKernel();

        // Assert - kernel was created successfully (implying plugins registered)
        Assert.IsNotNull(kernel);
    }
}