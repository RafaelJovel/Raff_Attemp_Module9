# Work Item 002: Create the Feature Lookup Agent

## Story Details

**Goal:** Build an agent that translates natural language queries into feature metadata.

## Acceptance Criteria

### Task 1: Feature Lookup Tools
**Status**: ✅ COMPLETED

- **Given** the `data/incoming/` directory contains feature folders with JIRA metadata
- **When** tools are invoked to list or retrieve feature information
- **Then** the system returns accurate feature metadata

**Commit**: `06d650a` - feat: implement feature lookup tools (Task 1)

### Task 2: Feature Lookup Agent with LLM
**Status**: ✅ COMPLETED

- **Given** a natural language query about feature readiness
- **When** the Feature Lookup Agent processes the query
- **Then** it correctly identifies the feature and target environment

**Commit**: `7a7a190` - feat: implement Feature Lookup Agent with LLM (Task 2)

#### Test Strategy (Archive - Task Complete)

**Unit Tests (Agent Behavior):**
1. Agent identifies feature by JIRA key and extracts Production target
   - Input: "Is PLAT-1523 ready for production?"
   - Expected: `feature_key="PLAT-1523"`, `target_environment="Production"`
   - Verify: Agent calls `get_feature_metadata("PLAT-1523")`

2. Agent identifies feature by name and extracts UAT target
   - Input: "Check maintenance scheduling for UAT"
   - Expected: Feature matched by name (fuzzy match), `target_environment="UAT"`
   - Verify: Agent calls `list_all_features()` first, then `get_feature_metadata()`

3. Agent handles feature not found gracefully
   - Input: "Is feature XYZ ready for production?"
   - Expected: Error response indicating feature not found
   - Verify: Agent provides helpful message

4. Agent extracts default target environment when not specified
   - Input: "Tell me about PLAT-1523"
   - Expected: Feature found, `target_environment="UAT"` (default)
   - Verify: Agent defaults to UAT when target not mentioned

5. Agent handles ambiguous feature names
   - Input: "Is the reservation feature ready?"
   - Expected: Agent picks best match or asks for clarification
   - Verify: Behavior is deterministic and documented

**Integration Tests:**
6. Verify Ollama connectivity and model availability
   - Check `http://localhost:11434` is reachable
   - Verify `qwen2.5` model is available
   - Test basic LLM invocation

**Test Approach:**
- Mock `IFeatureLookupTools` for unit tests (don't hit real file system)
- Use test-specific system prompts for predictable behavior
- Integration test validates Ollama is configured correctly

#### File Changes

**Package Dependencies:**
```bash
dotnet add src/FeatureAssessment.Core package Microsoft.SemanticKernel
dotnet add src/FeatureAssessment.Core package Microsoft.Extensions.Logging.Abstractions
```

**New Files:**
1. `src/FeatureAssessment.Core/Agents/IFeatureLookupAgent.cs` - Agent interface
   - Method: `Task<FeatureLookupResult> LookupFeatureAsync(string query, CancellationToken cancellationToken = default)`

2. `src/FeatureAssessment.Core/Agents/FeatureLookupAgent.cs` - Agent implementation
   - Creates Semantic Kernel with Ollama LLM provider
   - Registers `IFeatureLookupTools` as kernel plugins
   - Executes agent with system prompt
   - Parses agent response into structured result

3. `src/FeatureAssessment.Core/Models/FeatureLookupResult.cs` - Result model
   - Properties: `FeatureKey`, `FeatureId`, `TargetEnvironment`, `IsSuccess`, `ErrorMessage`

4. `src/FeatureAssessment.Core/Configuration/OllamaConfiguration.cs` - Configuration
   - Settings: `Endpoint` (http://localhost:11434), `ModelName` (qwen2.5:latest)

5. `src/FeatureAssessment.Core/Prompts/FeatureLookupSystemPrompt.cs` - System prompt
   - Instructions for parsing queries
   - Tool usage guidance
   - Output format specification

6. `tests/FeatureAssessment.Core.Tests/Agents/FeatureLookupAgentTests.cs` - Unit tests
   - Mock `IFeatureLookupTools` with Moq
   - Test all 5 scenarios from test strategy

7. `tests/FeatureAssessment.Core.Tests/Integration/OllamaConnectivityTests.cs` - Integration tests
   - Mark with `[TestCategory("Integration")]`
   - Verify Ollama endpoint and model

**Technical Decisions:**
- Hardcode Ollama endpoint `http://localhost:11434` for now (refactor to Options pattern later if needed)
- Return structured `FeatureLookupResult` for type safety
- Unit tests mock kernel, integration test uses real Ollama

**Ollama Configuration:**
- Endpoint: `http://localhost:11434`
- Model: `qwen2.5:latest`
- API: OpenAI-compatible (use Semantic Kernel's OpenAI connector with custom endpoint)

### Task 3: State Management, Configuration, & Resilience
**Status**: ✅ COMPLETED

**Commit**: `0caab82` - feat: implement state management, configuration, and resilience (Task 3)

**Acceptance Criteria:**

1. **State Management Integration**
   - **Given** the Feature Lookup Agent has identified a feature
   - **When** the node function updates application state
   - **Then** state contains `feature_id`, `feature_key`, `current_stage`, and `target_environment`

2. **Configuration Refactoring (Options Pattern)**
   - **Given** Ollama configuration is currently hardcoded
   - **When** the application initializes
   - **Then** configuration is loaded via `IOptions<OllamaConfiguration>` with validation

3. **Error Handling & Resilience**
   - **Given** Ollama may be temporarily unavailable or slow
   - **When** the Feature Lookup Agent makes LLM calls
   - **Then** retry policies and timeouts are applied, and failures are handled gracefully

4. **Integration Test Documentation**
   - **Given** integration tests require Ollama setup
   - **When** developers run tests or configure CI/CD
   - **Then** clear documentation exists for setting up test dependencies

#### Test Strategy

**Unit Tests (State Management):**
1. Test state initialization with `FeatureLookupResult`
   - Input: Valid `FeatureLookupResult` with all fields populated
   - Expected: State object contains correct `feature_id`, `feature_key`, `current_stage`, `target_environment`
   - Verify: State can be serialized/deserialized

2. Test state update with partial results
   - Input: `FeatureLookupResult` with only `feature_key` (no feature_id found)
   - Expected: State reflects partial success, error details captured
   - Verify: Graceful handling of incomplete data

**Unit Tests (Configuration):**
3. Test Options pattern validation
   - Input: Invalid configuration (empty endpoint, null model name)
   - Expected: Configuration validation fails with clear error messages
   - Verify: `IValidateOptions<OllamaConfiguration>` catches invalid config

4. Test configuration binding from IConfiguration
   - Input: Mock `IConfiguration` with Ollama settings
   - Expected: `OllamaConfiguration` properly bound via Options
   - Verify: Configuration values correctly mapped

**Unit Tests (Resilience):**
5. Test retry policy on transient failures
   - Input: Mock Ollama returns HTTP 503 (Service Unavailable) twice, then succeeds
   - Expected: Agent retries and eventually succeeds
   - Verify: Polly retry policy triggers correctly

6. Test timeout handling
   - Input: Mock Ollama takes >30 seconds to respond
   - Expected: Operation times out with appropriate error
   - Verify: Timeout policy enforced, no hanging requests

7. Test circuit breaker after repeated failures
   - Input: Mock Ollama returns 500 errors repeatedly
   - Expected: Circuit breaker opens, fast-fail subsequent requests
   - Verify: Circuit breaker policy prevents cascading failures

**Integration Tests:**
8. Test end-to-end with real Ollama and configuration
   - Verify: Application loads configuration from `appsettings.json`
   - Verify: Agent successfully calls real Ollama with configured settings
   - Verify: Resilience policies work with real network conditions

**Documentation Validation:**
9. Manual check: `TESTING.md` or `README.md` includes:
   - Ollama installation instructions
   - Model download command (`ollama pull qwen2.5:latest`)
   - How to run tests with/without integration tests
   - CI/CD setup guidance (Docker, test containers)

#### File Changes

**Modified Files:**

1. **`src/FeatureAssessment.Core/Configuration/OllamaConfiguration.cs`** - Refactor to Options pattern
   ```csharp
   public class OllamaConfiguration
   {
       public const string SectionName = "Ollama";

       public string Endpoint { get; set; } = "http://localhost:11434";
       public string ModelName { get; set; } = "qwen2.5:latest";
       public int TimeoutSeconds { get; set; } = 30;
       public int MaxRetries { get; set; } = 3;
   }
   ```

2. **`src/FeatureAssessment.Core/Agents/FeatureLookupAgent.cs`** - Update constructor
   - Accept `IOptions<OllamaConfiguration>` via constructor injection
   - Remove hardcoded configuration
   - Apply resilience policies when creating kernel

**New Files:**

3. **`src/FeatureAssessment.Core/Configuration/OllamaConfigurationValidator.cs`** - Validator
   ```csharp
   public class OllamaConfigurationValidator : IValidateOptions<OllamaConfiguration>
   {
       public ValidateOptionsResult Validate(string? name, OllamaConfiguration options)
       {
           // Validate Endpoint is valid URI
           // Validate ModelName is not empty
           // Validate TimeoutSeconds > 0
           // Validate MaxRetries >= 0
       }
   }
   ```

4. **`src/FeatureAssessment.Core/Models/AssessmentState.cs`** - State model
   ```csharp
   public record AssessmentState
   {
       public string? FeatureId { get; init; }
       public string? FeatureKey { get; init; }
       public string? CurrentStage { get; init; }
       public string? TargetEnvironment { get; init; }
       public bool IsFeatureIdentified { get; init; }
       public string? ErrorMessage { get; init; }
       public Dictionary<string, object> Metadata { get; init; } = new();
   }
   ```

5. **`src/FeatureAssessment.Core/Policies/ResiliencePolicies.cs`** - Polly policies
   ```csharp
   public static class ResiliencePolicies
   {
       public static IAsyncPolicy<HttpResponseMessage> CreateOllamaPolicy(int maxRetries, int timeoutSeconds);
       // Implements: Retry, Timeout, Circuit Breaker
   }
   ```

6. **`tests/FeatureAssessment.Core.Tests/Configuration/OllamaConfigurationValidatorTests.cs`** - Validator tests
   - Test valid configurations pass validation
   - Test invalid configurations fail with specific error messages
   - Test edge cases (empty strings, negative numbers)

7. **`tests/FeatureAssessment.Core.Tests/Models/AssessmentStateTests.cs`** - State tests
   - Test state initialization from `FeatureLookupResult`
   - Test state serialization/deserialization
   - Test partial state scenarios

8. **`tests/FeatureAssessment.Core.Tests/Agents/FeatureLookupAgentResilienceTests.cs`** - Resilience tests
   - Mock transient failures and verify retries
   - Mock timeouts and verify timeout handling
   - Mock circuit breaker scenarios
   - Use `WireMock.Net` for HTTP mocking

9. **`tests/FeatureAssessment.Core.Tests/Integration/OllamaEndToEndTests.cs`** - E2E integration test
   - Mark with `[TestCategory("Integration")]`
   - Test full flow: configuration → agent → real Ollama → state
   - Verify resilience policies work with real network

10. **`TESTING.md`** (or update `README.md`) - Integration test documentation
    ```markdown
    ## Running Tests

    ### Prerequisites
    - Ollama installed: https://ollama.com/download
    - Qwen2.5 model: `ollama pull qwen2.5:latest`
    - Ollama service running: `ollama serve` (default port 11434)

    ### Running Tests
    ```bash
    # Unit tests only (fast, no external dependencies)
    dotnet test --filter "Category!=Integration"

    # All tests including integration (requires Ollama)
    dotnet test

    # With coverage
    dotnet test /p:CollectCoverage=true
    ```

    ### CI/CD Setup
    - Use Docker: `ollama/ollama:latest` image
    - Pull model in CI pipeline: `docker exec ollama ollama pull qwen2.5:latest`
    - Alternative: Use test containers with Ollama
    ```

**Package Dependencies:**
```bash
dotnet add src/FeatureAssessment.Core package Polly
dotnet add src/FeatureAssessment.Core package Polly.Extensions.Http
dotnet add src/FeatureAssessment.Core package Microsoft.Extensions.Options
dotnet add src/FeatureAssessment.Core package Microsoft.Extensions.Options.DataAnnotations

dotnet add tests/FeatureAssessment.Core.Tests package WireMock.Net
```

**Configuration File:**

11. **`src/FeatureAssessment.Core/appsettings.json`** (or project root)
    ```json
    {
      "Ollama": {
        "Endpoint": "http://localhost:11434",
        "ModelName": "qwen2.5:latest",
        "TimeoutSeconds": 30,
        "MaxRetries": 3
      }
    }
    ```

#### Technical Decisions

**Options Pattern:**
- Use `IOptions<OllamaConfiguration>` for runtime configuration access
- Use `IValidateOptions<OllamaConfiguration>` for startup validation
- Bind configuration from `appsettings.json` or environment variables
- Fail fast on invalid configuration at application startup

**Resilience Strategy (Polly):**
- **Retry Policy**: Exponential backoff, max 3 retries, on HTTP 5xx or network errors
- **Timeout Policy**: 30 seconds default (configurable)
- **Circuit Breaker**: Open after 5 consecutive failures, half-open after 30 seconds
- Policies applied to `HttpClient` used by Semantic Kernel's Ollama connector

**State Management:**
- Use immutable `record` for `AssessmentState` (thread-safe)
- State transitions are explicit (not auto-updated)
- State includes error tracking for failed lookups
- Metadata dictionary allows extension without breaking changes

**Testing Approach:**
- Unit tests mock HTTP layer with `WireMock.Net`
- Integration tests require real Ollama (marked with `[TestCategory("Integration")]`)
- CI/CD can run unit tests only for fast feedback, integration tests in separate job
- Document Ollama setup clearly to reduce friction

**Documentation Priority:**
- `TESTING.md` is a MUST for this task to be complete
- Include troubleshooting section (common Ollama issues)
- Provide Docker Compose example for local development

### Task 4: Observability & Tracing + Integration Test Fixes
**Status**: 🔵 IN PROGRESS (PLAN)

**Acceptance Criteria:**

1. **Trace Context Initialization**
   - **Given** the Feature Lookup Agent is the entry point
   - **When** the agent executes
   - **Then** trace context is initialized and all operations are logged

2. **Fix Semantic Kernel + Ollama Integration Tests** (Known Limitations from Task 3)
   - **Given** Ollama is running with qwen2.5:0.5b model
   - **When** integration tests execute FeatureLookupAgent with real Ollama
   - **Then** the agent successfully calls tools and returns results

**ROOT CAUSE IDENTIFIED (Investigated during Task 3 REFLECT & ADAPT):**

✅ **Ollama connectivity verified:**
- Ollama IS running at `http://localhost:11434`
- Model `qwen2.5:0.5b` IS available
- Tests `OllamaEndpoint_IsReachable` and `OllamaModel_IsAvailable` PASS

❌ **Configuration Issues Found:**

**Issue 1: Missing `/v1` suffix in endpoint**
- **Current**: `OllamaConfiguration.Endpoint = "http://localhost:11434"` (line 18)
- **Required**: `"http://localhost:11434/v1"` (OpenAI-compatible API path)
- **Evidence**: `curl -X POST http://localhost:11434/v1/chat/completions` succeeds
- **Impact**: Semantic Kernel's OpenAI connector cannot reach the correct API endpoint

**Issue 2: Model name mismatch**
- **Configured default**: `OllamaConfiguration.ModelName = "qwen2.5:latest"` (line 24)
- **Actually available**: `qwen2.5:0.5b`
- **Evidence**: `curl http://localhost:11434/api/tags` shows only `qwen2.5:0.5b`
- **Impact**: Tests using "qwen2.5:latest" will fail with model-not-found errors

**Test Failure Analysis:**
- `FeatureLookupAgent_CanConnectToOllama`: Agent NEVER called tools (MockException: no invocations)
  - Root cause: LLM call failed before tool execution
  - Reason: Wrong endpoint (missing `/v1`)
- `FeatureLookupAgent_WithRealTools_CanIdentifyFeature`: Returns `IsSuccess = false`
  - Root cause: Same endpoint issue
  - Agent executes but LLM interaction fails silently

**Fixes Required:**
1. Change default endpoint in `OllamaConfiguration.cs` to `"http://localhost:11434/v1"`
2. Change default model to `"qwen2.5:0.5b"` OR update documentation to require specific model
3. Un-ignore integration tests in `OllamaConnectivityTests.cs` (lines 71, 109)
4. Verify tests pass with corrected configuration

#### Test Strategy

**Acceptance Criterion 1: Trace Context Initialization**

**Unit Tests (Tracing Behavior):**
1. Test Activity creation when agent executes
   - Input: Mock agent execution
   - Expected: Activity with name "FeatureLookupAgent.LookupFeature" is created
   - Verify: Activity.Current is set and has correct operation name

2. Test span hierarchy for tool calls
   - Input: Agent calls tools during execution
   - Expected: Child spans created for each tool invocation
   - Verify: Parent-child relationship preserved (tool spans nested under agent span)

3. Test trace attributes/tags are set
   - Input: Agent execution with query "Is PLAT-1523 ready?"
   - Expected: Span tags include: query text, feature_key, target_environment
   - Verify: Tags are properly attached to Activity

4. Test Activity propagation through async calls
   - Input: Async agent execution with multiple awaits
   - Expected: Activity.Current is maintained across async boundaries
   - Verify: Trace context not lost during async operations

5. Test error recording in spans
   - Input: Agent encounters error (feature not found)
   - Expected: Span marked with error status and exception details
   - Verify: Activity.SetStatus(ActivityStatusCode.Error) called

**Integration Tests (Observability):**
6. Test end-to-end trace with real Ollama
   - Input: Full agent execution with real LLM
   - Expected: Complete trace hierarchy visible (agent → LLM call → tool calls)
   - Verify: All operations captured in trace
   - Mark with `[TestCategory("Integration")]`

**Manual Verification:**
7. Run agent with trace exporter configured
   - Use console exporter or OTLP exporter
   - Verify trace output shows complete execution flow
   - Verify spans include timing information

**Acceptance Criterion 2: Fix Integration Tests**

**Configuration Fix Validation:**
8. Test OllamaConfiguration with corrected defaults
   - Input: Default configuration (no overrides)
   - Expected: Endpoint = "http://localhost:11434/v1", ModelName = "qwen2.5:0.5b"
   - Verify: Configuration validator accepts new defaults

**Integration Tests (Previously Failing):**
9. Un-ignore and run `FeatureLookupAgent_CanConnectToOllama`
   - Input: Query "Is PLAT-1523 ready for production?"
   - Expected: Agent successfully calls mock tools
   - Verify: Tools invoked at least once, no connection errors

10. Un-ignore and run `FeatureLookupAgent_WithRealTools_CanIdentifyFeature`
    - Input: Query with real file system tools
    - Expected: Agent returns `IsSuccess = true` with feature identified
    - Verify: Feature lookup succeeds, correct feature metadata returned

11. Test OllamaEndToEndTests with corrected configuration
    - Input: End-to-end test with real Ollama
    - Expected: All integration tests pass
    - Verify: No timeout, connection, or model-not-found errors

**Regression Tests:**
12. Verify existing unit tests still pass
    - Run all unit tests (excluding integration)
    - Expected: No regressions from configuration changes
    - Verify: `dotnet test --filter "Category!=Integration"` passes

#### File Changes

**Package Dependencies:**
```bash
# Add OpenTelemetry packages for tracing
dotnet add src/FeatureAssessment.Core package OpenTelemetry
dotnet add src/FeatureAssessment.Core package OpenTelemetry.Api
dotnet add src/FeatureAssessment.Core package OpenTelemetry.Extensions.Hosting

# For testing/debugging traces
dotnet add tests/FeatureAssessment.Core.Tests package OpenTelemetry.Exporter.Console
```

**Modified Files:**

1. **`src/FeatureAssessment.Core/Configuration/OllamaConfiguration.cs`** - Fix default configuration
   ```csharp
   // CHANGE LINE 18:
   - public string Endpoint { get; set; } = "http://localhost:11434";
   + public string Endpoint { get; set; } = "http://localhost:11434/v1";

   // CHANGE LINE 24 (or thereabouts):
   - public string ModelName { get; set; } = "qwen2.5:latest";
   + public string ModelName { get; set; } = "qwen2.5:0.5b";
   ```

2. **`src/FeatureAssessment.Core/Agents/FeatureLookupAgent.cs`** - Add tracing
   ```csharp
   // Add ActivitySource field
   private static readonly ActivitySource ActivitySource = new("FeatureAssessment.FeatureLookup");

   // Wrap LookupFeatureAsync with Activity
   public async Task<FeatureLookupResult> LookupFeatureAsync(string query, CancellationToken cancellationToken)
   {
       using var activity = ActivitySource.StartActivity("FeatureLookupAgent.LookupFeature");
       activity?.SetTag("query", query);

       try
       {
           // Existing logic...
           activity?.SetTag("feature_key", result.FeatureKey);
           activity?.SetTag("target_environment", result.TargetEnvironment);
           return result;
       }
       catch (Exception ex)
       {
           activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
           throw;
       }
   }
   ```

3. **`tests/FeatureAssessment.Core.Tests/Integration/OllamaConnectivityTests.cs`** - Un-ignore tests
   ```csharp
   // REMOVE [Ignore] attributes from lines 71 and 109:
   - [Ignore("Integration test - requires Ollama running")]
   + // [Ignore] removed - configuration fixed
   [TestMethod]
   [TestCategory("Integration")]
   public async Task FeatureLookupAgent_CanConnectToOllama()
   ```

**New Files:**

4. **`src/FeatureAssessment.Core/Observability/ActivitySources.cs`** - Centralized ActivitySource
   ```csharp
   namespace FeatureAssessment.Core.Observability;

   public static class ActivitySources
   {
       public const string ServiceName = "FeatureAssessment";

       public static readonly ActivitySource FeatureLookup = new(
           $"{ServiceName}.FeatureLookup",
           version: "1.0.0"
       );

       public static readonly ActivitySource Tools = new(
           $"{ServiceName}.Tools",
           version: "1.0.0"
       );
   }
   ```

5. **`tests/FeatureAssessment.Core.Tests/Observability/FeatureLookupAgentTracingTests.cs`** - Tracing tests
   ```csharp
   [TestClass]
   public class FeatureLookupAgentTracingTests
   {
       [TestMethod]
       public async Task LookupFeatureAsync_CreatesActivity()
       [TestMethod]
       public async Task LookupFeatureAsync_SetsSpanAttributes()
       [TestMethod]
       public async Task LookupFeatureAsync_RecordsErrorOnFailure()
       [TestMethod]
       public async Task LookupFeatureAsync_PreservesActivityAcrossAsync()
   }
   ```

6. **`tests/FeatureAssessment.Core.Tests/Integration/OllamaTracingEndToEndTests.cs`** - E2E trace test
   ```csharp
   [TestClass]
   public class OllamaTracingEndToEndTests
   {
       [TestMethod]
       [TestCategory("Integration")]
       public async Task FeatureLookupAgent_WithTracing_GeneratesCompleteTrace()
       {
           // Setup ActivityListener to capture activities
           // Execute agent with real Ollama
           // Verify activity hierarchy and attributes
       }
   }
   ```

**Documentation Updates:**

7. **`TESTING.md`** - Update with corrected configuration
   ```markdown
   ## Configuration

   ### Ollama Setup
   - **Endpoint**: `http://localhost:11434/v1` (note: `/v1` suffix required for OpenAI API compatibility)
   - **Model**: `qwen2.5:0.5b` (or any available model - check with `ollama list`)
   - **Installation**: See https://ollama.com/download
   - **Pull Model**: `ollama pull qwen2.5:0.5b`

   ### Running Integration Tests
   ```bash
   # Verify Ollama is running and model is available
   curl http://localhost:11434/api/tags
   curl -X POST http://localhost:11434/v1/chat/completions \
     -H "Content-Type: application/json" \
     -d '{"model":"qwen2.5:0.5b","messages":[{"role":"user","content":"test"}]}'

   # Run integration tests
   dotnet test --filter "Category=Integration"
   ```

   ### Observability (NEW)

   #### Viewing Traces
   ```bash
   # Run with console trace exporter for debugging
   OTEL_DOTNET_EXPERIMENTAL_CONSOLE_EXPORTER_ENABLED=true dotnet test
   ```
   ```

#### Technical Decisions

**Tracing Strategy:**
- Use System.Diagnostics.Activity (built-in .NET distributed tracing)
- OpenTelemetry for standardization and interoperability
- ActivitySource per component area (FeatureLookup, Tools, etc.)
- Manual instrumentation (explicit span creation) for clarity
- Console exporter for development, OTLP for production

**Activity Naming Convention:**
- Format: `{Component}.{Operation}` (e.g., "FeatureLookupAgent.LookupFeature")
- Tool calls: "Tools.{ToolName}" (e.g., "Tools.ListAllFeatures")
- Use descriptive operation names for observability

**Span Attributes (Tags):**
- `query`: Original user query
- `feature_key`: Identified feature JIRA key
- `feature_id`: Feature folder identifier
- `target_environment`: UAT or Production
- `is_success`: Boolean success indicator
- `error.message`: Error details if failed

**Configuration Fix Strategy:**
- Change defaults in OllamaConfiguration.cs (breaking change, but justified)
- Update documentation to reflect correct endpoint format
- Provide troubleshooting guidance in TESTING.md
- Consider adding configuration validation for `/v1` suffix

**Testing Approach:**
- Unit tests mock Activity creation (use ActivityListener)
- Integration tests verify real trace generation with Ollama
- Mark integration tests with `[TestCategory("Integration")]`
- Manual verification with console exporter during development

**Observability Foundation:**
- This is the FIRST agent, so tracing setup here is critical
- All future agents will follow same pattern
- Coordinator Agent will inherit trace context from Feature Lookup Agent
- Trace context propagates through entire assessment workflow

### Task 5: Manual Testing Harness
**Status**: ⚪ NOT STARTED

- **Given** the Feature Lookup Agent is implemented
- **When** the user runs the standalone test harness with sample queries
- **Then** the agent can be manually verified through console output showing feature identification and environment extraction

### Notes
 What to Build

1. **Define the Feature Lookup Tools:**
   - `list_all_features()` - Scans `data/incoming/` directory for all feature folders
     - Reads each `feature*/jira/feature_issue.json`
     - Returns list with: feature_id, JIRA key, summary, current stage
   - `get_feature_metadata(feature_identifier)` - Retrieves full JIRA metadata
     - Accepts JIRA key (e.g., "PLAT-1523"), feature ID (e.g., "feature1"), or feature name
     - Reads `data/incoming/<feature_id>/jira/feature_issue.json`
     - Returns complete JIRA issue data

2. **Create the Feature Lookup Agent:**
   - This is a **mini-agent** with LLM capabilities
   - Give it access to both feature lookup tools
   - Provide a system prompt that instructs it to:
     - Parse natural language queries
     - Use `list_all_features()` to discover available features
     - Use `get_feature_metadata()` to retrieve detailed information
     - Match fuzzy references (feature names, JIRA keys, IDs) to actual features
     - Extract target environment from query (UAT or Production)
     - Handle "feature not found" errors gracefully

3. **Create a Node Function (or equivalent):**
   - Function that takes state as input
   - Extracts user query from conversation/messages
   - Invokes the feature lookup agent
   - Parses agent's response
   - Updates state with: `feature_id`, `feature_key`, `current_stage`, `target_environment`
   - Adds a message to conversation history describing what was found
#### Testing & Verification

**Unit Tests:**
- Test `list_all_features()` returns correct feature list
- Test `get_feature_metadata()` with various identifiers (JIRA key, feature ID, name)
- Test error handling for non-existent features

**Agent Tests:**
- Test query: "Is PLAT-1523 ready for production?"
  - Verify: `feature_key="PLAT-1523"`, `target_environment="Production"`
- Test query: "Check maintenance scheduling for UAT"
  - Verify: Feature matched by name, `target_environment="UAT"`
- Test query: "Is feature XYZ ready?"
  - Verify: Error state set with helpful message

**Observability:**
- **Set up trace context initialization** - Since this is the first agent, this is where you'll start the trace that all subsequent agents will inherit
- Add logging/tracing for:
  - Feature lookup started (this should be the root span/trace)
  - Tools invoked (which tool, parameters)
  - Feature found/not found
  - Target environment determination
- Verify you can see:
  - Complete trace of agent execution
  - Tool calls and responses
  - LLM calls and prompts
- **Important:** The tracing setup here becomes the foundation for all subsequent agent tracing

**Manual Verification by user:**
Run the agent standalone with test queries and verify output makes sense.
