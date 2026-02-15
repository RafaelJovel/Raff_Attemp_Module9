using FeatureAssessment.Core.Clients;
using FeatureAssessment.Core.Configuration;
using FeatureAssessment.Core.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Moq;

namespace FeatureAssessment.Core.Tests.Helpers;

/// <summary>
/// Helper class for creating mock IKernelFactory instances for testing.
/// </summary>
public static class MockKernelFactoryHelper
{
    /// <summary>
    /// Creates a mock IKernelFactory that returns a kernel with mocked tools.
    /// </summary>
    public static Mock<IKernelFactory> CreateMockFactory(
        Mock<IFeatureLookupTools> mockTools,
        LlmProvider provider = LlmProvider.Ollama)
    {
        var mockFactory = new Mock<IKernelFactory>();

        mockFactory
            .Setup(f => f.CurrentProvider)
            .Returns(provider);

        mockFactory
            .Setup(f => f.CreateKernel())
            .Returns(() =>
            {
                var builder = Kernel.CreateBuilder();

                // Register the mock tools as a plugin
                builder.Plugins.AddFromObject(mockTools.Object, "FeatureLookup");

                return builder.Build();
            });

        return mockFactory;
    }

    /// <summary>
    /// Creates a mock IKernelFactory for use when no tools are needed.
    /// </summary>
    public static Mock<IKernelFactory> CreateBasicMockFactory(LlmProvider provider = LlmProvider.Ollama)
    {
        var mockFactory = new Mock<IKernelFactory>();

        mockFactory
            .Setup(f => f.CurrentProvider)
            .Returns(provider);

        mockFactory
            .Setup(f => f.CreateKernel())
            .Returns(() => Kernel.CreateBuilder().Build());

        return mockFactory;
    }
}
