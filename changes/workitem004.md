# Work Item 004: Create the Documentation Specialist Agent

## Story Details

**Goal:** Build a specialist agent that objectively assesses documentation completeness.

### Notes
What to Build

1. **Define Documentation Tools:**
   - `list_planning_docs(feature_id)` - Lists available planning documents
     - Scans `data/incoming/<feature_id>/planning/` directory
     - Returns list of available .md files
   - `read_planning_doc(feature_id, doc_name)` - Reads a specific document
     - Reads from `data/incoming/<feature_id>/planning/<doc_name>.md`
     - Returns full markdown content
     - Handles missing files gracefully with error messages

2. **Create Documentation Specialist Agent:**
   - Create an agent with access to documentation tools
   - Provide a system prompt:
     - Role: Objective documentation assessor
     - Responsibilities: Read docs, evaluate completeness, report facts, identify gaps
     - Document types: USER_STORY, DESIGN_DOC, ARCHITECTURE, DEPLOYMENT_PLAN, API_SPECIFICATION, DATABASE_SCHEMA
     - **Key principle:** Report FACTS only, no judgment calls on whether gaps are blockers
     - Always cite which documents exist and which are missing
     - Report on sections present vs missing within each document
   - Agent should return structured assessment as text

3. **Create Consultation Tool for Docs Agent:**
   - This is the KEY integration point
   - Create a tool that the coordinator can call: `consult_docs_agent(query, feature_id)`
   - Tool implementation:
     - Takes a query (e.g., "Assess documentation for production deployment")
     - Takes feature_id
     - Internally invokes the docs specialist agent
     - Extracts specialist's findings
     - Returns findings as string to coordinator
   - This tool will be given to the coordinator agent

4. **Update Coordinator Agent:**
   - Add the `consult_docs_agent` tool to coordinator
   - Update coordinator's system prompt to indicate:
     - It can consult a documentation specialist
     - It should delegate documentation assessment to the specialist
     - It should synthesize specialist's findings into decision-making

#### Testing & Verification

**Unit Tests:**
- Test `list_planning_docs()` returns correct list
- Test `read_planning_doc()` reads content correctly
- Test error handling for missing documents

**Agent Tests:**
- Test docs agent with query: "List all planning documents for feature1"
  - Verify: Returns list of available docs
- Test docs agent with query: "Assess USER_STORY completeness"
  - Verify: Reads document, evaluates completeness objectively
- Test docs agent with missing document scenario
  - Verify: Reports document as missing, doesn't fail

**Integration Tests:**
- Test consultation tool:
  - Call `consult_docs_agent("Assess all documentation", "feature1")`
  - Verify: Returns comprehensive assessment
- Test coordinator with docs agent:
  - Run graph with query: "Is PLAT-1523 ready for production?"
  - Verify: Coordinator calls `consult_docs_agent` tool
  - Verify: Coordinator receives and can reference docs findings
- Note: Coordinator still can't make full decision (needs metrics and reviews)

**Observability:**
- **This is where nested tracing becomes critical** - The docs specialist must inherit trace context from the coordinator's tool call
- Add logging/tracing for:
  - Docs agent invoked (from coordinator tool call)
  - Which tools docs agent calls
  - Documents read and assessed
  - Findings returned to coordinator
- Verify you can see the **nested trace hierarchy**:
  ```
  Graph Run (root trace)
  └── lookup_feature
      └── (tool calls)
  └── coordinator
      └── consult_docs_agent (tool call)
          └── docs_agent (specialist execution)
              └── (specialist's tool calls)
  ```
- **Key verification:** Coordinator → specialist delegation should show as nested spans within the same trace

**Manual Verification by user:**
Run the graph and examine traces:
- Should see coordinator invoke docs specialist
- Should see docs specialist read multiple documents
- Should see coordinator receive comprehensive documentation assessment
- **Critical:** All of this should be visible in a single trace view with proper nesting