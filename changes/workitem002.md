# Work Item 002: Create the Feature Lookup Agent

## Story Details

**Goal:** Build an agent that translates natural language queries into feature metadata.

## Acceptance Criteria

### Task 1: Feature Lookup Tools
**Status**: 🔵 IN PROGRESS (REFLECT & ADAPT)

- **Given** the `data/incoming/` directory contains feature folders with JIRA metadata
- **When** tools are invoked to list or retrieve feature information
- **Then** the system returns accurate feature metadata

#### Test Strategy
**Unit Tests:**
1. `list_all_features()` returns correct feature list
   - Test with sample features in `data/incoming/`
   - Verify feature_id, JIRA key, summary, current stage are extracted
   - Test with missing/malformed JSON (error handling)

2. `get_feature_metadata(feature_identifier)` with various identifiers
   - Test with JIRA key (e.g., "PLAT-1523")
   - Test with feature ID (e.g., "feature1")
   - Test with feature name (fuzzy match)
   - Test with non-existent feature (error handling)

**Test Data:** Use existing sample features in `data/incoming/feature1-4/`

#### Reflection (REFLECT & ADAPT Stage)

**Process Assessment:**
- ✅ Planning was clear and comprehensive - test strategy and file changes were well-defined
- ✅ Build & Assess went smoothly with no friction points
- ✅ Test coverage adequately validated acceptance criteria
- ✅ Quality validation passed cleanly (dotnet test, dotnet format, dotnet build)

**Future Task Assessment:**
- ✅ Task sequence remains optimal - no reordering needed
- ✅ Task breakdown is appropriate - no adjustments needed
- 📝 Note for Task 2: Ollama is running but needs configuration (endpoint, model selection)

**Process Improvements:**
- None needed - workflow was effective for this task

#### File Changes
**Project Setup:**
```bash
dotnet new sln -n FeatureReadinessAssessment
dotnet new classlib -n FeatureAssessment.Core -o src/FeatureAssessment.Core
dotnet new mstest -n FeatureAssessment.Core.Tests -o tests/FeatureAssessment.Core.Tests
dotnet sln add src/FeatureAssessment.Core tests/FeatureAssessment.Core.Tests
dotnet add tests/FeatureAssessment.Core.Tests reference src/FeatureAssessment.Core
dotnet add tests/FeatureAssessment.Core.Tests package FluentAssertions
dotnet add tests/FeatureAssessment.Core.Tests package Moq

# Add System.Text.Json for JSON parsing
dotnet add src/FeatureAssessment.Core package System.Text.Json
```

**LLM Configuration (Ollama + Qwen2.5):**
- Endpoint: `http://localhost:11434` (Ollama in Docker)
- Model: `qwen2.5` (or specific version like `qwen2.5:latest`)
- API: Ollama provides OpenAI-compatible API
- NOTE: Semantic Kernel integration will be added in Task 2 (Agent with LLM)

**New Files:**
1. `src/FeatureAssessment.Core/Tools/IFeatureLookupTools.cs` - Interface
2. `src/FeatureAssessment.Core/Tools/FeatureLookupTools.cs` - Implementation
3. `src/FeatureAssessment.Core/Models/FeatureInfo.cs` - Return model for `list_all_features`
4. `src/FeatureAssessment.Core/Models/FeatureMetadata.cs` - Return model for `get_feature_metadata`
5. `tests/FeatureAssessment.Core.Tests/Tools/FeatureLookupToolsTests.cs` - Unit tests

### Task 2: Feature Lookup Agent with LLM
**Status**: ⚪ NOT STARTED

- **Given** a natural language query about feature readiness
- **When** the Feature Lookup Agent processes the query
- **Then** it correctly identifies the feature and target environment

**📝 NOTE**: Ollama is running but needs configuration during this task:
- Configure endpoint: `http://localhost:11434`
- Select/verify model: `qwen2.5:latest` (or appropriate version)
- Test Ollama connectivity before implementing agent

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
