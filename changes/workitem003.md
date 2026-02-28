# Work Item 003: Create the Coordinator Agent (Supervisor)

## Story Details

**Goal:** Build the decision-making supervisor that will orchestrate specialist agents

### Notes
What to Build

1. **Define State Schema:**
   - Create a state object/class that will flow through your system
   - Should include:
     - Conversation history (messages or equivalent)
     - Feature metadata fields: `feature_id`, `feature_key`, `current_stage`, `target_environment`, `error`
     - Decision fields: `decision` (go/no_go/go_with_risks), `criteria_assessment`
   - In frameworks like LangGraph: inherit from `MessagesState` or equivalent
   - Ensure state supports incremental updates (nodes only update their fields)

2. **Load Decision Framework:**
   - Create or locate the decision framework document (see DESIGN.md for criteria)
   - This defines UAT vs Production deployment criteria
   - Make it accessible to the coordinator (load as context or system knowledge)

3. **Create Coordinator Agent (WITHOUT specialist tools yet):**
   - Create an agent with LLM capabilities
   - Provide a detailed system prompt:
     - Role: Decision-making supervisor
     - Responsibilities: Analyze target environment, delegate to specialists, synthesize findings, make final decision
     - Decision criteria: Reference the decision framework
     - Output format: Clear GO/NO_GO/GO_WITH_RISKS with reasoning
   - For now, give it NO tools (we'll add consultation tools in Step 4)

4. **Create a Simple Graph/Workflow:**
   - Build a simple execution flow:
     - START   lookup_feature   coordinator   END
   - Wire up the feature lookup node from Step 1
   - Add the coordinator node
   - Ensure state flows correctly between nodes

#### Testing & Verification

**Unit Tests:**
- Test state schema validation
- Test state updates (ensure incremental updates work correctly)
- Test coordinator prompt formatting

**Integration Tests:**
- Run the graph end-to-end with a query
- Verify:
  - Feature lookup populates state correctly
  - Coordinator receives correct context
  - State flows from lookup   coordinator   end
- Since coordinator has no specialist tools yet, it should respond that it lacks information to make a decision

**Observability:**
- **Ensure trace continuity** - The coordinator should inherit the trace context started by the lookup agent
- Add logging/tracing for:
  - Coordinator node started (should appear as child span in the same trace)
  - Context received (feature_id, target_environment)
  - Decision made (or lack of information)
- Verify you can see:
  - Full graph execution trace showing both lookup and coordinator nodes
  - Each node execution as part of the same trace hierarchy
  - State transitions between nodes

**Manual Verification by user:**
Run the graph with: "Is PLAT-1523 ready for production?"
- Should see feature lookup succeed
- Should see coordinator receive context but be unable to make informed decision yet
- Should see both operations in a single unified trace

---

## Task Breakdown

### Task 1: CoordinatorAgent - Core Implementation

**Status**: 🔵 IN PROGRESS

**Acceptance Criteria (Given-When-Then):**

> **Given** an AssessmentState with `IsFeatureIdentified=true`, `FeatureKey="PLAT-1523"`, `TargetEnvironment="Production"`
> **When** `ICoordinatorAgent.AssessAsync(state)` is called
> **Then**:
> - Returns updated `AssessmentState` with `CurrentStage="coordinator_completed"`
> - `CoordinatorResponse` is non-null and non-empty
> - Since no specialist tools are available, the response acknowledges insufficient data to make a confident final decision
> - An activity is created via `ActivitySources.Coordinator` with `feature_key` and `target_environment` tags
>
> **And Given** `IsFeatureIdentified=false`
> **When** `ICoordinatorAgent.AssessAsync(state)` is called
> **Then** returns state with `CurrentStage="error"` without making an LLM call

**Test Strategy:**

Unit tests (all with mocked kernel factory, no real LLM):
1. `AssessAsync_ReturnsCoordinatorCompleted_WhenFeatureIdentified` — mock kernel returns canned response, verify stage and response set
2. `AssessAsync_ReturnsError_WhenFeatureNotIdentified` — no LLM call made, stage="error"
3. `AssessAsync_CreatesActivity_WithCorrectTags` — ActivityListener captures coordinator activity with feature_key and target_environment tags
4. `AssessAsync_SetsErrorStatus_OnException` — kernel throws, activity has Error status, returns stage="error"

Integration test (Anthropic, `[TestCategory("Integration")]`):
5. `CoordinatorAgent_WithAnthropic_AcknowledgesInsufficientInformation` — real LLM, verifies coordinator responds (non-empty), acknowledges no tools available

**File Changes:**

New files:
- `src/FeatureAssessment.Core/Agents/ICoordinatorAgent.cs`
- `src/FeatureAssessment.Core/Agents/CoordinatorAgent.cs`
- `src/FeatureAssessment.Core/Prompts/CoordinatorSystemPrompt.cs`
- `tests/FeatureAssessment.Core.Tests/Agents/CoordinatorAgentTests.cs`

Modified files:
- `src/FeatureAssessment.Core/Models/AssessmentState.cs` — add `CoordinatorResponse` (string?) property and `WithCoordinatorResponse()` helper
- `src/FeatureAssessment.Core/Clients/KernelFactory.cs` — make `IFeatureLookupTools` nullable (null = no plugin registration; backward-compatible since all existing callers pass non-null)

**Design Notes:**
- `CoordinatorAgent` injects `IKernelFactory` (same pattern as `FeatureLookupAgent`)
- Coordinator does NOT use `FunctionChoiceBehavior.Auto()` — no tools available yet
- Coordinator builds its user message from the AssessmentState (feature_key, target_environment, current_stage)
- System prompt embeds the full decision framework (UAT + Production criteria from DESIGN.md)
- `CoordinatorResponse` stored directly on state (not in `Metadata`) — it's a core output field

---

### Task 2: AssessmentWorkflow - End-to-End Orchestration

**Status**: ⬜ PENDING

**Acceptance Criteria (Given-When-Then):**

> **Given** a user query "Is PLAT-1523 ready for production?"
> **When** `IAssessmentWorkflow.RunAsync(query)` is called
> **Then**:
> - `FeatureLookupAgent` executes first and populates `FeatureId`, `FeatureKey`, `TargetEnvironment`, `IsFeatureIdentified=true`
> - `CoordinatorAgent` executes next with the populated state
> - Final state has `CurrentStage="coordinator_completed"` and non-null `CoordinatorResponse`
> - Both coordinator and feature lookup activities appear in the trace hierarchy (coordinator as sibling/child of lookup)
>
> **And Given** feature lookup fails (feature not found)
> **When** `RunAsync(query)` is called
> **Then** workflow returns state with `CurrentStage="error"` (coordinator is not invoked)

**Test Strategy:**

Unit tests (all mocked):
1. `RunAsync_ExecutesLookupThenCoordinator_WhenFeatureFound` — verify both agents called in order, state flows correctly
2. `RunAsync_SkipsCoordinator_WhenFeatureNotFound` — lookup returns failure, coordinator not called, stage="error"
3. `RunAsync_ReturnsErrorState_OnLookupException` — lookup throws, workflow returns error state gracefully

Integration test (Anthropic, `[TestCategory("Integration")]`):
4. `Workflow_WithAnthropic_ExecutesFullFlow` — real Anthropic LLM, PLAT-1523 query, verify state populated through both agents

Tracing integration test:
5. `Workflow_CoordinatorActivity_AppearsInSameTrace` — ActivityListener captures both lookup and coordinator activities

**File Changes:**

New files:
- `src/FeatureAssessment.Core/Workflow/IAssessmentWorkflow.cs`
- `src/FeatureAssessment.Core/Workflow/AssessmentWorkflow.cs`
- `tests/FeatureAssessment.Core.Tests/Workflow/AssessmentWorkflowTests.cs`
- `tests/FeatureAssessment.Core.Tests/Integration/WorkflowIntegrationTests.cs`

**Design Notes:**
- `AssessmentWorkflow` injects `IFeatureLookupAgent` and `ICoordinatorAgent`
- **Workflow creates a root span** via `ActivitySources.Coordinator` named `"AssessmentWorkflow.Run"` — this ensures both the feature lookup and coordinator activities appear as children of a single trace rather than as two separate root spans. This satisfies the workitem003 requirement for trace continuity across both agents.
- If lookup fails (`IsFeatureIdentified=false`), coordinator is skipped and state returned with error
- Workflow lives in `FeatureAssessment.Core.Workflow` namespace

---

## Commit Map

| Task | Commit |
|------|--------|
| Task 1 | feat: add CoordinatorAgent with decision framework prompt |
| Task 2 | feat: add AssessmentWorkflow orchestrating lookup and coordinator |

---

## Branch

`feature/workitem003-coordinator-agent`
