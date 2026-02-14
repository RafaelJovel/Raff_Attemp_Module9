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
**Status**: 🔵 IN PROGRESS (PLAN)

- **Given** a natural language query about feature readiness
- **When** the Feature Lookup Agent processes the query
- **Then** it correctly identifies the feature and target environment

#### Test Strategy

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

### Task 3: State Management Integration
**Status**: ⚪ NOT STARTED

- **Given** the agent has identified a feature
- **When** the node function updates application state
- **Then** state contains `feature_id`, `feature_key`, `current_stage`, and `target_environment`

### Task 4: Observability & Tracing
**Status**: ⚪ NOT STARTED

- **Given** the Feature Lookup Agent is the entry point
- **When** the agent executes
- **Then** trace context is initialized and all operations are logged

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
