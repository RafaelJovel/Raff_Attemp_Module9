# Work Item XXX: [Feature Name]

## Story Details

> As a **[user role]**, I want **[capability]**, so that **[business value]**

### Notes
[Additional context, background information, or technical considerations]

---

## Acceptance Criteria (Tasks)

⚠️ **CRITICAL**: Tasks MUST be completed SEQUENTIALLY. Only ONE task can be "🔵 IN PROGRESS" at a time.

### Task 1: [Task Name]
**Given** [context/precondition]  
**When** [action/trigger]  
**Then** [expected behavior/outcome]

**Status**: ⚪ TODO / 🔵 IN PROGRESS / ✅ COMPLETE

### Task 2: [Task Name]
**Given** [context/precondition]  
**When** [action/trigger]  
**Then** [expected behavior/outcome]

**Status**: ⚪ TODO

### Task 3: [Task Name]
**Given** [context/precondition]  
**When** [action/trigger]  
**Then** [expected behavior/outcome]

**Status**: ⚪ TODO

---

## Current Task: Task N - [Task Name]

### Current Stage: 🔵 [PLAN / BUILD & ASSESS / REFLECT & ADAPT / COMMIT & PICK NEXT]

⚠️ **SEQUENTIAL TASK REMINDER**: Before implementing, verify:
- [ ] Is this the ONLY task marked "🔵 IN PROGRESS"?
- [ ] Have ALL previous tasks been marked "✅ COMPLETE"?
- [ ] Am I implementing ONLY this task's Given-When-Then scenario?

---

## PLAN Stage Details

### Test Strategy

#### Unit Tests (with mocked dependencies)
1. **[Component/Module] Tests**
   - `Test_Scenario_ExpectedBehavior`
   - `Test_Scenario_EdgeCase`

#### Integration Tests (with real dependencies)
1. **[Integration Scenario] Tests**
   - `Integration_Scenario_ExpectedBehavior`

### File Changes Needed

**Files to Create:**
- `path/to/new-file.ext` - Description of purpose

**Files to Modify:**
- `path/to/existing-file.ext` - Description of changes

### Dependencies Required
- `package-name` (version) - Purpose

### Implementation Order
1. First step
2. Second step
3. Third step

---

## BUILD & ASSESS Stage Details

### Build Progress
- [ ] Implementation step 1
- [ ] Implementation step 2
- [ ] Tests passing

### Quality Validation Status
- [ ] Unit tests passing (X/X tests pass)
- [ ] Integration tests passing (X/X tests pass)
- [ ] Type checking passed
- [ ] Linting passed (no errors/warnings)
- [ ] Manual verification successful

---

## REFLECT & ADAPT Stage Details

### Process Reflection
**What went well:**
- [Positive observations]

**What could improve:**
- [Friction points or inefficiencies]

**Actions for next time:**
- [Specific improvements to implement]

### Future Task Assessment
**Remaining tasks review:**
- Task N+1: [Status update - still valid / needs changes / reorder]
- Task N+2: [Status update]

**New tasks needed:**
- [Any new tasks discovered during implementation]

---

## COMMIT & PICK NEXT Stage Details

### Commit Information
**Commit Type:** feat / fix / refactor / test / docs / chore

**Commit Message:**
```
type: brief description (50 chars or less)

[Optional body with more details about the implementation]
```

### Next Task Selection
**Next Task:** Task N+1 - [Task Name]

**Checklist before starting next task:**
- [ ] Current task marked "✅ COMPLETE" in Acceptance Criteria
- [ ] Stage detail sections removed from this document
- [ ] Next task marked "🔵 IN PROGRESS"
- [ ] "Current Task" section updated above
- [ ] memory/WORKFLOW_STATUS.md "Current Status" updated