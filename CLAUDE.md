# CLAUDE.md

**🔴 MANDATORY SESSION INITIALIZATION**

**Every new chat session MUST start with these steps:**
1. Open [WORKFLOW_STATUS.md](WORKFLOW_STATUS.md)
2. Read the "Active Work Item" section (session checkpoint)
3. State the current state to the user
4. Validate it matches the work item file
5. Wait for user direction before proceeding

**The "Active Work Item" section is your checkpoint.** If it looks wrong, STOP and ask the user before proceeding.

---

## Repository Purpose

This is a **design and planning repository** for an AI-powered Feature Readiness Assessment System. The system uses multiple AI agents to determine if software features are ready to move from development → UAT → production.

**Current Status:** This repository contains comprehensive design documentation and sample data. **No implementation code exists yet** - this is the planning/design phase.

## System Overview

The Feature Readiness Assessment System automates deployment decisions using a 5-agent architecture:

1. **Feature Lookup Agent** - Identifies features from natural language queries
2. **Coordinator Agent** (Supervisor) - Makes final GO/NO_GO/GO_WITH_RISKS decisions
3. **Documentation Specialist** - Assesses planning document completeness
4. **Metrics Specialist** - Reports test coverage, security scans, performance data
5. **Reviews Specialist** - Checks approval status from design/security/UAT reviews

### Agent Architecture Pattern

The system uses a **supervisor pattern with tool-based delegation**:

```
Coordinator Agent (supervisor)
├─ Consults Documentation Specialist via tool
├─ Consults Metrics Specialist via tool
└─ Consults Reviews Specialist via tool
```

- Specialists gather objective evidence and report facts
- Coordinator synthesizes findings and makes deployment decisions
- Feature Lookup Agent identifies which feature to assess
- All agents communicate via structured messages

## Key Design Documents

Read these documents in order to understand the system:

1. **DESIGN.md** (58KB) - Complete system design
   - Agent responsibilities and architecture
   - Workflow and decision-making logic
   - Data model and tool specifications
   - Deployment criteria for UAT vs Production

2. **PLAN.md** - Implementation plan
   - Technology stack (.NET 10, Semantic Kernel, MSTest, Moq)
   - Project structure and dependencies
   - Testing approach and conventions

3. **data/planning/FOLDER_STRUCTURE.md** - Standard feature data organization

4. **data/incoming/COMMUNITY_SHARE.md** - Application context (CommunityShare platform)

## Data Structure

### Sample Feature Data

The `data/incoming/` directory contains sample data from 4 features:

```
data/incoming/
├── feature1/  # Maintenance Scheduling - READY FOR PRODUCTION
├── feature2/  # QR Code Check-in - IN UAT
├── feature3/  # Reservation System - IN UAT
└── feature4/  # Contribution Tracking - IN DEVELOPMENT
```

### Feature Folder Structure

Each feature follows this standardized structure:

```
featureX/
├── jira/                           # JIRA metadata
│   ├── feature_issue.json
│   └── issue_changelog.json
├── planning/                       # Planning documents (Markdown)
│   ├── USER_STORY.md
│   ├── DESIGN_DOC.md
│   ├── ARCHITECTURE.md
│   ├── API_SPECIFICATION.md
│   ├── DATABASE_SCHEMA.md
│   └── DEPLOYMENT_PLAN.md
├── metrics/                        # Quantitative data (JSON)
│   ├── test_coverage_report.json
│   ├── unit_test_results.json
│   ├── security_scan_results.json
│   ├── performance_benchmarks.json
│   └── pipeline_results.json
├── reviews/                        # Review artifacts
│   ├── design.md
│   ├── security.json
│   ├── stakeholders.json
│   └── uat.json
├── code/                          # Code diffs
│   └── commit_*.diff
└── github/                        # GitHub PR data
    └── pr_*.json
```

## Deployment Criteria

The system evaluates features against different criteria based on target environment:

### UAT Criteria (Development → UAT)
- ≥60% test coverage
- Unit tests passing
- No critical/high security vulnerabilities
- USER_STORY and DESIGN_DOC mostly complete
- Design review approved

### Production Criteria (UAT → Production)
- **UAT completed and approved** (HIGHEST PRIORITY BLOCKER)
- ≥80% test coverage
- All tests passing (unit + integration)
- Zero critical/high vulnerabilities
- Security review approved
- DEPLOYMENT_PLAN and ARCHITECTURE complete
- Performance benchmarks meet SLAs
- All stakeholder approvals obtained

## Decision Types

The coordinator makes one of three decisions:

1. **GO** - All required criteria met, minimal risks
2. **GO_WITH_RISKS** - Required criteria met, but notable risks identified
3. **NO_GO** - Required criteria not met (blockers present)

## Working with This Repository

### When Analyzing the Design

1. Start with DESIGN.md to understand the system architecture
2. Review the agent responsibilities and workflow
3. Examine the data model and state management
4. Study the deployment criteria decision logic

### When Planning Implementation

1. Check PLAN.md for technology stack and dependencies
2. Note: .NET 10, dotnet CLI for project management (NO manual .csproj edits)
3. Follow the implementation constitution:
   - Use `interface` keyword for interfaces (NOT abstract classes)
   - Unit tests in separate test projects with `.Tests` suffix
   - Test files use `Tests.cs` suffix (e.g., `CoordinatorAgentTests.cs`)
   - `/tests` folder only for integration tests
   - Use `dotnet new` and `dotnet add package` for dependencies
   - Use modern C# features: records, nullable reference types, file-scoped namespaces

### When Examining Sample Data

1. Pick a feature from `data/incoming/feature1-4/`
2. Read the JIRA metadata in `jira/feature_issue.json`
3. Review planning docs in `planning/*.md`
4. Check metrics in `metrics/*.json`
5. Examine review status in `reviews/*.{json,md}`

### When Implementing Agents

Reference DESIGN.md sections:
- Agent responsibilities (lines 61-405)
- Tool specifications (lines 1039-1151)
- Workflow steps (lines 407-878)
- State management (lines 881-1036)

## Important Notes

**No Live Code:** This repository contains only design documents and sample data. When implementation begins:
- Source code will be in a `src/` directory organized by project
- Tests will be in separate test projects (e.g., `FeatureAssessment.Core.Tests`)
- `/tests` folder for integration tests only
- Dependencies managed via `dotnet` CLI with `.csproj` files
- Solution file (`.sln`) manages all projects

**Data is Pre-fetched:** The system reads from local files in `data/incoming/`, not live APIs. A separate data ingestion process (not part of this design) would populate these files from real JIRA/GitHub/CI-CD systems.

**Application Context:** Sample features are from "CommunityShare", a resource management platform for communities. This provides realistic feature examples but is not the focus of this repository.

## Technology Stack (Planned)

From PLAN.md:

- .NET 10 with C# 13 and async/await (Task-based)
- dotnet CLI for project and dependency management
- Microsoft Semantic Kernel for agentic framework
- OpenRouter as LLM provider
- HttpClient (built-in) with IHttpClientFactory for HTTP client
- OpenTelemetry .NET for observability
- FluentValidation for validation
- MSTest for testing framework
- Moq for mocking, WireMock.Net for HTTP mocking
- Polly for resilience and retry policies

## Development Workflow (When Implementation Begins)

**Setup:**
```bash
# Create solution and projects
dotnet new sln -n FeatureReadinessAssessment
dotnet new classlib -n FeatureAssessment.Core -o src/FeatureAssessment.Core
dotnet new mstest -n FeatureAssessment.Core.Tests -o tests/FeatureAssessment.Core.Tests
dotnet sln add src/**/*.csproj tests/**/*.csproj

# Add packages (example)
dotnet add src/FeatureAssessment.Infrastructure package Microsoft.SemanticKernel
dotnet add src/FeatureAssessment.Infrastructure package OpenTelemetry
dotnet add tests/FeatureAssessment.Core.Tests package Moq
dotnet add tests/FeatureAssessment.Core.Tests package FluentAssertions
```

**Testing:**
```bash
dotnet test  # Run all tests
dotnet test --filter "TestCategory!=Integration"  # Run unit tests only
dotnet test tests/FeatureAssessment.IntegrationTests  # Run integration tests
dotnet test /p:CollectCoverage=true  # Run with coverage
```

**Building:**
```bash
dotnet build  # Build solution
dotnet build -c Release  # Build in release mode
dotnet clean  # Clean artifacts
```

**Key Conventions:**
- Use `interface` keyword for interfaces, not abstract classes
- Unit test projects separate from source: `FeatureAssessment.Core.Tests`
- Test classes marked with `[TestClass]`, test methods with `[TestMethod]`
- Test files: `FeatureLookupAgentTests.cs` (not `TestFeatureLookupAgent.cs`)
- Integration tests go in `/tests` directory, marked with `[TestCategory("Integration")]`
- Use `[TestInitialize]` for setup, `[TestCleanup]` for teardown
- Use modern C# features: records, nullable reference types, file-scoped namespaces, primary constructors
- Never manually edit `.csproj` files unless absolutely necessary
- Update this CLAUDE.md when architecture changes

## Implementation Workflow (MANDATORY)

**CRITICAL: All implementation work MUST follow the four-stage development process defined in [WORKFLOW_STATUS.md](WORKFLOW_STATUS.md).**

### Prerequisites Before Starting Work

**MANDATORY: Before beginning ANY work item or task, you MUST read these foundational documents:**

1. **[DESIGN.md](DESIGN.md)** - Complete system design and architecture
   - Understand agent responsibilities and interactions
   - Review workflow and decision-making logic
   - Study data model and tool specifications
   - Understand deployment criteria

2. **[PLAN.md](PLAN.md)** - Implementation conventions and technology stack
   - Technology choices (.NET 10, Semantic Kernel, MSTest, etc.)
   - Project structure and organization
   - Testing approach and patterns
   - Code style and conventions

**Why This Matters:**
- Ensures implementation aligns with system architecture
- Prevents rework due to misunderstanding requirements
- Maintains consistency across the codebase
- Avoids violating established patterns and conventions

**Enforcement:**
- At the start of a new work item, confirm: "I have reviewed DESIGN.md and PLAN.md"
- If asked to implement something that seems inconsistent with these documents, pause and clarify
- When uncertain about approach, re-read relevant sections of these documents

### Four-Stage Development Process

Every work item (story) is broken down into tasks (Given-When-Then acceptance criteria). Each task goes through these stages:

1. **PLAN**: Story planning → Task planning (test strategy, file changes) → Branch creation
2. **BUILD & ASSESS**: Implementation → Testing → Quality validation (ALL checks must pass)
3. **REFLECT & ADAPT**: Process assessment → Future task adjustment
4. **COMMIT & PICK NEXT**: Commit creation → Branch management → Next task selection

### Stage Gate Enforcement

**BEFORE taking ANY action on a task**, you MUST:

1. **Read the active work item file** (e.g., `changes/XXX-story-name.md`)
2. **Identify the current stage** from the work item file
3. **Validate stage-appropriate actions**:
   - **PLAN stage**: ONLY provide planning assistance, test strategy, file analysis
     - ❌ NEVER write implementation code
     - ❌ NEVER execute tests
   - **BUILD & ASSESS stage**: Implement and test ONLY after user authorizes transition
     - ✅ Write code and run tests
     - ❌ NEVER move to next stage without user approval
   - **REFLECT & ADAPT stage**: ONLY discuss process improvements and future tasks
     - ❌ NEVER create commits
   - **COMMIT & PICK NEXT stage**: ONLY create commits and select next task after user approval

4. **Wait for explicit user direction** for stage transitions:
   - User will explicitly say "move to BUILD & ASSESS" or similar
   - NEVER auto-advance stages or suggest moving forward
   - Continue collaborating on current stage until user directs transition

### Sequential Task Rule (ABSOLUTE REQUIREMENT)

**ONE TASK AT A TIME**: Tasks MUST be completed sequentially, one at a time. No exceptions.

- ✅ Complete Task N through ALL FOUR STAGES before starting Task N+1
- ❌ NEVER work on multiple tasks in parallel
- ❌ NEVER bundle "efficient" multi-task implementations
- ✅ Each commit maps to exactly ONE Given-When-Then acceptance criterion

**Validation Checklist** (before writing ANY code):
- [ ] Is the current task's acceptance criteria FULLY defined?
- [ ] Is there ONLY ONE task with status "🔵 IN PROGRESS"?
- [ ] Have I completed ALL previous tasks in sequence?
- [ ] Am I implementing ONLY the current task's Given-When-Then scenario?

### Quality Validation Requirements

In BUILD & ASSESS stage, ALL of these MUST pass with ZERO errors or warnings:

**Backend** (from project root):
```bash
dotnet test                                           # All tests pass
dotnet format --verify-no-changes                     # Code formatting valid
dotnet build /p:EnforceCodeStyleInBuild=true         # Analyzer checks pass
```

**Frontend** (if applicable):
```bash
npm test                                              # All tests pass
npm run type-check                                    # TypeScript valid
npm run lint                                          # ESLint passes
```

**BUILD & ASSESS stage cannot be marked complete until ALL quality validation passes cleanly.**

### Work Item Structure

- **Story** (Work Item): Complete feature, documented in `changes/XXX-feature-name.md`
  - Contains multiple Given-When-Then acceptance criteria
  - Represents scope of a feature branch
  - Results in a pull request when complete

- **Task** (Given-When-Then Scenario): Single behavioral scenario within a story
  - Expressed as Given-When-Then acceptance criteria
  - Represents 1-3 commits worth of work
  - Goes through complete four-stage process

### Current Status Tracking

**MANDATORY**: Update [WORKFLOW_STATUS.md](WORKFLOW_STATUS.md) "Current Status" section at the start of EVERY interaction:

1. Read Current Status section
2. Read active work item file
3. Compare - do they match?
4. If ANY mismatch: **STOP and update WORKFLOW_STATUS.md FIRST**

**When Updates Are REQUIRED**:
- At start of every new task
- When transitioning between stages
- When starting new work item
- At beginning of EVERY user interaction

### Workflow Commands Reference

See [WORKFLOW_STATUS.md](WORKFLOW_STATUS.md) for complete details on:
- Development server commands
- Database management
- Quality validation protocols
- Branch and commit workflow
- Working document patterns

## Questions or Uncertainties?

When working with this repository:
- Refer to DESIGN.md for authoritative system design
- Check PLAN.md for implementation conventions
- Examine `data/incoming/feature1/` as a complete example
- Remember: This is a design repository - adapt guidance when code exists
