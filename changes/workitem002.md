# Work Item 002: Create the Feature Lookup Agent

## Story Details

**Goal:** Build an agent that translates natural language queries into feature metadata.

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
