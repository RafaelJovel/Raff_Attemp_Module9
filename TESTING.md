# Testing Guide

This document provides comprehensive guidance for running tests, setting up test dependencies, and configuring CI/CD pipelines for the Feature Readiness Assessment System.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Running Tests](#running-tests)
3. [Test Categories](#test-categories)
4. [Setting Up Ollama](#setting-up-ollama)
5. [CI/CD Setup](#cicd-setup)
6. [Troubleshooting](#troubleshooting)

## Prerequisites

### Required Software

- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Ollama** (for integration tests only) - [Download](https://ollama.com/download)

### Ollama Setup (Integration Tests Only)

Integration tests require a running Ollama instance with the `qwen2.5:0.5b` model:

```bash
# 1. Install Ollama from https://ollama.com/download

# 2. Pull the qwen2.5:0.5b model (lightweight model for testing)
ollama pull qwen2.5:0.5b

# 3. Verify Ollama is running (should return model info)
curl http://localhost:11434/api/tags

# 4. Verify the OpenAI-compatible API endpoint works
curl -X POST http://localhost:11434/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{"model":"qwen2.5:0.5b","messages":[{"role":"user","content":"test"}]}'

# 5. Verify the model is available
ollama list
```

**Important:** The application uses Ollama's OpenAI-compatible API endpoint at `/v1`, not the native Ollama API.

**Note:** Unit tests do NOT require Ollama and run without any external dependencies.

## Running Tests

### Quick Start - Unit Tests Only

Run unit tests during development for fast feedback:

```bash
# Run all unit tests (excludes integration tests)
dotnet test --filter "TestCategory!=Integration"

# Run tests from project root
dotnet test

# Run with detailed output
dotnet test --filter "TestCategory!=Integration" --verbosity normal
```

### Running All Tests (Including Integration)

```bash
# Run ALL tests including integration tests
# Requires Ollama running locally
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Running Tests by Category

```bash
# Unit tests only (fast, no external dependencies)
dotnet test --filter "TestCategory!=Integration"

# Integration tests only (requires Ollama)
dotnet test --filter "TestCategory=Integration"

# Specific test class
dotnet test --filter "FullyQualifiedName~FeatureLookupAgentTests"

# Specific test method
dotnet test --filter "Name=Validate_WithValidConfiguration_ReturnsSuccess"
```

### Running Tests from Specific Projects

```bash
# Run tests from Core.Tests project
dotnet test tests/FeatureAssessment.Core.Tests

# Run tests from IntegrationTests project (when created)
dotnet test tests/FeatureAssessment.IntegrationTests
```

## Test Categories

### Unit Tests

**Location:** `tests/FeatureAssessment.Core.Tests/`

**Characteristics:**
- No external dependencies (file system, network, databases)
- Use mocks (Moq) and in-memory fakes
- Run in milliseconds
- Always run in CI/CD

**Test Projects:**
- `OllamaConfigurationValidatorTests` - Configuration validation
- `AssessmentStateTests` - State model tests
- `FeatureLookupAgentTests` - Agent behavior (mocked tools and LLM)
- `ResiliencePoliciesTests` - Polly policy tests (with WireMock)

### Integration Tests

**Location:** `tests/FeatureAssessment.Core.Tests/Integration/`

**Characteristics:**
- Marked with `[TestCategory("Integration")]`
- Require real Ollama instance
- Test end-to-end flows
- May take several seconds
- Skipped in fast CI builds, run in full CI/nightly builds

**Test Projects:**
- `OllamaConnectivityTests` - Validates Ollama endpoint and model availability
- `OllamaEndToEndTests` - Complete workflow validation

## Setting Up Ollama

### Local Development (Windows, macOS, Linux)

**Windows:**
```powershell
# Download and install from https://ollama.com/download
# Or use winget
winget install Ollama.Ollama

# Pull model
ollama pull qwen2.5:latest

# Start Ollama (runs as Windows service automatically)
# Verify at http://localhost:11434
```

**macOS:**
```bash
# Download from https://ollama.com/download
# Or use Homebrew
brew install ollama

# Pull model
ollama pull qwen2.5:latest

# Start Ollama
ollama serve
```

**Linux:**
```bash
# Install Ollama
curl -fsSL https://ollama.com/install.sh | sh

# Pull model
ollama pull qwen2.5:latest

# Start Ollama (runs as systemd service)
sudo systemctl start ollama
sudo systemctl enable ollama
```

### Docker Setup (For CI/CD or Isolated Testing)

```bash
# Run Ollama in Docker
docker run -d -p 11434:11434 --name ollama ollama/ollama:latest

# Pull model inside container
docker exec ollama ollama pull qwen2.5:latest

# Verify
curl http://localhost:11434/api/tags
```

### Docker Compose (Recommended for Local Development)

Create `docker-compose.yml` in project root:

```yaml
version: '3.8'
services:
  ollama:
    image: ollama/ollama:latest
    container_name: ollama
    ports:
      - "11434:11434"
    volumes:
      - ollama-data:/root/.ollama
    restart: unless-stopped

volumes:
  ollama-data:
```

Start Ollama:
```bash
# Start Ollama
docker-compose up -d

# Pull model
docker-compose exec ollama ollama pull qwen2.5:latest

# View logs
docker-compose logs -f ollama

# Stop Ollama
docker-compose down
```

## CI/CD Setup

### GitHub Actions

Create `.github/workflows/test.yml`:

```yaml
name: Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  unit-tests:
    name: Unit Tests (Fast)
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Run unit tests
        run: dotnet test --filter "TestCategory!=Integration" --no-restore --verbosity normal

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: test-results
          path: '**/TestResults/*.trx'

  integration-tests:
    name: Integration Tests (Requires Ollama)
    runs-on: ubuntu-latest
    services:
      ollama:
        image: ollama/ollama:latest
        ports:
          - 11434:11434
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'

      - name: Wait for Ollama
        run: |
          timeout 60 bash -c 'until curl -f http://localhost:11434/api/tags; do sleep 2; done'

      - name: Pull Ollama model
        run: |
          docker exec ${{ job.services.ollama.id }} ollama pull qwen2.5:latest

      - name: Restore dependencies
        run: dotnet restore

      - name: Run integration tests
        run: dotnet test --filter "TestCategory=Integration" --no-restore --verbosity normal
```

### Azure DevOps

Create `azure-pipelines.yml`:

```yaml
trigger:
  branches:
    include:
      - main
      - develop

pool:
  vmImage: 'ubuntu-latest'

stages:
  - stage: UnitTests
    displayName: 'Unit Tests'
    jobs:
      - job: RunUnitTests
        steps:
          - task: UseDotNet@2
            inputs:
              packageType: 'sdk'
              version: '10.0.x'

          - task: DotNetCoreCLI@2
            displayName: 'Restore dependencies'
            inputs:
              command: 'restore'

          - task: DotNetCoreCLI@2
            displayName: 'Run unit tests'
            inputs:
              command: 'test'
              arguments: '--filter "TestCategory!=Integration" --no-restore --logger trx'

          - task: PublishTestResults@2
            condition: always()
            inputs:
              testResultsFormat: 'VSTest'
              testResultsFiles: '**/*.trx'

  - stage: IntegrationTests
    displayName: 'Integration Tests'
    dependsOn: UnitTests
    jobs:
      - job: RunIntegrationTests
        services:
          ollama:
            image: ollama/ollama:latest
            ports:
              - 11434:11434
        steps:
          - task: UseDotNet@2
            inputs:
              packageType: 'sdk'
              version: '10.0.x'

          - script: |
              docker exec $(docker ps -q -f ancestor=ollama/ollama:latest) ollama pull qwen2.5:latest
            displayName: 'Pull Ollama model'

          - task: DotNetCoreCLI@2
            displayName: 'Restore dependencies'
            inputs:
              command: 'restore'

          - task: DotNetCoreCLI@2
            displayName: 'Run integration tests'
            inputs:
              command: 'test'
              arguments: '--filter "TestCategory=Integration" --no-restore --logger trx'
```

## Troubleshooting

### Ollama Connection Issues

**Problem:** Tests fail with "Connection refused" or "Ollama not reachable"

**Solutions:**
```bash
# 1. Verify Ollama is running
curl http://localhost:11434/api/tags

# 2. Check Ollama service status (Linux)
sudo systemctl status ollama

# 3. Check Ollama process (Windows/macOS)
ps aux | grep ollama

# 4. Restart Ollama
# Linux: sudo systemctl restart ollama
# macOS: brew services restart ollama
# Windows: Restart from Task Manager or Services
```

### Model Not Found

**Problem:** Integration tests fail with "model qwen2.5:0.5b not found"

**Solutions:**
```bash
# 1. List installed models
ollama list

# 2. Pull model if missing
ollama pull qwen2.5:0.5b

# 3. Verify model downloaded successfully
ollama list | grep qwen2.5
```

### OpenAI API Endpoint Not Reachable

**Problem:** Tests fail with "Connection refused" to `/v1` endpoint

**Solutions:**
```bash
# 1. Verify the OpenAI-compatible API endpoint works
curl -X POST http://localhost:11434/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{"model":"qwen2.5:0.5b","messages":[{"role":"user","content":"test"}]}'

# 2. Check Ollama version (older versions may not support /v1)
ollama --version

# 3. Update Ollama if necessary
# Windows: winget upgrade Ollama.Ollama
# macOS: brew upgrade ollama
# Linux: curl -fsSL https://ollama.com/install.sh | sh

# 4. Verify configuration includes /v1 suffix
# Endpoint should be: http://localhost:11434/v1 (NOT http://localhost:11434)
```

### Tests Timeout

**Problem:** Integration tests timeout after 30 seconds

**Solutions:**
- Increase timeout in `OllamaConfiguration` (default: 30s)
- Ensure Ollama has sufficient resources (CPU, RAM)
- Check if model is fully loaded: `docker logs ollama` (if using Docker)

### WireMock Port Conflicts

**Problem:** ResiliencePoliciesTests fail with "Address already in use"

**Solutions:**
```bash
# 1. Check what's using the port
netstat -ano | findstr :8080  # Windows
lsof -i :8080  # macOS/Linux

# 2. Kill the process or let WireMock pick a random port (default behavior)
```

### Test Discovery Issues

**Problem:** "No tests found" or tests not discovered

**Solutions:**
```bash
# 1. Clean and rebuild
dotnet clean
dotnet build

# 2. Check test project references
dotnet list tests/FeatureAssessment.Core.Tests/FeatureAssessment.Core.Tests.csproj reference

# 3. Verify MSTest package is referenced
dotnet list tests/FeatureAssessment.Core.Tests/FeatureAssessment.Core.Tests.csproj package
```

## Running Specific Test Scenarios

### Development Workflow

```bash
# 1. During active development (fast feedback)
dotnet test --filter "TestCategory!=Integration" --no-build

# 2. Before committing (full validation)
dotnet test

# 3. Before creating PR (with coverage)
dotnet test /p:CollectCoverage=true
```

### CI/CD Workflow

```bash
# 1. Fast CI build (on every commit)
dotnet test --filter "TestCategory!=Integration"

# 2. Full CI build (on PR to main)
dotnet test

# 3. Nightly build (comprehensive)
dotnet test /p:CollectCoverage=true /p:Threshold=80
```

## Test Configuration

### appsettings.Test.json

Create for test-specific configuration:

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434/v1",
    "ModelName": "qwen2.5:0.5b",
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "Temperature": 0.0,
    "MaxTokens": 500
  }
}
```

**Note:** The `/v1` suffix is **required** for OpenAI API compatibility.

## Coverage Reports

Generate and view coverage reports:

```bash
# Generate coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Install ReportGenerator tool (once)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator -reports:**/coverage.opencover.xml -targetdir:coverage-report -reporttypes:Html

# View report (Windows)
start coverage-report/index.html

# View report (macOS)
open coverage-report/index.html

# View report (Linux)
xdg-open coverage-report/index.html
```

## Observability & Distributed Tracing

The Feature Readiness Assessment System uses OpenTelemetry for distributed tracing. This allows you to observe the flow of requests through agents, tools, and LLM calls.

### Viewing Traces During Tests

#### Console Exporter (Development)

View traces in the console output during test execution:

```bash
# Enable console trace exporter (requires OpenTelemetry.Exporter.Console package)
# Traces will appear in test output automatically when ActivityListener is configured

# Run tests with detailed output to see trace information
dotnet test --verbosity normal
```

#### Programmatic Trace Inspection

Tests can capture and inspect activities programmatically using `ActivityListener`:

```csharp
var capturedActivities = new List<Activity>();
var listener = new ActivityListener
{
    ShouldListenTo = source => source.Name.StartsWith("FeatureAssessment"),
    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
    ActivityStarted = activity => capturedActivities.Add(activity)
};
ActivitySource.AddActivityListener(listener);

// Execute code under test
await agent.LookupFeatureAsync("query");

// Inspect captured activities
foreach (var activity in capturedActivities)
{
    Console.WriteLine($"{activity.OperationName} - {activity.Duration}");
    foreach (var tag in activity.Tags)
    {
        Console.WriteLine($"  {tag.Key}: {tag.Value}");
    }
}
```

### Trace Hierarchy

When the Feature Lookup Agent executes, you'll see a trace hierarchy like:

```
FeatureLookupAgent.LookupFeature (parent)
├─ query: "Is PLAT-1523 ready for production?"
├─ feature_key: "PLAT-1523"
├─ feature_id: "feature1"
├─ target_environment: "Production"
├─ is_success: true
└─ Duration: 2.5s
```

### Activity Sources

The system defines ActivitySources for each component:

- **FeatureAssessment.FeatureLookup** - Feature identification and query processing
- **FeatureAssessment.Tools** - Tool invocations (file reads, data parsing)
- **FeatureAssessment.Coordinator** - Coordinator agent operations
- **FeatureAssessment.Specialists** - Specialist agent consultations

### Span Attributes (Tags)

Common attributes attached to activities:

- `query` - User query text
- `feature_key` - JIRA key (e.g., "PLAT-1523")
- `feature_id` - Feature folder ID (e.g., "feature1")
- `target_environment` - "UAT" or "Production"
- `is_success` - Boolean success indicator
- `exception.type` - Exception type on error
- `exception.message` - Error message
- `service.name` - Always "FeatureAssessment"

### Integration with Observability Backends

For production observability, export traces to:

- **Jaeger** - Open-source distributed tracing
- **Zipkin** - Distributed tracing system
- **Azure Application Insights** - Cloud observability
- **OTLP Collector** - OpenTelemetry Protocol collector

Example configuration (add to your host builder):

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracingBuilder => tracingBuilder
        .AddSource("FeatureAssessment.*")
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
        }));
```

## Best Practices

1. **Run unit tests frequently** during development (they're fast!)
2. **Run integration tests** before commits and PRs
3. **Use Docker Compose** for consistent local Ollama setup
4. **Keep integration tests minimal** - they're slower and more brittle
5. **Mark tests clearly** with `[TestCategory("Integration")]`
6. **Document external dependencies** in test comments
7. **Use `[Ignore]` with reason** for tests requiring manual setup

## Model Selection and Known Limitations

### Recommended Models for Tool Calling

**After extensive testing, the following models are recommended:**

✅ **llama3.1:8b** (Recommended - Proven tool calling support)
- Size: ~4.9GB
- Best tool calling reliability with Semantic Kernel
- Consistent JSON formatting
- Hardware: Requires 8GB+ RAM/VRAM

✅ **llama3.2:latest** (Alternative)
- Size: varies by version
- Good tool calling support
- Faster than 8B models

✅ **llama3.3:latest** (If available)
- Excellent tool calling
- Latest improvements

❌ **qwen2.5 models** (NOT Recommended)
- Limited tool calling support
- Poor JSON formatting
- Tests show high failure rate

### Updating Model Configuration

To switch models, update `OllamaConfiguration.cs`:

```csharp
public string ModelName { get; set; } = "llama3.1:8b"; // Change here
```

Or override in `appsettings.json`:

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "ModelName": "llama3.1:8b"
  }
}
```

### Known Limitations with Local LLMs

**Test Flakiness:**
- Integration tests may show 80-90% pass rate (normal for local LLMs)
- LLM non-determinism persists even with temperature=0
- Edge cases (non-existent features) more prone to variability
- Retrying failed tests may yield different results

**Performance:**
- 8B models require 8GB+ RAM/VRAM
- RTX 4090 (24GB): Excellent performance
- Response times: 1-5 seconds typical
- First query after loading: 5-10 seconds

**Reliability vs Cloud Models:**
- Local models: 80-90% reliability in tool calling
- Cloud models (GPT-4, Claude): 95-99% reliability
- Consider cloud models for production CI/CD pipelines

### Configuration for Ollama Connector

**IMPORTANT:** The Ollama connector (not OpenAI connector) requires specific configuration:

```csharp
// Correct configuration for Ollama connector
builder.AddOllamaChatCompletion(
    modelId: "llama3.1:8b",
    endpoint: new Uri("http://localhost:11434")); // NO /v1 suffix

// Execution settings
var settings = new OllamaPromptExecutionSettings
{
    Temperature = 0.0f,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};
```

**Package Requirements:**
- `Microsoft.SemanticKernel` v1.70.0+
- `Microsoft.SemanticKernel.Connectors.Ollama` v1.70.0-alpha+ (prerelease)

## Manual Testing Harness

In addition to automated tests, the project includes an interactive **Manual Testing Harness** for hands-on validation of the Feature Lookup Agent.

### What is the Manual Testing Harness?

A standalone console application that allows you to:
- Test the Feature Lookup Agent interactively
- Run pre-defined test scenarios
- Enter custom queries and see results in real-time
- View trace information and tool calls
- Validate agent behavior manually

**Location:** `tests/FeatureAssessment.TestHarness/`

### Running the Harness

```bash
# From repository root
cd tests/FeatureAssessment.TestHarness
dotnet run

# Or directly from root
dotnet run --project tests/FeatureAssessment.TestHarness
```

### Features

1. **Run All Scenarios** - Execute 14 pre-defined test scenarios
2. **Run by Category** - Choose a specific category (Happy Path, Error Handling, etc.)
3. **Run Single Scenario** - Pick one specific scenario to test
4. **Custom Query** - Enter your own natural language query
5. **Show Configuration** - Display current Ollama settings

### Pre-defined Test Scenarios

The harness includes 14 scenarios across 5 categories:

- **Happy Path** (3 scenarios) - Basic feature identification
- **Environment Extraction** (3 scenarios) - Production vs UAT detection
- **Error Handling** (3 scenarios) - Non-existent features, invalid input
- **Tool Calling** (2 scenarios) - Verify tools are invoked correctly
- **Edge Cases** (3 scenarios) - Partial names, case sensitivity, ambiguity

### Example Output

```
  _____ _____    _  _____ _   _ ____  _____   _     ___   ___  _  ___   _ ____
 |  ___| ____|  / \|_   _| | | |  _ \| ____| | |   / _ \ / _ \| |/ / | | |  _ \
 | |_  |  _|   / _ \ | | | | | | |_) |  _|   | |  | | | | | | | ' /| | | | |_) |
 |  _| | |___ / ___ \| | | |_| |  _ <| |___  | |__| |_| | |_| | . \| |_| |  __/
 |_|   |_____/_/   \_\_|  \___/|_| \_\_____| |_____\___/ \___/|_|\_\\___/|_|

Manual Testing Harness for Feature Lookup Agent

┌─────────────────────────────────────────────┐
│        Current Configuration                 │
├───────────────────────┬─────────────────────┤
│      Setting          │        Value        │
├───────────────────────┼─────────────────────┤
│ Ollama Endpoint       │ http://localhost... │
│ Model Name            │ llama3.1:8b         │
│ Timeout (seconds)     │ 60                  │
│ Max Retries           │ 3                   │
└───────────────────────┴─────────────────────┘

What would you like to do?
❯ Run all scenarios
  Run scenarios by category
  Run single scenario
  Enter custom query
  Show configuration
  Exit
```

### Prerequisites for Manual Testing

1. **Ollama running** - `ollama serve`
2. **Model downloaded** - `ollama pull llama3.1:8b`
3. **Sample data exists** - `data/incoming/feature1-4/` directories with JIRA metadata

### Configuration

The harness uses its own `appsettings.json`:

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "ModelName": "llama3.1:8b",
    "TimeoutSeconds": 60,
    "MaxRetries": 3
  }
}
```

### When to Use the Harness

- **During development** - Quick validation of agent behavior
- **After changes** - Verify modifications don't break existing functionality
- **Demo purposes** - Show stakeholders how the agent works
- **Debugging** - See tool calls and trace information in real-time
- **Model testing** - Compare different LLM models (llama3.1 vs llama3.2, etc.)

### Detailed Documentation

For complete usage instructions, troubleshooting, and expected behavior documentation, see:

**[tests/FeatureAssessment.TestHarness/README.md](../tests/FeatureAssessment.TestHarness/README.md)**

This includes:
- Model requirements and recommendations
- Expected behavior with local LLMs
- Interpreting results (success vs failure)
- Troubleshooting common issues
- Sample session walkthrough

## Additional Resources

- [Ollama Documentation](https://github.com/ollama/ollama/tree/main/docs)
- [Semantic Kernel Ollama Connector](https://github.com/microsoft/semantic-kernel)
- [MSTest Documentation](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-mstest)
- [Moq Documentation](https://github.com/moq/moq4)
- [Polly Documentation](https://www.thepollyproject.org/)
- [Coverlet Coverage Tool](https://github.com/coverlet-coverage/coverlet)
