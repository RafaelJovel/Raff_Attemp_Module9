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

Integration tests require a running Ollama instance with the `qwen2.5:latest` model:

```bash
# 1. Install Ollama from https://ollama.com/download

# 2. Pull the qwen2.5 model
ollama pull qwen2.5:latest

# 3. Verify Ollama is running (should return model info)
curl http://localhost:11434/api/tags

# 4. Verify the model is available
ollama list
```

**Note:** Unit tests do NOT require Ollama and run without any external dependencies.

## Running Tests

### Quick Start - Unit Tests Only

Run unit tests during development for fast feedback:

```bash
# Run all unit tests (excludes integration tests)
dotnet test --filter "Category!=Integration"

# Run tests from project root
dotnet test

# Run with detailed output
dotnet test --filter "Category!=Integration" --verbosity normal
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
dotnet test --filter "Category!=Integration"

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
        run: dotnet test --filter "Category!=Integration" --no-restore --verbosity normal

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
              arguments: '--filter "Category!=Integration" --no-restore --logger trx'

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

**Problem:** Integration tests fail with "model qwen2.5:latest not found"

**Solutions:**
```bash
# 1. List installed models
ollama list

# 2. Pull model if missing
ollama pull qwen2.5:latest

# 3. Verify model downloaded successfully
ollama list | grep qwen2.5
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
dotnet test --filter "Category!=Integration" --no-build

# 2. Before committing (full validation)
dotnet test

# 3. Before creating PR (with coverage)
dotnet test /p:CollectCoverage=true
```

### CI/CD Workflow

```bash
# 1. Fast CI build (on every commit)
dotnet test --filter "Category!=Integration"

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
    "Endpoint": "http://localhost:11434",
    "ModelName": "qwen2.5:latest",
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "Temperature": 0.0,
    "MaxTokens": 500
  }
}
```

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

## Best Practices

1. **Run unit tests frequently** during development (they're fast!)
2. **Run integration tests** before commits and PRs
3. **Use Docker Compose** for consistent local Ollama setup
4. **Keep integration tests minimal** - they're slower and more brittle
5. **Mark tests clearly** with `[TestCategory("Integration")]`
6. **Document external dependencies** in test comments
7. **Use `[Ignore]` with reason** for tests requiring manual setup

## Additional Resources

- [Ollama Documentation](https://github.com/ollama/ollama/tree/main/docs)
- [MSTest Documentation](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-mstest)
- [Moq Documentation](https://github.com/moq/moq4)
- [Polly Documentation](https://www.thepollyproject.org/)
- [Coverlet Coverage Tool](https://github.com/coverlet-coverage/coverlet)
