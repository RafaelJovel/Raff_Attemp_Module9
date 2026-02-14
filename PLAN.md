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
- **Ollama with Qwen2.5** as the LLM provider (localhost:11434 in Docker)
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
│   │   ├── Models/                       # Domain models, DTOs, state classes
│   │   │   ├── FeatureMetadata.cs
│   │   │   ├── AssessmentResult.cs
│   │   │   ├── FeatureReadinessState.cs  # Workflow state
│   │   │   ├── CriteriaAssessment.cs     # Assessment results
│   │   │   └── CriterionResult.cs
│   │   ├── Interfaces/                   # Agent and tool interfaces
│   │   │   ├── IFeatureLookupAgent.cs
│   │   │   ├── ICoordinatorAgent.cs
│   │   │   ├── IDocumentationTool.cs
│   │   │   └── IMetricsTool.cs
│   │   └── FeatureAssessment.Core.csproj
│   ├── FeatureAssessment.Agents/         # Agent implementations
│   │   ├── Lookup/                       # Feature lookup agent
│   │   │   ├── FeatureLookupAgent.cs
│   │   │   └── FeatureLookupResult.cs
│   │   ├── Coordinator/                  # Coordinator (supervisor) agent
│   │   │   ├── CoordinatorAgent.cs
│   │   │   └── DecisionFramework.cs
│   │   ├── Specialists/                  # Specialist agents
│   │   │   ├── DocumentationSpecialist.cs
│   │   │   ├── MetricsSpecialist.cs
│   │   │   └── ReviewsSpecialist.cs
│   │   └── FeatureAssessment.Agents.csproj
│   ├── FeatureAssessment.Tools/          # Tool implementations
│   │   ├── Documentation/                # Documentation assessment tools
│   │   │   ├── ListPlanningDocsTool.cs
│   │   │   └── ReadPlanningDocTool.cs
│   │   ├── Metrics/                      # Metrics retrieval tools
│   │   │   ├── GetTestCoverageTool.cs
│   │   │   ├── GetTestResultsTool.cs
│   │   │   ├── GetSecurityScanTool.cs
│   │   │   └── GetPerformanceMetricsTool.cs
│   │   ├── Reviews/                      # Review status tools
│   │   │   └── GetReviewStatusTool.cs
│   │   └── FeatureAssessment.Tools.csproj
│   └── FeatureAssessment.Infrastructure/ # Infrastructure concerns
│       ├── LLM/                          # LLM integration
│       │   ├── OpenRouterClient.cs
│       │   └── SemanticKernelSetup.cs
│       ├── Configuration/                # Configuration classes
│       │   ├── LLMOptions.cs
│       │   ├── AssessmentOptions.cs
│       │   └── Validators/
│       │       ├── LLMOptionsValidator.cs
│       │       └── AssessmentOptionsValidator.cs
│       ├── Telemetry/                    # Observability
│       │   ├── OpenTelemetrySetup.cs
│       │   └── LoggingExtensions.cs
│       └── FeatureAssessment.Infrastructure.csproj
├── tests/
│   ├── FeatureAssessment.Core.Tests/
│   │   ├── Models/
│   │   └── FeatureAssessment.Core.Tests.csproj
│   ├── FeatureAssessment.Agents.Tests/
│   │   ├── Lookup/
│   │   │   └── FeatureLookupAgentTests.cs
│   │   ├── Coordinator/
│   │   │   └── CoordinatorAgentTests.cs
│   │   └── FeatureAssessment.Agents.Tests.csproj
│   ├── FeatureAssessment.Tools.Tests/
│   │   ├── Documentation/
│   │   ├── Metrics/
│   │   └── FeatureAssessment.Tools.Tests.csproj
│   └── FeatureAssessment.IntegrationTests/
│       ├── EndToEnd/
│       │   └── FullAssessmentWorkflowTests.cs
│       └── FeatureAssessment.IntegrationTests.csproj
├── data/                                  # Sample data (not part of solution)
│   └── incoming/
│       ├── feature1/
│       ├── feature2/
│       ├── feature3/
│       └── feature4/
├── FeatureReadinessAssessment.sln
└── global.json                            # Pin .NET SDK version
```

### Project Structure Details

**State Management Location:**
- State classes (`FeatureReadinessState`, `CriteriaAssessment`, `CriterionResult`) reside in **`FeatureAssessment.Core/Models/`**
- These are domain models that represent workflow state and assessment results
- Enums like `DecisionType`, `CriterionStatus`, `TargetEnvironment` also go in `Core/Models/`

**Infrastructure Subdirectories:**
- **`LLM/`** - LLM client integration, Semantic Kernel setup
- **`Configuration/`** - Strongly-typed configuration classes and validators
- **`Telemetry/`** - OpenTelemetry setup, logging extensions, observability

**File Organization Guidelines:**
- **Create subdirectories when:**
  - You have 3+ related files (e.g., multiple tools of the same type)
  - Files represent a logical grouping (e.g., all metrics tools)
  - You want to mirror the project structure in tests
- **Keep flat when:**
  - Only 1-2 files in a category
  - Files are utilities or shared infrastructure
- **Example:** If Coordinator agent needs multiple helper classes, create `Coordinator/Helpers/` subdirectory

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
# Source projects reference Core
dotnet add src/FeatureAssessment.Agents reference src/FeatureAssessment.Core
dotnet add src/FeatureAssessment.Tools reference src/FeatureAssessment.Core
dotnet add src/FeatureAssessment.Infrastructure reference src/FeatureAssessment.Core

# Agents may need Infrastructure (for LLM client)
dotnet add src/FeatureAssessment.Agents reference src/FeatureAssessment.Infrastructure

# Test projects reference their source projects
dotnet add tests/FeatureAssessment.Core.Tests reference src/FeatureAssessment.Core
dotnet add tests/FeatureAssessment.Agents.Tests reference src/FeatureAssessment.Agents
dotnet add tests/FeatureAssessment.Agents.Tests reference src/FeatureAssessment.Core
dotnet add tests/FeatureAssessment.Tools.Tests reference src/FeatureAssessment.Tools
dotnet add tests/FeatureAssessment.Tools.Tests reference src/FeatureAssessment.Core

# Integration tests reference all projects
dotnet add tests/FeatureAssessment.IntegrationTests reference src/FeatureAssessment.Core
dotnet add tests/FeatureAssessment.IntegrationTests reference src/FeatureAssessment.Agents
dotnet add tests/FeatureAssessment.IntegrationTests reference src/FeatureAssessment.Tools
dotnet add tests/FeatureAssessment.IntegrationTests reference src/FeatureAssessment.Infrastructure
```

**Project Dependency Graph:**
```
FeatureAssessment.Core (no dependencies)
   ↑
   ├─ FeatureAssessment.Infrastructure
   ├─ FeatureAssessment.Agents → FeatureAssessment.Infrastructure
   └─ FeatureAssessment.Tools

Tests:
- Core.Tests → Core
- Agents.Tests → Agents, Core
- Tools.Tests → Tools, Core
- IntegrationTests → Core, Agents, Tools, Infrastructure
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

### Namespace Hierarchy

| Project | Namespace | Purpose | Example Classes |
|---------|-----------|---------|-----------------|
| **FeatureAssessment.Core** | `FeatureAssessment.Core.Models` | Domain models, DTOs, state | `FeatureMetadata`, `FeatureReadinessState`, `AssessmentResult` |
| | `FeatureAssessment.Core.Interfaces` | Agent and tool interfaces | `IFeatureLookupAgent`, `ICoordinatorAgent`, `IDocumentationTool` |
| **FeatureAssessment.Agents** | `FeatureAssessment.Agents.Lookup` | Feature lookup agent | `FeatureLookupAgent`, `FeatureLookupResult` |
| | `FeatureAssessment.Agents.Coordinator` | Coordinator/supervisor agent | `CoordinatorAgent`, `DecisionFramework` |
| | `FeatureAssessment.Agents.Specialists` | Specialist agents | `DocumentationSpecialist`, `MetricsSpecialist`, `ReviewsSpecialist` |
| **FeatureAssessment.Tools** | `FeatureAssessment.Tools.Documentation` | Documentation tools | `ListPlanningDocsTool`, `ReadPlanningDocTool` |
| | `FeatureAssessment.Tools.Metrics` | Metrics retrieval tools | `GetTestCoverageTool`, `GetSecurityScanTool` |
| | `FeatureAssessment.Tools.Reviews` | Review status tools | `GetReviewStatusTool` |
| **FeatureAssessment.Infrastructure** | `FeatureAssessment.Infrastructure.LLM` | LLM client integration | `OpenRouterClient`, `SemanticKernelSetup` |
| | `FeatureAssessment.Infrastructure.Configuration` | Configuration classes | `LLMOptions`, `AssessmentOptions`, validators |
| | `FeatureAssessment.Infrastructure.Telemetry` | Observability setup | `OpenTelemetrySetup`, `LoggingExtensions` |
| **Test Projects** | `FeatureAssessment.{Project}.Tests` | Test namespaces mirror source | `FeatureAssessment.Agents.Tests.Lookup.FeatureLookupAgentTests` |

**Namespace Conventions:**
- Use file-scoped namespaces: `namespace FeatureAssessment.Core.Models;`
- Match folder structure: `src/FeatureAssessment.Agents/Lookup/` → `namespace FeatureAssessment.Agents.Lookup;`
- Test namespaces mirror source: `FeatureAssessment.Agents.Lookup.FeatureLookupAgent` → `FeatureAssessment.Agents.Tests.Lookup.FeatureLookupAgentTests`

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

### Configuration File Location

**Primary Configuration File:**
- **File:** `appsettings.json`
- **Location:** Project root directory (same directory as `.csproj` file)
- **Format:** JSON with hierarchical structure

**Environment-Specific Configuration Files:**
- `appsettings.Development.json` - Development environment overrides
- `appsettings.Staging.json` - Staging environment overrides
- `appsettings.Production.json` - Production environment overrides

**File Loading:** Environment-specific files override base `appsettings.json` settings. Settings are merged hierarchically (base → environment-specific).

### Environment Variable Overrides

**Purpose:** Allow runtime configuration overrides without modifying files. Critical for production deployments and secret management.

**Naming Convention:**
- Use double underscore `__` to represent hierarchy levels
- Format: `ParentSection__ChildSection__PropertyName`

**Examples:**
```bash
# Override LLM API Key
LLM__ApiKey=sk-or-v1-your-actual-key-here

# Override LLM Model
LLM__Model=anthropic/claude-3.7-sonnet

# Override Data Path
Assessment__DataPath=/var/data/features

# Override Production Criteria
Assessment__ProductionCriteria__TestCoverageThreshold=85
```

**Configuration Precedence (highest to lowest):**
1. Environment variables (highest priority)
2. `appsettings.{Environment}.json` (environment-specific)
3. `appsettings.json` (base configuration)
4. Default values in code (lowest priority)

**Security Best Practice:**
- **ALWAYS** use environment variables for secrets in production
- **NEVER** commit API keys or passwords to `appsettings.json` in source control
- Use placeholder values in `appsettings.json` (e.g., `"ApiKey": "your-api-key-here"`)

### Configuration Loading

**Startup Configuration Pattern:**

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// In Program.cs or Startup.cs
var builder = Host.CreateApplicationBuilder(args);

// Configuration is automatically loaded from:
// 1. appsettings.json
// 2. appsettings.{Environment}.json
// 3. User secrets (Development only)
// 4. Environment variables
// 5. Command-line arguments

// Access configuration
var configuration = builder.Configuration;

// Register strongly-typed configuration using Options pattern
builder.Services.Configure<LLMOptions>(
    configuration.GetSection("LLM"));
builder.Services.Configure<AssessmentOptions>(
    configuration.GetSection("Assessment"));

// Build host
var host = builder.Build();
```

**Accessing Configuration in Code:**

**Option 1: IOptions<T> (Recommended for most cases)**
```csharp
public class CoordinatorAgent
{
    private readonly LLMOptions _llmOptions;

    public CoordinatorAgent(IOptions<LLMOptions> llmOptions)
    {
        _llmOptions = llmOptions.Value;
    }

    public void UseConfiguration()
    {
        var apiKey = _llmOptions.ApiKey;
        var model = _llmOptions.Model;
    }
}
```

**Option 2: IConfiguration (Direct access)**
```csharp
public class SomeService
{
    private readonly IConfiguration _configuration;

    public SomeService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void UseConfiguration()
    {
        var apiKey = _configuration["LLM:ApiKey"];
        var threshold = _configuration.GetValue<int>("Assessment:ProductionCriteria:TestCoverageThreshold");
    }
}
```

**Configuration Classes (for Options pattern):**

```csharp
// LLMOptions.cs
public class LLMOptions
{
    public required string Provider { get; init; }
    public required string ApiKey { get; init; }
    public required string Model { get; init; }
    public int MaxTokens { get; init; } = 4096;
    public double Temperature { get; init; } = 0.0;
}

// AssessmentOptions.cs
public class AssessmentOptions
{
    public required string DataPath { get; init; }
    public required UATCriteriaOptions UATCriteria { get; init; }
    public required ProductionCriteriaOptions ProductionCriteria { get; init; }
}

public class UATCriteriaOptions
{
    public int TestCoverageThreshold { get; init; } = 60;
    public bool AllowHighVulnerabilities { get; init; } = false;
}

public class ProductionCriteriaOptions
{
    public int TestCoverageThreshold { get; init; } = 80;
    public bool AllowHighVulnerabilities { get; init; } = false;
    public bool RequireUATApproval { get; init; } = true;
}
```

### Configuration Validation

**Purpose:** Validate configuration at startup to fail fast with clear error messages rather than runtime failures.

**Validation Rules (using FluentValidation):**

```csharp
using FluentValidation;

public class LLMOptionsValidator : AbstractValidator<LLMOptions>
{
    public LLMOptionsValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .WithMessage("LLM Provider is required");

        RuleFor(x => x.ApiKey)
            .NotEmpty()
            .WithMessage("LLM API Key is required")
            .Must(key => !key.Contains("your-api-key-here"))
            .WithMessage("LLM API Key must be set (placeholder value detected)");

        RuleFor(x => x.Model)
            .NotEmpty()
            .WithMessage("LLM Model is required");

        RuleFor(x => x.MaxTokens)
            .GreaterThan(0)
            .LessThanOrEqualTo(200000)
            .WithMessage("MaxTokens must be between 1 and 200000");

        RuleFor(x => x.Temperature)
            .InclusiveBetween(0.0, 2.0)
            .WithMessage("Temperature must be between 0.0 and 2.0");
    }
}

public class AssessmentOptionsValidator : AbstractValidator<AssessmentOptions>
{
    public AssessmentOptionsValidator()
    {
        RuleFor(x => x.DataPath)
            .NotEmpty()
            .WithMessage("Assessment DataPath is required");

        RuleFor(x => x.UATCriteria.TestCoverageThreshold)
            .InclusiveBetween(0, 100)
            .WithMessage("UAT TestCoverageThreshold must be between 0 and 100");

        RuleFor(x => x.ProductionCriteria.TestCoverageThreshold)
            .InclusiveBetween(0, 100)
            .WithMessage("Production TestCoverageThreshold must be between 0 and 100");

        RuleFor(x => x.ProductionCriteria.TestCoverageThreshold)
            .GreaterThanOrEqualTo(x => x.UATCriteria.TestCoverageThreshold)
            .WithMessage("Production threshold must be >= UAT threshold");
    }
}
```

**Validation Registration and Execution:**

```csharp
// Register validators
builder.Services.AddSingleton<IValidator<LLMOptions>, LLMOptionsValidator>();
builder.Services.AddSingleton<IValidator<AssessmentOptions>, AssessmentOptionsValidator>();

// Validate on startup (in Program.cs after building host)
var llmOptions = host.Services.GetRequiredService<IOptions<LLMOptions>>().Value;
var llmValidator = host.Services.GetRequiredService<IValidator<LLMOptions>>();
var llmValidationResult = llmValidator.Validate(llmOptions);

if (!llmValidationResult.IsValid)
{
    var errors = string.Join(Environment.NewLine,
        llmValidationResult.Errors.Select(e => $"  - {e.ErrorMessage}"));
    throw new InvalidOperationException(
        $"LLM configuration validation failed:{Environment.NewLine}{errors}");
}

// Repeat for other options...
```

**Required vs Optional Settings:**

| Setting | Required | Default Value | Validation |
|---------|----------|---------------|------------|
| `LLM.Provider` | Yes | None | Must not be empty |
| `LLM.ApiKey` | Yes | None | Must not be empty or placeholder |
| `LLM.Model` | Yes | None | Must not be empty |
| `LLM.MaxTokens` | No | 4096 | 1-200000 |
| `LLM.Temperature` | No | 0.0 | 0.0-2.0 |
| `Assessment.DataPath` | Yes | None | Must not be empty |
| `Assessment.UATCriteria.TestCoverageThreshold` | No | 60 | 0-100 |
| `Assessment.UATCriteria.AllowHighVulnerabilities` | No | false | Boolean |
| `Assessment.ProductionCriteria.TestCoverageThreshold` | No | 80 | 0-100, >= UAT threshold |
| `Assessment.ProductionCriteria.AllowHighVulnerabilities` | No | false | Boolean |
| `Assessment.ProductionCriteria.RequireUATApproval` | No | true | Boolean |

### Secret Management

**Development Environment:**

Use .NET User Secrets for local development to avoid committing secrets to source control.

**Setup User Secrets:**
```bash
# Initialize user secrets for the project
dotnet user-secrets init --project src/FeatureAssessment.Infrastructure

# Set secrets
dotnet user-secrets set "LLM:ApiKey" "sk-or-v1-your-dev-key" --project src/FeatureAssessment.Infrastructure
dotnet user-secrets set "LLM:Model" "anthropic/claude-3.5-sonnet" --project src/FeatureAssessment.Infrastructure
```

**User Secrets Location:**
- Windows: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- macOS/Linux: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

**Production Environment:**

**Option 1: Environment Variables (Recommended for containers/VMs)**
```bash
# Set in deployment environment
export LLM__ApiKey="sk-or-v1-your-production-key"
export LLM__Model="anthropic/claude-3.7-sonnet"
```

**Option 2: Azure Key Vault (Recommended for Azure deployments)**
```csharp
// Add package: Azure.Extensions.AspNetCore.Configuration.Secrets
using Azure.Identity;

builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-keyvault.vault.azure.net/"),
    new DefaultAzureCredential());
```

Store secrets in Key Vault with naming convention:
- `LLM--ApiKey` (note: double dash for Key Vault)
- `LLM--Model`

**Security Checklist:**

- [ ] `.gitignore` includes `appsettings.*.json` (except base `appsettings.json`)
- [ ] `appsettings.json` uses placeholder values for secrets (e.g., `"your-api-key-here"`)
- [ ] User Secrets configured for local development
- [ ] Production secrets managed via environment variables or Key Vault
- [ ] Configuration validation checks for placeholder values
- [ ] No secrets in source control (verify with `git log -p | grep -i "sk-"`)
- [ ] README documents secret configuration steps
- [ ] CI/CD pipeline injects secrets from secure store

**Example Production Deployment:**

```bash
# Docker
docker run -e LLM__ApiKey="actual-key" -e Assessment__DataPath="/data" your-app:latest

# Kubernetes Secret
kubectl create secret generic app-config \
  --from-literal=LLM__ApiKey="actual-key" \
  --from-literal=LLM__Model="anthropic/claude-3.7-sonnet"

# Azure App Service (via portal or CLI)
az webapp config appsettings set --name your-app --resource-group your-rg \
  --settings LLM__ApiKey="actual-key" LLM__Model="anthropic/claude-3.7-sonnet"
```

## Next Steps

When implementing, refer to:
- **DESIGN.md** for system architecture and agent responsibilities
- **STEPS.md** (if exists) for recommended implementation order
- **Microsoft Semantic Kernel documentation** for agentic patterns
- **.NET 10 documentation** for latest language features
