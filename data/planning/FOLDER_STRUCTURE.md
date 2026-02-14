# Standardized Feature Data Folder Structure

## Overview

Each feature should follow this exact structure to enable consistent data ingestion pipelines.

## Folder Structure

```
featureX/
│
├── code/                  # Code artifacts (Diffs → AST → Graph DB)
│   ├── commit_001_*.diff
│   ├── commit_002_*.diff
│   └── ...
│
├── github/                # GitHub integration data (Graph Nodes/Edges)
│   ├── pull_request_XXX.json
│   └── pr_review_comments_XXX.json
│
├── jira/                  # JIRA/ticketing data (Graph Nodes/Edges)
│   ├── feature_issue.json
│   └── issue_changelog.json
│
├── metrics/               # Test results and metrics (JSON → Metrics API)
│   ├── test_coverage_report.json
│   ├── unit_test_results.json
│   ├── pipeline_results.json
│   ├── performance_benchmarks.json
│   └── security_scan_results.json
│
├── planning/              # Planning documentation (Markdown → Vector DB)
│   ├── USER_STORY.md
│   ├── DESIGN_DOC.md
│   ├── ARCHITECTURE.md
│   ├── API_SPECIFICATION.md
│   ├── DATABASE_SCHEMA.md
│   └── DEPLOYMENT_PLAN.md
│
└── reviews/               # All review artifacts (Graph Nodes/Edges)
    ├── design_review_YYYY-MM-DD.md
    ├── security_review_YYYY-MM-DD.json
    ├── stakeholder_review_YYYY-MM-DD.json
    └── uat_results|signoffs_YYYY-MM-DD.json
 
```

## Folder Purposes

### code/
**Purpose:** Code changes and implementation artifacts
**Format:** .diff files and JSON summaries
**Destination:** AST parsing → Graph database
**Contents:**
- commit_XXX_description.diff: Sequential commit diffs

### github/
**Purpose:** GitHub integration data
**Format:** JSON files
**Destination:** Graph database
**Contents:**
- pull_request_XXX.json: Pull request metadata
- pr_review_comments_XXX.json: Review comments from GitHub

**Note:** This mirrors what GitHub's API would return.

### jira/
**Purpose:** JIRA/ticketing system data
**Format:** JSON files
**Destination:** Graph database
**Contents:**
- feature_issue.json: Main feature ticket
- issue_changelog.json: Status transition history

### metrics/
**Purpose:** Quantitative test results and performance data
**Format:** JSON files
**Destination:** Metrics API / Time-series database
**Contents:**
- Required: test_coverage_report.json, unit_test_results.json, integration_test_results.json, pipeline_results.json, performance_benchmarks.json, security_scan_results.json
- Optional: Feature-specific metrics (mobile_app_build_status.json, uat_test_results.json, deployment_history.json)

### planning/
**Purpose:** All planning and design documentation
**Format:** Markdown files
**Destination:** Vector database for semantic search
**Contents:**
- Required: USER_STORY.md, DESIGN_DOC.md, ARCHITECTURE.md, API_SPECIFICATION.md, DATABASE_SCHEMA.md, DEPLOYMENT_PLAN.md
- Optional: Feature-specific planning docs (MOBILE_APP_SPEC.md, INTEGRATION_PLAN.md, USER_DOCUMENTATION.md, etc.)

### reviews/
**Purpose:** All review artifacts organized by type
**Format:** JSON and Markdown
**Destination:** Graph database (nodes and edges for review events)
**Structure:**
- code_reviews/: Code review comments and approvals from PRs
- design/: Design review meeting notes and decisions
- security/: Security assessment reports
- uat/: User acceptance testing results and feedback

**Note:** This consolidates review data that may also appear in the github/ folder, providing a single location for all review-related artifacts.

## Data Relationships

### How folders connect:
- **jira/feature_issue.json** → Links to feature metadata
- **github/pull_request_XXX.json** → Links to code commits
- **reviews/** → Links to feature, PRs, commits, and team members
- **metrics/** → Time-series data linked to feature and stage
