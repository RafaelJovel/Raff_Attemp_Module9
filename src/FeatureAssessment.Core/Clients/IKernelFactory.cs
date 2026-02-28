using Microsoft.SemanticKernel;
using FeatureAssessment.Core.Configuration;

namespace FeatureAssessment.Core.Clients;

/// <summary>
/// Factory for creating configured Semantic Kernel instances based on LLM provider selection.
/// Abstracts away provider-specific client initialization.
/// </summary>
public interface IKernelFactory
{
    /// <summary>
    /// Creates a configured Semantic Kernel with the appropriate LLM provider
    /// and plugins registered.
    /// </summary>
    /// <returns>Configured Kernel instance ready for agent use.</returns>
    Kernel CreateKernel();

    /// <summary>
    /// Gets the currently configured LLM provider.
    /// </summary>
    LlmProvider CurrentProvider { get; }
}
