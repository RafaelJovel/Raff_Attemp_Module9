# Feature Readiness Assessment System - Design Document

## Overview

This is an **AI-powered decision support system** that automates the assessment of software features to determine if they are ready to move from UAT (User Acceptance Testing) to Production deployment. The system employs **five AI agents** working in concert using a **supervisor pattern**, where specialist agents gather evidence and a coordinator synthesizes findings to make deployment decisions.

### Core Purpose

- Analyze feature readiness based on objective evidence from documentation, metrics, and reviews
- Distinguish between UAT and Production deployment criteria
- Provide transparent, evidence-based GO/NO_GO/GO_WITH_RISKS recommendations
- Identify blockers (must-fix issues) vs risks (concerns but not blocking)

---

## System Architecture

### Architecture Pattern: Supervisor with Tool-Based Delegation

The system uses a **simplified supervisor pattern** where:

1. A **coordinator agent** acts as the decision-making supervisor
2. **Specialist agents** are invoked as tools by the coordinator (not through graph routing)
3. Specialists gather evidence and report facts objectively
4. The coordinator synthesizes all findings and makes the final decision

**Key Design Principle:** Separation of concerns - specialists gather facts without judgment; coordinator decides based on comprehensive evidence.

### Agent Hierarchy

```
┌──────────────────────────────────────────────────┐
│          COORDINATOR AGENT                       │
│          (Decision Supervisor)                   │
│  • Analyzes target environment                   │
│  • Delegates to specialists                      │
│  • Synthesizes evidence                          │
│  • Makes final GO/NO_GO decision                 │
└───────────────┬──────────────────────────────────┘
                │
                │ Invokes via consultation tools
                │
       ┌────────┴────────┬─────────────┬────────────────┐
       │                 │             │                │
       ▼                 ▼             ▼                ▼
┌──────────┐     ┌──────────┐   ┌──────────┐    ┌──────────┐
│   DOCS   │     │ METRICS  │   │ REVIEWS  │    │  LOOKUP  │
│SPECIALIST│     │SPECIALIST│   │SPECIALIST│    │ FEATURE  │
│          │     │          │   │          │    │  (Mini)  │
│• Assesses│     │• Reports │   │• Reports │    │• Finds   │
│  docs    │     │  metrics │   │  review  │    │  feature │
│  complete│     │• Test    │   │  status  │    │• Extracts│
│  ness    │     │  coverage│   │• Approval│    │  metadata│
│• Reports │     │• Vulns   │   │  status  │    │          │
│  gaps    │     │• Perf    │   │• Feedback│    │          │
└──────────┘     └──────────┘   └──────────┘    └──────────┘
```

---

## The Five Agents

### Agent 1: Feature Lookup Agent (Mini-Agent)

**Role:** Feature identification and metadata extraction

**Responsibilities:**

- Parse natural language queries to identify which feature the user is asking about
- Match fuzzy references (feature names, JIRA keys, feature IDs) to actual features
- Extract comprehensive feature metadata from JIRA issue data
- Determine target environment (UAT or Production) from query context
- Handle errors gracefully when features cannot be found

**Input:** User query like "Is PLAT-1523 ready for production?" or "Check if maintenance scheduling feature is ready for UAT"

**Output:**

- Feature ID (internal identifier)
- JIRA key (e.g., "PLAT-1523")
- Current stage (e.g., "Development", "UAT")
- Target environment ("UAT" or "Production")
- Feature summary and metadata

**Tools Available:**

- `list_all_features()` - Lists all available features with basic metadata
  - Scans the `data/incoming/` directory to find all feature folders
  - Reads `data/incoming/feature*/jira/feature_issue.json` for each feature
  - Returns list of features with: feature_id, JIRA key, summary, current stage

- `get_feature_metadata(feature_identifier)` - Retrieves full JIRA metadata for a specific feature
  - Accepts JIRA key (e.g., "PLAT-1523"), feature ID (e.g., "feature1"), or feature name
  - Reads `data/incoming/<feature_id>/jira/feature_issue.json`
  - Returns complete JIRA issue data including status, assignee, priority, dependencies

**Data Sources:**

- JIRA issue metadata stored at: `data/incoming/<feature_id>/jira/feature_issue.json`

---

### Agent 2: Coordinator Agent (Supervisor)

**Role:** Decision-making supervisor that orchestrates the entire assessment process

**Responsibilities:**

- Analyze the target environment (UAT vs Production) to determine which criteria apply
- Delegate evidence gathering to specialist agents
- Synthesize all specialist findings into a coherent assessment
- Map gathered evidence against deployment criteria
- Distinguish between blockers (required criteria not met) and risks (concerns but not blocking)
- Make final deployment recommendation: GO / NO_GO / GO_WITH_RISKS
- Provide transparent explanations with evidence citations

**Decision Framework:**

**NO_GO Decision:** Required criteria are NOT met

- Example: UAT not completed (for Production deployment)
- Example: Critical security vulnerabilities present
- Example: Integration tests failing

**GO_WITH_RISKS Decision:** All required criteria met, but notable risks exist

- Example: Medium-severity vulnerabilities accepted by security team
- Example: Some optional documentation incomplete
- Example: Performance near but above SLA threshold

**GO Decision:** All required criteria met with minimal risks

- Clean metrics across all dimensions
- Complete documentation
- All approvals obtained

**Tools Available:**

- `consult_docs_agent(query, feature_id)` - Delegates to Documentation Specialist
- `consult_metrics_agent(query, feature_id)` - Delegates to Metrics Specialist
- `consult_reviews_agent(query, feature_id)` - Delegates to Reviews Specialist

**Consultation Pattern:**
The coordinator can invoke specialists multiple times with different queries as needed. For example:

- "Assess overall documentation completeness"
- "Check if deployment plan is complete"
- "Verify all required reviews are approved"

**Access to Decision Framework:**
The coordinator has access to a decision framework document that defines specific criteria for UAT and Production deployments. This ensures consistent, objective decision-making.

---

### Agent 3: Documentation Specialist Agent

**Role:** Objective assessment of planning documentation completeness and quality

**Responsibilities:**

- Read and analyze planning documents
- Evaluate document completeness objectively against expected structure
- Report facts about what content is present vs missing
- Identify gaps, placeholder text, or incomplete sections
- **Does NOT make deployment decisions** - only reports findings

**Document Types Assessed:**

- USER_STORY - Feature requirements and acceptance criteria
- DESIGN_DOC - Technical design and approach
- ARCHITECTURE - System architecture and component design
- DEPLOYMENT_PLAN - Step-by-step deployment instructions
- API_SPECIFICATION - API contracts and endpoints
- DATABASE_SCHEMA - Database structure and migrations

**Assessment Approach:**
For each document, the agent reports:

- Overall completeness (e.g., "Complete", "Mostly Complete", "Partial", "Missing")
- Specific sections present vs missing
- Quality indicators (e.g., presence of placeholders, level of detail)
- Factual observations without subjective judgment

**Example Output:**

```
USER_STORY: Complete (5/5 sections)
- Feature Overview: Present and detailed
- User Stories: 6 stories with acceptance criteria
- Technical Requirements: Fully specified
- Dependencies: Listed (FEAT-NS-001)
- Success Metrics: Defined

DESIGN_DOC: Mostly Complete (4/5 sections)
- Overview: Present
- Architecture: Detailed diagrams and explanations
- Security: Complete
- Monitoring: Missing
- Deployment: Present

DEPLOYMENT_PLAN: Missing
```

**Tools Available:**

- `list_planning_docs(feature_id)` - Lists all available planning documents for a feature
  - Scans `data/incoming/<feature_id>/planning/` directory
  - Returns list of available .md files (USER_STORY.md, DESIGN_DOC.md, etc.)

- `read_planning_doc(feature_id, doc_name)` - Reads a specific planning document
  - Reads from `data/incoming/<feature_id>/planning/<doc_name>.md`
  - Returns full markdown content of the specified document
  - Handles missing files gracefully with error messages

**Data Sources:**

- Planning documents stored at: `data/incoming/<feature_id>/planning/*.md`
- Expected documents: USER_STORY.md, DESIGN_DOC.md, ARCHITECTURE.md, DEPLOYMENT_PLAN.md, API_SPECIFICATION.md, DATABASE_SCHEMA.md

**Key Design Principle:**
This agent reports **objective facts only**. It does not interpret whether missing documentation is a blocker - that judgment is made by the coordinator based on the target environment's criteria.

---

### Agent 4: Metrics Specialist Agent

**Role:** Objective reporting of quantitative data about software features

**Responsibilities:**

- Fetch and report test coverage metrics (line, function, branch coverage)
- Retrieve test results (pass/fail counts, names of failed tests)
- Report security vulnerability scan results (by severity level)
- Provide performance benchmark data (response times, SLA compliance)
- Report CI/CD pipeline execution results
- Present raw numbers objectively without interpretation

**Metrics Categories:**

**Test Coverage:**

- Overall coverage percentage
- Coverage by type: lines, functions, branches
- Comparison to threshold (e.g., 80% required)
- Pass/fail status

**Test Results:**

- Total tests, passed, failed
- Names and details of failed tests
- Test suite execution time
- Flaky test indicators

**Security Scans:**

- Vulnerability counts by severity (Critical, High, Medium, Low)
- CVE details for each vulnerability
- CVSS scores
- Security tool recommendations

**Performance Benchmarks:**

- Response times (p50, p95, p99)
- SLA threshold compliance
- Resource utilization metrics
- Performance test pass/fail status

**CI/CD Pipeline:**

- Build status (success/failure)
- Pipeline stage results
- Artifact generation status
- Deployment readiness

**Tools Available:**

- `get_test_coverage(feature_id)` - Returns test coverage report
  - Reads from `data/incoming/<feature_id>/metrics/test_coverage_report.json`
  - Returns JSON string with coverage percentages and thresholds

- `get_test_results(feature_id)` - Returns unit test execution results
  - Reads from `data/incoming/<feature_id>/metrics/unit_test_results.json`
  - Returns JSON string with test counts, pass/fail status, and failed test names

- `get_security_scan(feature_id)` - Returns security vulnerability scan
  - Reads from `data/incoming/<feature_id>/metrics/security_scan_results.json`
  - Returns JSON string with vulnerabilities by severity, CVE details, and recommendations

- `get_performance_metrics(feature_id)` - Returns performance benchmarks
  - Reads from `data/incoming/<feature_id>/metrics/performance_benchmarks.json`
  - Returns JSON string with response times, SLA thresholds, and pass/fail status

- `get_pipeline_results(feature_id)` - Returns CI/CD pipeline status
  - Reads from `data/incoming/<feature_id>/metrics/pipeline_results.json`
  - Returns JSON string with build status, stage results, and artifacts

**Data Sources:**

- All metrics stored at: `data/incoming/<feature_id>/metrics/*.json`
- Files: test_coverage_report.json, unit_test_results.json, security_scan_results.json, performance_benchmarks.json, pipeline_results.json

**Reporting Style:**
The agent always cites which tool/metric provided each piece of data and highlights concerning patterns:

- "Test coverage is 87% (from test_coverage_report.json)"
- "3 critical vulnerabilities found (from security_scan_results.json)"
- "15 of 150 integration tests failed (from unit_test_results.json)"

**Key Design Principle:**
This agent provides **raw data and highlights issues** but does not judge severity or make recommendations. The coordinator decides if metrics meet deployment criteria.

---

### Agent 5: Reviews Specialist Agent

**Role:** Objective reporting of review and approval status

**Responsibilities:**

- Fetch review status for different review types
- Report approval status: approved, rejected, pending, missing
- Note key feedback and any blockers identified by reviewers
- List who performed and approved each review
- Report facts only - does not judge if reviews are sufficient

**Review Types:**

**Design Review:**

- Technical design approval
- Architecture review feedback
- Design decisions validation
- Reviewer: Engineering leads

**Security Review:**

- Security assessment results
- Threat model validation
- Security requirements verification
- Reviewer: Security team

**Stakeholder Review:**

- Business stakeholder approval
- Product owner sign-off
- Alignment with business objectives
- Reviewer: Product managers, business owners

**UAT (User Acceptance Testing) Review:**

- UAT test execution results
- Test case pass rates
- User acceptance sign-off
- Production readiness assessment
- Reviewer: QA team, business users

**Assessment Output for Each Review:**

- Overall status: APPROVED | REJECTED | PENDING | MISSING
- Reviewer name(s)
- Review date
- Key feedback points
- Blocking issues (if any)
- Recommendations

**Example Output:**

```
Design Review: APPROVED
- Reviewer: John Smith (Engineering Lead)
- Date: 2024-01-15
- Feedback: "Architecture is sound. Recommend adding monitoring for new endpoints."
- No blockers

Security Review: APPROVED
- Reviewer: Security Team
- Date: 2024-01-18
- Feedback: "2 medium vulnerabilities accepted as business risks"
- No blockers

UAT Review: APPROVED
- Pass Rate: 100% (88/88 test cases)
- Critical Issues: 0
- Production Ready: Yes
```

**Tools Available:**

- `get_review_status(feature_id, review_type)` - Returns review document for specified type
  - review_type options: "design", "security", "stakeholders", "uat"
  - Reads from `data/incoming/<feature_id>/reviews/<review_type>.{json,md}`
  - First tries .json format, falls back to .md format
  - Returns document content as string (JSON or Markdown)
  - Handles missing review files gracefully

**Data Sources:**

- Review documents stored at: `data/incoming/<feature_id>/reviews/*.{json,md}`
- Files: design.md, security.json, stakeholders.json, uat.json
- Supports both structured (JSON) and unstructured (Markdown) formats

**Key Design Principle:**
This agent reports **review status facts only**. It does not determine if approvals are sufficient for deployment - the coordinator makes that judgment based on deployment criteria.

---

## Workflow and Agent Interaction

### High-Level Execution Flow

```
1. USER QUERY
   "Is PLAT-1523 ready for production?"

2. FEATURE LOOKUP
   - Lookup agent analyzes query
   - Matches "PLAT-1523" to feature
   - Extracts metadata (current stage: UAT)
   - Determines target environment: Production
   - Returns: feature_id, feature_key, metadata

3. COORDINATOR TAKES OVER
   - Receives feature metadata and target environment
   - Analyzes: "This is a Production assessment"
   - Identifies required criteria for Production

4. EVIDENCE GATHERING (Specialist Consultation)

   a) Consult Documentation Specialist
      Query: "Assess documentation completeness for production deployment"
      Response: "USER_STORY: Complete, DESIGN_DOC: Complete,
                 ARCHITECTURE: Complete, DEPLOYMENT_PLAN: Complete..."

   b) Consult Metrics Specialist
      Query: "Check all metrics for production readiness"
      Response: "Test coverage: 87% (exceeds 80% threshold),
                 All tests passing (150/150),
                 Security: 0 critical/high vulnerabilities,
                 Performance: All benchmarks meet SLAs..."

   c) Consult Reviews Specialist
      Query: "Verify all required reviews are approved"
      Response: "Design review: APPROVED,
                 Security review: APPROVED,
                 UAT review: APPROVED (100% pass rate),
                 Stakeholder reviews: All APPROVED..."

5. EVIDENCE SYNTHESIS
   - Coordinator maps evidence to Production criteria
   - Checks each required criterion:
     - UAT completed and approved
     - e80% test coverage (87%)
     - All tests passing
     - No critical/high vulnerabilities
     - Security review approved
     - All documentation complete
     - Performance meets SLAs
     - All stakeholder approvals obtained

6. DECISION MAKING
   � All required criteria: MET
   � Identified risks: None significant
   � Decision: GO

7. FINAL OUTPUT
   "Decision: GO - PLAT-1523 is ready for production deployment.

   All required criteria are met:
   - UAT completed with 100% test pass rate
   - Test coverage at 87% (exceeds 80% threshold)
   - All 150 tests passing
   - Zero critical or high-severity vulnerabilities
   - All documentation complete (USER_STORY, DESIGN_DOC, ARCHITECTURE, DEPLOYMENT_PLAN)
   - All required reviews approved (Design, Security, UAT, Stakeholders)
   - Performance benchmarks meet all SLAs

   No blockers identified. No significant risks."
```

### Detailed Workflow Steps

#### Step 1: Feature Identification

**Agent:** Feature Lookup Agent

**Input:** User's natural language query

**Process:**

1. Parse query to extract feature reference
   - Could be JIRA key ("PLAT-1523")
   - Could be feature ID ("FEAT-MS-001")
   - Could be feature name ("maintenance scheduling")
2. Use `list_all_features()` to get all available features
3. Match query against feature metadata
4. Use `get_feature_metadata()` to retrieve full details
5. Determine target environment from query context
   - Keywords like "uat", "production", "prod"
   - Default to Production if ambiguous

**Output (added to shared state):**

- `feature_id`: Internal identifier
- `feature_key`: JIRA key
- `current_stage`: Current deployment stage
- `target_environment`: "UAT" or "Production"
- `error`: Error message if feature not found

**Error Handling:**
If feature cannot be found, the workflow stops here with an informative error message.

---

#### Step 2: Coordinator Analysis

**Agent:** Coordinator Agent

**Input:** Feature metadata and target environment from Step 1

**Process:**

1. **Analyze Target Environment**
   - If target is "UAT": Apply UAT criteria (lower bar)
   - If target is "Production": Apply Production criteria (higher bar)

2. **Identify Required Criteria**

   **UAT Criteria:**
   - e60% test coverage
   - Unit tests passing
   - No critical/high security vulnerabilities
   - USER_STORY mostly complete
   - DESIGN_DOC mostly complete
   - Design review approved

   **Production Criteria:**
   - UAT completed and approved (REQUIRED - blocker if missing)
   - e80% test coverage
   - All unit + integration tests passing
   - Zero critical/high vulnerabilities
   - Security review approved
   - DEPLOYMENT_PLAN complete
   - ARCHITECTURE complete
   - Performance benchmarks meet SLAs
   - All stakeholder approvals obtained

3. **Plan Specialist Consultations**
   - Determine which specialists to consult
   - Formulate queries for each specialist
   - Typically consults all three for comprehensive assessment

---

#### Step 3: Specialist Consultation (Evidence Gathering)

**Agent:** Coordinator (delegating to specialists)

**Consultation Mechanism:**
The coordinator invokes specialists using consultation tools. Each consultation:

1. Coordinator calls tool with query and feature_id
2. Tool internally invokes the specialist agent
3. Specialist uses its tools to gather data
4. Specialist returns findings as structured text
5. Coordinator receives specialist's response
6. Conversation history is updated with the exchange

**Order of Consultation:**
The coordinator decides the order based on the assessment strategy. Common pattern:

1. Documentation first (quick, establishes baseline)
2. Reviews second (approval status gates other checks)
3. Metrics last (most detailed technical assessment)

However, the coordinator can adapt this order or make multiple consultations to the same specialist with different queries.

**Example Documentation Consultation:**

```
Coordinator: consult_docs_agent(
    query="Assess all documentation for production deployment readiness",
    feature_id="feature1"
)

Documentation Specialist:
- Uses read_planning_doc() for each document type
- Analyzes completeness and quality
- Returns structured assessment

Response: "Documentation Assessment:
- USER_STORY: Complete (5/5 sections)
- DESIGN_DOC: Complete (5/5 sections)
- ARCHITECTURE: Complete with detailed diagrams
- DEPLOYMENT_PLAN: Complete with 5-step process
- API_SPECIFICATION: Complete
- DATABASE_SCHEMA: Complete with migration scripts

All required documentation is complete and comprehensive."
```

**Example Metrics Consultation:**

```
Coordinator: consult_metrics_agent(
    query="Check all metrics for production readiness criteria",
    feature_id="feature1"
)

Metrics Specialist:
- Uses get_test_coverage()
- Uses get_test_results()
- Uses get_security_scan()
- Uses get_performance_metrics()
- Uses get_pipeline_results()
- Aggregates and reports findings

Response: "Metrics Assessment:
- Test Coverage: 87% overall (exceeds 80% threshold)
  - Lines: 89%, Functions: 91%, Branches: 82%
- Test Results: 150/150 tests passing
  - Unit tests: 100/100 passing
  - Integration tests: 50/50 passing
- Security: 0 critical/high vulnerabilities
  - 2 medium vulnerabilities (accepted by security)
  - 5 low vulnerabilities (non-blocking)
- Performance: All benchmarks meet SLAs
  - API response p95: 180ms (threshold: 200ms)
  - Database query p95: 45ms (threshold: 50ms)
- CI/CD: Latest pipeline successful

All critical metrics meet production requirements."
```

**Example Reviews Consultation:**

```
Coordinator: consult_reviews_agent(
    query="Verify all required reviews for production deployment",
    feature_id="feature1"
)

Reviews Specialist:
- Uses get_review_status(feature_id, "design")
- Uses get_review_status(feature_id, "security")
- Uses get_review_status(feature_id, "stakeholders")
- Uses get_review_status(feature_id, "uat")
- Compiles approval status

Response: "Reviews Assessment:
- Design Review: APPROVED
  - Reviewer: John Smith (Engineering Lead)
  - No blocking issues
- Security Review: APPROVED
  - Reviewer: Security Team
  - 2 medium vulnerabilities accepted
- UAT Review: APPROVED
  - Test pass rate: 100% (88/88)
  - Production ready: Yes
- Stakeholder Reviews: All APPROVED
  - Product: APPROVED
  - Business: APPROVED
  - Operations: APPROVED

All required reviews are approved."
```

---

#### Step 4: Evidence Synthesis and Mapping

**Agent:** Coordinator Agent

**Input:** All specialist findings from Step 3

**Process:**

1. **Create Criteria Assessment Structure**
   For each criterion, create an assessment entry:

   ```
   {
       "criterion_name": {
           "status": "pass" | "fail",
           "value": <actual_value>,
           "threshold": <required_value>,
           "evidence": "Citation from specialist findings",
           "message": "Human-readable explanation"
       }
   }
   ```

2. **Map Evidence to Criteria**
   Extract relevant data from specialist responses and map to each criterion:

   - Test Coverage Criterion:
     - Extract: "87% overall" from Metrics Specialist
     - Threshold: 80%
     - Status: PASS
     - Evidence: "Test coverage at 87% (from metrics specialist)"

   - UAT Approval Criterion:
     - Extract: "APPROVED, 100% pass rate" from Reviews Specialist
     - Threshold: Approved
     - Status: PASS
     - Evidence: "UAT review approved with 100% pass rate (from reviews specialist)"

   - Documentation Criterion:
     - Extract: "DEPLOYMENT_PLAN: Complete" from Docs Specialist
     - Threshold: Complete
     - Status: PASS
     - Evidence: "Deployment plan complete with 5-step process (from docs specialist)"

3. **Classify Issues by Severity**

   **Blocker:** Required criterion NOT met
   - Prevents deployment
   - Must be fixed before proceeding
   - Example: "UAT not completed" for Production

   **Risk:** Concern but not blocking
   - Deployment can proceed with caution
   - Should be monitored or mitigated
   - Example: "2 medium vulnerabilities accepted by security"

4. **Generate Structured Assessment**

   ```
   {
       "criteria_met": 9,
       "criteria_total": 9,
       "blockers": [],
       "risks": ["2 medium vulnerabilities (accepted)"],
       "criteria_details": {
           "uat_approval": {"status": "pass", ...},
           "test_coverage": {"status": "pass", ...},
           ...
       }
   }
   ```

---

#### Step 5: Decision Making

**Agent:** Coordinator Agent

**Input:** Criteria assessment from Step 4

**Decision Logic:**

**1. Check for Blockers**

```
IF any criterion with status="fail" AND is_required=true:
    decision = "NO_GO"
    explanation = "Required criteria not met: [list blockers]"
    RETURN
```

**2. Check for Significant Risks**

```
IF no blockers BUT risks exist:
    IF risks are significant:
        decision = "GO_WITH_RISKS"
        explanation = "All required criteria met, but note these risks: [list risks]"
    ELSE:
        decision = "GO"
        explanation = "Minor risks noted but acceptable"
    RETURN
```

**3. Clear for Deployment**

```
IF no blockers AND no significant risks:
    decision = "GO"
    explanation = "All criteria met with no significant risks"
    RETURN
```

**What Makes a Risk "Significant"?**

- Medium-severity security vulnerabilities
- Performance metrics close to (but above) thresholds
- Optional documentation missing
- Non-critical test flakiness

**Examples of Decisions:**

**NO_GO Example:**

```
Decision: NO_GO

Blockers preventing deployment:
1. UAT review not completed (required for Production)
2. 2 critical security vulnerabilities unresolved
3. 15 integration tests failing

Required actions before deployment:
- Complete UAT testing and obtain approval
- Resolve critical vulnerabilities (CVE-2024-1234, CVE-2024-5678)
- Fix failing integration tests
```

**GO_WITH_RISKS Example:**

```
Decision: GO_WITH_RISKS

All required criteria are met, but note these risks:
1. 2 medium-severity vulnerabilities accepted by security team
2. API response time p95 at 180ms (close to 200ms threshold)
3. ARCHITECTURE.md missing monitoring section (optional but recommended)

Recommendations:
- Monitor API performance closely post-deployment
- Plan vulnerability remediation in next sprint
- Complete monitoring documentation
```

**GO Example:**

```
Decision: GO

All required criteria are met:
- UAT completed with 100% test pass rate
- Test coverage at 87% (exceeds 80% threshold)
- All 150 tests passing
- Zero critical or high-severity vulnerabilities
- All documentation complete
- All required reviews approved
- Performance benchmarks meet all SLAs

No blockers identified. No significant risks.
```

---

#### Step 6: Output Generation

**Agent:** Coordinator Agent

**Final Output Structure:**

**1. Decision Statement**

- Clear GO / NO_GO / GO_WITH_RISKS declaration
- Feature identifier and target environment

**2. Summary**

- High-level assessment result
- Key metrics overview

**3. Detailed Criteria Assessment**

- Each criterion: PASS or FAIL
- Actual values vs thresholds
- Evidence citations

**4. Blockers (if NO_GO)**

- List of all blocking issues
- Required actions to resolve

**5. Risks (if GO_WITH_RISKS)**

- List of identified risks
- Mitigation recommendations

**6. Evidence Trail**

- Citations from specialist findings
- Transparency into decision reasoning

---

## Data Model and State Management

### Shared State Schema

The system maintains a shared state object that flows through the workflow:

```csharp
public record FeatureReadinessState
{
    // Message History (conversation-style)
    public IReadOnlyList<Message> Messages { get; init; } = new List<Message>
    {
        new UserMessage("Is PLAT-1523 ready for production?"),
        new AssistantMessage("Found feature PLAT-1523..."),
        new AssistantMessage("Consulting documentation specialist..."),
        new AssistantMessage("Documentation assessment: ..."),
        // ...
    };

    // Feature Metadata (populated by lookup agent)
    public string? FeatureId { get; init; }              // e.g., "feature1"
    public string? FeatureKey { get; init; }             // e.g., "PLAT-1523"
    public string? CurrentStage { get; init; }           // e.g., "UAT", "Development"
    public TargetEnvironment? Target { get; init; }      // UAT or Production
    public string? Error { get; init; }                  // Error if lookup fails

    // Assessment Results (populated by coordinator)
    public DecisionType? Decision { get; init; }         // Go, NoGo, or GoWithRisks
    public CriteriaAssessment? Assessment { get; init; }
}

public record CriteriaAssessment
{
    public required int CriteriaMet { get; init; }
    public required int CriteriaTotal { get; init; }
    public IReadOnlyList<string> Blockers { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Risks { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, CriterionResult> CriteriaDetails { get; init; }
        = new Dictionary<string, CriterionResult>();
}

public record CriterionResult
{
    public required CriterionStatus Status { get; init; }  // Pass or Fail
    public required object Value { get; init; }
    public required object Threshold { get; init; }
    public required string Evidence { get; init; }
    public required string Message { get; init; }
}

public enum DecisionType { Go, NoGo, GoWithRisks }
public enum CriterionStatus { Pass, Fail }
public enum TargetEnvironment { UAT, Production }
```

### State Evolution Through Workflow

**Initial State (user query):**

```
{
    messages: [{role: "user", content: "Is PLAT-1523 ready for production?"}],
    feature_id: null,
    feature_key: null,
    current_stage: null,
    target_environment: null,
    error: null,
    decision: null,
    criteria_assessment: null
}
```

**After Feature Lookup:**

```
{
    messages: [
        {role: "user", content: "Is PLAT-1523 ready for production?"},
        {role: "assistant", content: "Found feature PLAT-1523 (FEAT-MS-001): Maintenance Scheduling & Alert System. Current stage: UAT. Assessing readiness for Production deployment."}
    ],
    feature_id: "feature1",
    feature_key: "PLAT-1523",
    current_stage: "UAT",
    target_environment: "Production",
    error: null,
    decision: null,
    criteria_assessment: null
}
```

**During Specialist Consultation:**

```
{
    messages: [
        {role: "user", content: "Is PLAT-1523 ready for production?"},
        {role: "assistant", content: "Found feature PLAT-1523..."},
        {role: "assistant", content: "Consulting documentation specialist..."},
        {role: "assistant", content: "Documentation Assessment: All required docs complete..."},
        {role: "assistant", content: "Consulting metrics specialist..."},
        {role: "assistant", content: "Metrics Assessment: Test coverage 87%, all tests passing..."},
        {role: "assistant", content: "Consulting reviews specialist..."},
        {role: "assistant", content: "Reviews Assessment: All approvals obtained..."}
    ],
    feature_id: "feature1",
    // ... other fields unchanged
}
```

**Final State (after decision):**

```
{
    messages: [
        // ... all previous messages ...
        {role: "assistant", content: "Decision: GO - Feature PLAT-1523 is ready for production. All required criteria met..."}
    ],
    feature_id: "feature1",
    feature_key: "PLAT-1523",
    current_stage: "UAT",
    target_environment: "Production",
    error: null,
    decision: "go",
    criteria_assessment: {
        criteria_met: 9,
        criteria_total: 9,
        blockers: [],
        risks: [],
        criteria_details: {
            "uat_approval": {
                status: "pass",
                value: "APPROVED (100% pass rate)",
                threshold: "APPROVED",
                evidence: "UAT review approved with 100% test pass rate",
                message: "UAT completed successfully"
            },
            "test_coverage": {
                status: "pass",
                value: 87,
                threshold: 80,
                evidence: "Test coverage at 87% from metrics specialist",
                message: "Test coverage exceeds Production threshold"
            },
            // ... other criteria ...
        }
    }
}
```

### Message-Based Communication

**Key Design Choice:** The system uses a **message-based state** where all agent interactions are captured as messages in a conversation history.

**Advantages:**

- Full transparency - every agent interaction is visible
- Context preservation - agents can reference previous findings
- User-friendly output - messages can be displayed as a conversation
- Debugging - full trace of decision-making process

**Message Types:**

- **User messages:** Original queries from users
- **Assistant messages:** Agent responses, findings, and decisions
- **Tool calls:** (Optional) Explicit tool invocations can be logged as messages

---

## Tools and Capabilities

### Tool Categories

**1. Feature Discovery Tools**
Used by: Feature Lookup Agent

- `list_all_features()`
  - Returns: List of all features with basic metadata
  - Data: Feature ID, JIRA key, summary, current stage
  - Use: Initial feature search and matching

- `get_feature_metadata(feature_identifier: string)`
  - Input: JIRA key, Feature ID, or feature name
  - Returns: Complete JIRA issue metadata
  - Data: Status, assignee, priority, dependencies, custom fields
  - Use: Detailed feature information extraction

**2. Documentation Tools**
Used by: Documentation Specialist Agent

- `list_planning_docs(feature_id: string)`
  - Returns: List of available planning documents
  - Data: Document names and types
  - Use: Discover what documentation exists

- `read_planning_doc(feature_id: string, doc_name: string)`
  - Input: Document name (USER_STORY, DESIGN_DOC, etc.)
  - Returns: Full document content (markdown)
  - Use: Read and assess specific documents

**3. Metrics Tools**
Used by: Metrics Specialist Agent

- `get_test_coverage(feature_id: string)`
  - Returns: Test coverage report (JSON)
  - Data: Overall %, line/function/branch coverage, threshold
  - Use: Assess test coverage metrics

- `get_test_results(feature_id: string)`
  - Returns: Test execution results (JSON)
  - Data: Total tests, passed, failed, failed test names
  - Use: Verify all tests passing

- `get_security_scan(feature_id: string)`
  - Returns: Security scan results (JSON)
  - Data: Vulnerabilities by severity, CVE details, recommendations
  - Use: Identify security issues

- `get_performance_metrics(feature_id: string)`
  - Returns: Performance benchmarks (JSON)
  - Data: Response times, SLA thresholds, resource usage
  - Use: Verify performance meets requirements

- `get_pipeline_results(feature_id: string)`
  - Returns: CI/CD pipeline status (JSON)
  - Data: Build status, stage results, artifacts
  - Use: Verify successful builds and deployments

**4. Review Tools**
Used by: Reviews Specialist Agent

- `get_review_status(feature_id: string, review_type: string)`
  - Input: review_type = "design" | "security" | "stakeholders" | "uat"
  - Returns: Review document (JSON or Markdown)
  - Data: Approval status, reviewer, date, feedback, blockers
  - Use: Verify review approvals

**5. Consultation Tools**
Used by: Coordinator Agent

- `consult_docs_agent(query: string, feature_id: string)`
  - Invokes: Documentation Specialist Agent
  - Returns: Documentation assessment findings (string)
  - Use: Delegate documentation assessment

- `consult_metrics_agent(query: string, feature_id: string)`
  - Invokes: Metrics Specialist Agent
  - Returns: Metrics assessment findings (string)
  - Use: Delegate metrics assessment

- `consult_reviews_agent(query: string, feature_id: string)`
  - Invokes: Reviews Specialist Agent
  - Returns: Reviews assessment findings (string)
  - Use: Delegate review status assessment

### Tool Implementation Pattern

All tools follow a consistent pattern:

**1. Validation**

- Validate feature_id exists in `data/incoming/`
- Validate required parameters present
- Check that feature directory structure is valid

**2. Data Retrieval**

- Read from appropriate file in `data/incoming/<feature_folder>/`
- Parse JSON or Markdown as appropriate
- Handle missing files gracefully

**3. Error Handling**

- Return structured errors when files missing
- Provide helpful messages indicating which file is missing
- Don't crash on malformed data - return parse errors

**4. Response Format**

- Consistent structure across all tools
- Include metadata (source file path)
- Return data as strings (JSON strings for metrics tools)

---

## Data Sources and External Systems

### File System Structure

The system reads from a hierarchical directory structure:

```
data/incoming/
├── feature1/
│   ├── jira/
│   │   └── feature_issue.json          # JIRA metadata
│   ├── planning/
│   │   ├── USER_STORY.md
│   │   ├── DESIGN_DOC.md
│   │   ├── ARCHITECTURE.md
│   │   ├── DEPLOYMENT_PLAN.md
│   │   ├── API_SPECIFICATION.md
│   │   └── DATABASE_SCHEMA.md
│   ├── metrics/
│   │   ├── test_coverage_report.json
│   │   ├── unit_test_results.json
│   │   ├── security_scan_results.json
│   │   ├── performance_benchmarks.json
│   │   └── pipeline_results.json
│   ├── reviews/
│   │   ├── design.md
│   │   ├── security.json
│   │   ├── stakeholders.json
│   │   └── uat.json
├── feature2/
├── feature3/
└── ...
```

### Data Format Specifications

**JIRA Metadata (feature_issue.json):**

```json
{
  "key": "PLAT-1523",
  "fields": {
    "summary": "Feature name and description",
    "status": {"name": "UAT"},
    "customfield_10001": "FEAT-MS-001",
    "priority": {"name": "High"},
    "assignee": {"displayName": "Developer Name"},
    "project": {"key": "PLAT", "name": "Platform"},
    "issuetype": {"name": "Story"}
  }
}
```

**Test Coverage Report (test_coverage_report.json):**

```json
{
  "overall_coverage": 87,
  "coverage_by_type": {
    "lines": 89,
    "functions": 91,
    "branches": 82
  },
  "threshold": 80,
  "passed": true,
  "generated_at": "2024-01-20T10:30:00Z"
}
```

**Test Results (unit_test_results.json):**

```json
{
  "total_tests": 150,
  "tests_passed": 150,
  "tests_failed": 0,
  "pass_rate": 100.0,
  "failed_tests": [],
  "execution_time_seconds": 45,
  "test_suites": [
    {
      "suite_name": "unit_tests",
      "total": 100,
      "passed": 100,
      "failed": 0
    },
    {
      "suite_name": "integration_tests",
      "total": 50,
      "passed": 50,
      "failed": 0
    }
  ]
}
```

**Security Scan (security_scan_results.json):**

```json
{
  "scan_date": "2024-01-18T14:00:00Z",
  "scanner": "Snyk",
  "vulnerabilities_by_severity": {
    "critical": 0,
    "high": 0,
    "medium": 2,
    "low": 5
  },
  "vulnerabilities": [
    {
      "severity": "medium",
      "cve": "CVE-2024-1234",
      "title": "Regular Expression Denial of Service",
      "cvss_score": 5.3,
      "package": "lodash@4.17.15",
      "recommendation": "Upgrade to lodash@4.17.21"
    }
  ]
}
```

**Performance Benchmarks (performance_benchmarks.json):**

```json
{
  "benchmark_date": "2024-01-19T16:00:00Z",
  "metrics": {
    "api_response_time_ms": {
      "p50": 85,
      "p95": 180,
      "p99": 250
    },
    "database_query_time_ms": {
      "p50": 12,
      "p95": 45,
      "p99": 78
    }
  },
  "sla_thresholds": {
    "api_response_p95": 200,
    "database_query_p95": 50
  },
  "all_benchmarks_passed": true
}
```

**UAT Review (uat.json):**

```json
{
  "overall_status": "APPROVED",
  "test_coverage": {
    "total_test_cases": 88,
    "test_cases_passed": 88,
    "test_cases_failed": 0,
    "pass_rate": 100.0
  },
  "critical_issues_found": 0,
  "major_issues_found": 0,
  "minor_issues_found": 2,
  "production_ready": true,
  "sign_off": {
    "approver": "QA Lead",
    "date": "2024-01-22",
    "comments": "All test cases passed. Minor UI polish items noted but non-blocking."
  }
}
```

### No Live External APIs

**Important:** This system is designed to operate on **pre-fetched, locally-stored data** in the `data/incoming/` directory structure.

**Why?**

- **Performance:** No network latency or API rate limits
- **Reliability:** No dependency on external service availability
- **Reproducibility:** Consistent results for testing and automation
- **Offline capability:** Can run without internet access
- **Simplicity:** Focus on assessment logic, not data fetching

**Data Preparation:**
All required data must be present in the `data/incoming/` directory before assessment:

1. Feature metadata from JIRA in `<feature_id>/jira/feature_issue.json`
2. Planning documents in `<feature_id>/planning/*.md`
3. Metrics data in `<feature_id>/metrics/*.json`
4. Review documents in `<feature_id>/reviews/*.{json,md}`

The assessment system only reads from these files. A separate data ingestion process (not part of this design) would be responsible for populating this directory structure from live systems.

---

## Deployment Criteria

### UAT Deployment Criteria

**Purpose:** Determine if a feature is ready to move from Development to UAT testing.

**Required Criteria (Blockers if not met):**

1. **Test Coverage e 60%**
   - Minimum code coverage threshold
   - Ensures basic testing is in place

2. **Unit Tests Passing**
   - All unit tests must pass
   - No failing tests allowed

3. **No Critical/High Security Vulnerabilities**
   - Zero critical severity vulnerabilities
   - Zero high severity vulnerabilities
   - Medium/low vulnerabilities acceptable with acknowledgment

4. **USER_STORY Mostly Complete**
   - Core sections must be present
   - Acceptance criteria defined
   - Partial completion acceptable

5. **DESIGN_DOC Mostly Complete**
   - Technical approach documented
   - Major components described
   - Partial completion acceptable

6. **Design Review Approved**
   - Engineering lead sign-off
   - No blocking technical concerns

**Risks (Not blockers):**

- Medium-severity security vulnerabilities
- Other documentation incomplete (ARCHITECTURE, DEPLOYMENT_PLAN)
- Test coverage between 60-80%
- Minor test flakiness

**Decision Logic:**

- **NO_GO:** Any required criterion not met
- **GO_WITH_RISKS:** All required met, but risks exist
- **GO:** All required met, minimal risks

---

### Production Deployment Criteria

**Purpose:** Determine if a feature is ready to deploy to Production.

**Required Criteria (Blockers if not met):**

1. **UAT Completed and Approved** (HIGHEST PRIORITY)
   - UAT testing fully completed
   - All UAT test cases passed or exceptions documented
   - QA team sign-off obtained
   - **This is the most critical blocker - no Production without UAT approval**

2. **Test Coverage e 80%**
   - Higher coverage threshold for Production
   - Must exceed threshold, not just meet it

3. **All Tests Passing**
   - Unit tests: 100% passing
   - Integration tests: 100% passing
   - No test failures tolerated

4. **Zero Critical/High Security Vulnerabilities**
   - Critical vulnerabilities: 0
   - High vulnerabilities: 0
   - Medium vulnerabilities must be reviewed and accepted by security team

5. **Security Review Approved**
   - Security team sign-off
   - Threat model reviewed
   - Security requirements verified

6. **DEPLOYMENT_PLAN Complete**
   - Step-by-step deployment instructions
   - Rollback procedures documented
   - Monitoring and alerting defined

7. **ARCHITECTURE Complete**
   - System architecture fully documented
   - Component interactions clear
   - Technical debt acknowledged

8. **Performance Benchmarks Meet SLAs**
   - All performance metrics meet thresholds
   - Response time targets met
   - Resource usage acceptable

9. **All Stakeholder Approvals Obtained**
   - Product owner approval
   - Business stakeholder approval
   - Operations team approval

**Risks (Not blockers):**

- Medium-severity vulnerabilities (if security-accepted)
- Performance metrics close to (but above) thresholds
- Optional documentation gaps
- Non-critical monitoring gaps

**Decision Logic:**

- **NO_GO:** Any required criterion not met (especially UAT)
- **GO_WITH_RISKS:** All required met, but notable risks exist
- **GO:** All required met, minimal risks

**Key Difference from UAT:**
Production criteria are significantly more stringent:

- Higher test coverage (80% vs 60%)
- All test types required (not just unit)
- More documentation required (DEPLOYMENT_PLAN, ARCHITECTURE)
- More approvals required (security, stakeholders)
- Performance requirements enforced
- **UAT approval is mandatory**

---

## Implementation Guidance

### Graph Structure

**Node Sequence:**

```
START -> lookup_feature -> coordinator -> END
```

**Simple Linear Flow:**

- No conditional routing required
- No complex branching logic
- All decisions made within agents, not graph structure

> **If using LangGraph:** This is a simple StateGraph with three nodes and two edges. The coordinator can be a prebuilt ReAct agent with tools, or a custom agent node with tool-calling.

### Agent Implementation Approaches

**Approach 1: LLM with Tool Calling**

- Use a foundation model with tool-calling capabilities
- **Implementation**: Ollama with Qwen2.5 (localhost:11434 in Docker)
- Provide tools to the agent
- Let the model decide when and how to use tools
- Best for: Coordinator, Lookup Agent

**Approach 2: Hardcoded Logic with LLM Enhancement**

- Use programmatic logic for tool orchestration
- Use LLM only for specific tasks (e.g., query understanding, synthesis)
- More predictable and debuggable
- Best for: Specialist agents

**Approach 3: Hybrid**

- Combine programmatic routing with LLM decision-making
- Example: Specialist uses LLM to understand query, then programmatically calls tools
- Balances flexibility and control

### Specialist Agent Invocation Patterns

**Pattern 1: Tool-Based Invocation (Simpler)**

- Coordinator has tools that invoke specialist agents
- Specialists are separate agent systems
- Tools handle invocation and response extraction
- **Advantage:** Simpler graph structure, cleaner separation
- **Trade-off:** Less visible in tracing tools

**Pattern 2: Graph Routing (More Visible)**

- Specialists are nodes in the main graph
- Coordinator routes to specialist nodes via commands
- Specialists return to coordinator via routing
- **Advantage:** More visible in tracing, can save state between specialists
- **Trade-off:** More complex graph structure

Both patterns are valid - choose based on framework capabilities and preferences.

### State Management

**Key Principles:**

1. **Immutable Updates:** Always return new state, never mutate existing
2. **Incremental Updates:** Agents update only their relevant fields
3. **Message Accumulation:** Messages append, never replace
4. **Error Propagation:** Errors in state stop workflow

**State Update Patterns:**

**Lookup Agent Updates:**

```csharp
return new FeatureReadinessState
{
    FeatureId = "feature1",
    FeatureKey = "PLAT-1523",
    CurrentStage = "UAT",
    TargetEnvironment = "Production",
    Messages = messages.Append(new AssistantMessage("Found feature...")).ToList()
};
```

**Coordinator Updates:**

```csharp
return state with
{
    Decision = DecisionType.Go,
    CriteriaAssessment = new CriteriaAssessment { ... },
    Messages = state.Messages.Append(new AssistantMessage("Decision: GO...")).ToList()
};
```

### Tool Design Best Practices

**1. Single Responsibility**

- Each tool does one thing well
- Easy to test and debug
- Clear documentation

**2. Consistent Error Handling**

- All tools return structured errors
- Include helpful context
- Don't raise exceptions unless critical

**3. Input Validation**

- Validate all parameters
- Provide clear error messages
- Handle edge cases gracefully

**4. Idempotency**

- Tools should be safe to call multiple times
- No side effects
- Deterministic results for same inputs

**5. Documentation**

- Clear description of what the tool does
- Parameter types and constraints
- Example usage
- Expected return format
- Which file(s) in `data/incoming/` the tool reads from

---

## Observability and Monitoring

### Tracing

**Requirements:**

- Trace every agent invocation
- Capture all tool calls
- Record timing information
- Link related operations

**Trace Structure:**

```
RootTrace: Feature Readiness Assessment
├─ Span: lookup_feature
│  ├─ Span: list_all_features (tool)
│  └─ Span: get_feature_metadata (tool)
└─ Span: coordinator
   ├─ Span: consult_docs_agent (tool)
   │  └─ Span: docs_agent_invocation
   │     ├─ Span: read_planning_doc (tool)
   │     ├─ Span: read_planning_doc (tool)
   │     └─ Span: read_planning_doc (tool)
   ├─ Span: consult_metrics_agent (tool)
   │  └─ Span: metrics_agent_invocation
   │     ├─ Span: get_test_coverage (tool)
   │     ├─ Span: get_test_results (tool)
   │     └─ Span: get_security_scan (tool)
   └─ Span: consult_reviews_agent (tool)
      └─ Span: reviews_agent_invocation
         └─ Span: get_review_status (tool)
```

**Trace Attributes:**

- feature_id
- feature_key
- target_environment
- agent_name
- tool_name
- decision (for coordinator span)
- error_status

### Logging

**Log Levels:**

- INFO: Normal operations (agent invocations, decisions)
- WARN: Unexpected but handled situations (missing data)
- ERROR: Failures requiring attention

**Key Log Events:**

- Feature lookup started/completed
- Specialist consultation started/completed
- Tool invocation started/completed
- Decision made
- Errors encountered

**Log Format:**

```json
{
  "timestamp": "2024-01-20T10:30:00Z",
  "level": "INFO",
  "agent": "coordinator",
  "event": "decision_made",
  "feature_id": "feature1",
  "feature_key": "PLAT-1523",
  "target_environment": "Production",
  "decision": "go",
  "duration_ms": 2340
}
```

### Metrics

**Key Metrics to Track:**

- Assessment duration (p50, p95, p99)
- Decision distribution (GO vs NO_GO vs GO_WITH_RISKS)
- Tool invocation counts
- Error rates
- Specialist consultation frequency

**Use for:**

- Performance optimization
- Identifying bottlenecks
- Understanding usage patterns
- Capacity planning

---

## Error Handling and Edge Cases

### Feature Not Found

**Scenario:** User queries a non-existent feature

**Handling:**

1. Lookup agent searches all features
2. No match found
3. Return error state with helpful message
4. Workflow stops at lookup node

**Example:**

```
User: "Is feature XYZ ready for production?"
System: "Error: Feature 'XYZ' not found. Available features: [list]"
```

### Missing Data

**Scenario:** Required data file doesn't exist in `data/incoming/<feature_id>/`

**Handling:**

1. Tool detects missing file when trying to read from filesystem
2. Return structured error indicating which file is missing
3. Specialist reports missing data to coordinator
4. Coordinator treats as criterion failure

**Example:**

```
Metrics Specialist: "Test coverage data not found at
data/incoming/feature1/metrics/test_coverage_report.json
Cannot assess test coverage criterion."

Coordinator Decision: "NO_GO - Unable to verify test coverage (data missing)"
```

### Malformed Data

**Scenario:** Data file exists but is corrupted or invalid

**Handling:**

1. Tool attempts to parse data
2. Parse failure caught
3. Return error with details
4. Specialist reports issue
5. Coordinator treats as criterion failure

### Partial Data

**Scenario:** Some but not all required data is available

**Handling:**

1. Specialists report what they can assess
2. Clearly indicate what's missing
3. Coordinator decides if missing data is blocking

**Example:**

```
Docs Specialist: "USER_STORY: Complete. DESIGN_DOC: Missing.
ARCHITECTURE: Complete."

Coordinator: "For Production, DESIGN_DOC is recommended but not required.
Not a blocker."
```

### Conflicting Information

**Scenario:** Different sources provide conflicting data

**Handling:**

1. Specialists report what they observe
2. Note the conflict explicitly
3. Coordinator escalates for human review

**Example:**

```
Reviews Specialist: "Security review status: APPROVED in security.json,
but stakeholders.json indicates security concerns unresolved."

Coordinator: "NO_GO - Conflicting information requires human review."
```

### LLM Hallucination

**Scenario:** Agent invents data or makes unsupported claims

**Mitigation Strategies:**

1. **Specialist agents cite sources:** Always reference which tool provided data
2. **Coordinator requires evidence:** Cannot make decision without specialist findings
3. **Structured outputs:** Use schema validation for agent responses
4. **Verification:** Cross-reference multiple sources
5. **Logging:** Full conversation history for audit

**Example:**

```
BAD: "Test coverage is 90%"
GOOD: "Test coverage is 87% (from data/incoming/feature1/metrics/test_coverage_report.json)"
```

### Timeout Handling

**Scenario:** Agent takes too long to respond

**Handling:**

1. Set reasonable timeouts for each agent
2. Cancel operation after timeout
3. Return partial results if possible
4. Log timeout event
5. Allow retry

---

## Testing Strategy

### Unit Testing

**Test Each Agent Independently:**

**Lookup Agent Tests:**

- Match JIRA key to feature
- Match feature name to feature
- Handle fuzzy matching
- Handle not found
- Extract target environment from query

**Specialist Agent Tests:**

- Tool invocation and response parsing
- Missing data handling
- Malformed data handling
- Assessment logic
- Citation generation

**Coordinator Tests:**

- Criterion mapping
- Decision logic (GO/NO_GO/GO_WITH_RISKS)
- Blocker vs risk classification
- Evidence synthesis

**Tool Tests:**

- Data retrieval from `data/incoming/`
- Missing file handling
- Malformed JSON handling
- Response formatting

### Integration Testing

**Test Agent Interactions:**

**Lookup � Coordinator:**

- Verify state is properly passed
- Verify feature metadata is accessible

**Coordinator � Specialists:**

- Verify consultation invocation
- Verify responses are processed correctly
- Verify multiple consultations work

**End-to-End:**

- Full workflow from query to decision
- Various feature scenarios (ready, not ready, partial data)
- Different environments (UAT, Production)

### Scenario Testing

**Test Real-World Scenarios:**

**Scenario 1: Feature Ready for Production**

- All criteria met
- Expected: GO decision

**Scenario 2: Feature Not Ready (Missing UAT)**

- UAT not completed
- Expected: NO_GO decision with UAT blocker

**Scenario 3: Feature Ready with Risks**

- All required met
- Medium vulnerabilities present
- Expected: GO_WITH_RISKS decision

**Scenario 4: UAT Assessment**

- Lower criteria than Production
- Expected: Different decision than Production assessment

**Scenario 5: Missing Data**

- Some metrics files missing from `data/incoming/<feature_id>/metrics/`
- Expected: NO_GO with clear error message indicating which files are missing

### Performance Testing

**Measure:**

- End-to-end assessment duration
- Individual agent duration
- Tool invocation latency
- Token usage (for LLM-based agents)

**Targets:**

- Full assessment: < 10 seconds
- Specialist consultation: < 3 seconds each
- Tool invocation: < 500ms

---

## Configuration and Customization

### Deployment Criteria Configuration

**Criteria should be configurable** to support different organizations and practices.

**Configuration Format:**

```yaml
criteria:
  uat:
    required:
      - name: test_coverage
        threshold: 60
        operator: ">="
      - name: unit_tests
        status: passing
      - name: vulnerabilities_critical
        threshold: 0
        operator: "=="
      # ... more criteria
    recommended:
      - name: architecture_doc
        completeness: partial_ok

  production:
    required:
      - name: uat_approval
        status: approved
        priority: highest  # Block immediately if not met
      - name: test_coverage
        threshold: 80
        operator: ">="
      # ... more criteria
```

**Benefits:**

- Easy to adjust thresholds
- Add new criteria without code changes
- Different criteria for different projects
- Version control for criteria changes

### Agent Customization

**System Prompts:**
Each agent's behavior can be tuned via system prompts:

**Coordinator Prompt:**

- Decision-making style (conservative vs aggressive)
- Risk tolerance
- Explanation detail level

**Specialist Prompts:**

- Assessment thoroughness
- Reporting verbosity
- Citation style

**Tool Selection:**

- Enable/disable specific tools
- Add custom tools for organization-specific data
- Remove tools for simpler assessments

## Glossary

**Agent:** An AI system with specific capabilities and responsibilities that can use tools and make decisions

**Specialist Agent:** An agent focused on a specific domain (docs, metrics, reviews) that gathers and reports facts

**Coordinator Agent:** The supervisor agent that delegates to specialists and makes final decisions

**Tool:** A function that an agent can invoke to interact with data or other systems

**Consultation:** The process of the coordinator invoking a specialist agent to gather information

**Blocker:** A required criterion that is not met, preventing deployment

**Risk:** A concern or gap that doesn't prevent deployment but should be noted

**State:** The shared data structure that flows through the workflow, containing all context and results

**Message:** A unit of communication between agents or with users, stored in conversation history

**Criterion:** A specific requirement that must be assessed (e.g., "test coverage e 80%")

**Decision:** The final recommendation: GO, NO_GO, or GO_WITH_RISKS

**Evidence:** Data from specialists that supports the decision (with citations)

**Target Environment:** The deployment destination being assessed (UAT or Production)
