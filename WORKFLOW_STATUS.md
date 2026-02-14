# Workflow & Status

This document defines the development workflow, task breakdown approach, and commit practices for the Configuration Service project.

## Four-Stage Development Process

Every work item follows a structured four-stage process to ensure quality, consistency, and continuous improvement. The coding assistant and user must both understand and follow this process rigorously.

### Process Overview

1. **PLAN**: Story planning → Task planning (given-when-then acceptance criteria)
2. **BUILD & ASSESS**: Implementation → Testing → Quality validation
3. **REFLECT & ADAPT**: Process assessment → Future task adjustment
4. **COMMIT & PICK NEXT**: Commit creation → Branch management → Next task selection

### Stage Definitions

#### Stage 1: PLAN
**Story Planning → Task Planning → Branch Creation**

- **Story Planning**: Break down work item into acceptance criteria using Given-When-Then format
- **Task Planning**: For each Given-When-Then scenario, define:
  - **Test Strategy**: What tests are needed for confidence (unit, integration, edge cases)?
  - **File Changes**: What code changes are required?
- **Branch Setup**: Create feature branch and commit the completed plan
- **Output**: Clear understanding of what to test and what to implement, with plan committed to feature branch
- **Completion Criteria**: Test strategy and file changes identified, feature branch created, and plan committed

#### Stage 2: BUILD & ASSESS
**Implementation → Testing → Quality Validation**

- **Build Loop**: Implement the behavior and tests identified in planning
- **Assess Loop**: Validate current working tree against task requirements
  - Run tests and ensure they pass
  - Verify implementation matches acceptance criteria
  - Ensure all quality checks pass (linting, formatting, type checking)
  - Ensure consistent coding and testing patterns
  - **Check test file sizes** and flag for refactoring if needed:
    - More than 20 tests in a single file
    - More than 500 lines in a test file
    - Tests covering more than 3 distinct concerns
- **Output**: Working, tested code that satisfies the acceptance criteria
- **Completion Criteria**: ALL quality validation passes cleanly without errors or warnings

#### Stage 3: REFLECT & ADAPT
**Process Assessment → Future Task Adjustment**

- **Process Reflection**: Assess how the PLAN and BUILD & ASSESS stages went
  - What friction was encountered?
  - How can the process be improved next time?
  - Should templates or workflows be updated?
- **Future Task Assessment**: Review remaining work in light of current implementation
  - Do remaining Given-When-Then tasks need adjustment?
  - Are new tasks needed?
  - Should task order be rearranged?
- **Output**: Process improvements and updated task plan
- **Completion Criteria**: Both process assessment and future task review are complete

#### Stage 4: COMMIT & PICK NEXT
**Commit Creation → Branch Management → Next Task Selection**

- **Commit Preparation**: Create commit with conventional commit message
- **Branch Management**: Ensure proper branch setup and tracking
- **Next Task Selection**: Choose the next Given-When-Then task to work on
- **Output**: Clean commit and clear next steps
- **Completion Criteria**: Commit is made and next task is identified

### Stage Transition Rules

**CRITICAL**: Each stage must be completed fully before moving to the next stage. No exceptions.

**USER-DRIVEN STAGE COMPLETION PROTOCOL**:
- **ONLY the user decides when a stage is complete and when to transition to the next stage**
- The assistant MUST NOT declare stage completion or suggest moving to the next stage
- The assistant should continue collaborating on the current stage until the user explicitly states readiness to move forward
- The user will be very clear and explicit when directing stage transitions (e.g., "Let's move to BUILD & ASSESS stage")
- Assistant should focus on completing stage work thoroughly and responding to user direction

**ENFORCEMENT MECHANISM - STAGE GATE VALIDATION**:

Before taking ANY action on a task, the assistant MUST perform this validation:

1. **Read Current Stage**: Check the work item file to identify the current stage
2. **Stage Gate Check**:
   - If current stage is **PLAN**: 
     - ONLY provide planning assistance (test strategy, file analysis, discussion)
     - NEVER write implementation code
     - NEVER execute tests
     - WAIT for user to explicitly say "move to BUILD & ASSESS" or similar
   - If current stage is **BUILD & ASSESS**: 
     - Implement code and run tests ONLY after user authorized transition
     - NEVER move to REFLECT & ADAPT without user approval
   - If current stage is **REFLECT & ADAPT**: 
     - ONLY discuss process improvements and future tasks
     - NEVER create commits
     - WAIT for user to say "move to COMMIT & PICK NEXT" or similar
   - If current stage is **COMMIT & PICK NEXT**: 
     - ONLY create commits and select next task after user approval
     - NEVER start planning next task without user directive

3. **Violation Response**: If the assistant catches itself about to violate stage boundaries:
   - STOP immediately
   - State: "I cannot proceed with [action] because we are currently in [stage] and this action belongs to [other stage]"
   - Ask user: "Would you like to transition to [other stage] now?"

**Stage Transition Requirements:**
- **PLAN → BUILD & ASSESS**: User confirms test strategy and file changes are clearly defined, then **explicitly directs** transition
- **BUILD & ASSESS → REFLECT & ADAPT**: User confirms ALL quality validation passes cleanly, then **explicitly directs** transition
- **REFLECT & ADAPT → COMMIT & PICK NEXT**: User confirms process assessment and future task review are complete, then **explicitly directs** transition
- **COMMIT & PICK NEXT → PLAN**: User **explicitly directs** beginning of next Given-When-Then task planning

## Work Item Structure

### Story (Work Item)
A **story** represents a complete feature that provides business value:
- Documented in numbered work item file (e.g., `changes/001-feature-name.md`)
- Contains multiple Given-When-Then acceptance criteria
- Represents the scope of a feature branch
- Results in a pull request when complete

### Task (Given-When-Then Scenario)
A **task** represents a single behavioral scenario within a story:
- Expressed as Given-When-Then acceptance criteria
- Represents roughly 1-3 commits worth of work
- Goes through the complete four-stage process
- Has clear, testable completion criteria

## CRITICAL: Sequential Task Rule

**ABSOLUTE REQUIREMENT**: Tasks MUST be completed one at a time, in sequence. No exceptions.

### The Rule
- **ONE task at a time**: Only ONE task may be in "IN PROGRESS" status at any given time
- **Complete before starting next**: A task MUST complete ALL FOUR STAGES (PLAN → BUILD & ASSESS → REFLECT & ADAPT → COMMIT & PICK NEXT) before the next task begins
- **No parallel work**: Never work on Task N+1 while Task N is incomplete
- **No "efficient" bundling**: Even if it seems efficient to implement multiple tasks together, DON'T

### Why This Rule Exists
1. **Focused Progress**: Ensures concentrated effort on one behavioral scenario at a time
2. **Clear Commits**: Each commit maps to exactly one acceptance criterion
3. **Reviewable Changes**: Smaller, focused commits are easier to review and understand
4. **Traceability**: Clear lineage from requirement → task → commit → code
5. **Scope Control**: Prevents scope creep during implementation
6. **Quality Gates**: Forces quality validation at each task boundary

### Validation Checklist (Use BEFORE starting any implementation)
Before writing ANY code, verify:
- [ ] Is the current task's acceptance criteria FULLY defined?
- [ ] Is there currently ONLY ONE task with status "🔵 IN PROGRESS"?
- [ ] Have I completed ALL previous tasks in sequence?
- [ ] Am I implementing ONLY the current task's Given-When-Then scenario?

### Common Violations to Avoid
❌ **DON'T**: Implement empty state handling while building the main list component (Tasks 2 & 3 together)
✅ **DO**: Build list component first, commit, THEN add empty state in separate task

❌ **DON'T**: Add error handling for multiple scenarios at once
✅ **DO**: Handle one error scenario per task, commit, move to next

❌ **DON'T**: "While I'm here, I'll also add..." thinking
✅ **DO**: Stick strictly to current task scope, note future work for later tasks

### Enforcement Protocol
**Assistant's Responsibility:**
1. Before implementation, check work item file task statuses
2. Verify only ONE task is marked "🔵 IN PROGRESS"
3. If tempted to implement features beyond current task scope, STOP
4. Complete current task through all four stages FIRST
5. Only in Stage 4 (COMMIT & PICK NEXT) should the next task be selected

**User's Responsibility:**
- Monitor for scope creep during BUILD & ASSESS
- Review commits to ensure they map to single tasks
- Call out violations when spotted
- Guide assistant back to sequential workflow if deviation occurs

### Working Document Pattern
- Copy `changes/template.md` to create numbered story files
- Story file becomes the active working document
- Contains all detailed tracking for current task and stage
- Once task is complete and committed, remove all stage details except acceptance criteria
- Update with next task details
- Document always reflects current task and stage

## Development Commands

### Development Servers
```bash
# Backend API server (from svc/ directory):
dotnet run                                            # Start API on http://localhost:5000
# Or with environment specified:
dotnet run --environment Development

# Frontend dev server (from ui/ directory):
npm run dev                                           # Start Vite dev server on http://localhost:3000
# Or if using Create React App:
npm start
```

### Quality Validation (Required Every BUILD & ASSESS Stage)
```bash
# Backend Quality Checks (run from svc/ directory):
dotnet test                                           # Run xUnit unit tests
dotnet format --verify-no-changes                     # Verify C# code formatting
dotnet build /p:EnforceCodeStyleInBuild=true         # Build with analyzer checks

# Frontend Quality Checks (run from ui/ directory):
npm test                                              # Run Jest unit tests
npm run type-check                                    # TypeScript validation
npm run lint                                          # ESLint (if configured)

# Before merging to main (run all tests including integration):
dotnet test --filter "Category!=Integration"          # Fast unit tests during dev
dotnet test                                           # All tests including integration
npm test                                              # Frontend tests
```

**CRITICAL QUALITY PROTOCOL**
- ALL testing, linting, and formatting commands MUST NOT have ANY errors or warnings
- Unit tests run fast and validate logic with mocked I/O operations
- Integration tests run slower and validate real system interactions
- BUILD & ASSESS stage cannot be marked complete until ALL quality validation passes cleanly

### Database Management
```bash
# Apply migrations (from svc/ directory):
dotnet fm migrate -c "Server=(localdb)\\MSSQLLocalDB;Database=ConfigService;Trusted_Connection=true;"
# Or if using EF Core migrations:
dotnet ef database update

# Create new migration:
dotnet ef migrations add MigrationName
```

### Build for Production
```bash
# Backend (from svc/ directory):
dotnet build --configuration Release

# Frontend (from ui/ directory):
npm run build                                         # Creates production build in dist/
```

### Branch and Commit Workflow
```bash
# Story setup (once per work item):
cp changes/template.md changes/XXX-work-item-name.md
# Fill in story details and Given-When-Then acceptance criteria

# End of PLAN stage:
git checkout -b feature/work-item-name
git add changes/XXX-work-item-name.md
git commit -m "plan: add work item XXX planning document

Initial planning for [work item description]

# Task iteration (repeat for each Given-When-Then):
# Stage 1: PLAN - Update story file with test strategy and file changes
# Stage 2: BUILD & ASSESS - Implement and validate until all quality passes
# Stage 3: REFLECT & ADAPT - Document process assessment and future task review
# Stage 4: COMMIT & PICK NEXT - Commit and select next task

git add .
git commit -m "feat: implement [task description]

[Optional body describing the Given-When-Then behavior implemented]
```

## Current Status

**ENFORCEMENT MECHANISM - MANDATORY CURRENT STATUS UPDATE**:

The assistant MUST update this "Current Status" section BEFORE taking any action on a task. This is NON-NEGOTIABLE.

**Pre-Action Validation Checklist** (Execute at the start of EVERY interaction):
1. Read the Current Status section below
2. Read the active work item file (if different from what's listed)
3. Compare the two - do they match?
   - Does the Work Item File field match the actual active work item?
   - Does the Current Task match the task in the work item file?
   - Does the Current Stage match the stage in the work item file?
   - Is the Last Updated date current?
4. If ANY mismatch is found:
   - **STOP all other work immediately**
   - **Update this section FIRST** before proceeding
   - Inform user: "Updated WORKFLOW_STATUS.md Current Status to reflect current work"

**When Updates Are MANDATORY**:
- At the start of every new task (even if continuing on the same work item)
- When transitioning between stages (PLAN → BUILD & ASSESS, etc.)
- When starting a new work item
- At the beginning of EVERY interaction with the user (verify it's current)

**Failure to Update = System Error**: Failing to keep this section synchronized is a HIGH PRIORITY violation that compromises work continuity after memory resets.

### Active Work Item
- **Work Item File**: `changes/workitem001.md` - Feature Readiness Assessment System Prerequisites
- **Current Task**: All tasks complete (Tasks 1-4)
- **Current Stage**: Work item complete, ready for final commit
- **Last Updated**: 2026-02-14

### When Active
When working on a story, this section will show:
- **Work Item File**: `changes/XXX-story-name.md` (link to active working document)
- **Current Task**: Task number and brief description from acceptance criteria
- **Current Stage**: PLAN / BUILD & ASSESS / REFLECT & ADAPT / COMMIT & PICK NEXT
- **Last Updated**: YYYY-MM-DD

*All detailed tracking lives in the individual work item file. This status section only provides lightweight pointers.*