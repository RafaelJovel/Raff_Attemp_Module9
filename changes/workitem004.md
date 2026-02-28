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

---

## Task Breakdown

### Task 1: DocumentationTools - Core Implementation

**Status**: 🔵 IN PROGRESS (PLAN stage)

**Acceptance Criteria (Given-When-Then):**

> **Given** a feature_id "feature1"
> **When** `IDocumentationTools.ListPlanningDocsAsync(feature_id)` is called
> **Then**:
> - Returns list of available .md files in `data/incoming/feature1/planning/`
> - List is ordered alphabetically
> - Includes USER_STORY.md, DESIGN_DOC.md, ARCHITECTURE.md, etc. (all files found)
> - Returns empty list if directory doesn't exist (no error thrown)
>
> **And Given** feature_id="feature1", doc_name="USER_STORY"
> **When** `IDocumentationTools.ReadPlanningDocAsync(feature_id, doc_name)` is called
> **Then**:
> - Returns full markdown content of the document
> - Or returns graceful error message if file not found
> - Handles missing doc extension (.md added if not provided)

**Test Strategy:**

Unit tests (fast, no file I/O mocking):
1. `ListPlanningDocsAsync_ReturnsCorrectList_WhenDirectoryExists` — Mock file system, verify list structure
2. `ListPlanningDocsAsync_ReturnsEmptyList_WhenDirectoryNotFound` — Directory doesn't exist, returns empty
3. `ListPlanningDocsAsync_ReturnsOrderedList` — Results are alphabetically ordered
4. `ReadPlanningDocAsync_ReturnsContent_WhenFileExists` — Mock file read, returns content
5. `ReadPlanningDocAsync_ReturnsError_WhenFileNotFound` — File doesn't exist, returns error message (no exception)
6. `ReadPlanningDocAsync_HandlesExtension_AutoAppendsMd` — If input is "USER_STORY", finds "USER_STORY.md"

Integration tests (real file I/O):
7. `ListPlanningDocsAsync_WithRealData_ReturnsAllFeature1Docs` — Uses actual data/incoming/feature1/planning/
8. `ListPlanningDocAsync_WithRealData_ReturnsUserStoryContent` — Reads actual USER_STORY.md
9. `ReadPlanningDocAsync_WithRealData_VerifyDocSections` — Content includes expected sections

**File Changes:**

New files:
- `src/FeatureAssessment.Core/Tools/IDocumentationTools.cs` — Interface definition
- `src/FeatureAssessment.Core/Tools/DocumentationTools.cs` — Implementation
- `tests/FeatureAssessment.Core.Tests/Tools/DocumentationToolsTests.cs` — Unit tests
- `tests/FeatureAssessment.Core.Tests/Integration/DocumentationToolsIntegrationTests.cs` — Integration tests

Modified files:
- (None - this is a new tool, orthogonal to existing code)

**Design Notes:**
- `DocumentationTools` injects `ILogger<DocumentationTools>` for error logging
- Use `Directory.GetFiles()` pattern matching to find .md files
- `ListPlanningDocsAsync` returns `Task<List<string>>` with file names only (no full paths)
- `ReadPlanningDocAsync` returns `Task<string>` — content OR error message (never throws)
- Path construction: `data/incoming/{feature_id}/planning/{doc_name}.md`
- Error messages should indicate: "File not found:" or "Directory not found:"

---

### Task 2: DocumentationSpecialistAgent - Core Implementation

**Status**: 🔳 NOT STARTED

**Acceptance Criteria (Given-When-Then):**

> **Given** a feature_id "feature1" and query "Assess USER_STORY completeness"
> **When** `IDocumentationSpecialistAgent.AssessAsync(query, feature_id)` is called
> **Then**:
> - Agent lists all available planning documents first (calls ListPlanningDocsAsync)
> - Agent reads relevant documents based on query (calls ReadPlanningDocAsync)
> - Returns assessment text summarizing which documents exist/missing
> - Reports on document completeness objectively (facts only, no judgment)
> - Does NOT throw exceptions (errors handled as strings in response)
> - Activity created via `ActivitySources.DocumentationSpecialist` with feature_id and query tags

**Test Strategy:**

Unit tests (mocked tools):
1. `AssessAsync_ListsAllDocs_ThenReadsRelevant` — Mock tools, verify both calls made
2. `AssessAsync_ReturnsFactsOnly_NoJudgment` — Response mentions docs present/missing, no "blocker" language
3. `AssessAsync_HandlesToolErrors_ReturnsGracefully` — Tool throws error, agent incorporates as message
4. `AssessAsync_WithMissingFeature_ReturnsError` — No planning docs exist, returns informative message

Integration tests (real Anthropic LLM):
5. `DocumentationAgent_WithAnthropic_AssessesFeature1` — Real query, verifies response quality
6. `DocumentationAgent_WithAnthropic_CreatesActivityWithTags` — Activity has feature_id and query tags

**File Changes:**

New files:
- `src/FeatureAssessment.Core/Agents/IDocumentationSpecialistAgent.cs` — Interface
- `src/FeatureAssessment.Core/Agents/DocumentationSpecialistAgent.cs` — Implementation
- `src/FeatureAssessment.Core/Prompts/DocumentationSpecialistSystemPrompt.cs` — System prompt
- `tests/FeatureAssessment.Core.Tests/Agents/DocumentationSpecialistAgentTests.cs` — Unit tests
- `tests/FeatureAssessment.Core.Tests/Integration/DocumentationAgentIntegrationTests.cs` — Integration tests

Modified files:
- `src/FeatureAssessment.Core/Observability/ActivitySources.cs` — Add DocumentationSpecialist source
- `src/FeatureAssessment.Core/Clients/KernelFactory.cs` — Register DocumentationTools plugin when creating kernel

**Design Notes:**
- `DocumentationSpecialistAgent` injects `IKernelFactory` and `IDocumentationTools` (or embedded in kernel via plugin)
- System prompt: Role = "Objective documentation assessor", Key = "Report facts, not judgment"
- Agent uses `FunctionChoiceBehavior.Auto()` to invoke documentation tools
- Inherits trace context from coordinator's tool call (nested spans)
- Response type: simple string (not structured JSON)

---

### Task 3: ConsultationTool & Coordinator Integration

**Status**: 🔳 NOT STARTED

**Acceptance Criteria (Given-When-Then):**

> **Given** a coordinator needing documentation assessment
> **When** coordinator has `consult_docs_specialist` tool available
> **Then**:
> - Tool takes query and feature_id parameters
> - Tool internally invokes DocumentationSpecialistAgent
> - Tool returns specialist's findings as string
> - Coordinator can see and reference the findings in reasoning

**File Changes:**

New files:
- `src/FeatureAssessment.Core/Tools/ConsultDocumentationSpecialistTool.cs` — Tool implementation
- `tests/FeatureAssessment.Core.Tests/Integration/ConsultDocToolIntegrationTests.cs` — Tests

Modified files:
- `src/FeatureAssessment.Core/Agents/CoordinatorAgent.cs` — Register ConsultDocumentationSpecialistTool in kernel
- `src/FeatureAssessment.Core/Prompts/CoordinatorSystemPrompt.cs` — Add documentation specialist to delegation instructions

**Design Notes:**
- Tool is registered as kernel plugin function
- Tool signature: `ConsultDocumentationSpecialist(string query, string featureId)`
- Coordinator system prompt: "You can consult a documentation specialist by using the consult_docs_specialist tool"

---

### Task 4: End-to-End Testing & Tracing

**Status**: 🔳 NOT STARTED

**Acceptance Criteria (Given-When-Then):**

> **Given** full workitem004 implementation (tasks 1-3)
> **When** workflow runs with query "Is PLAT-1523 ready for production?"
> **Then**:
> - Coordinator calls consult_docs_specialist tool
> - Documentation specialist reads PLAT-1523 docs
> - Coordinator receives assessment in its thinking
> - Trace shows nested hierarchy: workflow → coordinator → consult_docs_specialist → specialist_agent → doc_tools
> - All in single unified trace

**File Changes:**

Modified files:
- `tests/FeatureAssessment.Core.Tests/Integration/WorkflowIntegrationTests.cs` — Add tests for docs specialist invocation

**Design Notes:**
- Activity nesting: coordinator activity → tool invocation activity → specialist activity
- Verify trace continuity across all levels
- Manual verification: run test harness, examine traces

---

## Commit Map

| Task | Commit Message |
|------|-----------------|
| Task 1 | feat: add DocumentationTools for reading planning docs |
| Task 2 | feat: add DocumentationSpecialistAgent with assessment prompt |
| Task 3 | feat: add ConsultDocumentationSpecialist tool and coordinator integration |
| Task 4 | feat: add e2e tests for documentation specialist in workflow |

---

## Branch

`feature/workitem004-documentation-specialist`

---

## Next Steps

✅ PLAN stage complete — waiting for user review and transition to BUILD & ASSESS
