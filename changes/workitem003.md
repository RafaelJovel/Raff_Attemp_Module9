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
