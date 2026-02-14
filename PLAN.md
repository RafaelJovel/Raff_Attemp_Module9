# Feature Readiness Assessment Implementation Plan (.NET 10)

## Overview

.NET 10 implementation of the Feature Readiness Assessment System. See [DESIGN.md](DESIGN.md) for more about **what** this system does and **why** design decisions were made.

This document covers **how** to build the agent in .NET - specific packages, project structure, testing approach, and implementation details.

## Implementation Goals

- Clear, readable C# code that shows exactly what's happening
- Multi-provider support (Anthropic, OpenRouter, Ollama, etc)
- OpenTelemetry observability
- Context window management
- Retry mechanism with exponential backoff (using Polly)
- Tool calling foundation
- Interaction persistence
- Basic reasoning and evaluations

## Implementation Constitution

- Clear, readable C# code that shows exactly what's happening
- For interfaces, use `interface` keyword (NOT abstract classes)
- Place unit tests in a separate test project following .NET conventions
- Unit test projects use `.Tests` suffix (e.g., `FeatureAssessment.Tests`)
- Unit test files use `Tests.cs` suffix (e.g., `CoordinatorAgentTests.cs`)
- The `tests/` folder should contain integration tests only
- Use modern C# features: records, nullable reference types, pattern matching, file-scoped namespaces
- Use `dotnet new` to create projects and `dotnet add package` for dependencies
- Never manually edit `.csproj` files unless absolutely necessary

## Implementation Steps

The recommended order of implementation is defined in [STEPS.md](STEPS.md). The phases of implementation defined later in this document align with these progression of steps.

## Technology Stack

- **.NET 10** with C# 13 and async/await (Task-based)
- **dotnet CLI** for project and dependency management
- **OpenRouter** as the LLM provider
- **Microsoft Semantic Kernel** as the agentic framework
- **HttpClient** (built-in) for HTTP client with IHttpClientFactory
- **OpenTelemetry .NET** for traces and metrics
- **Microsoft.Extensions.Configuration** for configuration settings
- **FluentValidation** for validation
- **MSTest** for testing
- **Moq** for mocking, **WireMock.Net** for HTTP mocking
- **Polly** for resilience and retry policies

## Project Structure

```
FeatureReadinessAssessment/
├── src/
│   ├── FeatureAssessment.Core/           # Core domain models and interfaces
│   │   ├── Models/
│   │   ├── Interfaces/
│   │   └── FeatureAssessment.Core.csproj
│   ├── FeatureAssessment.Agents/         # Agent implementations
│   │   ├── Lookup/
│   │   ├── Coordinator/
│   │   ├── Specialists/
│   │   └── FeatureAssessment.Agents.csproj
│   ├── FeatureAssessment.Tools/          # Tool implementations
│   │   ├── Documentation/
│   │   ├── Metrics/
│   │   ├── Reviews/
│   │   └── FeatureAssessment.Tools.csproj
│   └── FeatureAssessment.Infrastructure/ # Infrastructure concerns
│       ├── LLM/
│       ├── Telemetry/
│       └── FeatureAssessment.Infrastructure.csproj
├── tests/
│   ├── FeatureAssessment.Core.Tests/
│   ├── FeatureAssessment.Agents.Tests/
│   ├── FeatureAssessment.Tools.Tests/
│   └── FeatureAssessment.IntegrationTests/
├── data/                                  # Sample data (not part of solution)
│   └── incoming/
├── FeatureReadinessAssessment.sln
└── global.json                            # Pin .NET SDK version
```

## Key .NET Packages

### Agentic Framework
- **Microsoft.SemanticKernel** - Microsoft's AI orchestration SDK for AI orchestration and tool calling

### LLM Integration
- **Microsoft.SemanticKernel.Connectors.OpenAI** - For OpenAI-compatible APIs
- Custom HTTP client for OpenRouter integration

### Observability
- **OpenTelemetry** - Core package
- **OpenTelemetry.Exporter.Console** - Development/testing
- **OpenTelemetry.Exporter.OpenTelemetryProtocol** - Production OTLP export
- **OpenTelemetry.Instrumentation.Http** - HTTP client instrumentation

### Resilience
- **Polly** - Retry, circuit breaker, timeout policies
- **Microsoft.Extensions.Http.Polly** - Polly integration with IHttpClientFactory

### Configuration & Validation
- **Microsoft.Extensions.Configuration** - Configuration framework
- **Microsoft.Extensions.Configuration.Json** - JSON config files
- **Microsoft.Extensions.Options** - Options pattern
- **FluentValidation** - Fluent validation library

### Testing
- **MSTest** - Microsoft's official testing framework (MSTest.TestFramework, MSTest.TestAdapter)
- **Moq** - Popular mocking framework for .NET
- **FluentAssertions** - Fluent assertion library for more readable tests
- **WireMock.Net** - HTTP API mocking for integration tests
- **Microsoft.NET.Test.Sdk** - Test runner
- **coverlet.collector** - Code coverage collection

### Utilities
- **System.Text.Json** - JSON serialization (built-in, high performance)
- **Microsoft.Extensions.DependencyInjection** - Dependency injection
- **Microsoft.Extensions.Logging** - Logging abstractions

## Development Workflow

### Initial Setup
```bash
# Create solution
dotnet new sln -n FeatureReadinessAssessment

# Create projects
dotnet new classlib -n FeatureAssessment.Core -o src/FeatureAssessment.Core
dotnet new classlib -n FeatureAssessment.Agents -o src/FeatureAssessment.Agents
dotnet new classlib -n FeatureAssessment.Tools -o src/FeatureAssessment.Tools
dotnet new classlib -n FeatureAssessment.Infrastructure -o src/FeatureAssessment.Infrastructure

# Create test projects
dotnet new mstest -n FeatureAssessment.Core.Tests -o tests/FeatureAssessment.Core.Tests
dotnet new mstest -n FeatureAssessment.Agents.Tests -o tests/FeatureAssessment.Agents.Tests
dotnet new mstest -n FeatureAssessment.Tools.Tests -o tests/FeatureAssessment.Tools.Tests
dotnet new mstest -n FeatureAssessment.IntegrationTests -o tests/FeatureAssessment.IntegrationTests

# Add projects to solution
dotnet sln add src/**/*.csproj
dotnet sln add tests/**/*.csproj

# Add project references
dotnet add src/FeatureAssessment.Agents reference src/FeatureAssessment.Core
dotnet add src/FeatureAssessment.Tools reference src/FeatureAssessment.Core
dotnet add tests/FeatureAssessment.Core.Tests reference src/FeatureAssessment.Core
# ... and so on
```

### Adding Packages
```bash
# Add Semantic Kernel
dotnet add src/FeatureAssessment.Infrastructure package Microsoft.SemanticKernel

# Add OpenTelemetry
dotnet add src/FeatureAssessment.Infrastructure package OpenTelemetry
dotnet add src/FeatureAssessment.Infrastructure package OpenTelemetry.Exporter.Console

# Add Polly for resilience
dotnet add src/FeatureAssessment.Infrastructure package Polly
dotnet add src/FeatureAssessment.Infrastructure package Microsoft.Extensions.Http.Polly

# Add testing packages
dotnet add tests/FeatureAssessment.Core.Tests package Moq
dotnet add tests/FeatureAssessment.Core.Tests package FluentAssertions
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run only unit tests (exclude integration)
dotnet test --filter "Category!=Integration"

# Run specific test project
dotnet test tests/FeatureAssessment.Core.Tests
```

### Building
```bash
# Build solution
dotnet build

# Build in Release mode
dotnet build -c Release

# Clean build artifacts
dotnet clean
```

## Code Style & Conventions

### Naming Conventions
- **Interfaces**: `IFeatureLookupAgent`, `IDocumentationTool`
- **Implementations**: `FeatureLookupAgent`, `DocumentationTool`
- **Records**: `FeatureMetadata`, `AssessmentResult`
- **Test classes**: `FeatureLookupAgentTests`, `DocumentationToolTests`
- **Test methods**: `ShouldReturnFeatureMetadata_WhenJiraKeyIsValid()`

### Modern C# Features to Use
- **File-scoped namespaces**: `namespace FeatureAssessment.Core;`
- **Records for DTOs**: `public record FeatureMetadata(...);`
- **Nullable reference types**: Enable in all projects
- **Pattern matching**: Use for decision logic
- **Primary constructors**: For simple classes (C# 12+)
- **Required members**: For mandatory properties
- **Init-only properties**: For immutable objects

### Example Code Structure

```csharp
// IFeatureLookupAgent.cs
namespace FeatureAssessment.Agents.Lookup;

public interface IFeatureLookupAgent
{
    Task<FeatureLookupResult> LookupFeatureAsync(
        string query,
        CancellationToken cancellationToken = default);
}

// FeatureLookupAgent.cs
namespace FeatureAssessment.Agents.Lookup;

public class FeatureLookupAgent(
    IFeatureMetadataTool featureMetadataTool,
    ILogger<FeatureLookupAgent> logger) : IFeatureLookupAgent
{
    public async Task<FeatureLookupResult> LookupFeatureAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Looking up feature from query: {Query}", query);

        // Implementation here

        return new FeatureLookupResult
        {
            FeatureId = "feature1",
            FeatureKey = "PLAT-1523",
            // ...
        };
    }
}

// FeatureLookupResult.cs
namespace FeatureAssessment.Core.Models;

public record FeatureLookupResult
{
    public required string FeatureId { get; init; }
    public required string FeatureKey { get; init; }
    public string? CurrentStage { get; init; }
    public string? TargetEnvironment { get; init; }
    public string? Error { get; init; }
}
```

## Testing Strategy

### Unit Tests
- Test each agent and tool in isolation
- Mock all dependencies using Moq or NSubstitute
- Use FluentAssertions for readable test assertions
- Follow AAA pattern (Arrange, Act, Assert)

### Integration Tests
- Test full workflow from query to decision
- Use real file system with test data fixtures
- Mark with `[Trait("Category", "Integration")]`
- May use WireMock.Net for external HTTP dependencies

### Example Test

```csharp
namespace FeatureAssessment.Agents.Tests;

[TestClass]
public class FeatureLookupAgentTests
{
    private Mock<IFeatureMetadataTool> _mockMetadataTool = null!;
    private Mock<ILogger<FeatureLookupAgent>> _mockLogger = null!;
    private FeatureLookupAgent _agent = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockMetadataTool = new Mock<IFeatureMetadataTool>();
        _mockLogger = new Mock<ILogger<FeatureLookupAgent>>();
        _agent = new FeatureLookupAgent(_mockMetadataTool.Object, _mockLogger.Object);
    }

    [TestMethod]
    public async Task ShouldReturnFeatureMetadata_WhenJiraKeyIsValid()
    {
        // Arrange
        var expectedMetadata = new FeatureMetadata
        {
            FeatureId = "feature1",
            JiraKey = "PLAT-1523"
        };

        _mockMetadataTool
            .Setup(x => x.GetFeatureMetadataAsync("PLAT-1523", default))
            .ReturnsAsync(expectedMetadata);

        // Act
        var result = await _agent.LookupFeatureAsync("Is PLAT-1523 ready?");

        // Assert
        result.Should().NotBeNull();
        result.FeatureKey.Should().Be("PLAT-1523");
        result.Error.Should().BeNull();
    }
}
```

## Configuration

### appsettings.json
```json
{
  "LLM": {
    "Provider": "OpenRouter",
    "ApiKey": "sk-or-...",
    "Model": "anthropic/claude-3.5-sonnet",
    "MaxTokens": 4096,
    "Temperature": 0.0
  },
  "Assessment": {
    "DataPath": "./data/incoming",
    "UATCriteria": {
      "TestCoverageThreshold": 60,
      "AllowHighVulnerabilities": false
    },
    "ProductionCriteria": {
      "TestCoverageThreshold": 80,
      "AllowHighVulnerabilities": false,
      "RequireUATApproval": true
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "OpenTelemetry": {
    "ServiceName": "FeatureReadinessAssessment",
    "Enabled": true
  }
}
```

## Next Steps

When implementing, refer to:
- **DESIGN.md** for system architecture and agent responsibilities
- **STEPS.md** (if exists) for recommended implementation order
- **Microsoft Semantic Kernel documentation** for agentic patterns
- **.NET 10 documentation** for latest language features
