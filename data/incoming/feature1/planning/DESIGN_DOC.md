# Design Document: Maintenance Scheduling & Alert System

## Overview
This document outlines the design and implementation approach for the Maintenance Scheduling & Alert System, including architecture, data models, UI/UX design, wireframes, user flows, and interaction patterns.

## Problem Statement
Community resource managers currently lack a systematic way to track and schedule routine maintenance for shared facilities. This leads to:
- Missed maintenance windows resulting in equipment degradation
- Reactive rather than proactive maintenance approach
- No centralized history of maintenance activities
- Difficulty ensuring compliance with maintenance requirements

This feature addresses these problems by providing automated scheduling, reminders, and comprehensive tracking of all maintenance activities.

## Design Decisions & Trade-offs

### Architecture Decisions

**Decision 1: Recurring Schedule Engine**
- **Chosen Approach**: Cron-like background job checking due dates daily
- **Alternatives Considered**: Event-driven immediate scheduling, Database triggers
- **Rationale**: Daily batch processing is sufficient for maintenance schedules (vs. real-time events), reduces system complexity, and allows for easier monitoring and debugging
- **Trade-off**: Alerts may be delayed up to 24 hours, but this is acceptable for maintenance workflows

**Decision 2: Photo Storage**
- **Chosen Approach**: S3 with presigned URLs
- **Alternatives Considered**: Base64 in database, Third-party image hosting service
- **Rationale**: S3 provides scalable storage, presigned URLs maintain security, cost-effective for varying file sizes
- **Trade-off**: Additional AWS dependency, but team already manages S3 infrastructure

**Decision 3: Calendar Library**
- **Chosen Approach**: FullCalendar.js
- **Alternatives Considered**: Custom-built calendar, react-big-calendar
- **Rationale**: FullCalendar provides robust features, excellent mobile support, and matches our existing design system
- **Trade-off**: Adds ~50KB to bundle size, but provides significant development time savings

### Data Model

```sql
-- Maintenance Schedules Table
CREATE TABLE maintenance_schedules (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  resource_id UUID NOT NULL REFERENCES resources(id) ON DELETE CASCADE,
  maintenance_type VARCHAR(50) NOT NULL CHECK (maintenance_type IN ('inspection', 'cleaning', 'repair', 'replacement', 'other')),
  frequency VARCHAR(20) NOT NULL CHECK (frequency IN ('one-time', 'daily', 'weekly', 'monthly', 'quarterly', 'annually')),
  start_date DATE NOT NULL,
  next_due_date DATE NOT NULL,
  description TEXT,
  assigned_to UUID REFERENCES users(id) ON DELETE SET NULL,
  email_alerts_enabled BOOLEAN DEFAULT true,
  sms_alerts_enabled BOOLEAN DEFAULT false,
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP DEFAULT NOW(),
  created_by UUID NOT NULL REFERENCES users(id),

  INDEX idx_next_due_date (next_due_date),
  INDEX idx_resource_id (resource_id),
  INDEX idx_assigned_to (assigned_to)
);

-- Maintenance Logs Table
CREATE TABLE maintenance_logs (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  schedule_id UUID REFERENCES maintenance_schedules(id) ON DELETE SET NULL,
  resource_id UUID NOT NULL REFERENCES resources(id) ON DELETE CASCADE,
  maintenance_type VARCHAR(50) NOT NULL,
  scheduled_date DATE,
  completed_date TIMESTAMP NOT NULL,
  performed_by UUID NOT NULL REFERENCES users(id),
  notes TEXT,
  photos JSONB DEFAULT '[]', -- Array of S3 URLs
  issues_found VARCHAR(20) CHECK (issues_found IN ('none', 'minor', 'major')),
  created_at TIMESTAMP DEFAULT NOW(),

  INDEX idx_resource_id (resource_id),
  INDEX idx_completed_date (completed_date),
  INDEX idx_schedule_id (schedule_id)
);
```

**Key Design Decisions**:
- UUIDs for all primary keys to support distributed systems and prevent ID enumeration
- Separate `maintenance_logs` table to preserve history even if schedules are deleted
- `schedule_id` nullable in logs to allow manual logging of unscheduled maintenance
- JSONB for photo URLs to avoid additional join table for small arrays
- Indexes on frequently queried fields (due_date, resource_id) for performance

## Testing Strategy

### Unit Testing
- **Coverage Target**: ≥ 80% code coverage
- **Scope**: All business logic, controllers, services, and components
- **Test Framework**: Jest for JavaScript/TypeScript, Pytest for Python
- **Key Test Areas**:
  - Schedule creation and validation
  - Recurring schedule calculations
  - Alert generation logic
  - Photo upload and retrieval
  - Database operations (CRUD)
  - Edge cases (timezone handling, date boundaries)

### Integration Testing
- **Scope**: API endpoints, database interactions, third-party service integration
- **Key Test Scenarios**:
  - End-to-end schedule workflow
  - Alert delivery (email/SMS)
  - S3 photo upload flow
  - Background job execution
  - Feature flag integration

### Performance Testing
- **Load Testing**: 500 concurrent users, 150 req/sec throughput
- **Performance Benchmarks**:
  - API p95 latency < 200ms (measured across all maintenance endpoints)
  - API p99 latency < 300ms
  - Database query p95 < 50ms
  - Memory usage < 512MB per container under load
  - CPU utilization < 70% under load
- **Success Criteria**:
  - Zero errors during 10-minute sustained load test
  - p95 latency within thresholds for all endpoints
  - No memory leaks detected (heap size stable over 30 minutes)
  - Database connection pool remains healthy (< 80% utilization)
- **Tools**: Artillery for load testing, CloudWatch for monitoring

### User Acceptance Testing (UAT)
- **Participants**: 10 beta users (experienced community admins from diverse organizations)
- **Test Cases**: 88 test scenarios covering all user workflows including:
  - Happy path: Schedule creation, modification, completion logging
  - Error handling: Invalid inputs, network failures, permission issues
  - Edge cases: Timezone handling, recurring schedules across DST, bulk operations
- **Success Criteria**:
  - Test pass rate ≥ 95% (84/88 scenarios passing)
  - User satisfaction score ≥ 4.0/5.0 (average from post-UAT survey)
  - Zero critical usability issues reported
  - Average task completion time within 20% of design target

### Security Testing
- **SAST**: Automated static analysis on every PR
- **Dependency Scanning**: Regular vulnerability scans
- **Acceptance Criteria**: Zero critical/high vulnerabilities for production

## Design Principles
1. **Clarity**: Maintenance schedules should be immediately visible and understandable
2. **Efficiency**: Common tasks (creating schedules, logging completion) should require minimal clicks
3. **Consistency**: Design aligns with existing CommunityShare design system
4. **Accessibility**: WCAG 2.1 AA compliant

## UI Components

### 1. Maintenance Calendar View
**Location**: Main dashboard tab, `/maintenance/calendar`

**Layout**:
```
┌─────────────────────────────────────────────────────────────┐
│  Maintenance Calendar                    [+ New Schedule]   │
├─────────────────────────────────────────────────────────────┤
│  Filters: [All Resources ▾] [All Types ▾] [This Month ▾]   │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│     Calendar Component (FullCalendar.js)                     │
│                                                               │
│     October 2025                                             │
│     ┌───┬───┬───┬───┬───┬───┬───┐                          │
│     │Mon│Tue│Wed│Thu│Fri│Sat│Sun│                          │
│     ├───┼───┼───┼───┼───┼───┼───┤                          │
│     │ 1 │ 2 │ 3 │ 4 │ 5 │ 6 │ 7 │                          │
│     │   │🔧 │   │   │   │   │   │ <- Maintenance due       │
│     ├───┼───┼───┼───┼───┼───┼───┤                          │
│     │ 8 │ 9 │...                                            │
│                                                               │
│     Legend:                                                   │
│     🔧 Scheduled  ✅ Completed  ⚠️ Overdue                   │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

**Color Coding**:
- Blue: Scheduled maintenance
- Green: Completed maintenance
- Red: Overdue maintenance
- Yellow: Due within 3 days

**Interactions**:
- Click on calendar event → Shows maintenance details modal
- Click "+ New Schedule" → Opens schedule creation form
- Hover over event → Shows tooltip with resource name and maintenance type

### 2. Schedule Creation/Edit Form
**Modal Dialog**: Opens on top of calendar view

**Form Fields**:
```
┌─────────────────────────────────────────────────┐
│  Create Maintenance Schedule            [✕]     │
├─────────────────────────────────────────────────┤
│                                                  │
│  Resource *                                      │
│  [Select a resource... ▾]                       │
│                                                  │
│  Maintenance Type *                              │
│  ( ) Inspection   ( ) Cleaning                  │
│  ( ) Repair       ( ) Replacement               │
│  ( ) Other: [____________]                      │
│                                                  │
│  Frequency *                                     │
│  ( ) One-time  ( ) Daily    ( ) Weekly          │
│  ( ) Monthly   ( ) Quarterly ( ) Annually       │
│                                                  │
│  Start Date *                                    │
│  [📅 10/15/2025]                                │
│                                                  │
│  Description                                     │
│  [________________________________]             │
│  [________________________________]             │
│  [________________________________]             │
│                                                  │
│  Assign To (Optional)                            │
│  [Select user... ▾]                             │
│                                                  │
│  Enable Alerts                                   │
│  [✓] Email notification 1 day before            │
│  [✓] Email notification on due date             │
│  [ ] SMS notification (Premium only)            │
│                                                  │
│           [Cancel]  [Save Schedule]             │
│                                                  │
└─────────────────────────────────────────────────┘
```

**Validation**:
- All fields marked with * are required
- Start date cannot be in the past
- Frequency cannot be changed once maintenance has been completed (must create new schedule)

### 3. Maintenance Log Table
**Location**: Resource detail page, "Maintenance History" tab

**Table Layout**:
```
┌──────────────────────────────────────────────────────────────────┐
│  Maintenance History                           [Export CSV]       │
├──────┬──────────────┬────────────┬──────────────┬──────┬────────┤
│ Date │ Type         │ Status     │ Performed By │ Notes│ Photo  │
├──────┼──────────────┼────────────┼──────────────┼──────┼────────┤
│10/15 │ Inspection   │ ✅Complete │ Bob Martinez │ View │ 📷 (2) │
├──────┼──────────────┼────────────┼──────────────┼──────┼────────┤
│09/15 │ Cleaning     │ ✅Complete │ Alice Johnson│ View │ -      │
├──────┼──────────────┼────────────┼──────────────┼──────┼────────┤
│08/15 │ Repair       │ ✅Complete │ Carol Smith  │ View │ 📷 (1) │
├──────┼──────────────┼────────────┼──────────────┼──────┼────────┤
│07/15 │ Inspection   │ ⚠️ Overdue │ (unassigned) │ -    │ -      │
└──────┴──────────────┴────────────┴──────────────┴──────┴────────┘
```

**Features**:
- Click on "View" in Notes column → Expands row to show full notes
- Click on photo icon → Opens lightbox gallery
- Export CSV includes all maintenance records for the resource

### 4. Maintenance Completion Modal
**Triggered**: When marking maintenance as complete

```
┌─────────────────────────────────────────────────┐
│  Complete Maintenance                   [✕]     │
├─────────────────────────────────────────────────┤
│                                                  │
│  Resource: Basketball Court #3                  │
│  Type: Monthly Inspection                       │
│  Scheduled: October 15, 2025                    │
│                                                  │
│  Completion Date *                               │
│  [📅 10/15/2025 🕐 2:30 PM]                    │
│                                                  │
│  Notes                                           │
│  [________________________________]             │
│  [________________________________]             │
│  [________________________________]             │
│  (Describe any issues found or work performed)  │
│                                                  │
│  Photos (Optional)                               │
│  [📷 Upload Photos] or [Drag files here]       │
│                                                  │
│  Issues Found?                                   │
│  ( ) No issues                                  │
│  ( ) Minor issues (logged for tracking)         │
│  ( ) Major issues (requires immediate action)   │
│                                                  │
│           [Cancel]  [Mark Complete]             │
│                                                  │
└─────────────────────────────────────────────────┘
```

### 5. Alert Notification Design

**Email Template**:
```
Subject: Maintenance Due: [Resource Name] - [Maintenance Type]

Hi [User Name],

A maintenance task is due for one of your resources:

Resource: Basketball Court #3
Type: Monthly Safety Inspection
Due Date: October 15, 2025
Assigned To: Bob Martinez

[View in CommunityShare] [Mark as Complete]

Description:
Check all safety equipment, court surface, and lighting.

---
You're receiving this because you manage this resource.
Update your notification preferences in Settings.
```

**In-App Notification**:
- Bell icon in top nav shows count of pending maintenance items
- Clicking bell opens notification dropdown with list
- Each notification has "View" and "Dismiss" actions

## User Flows

### Flow 1: Creating a Maintenance Schedule
1. User navigates to Maintenance Calendar
2. Clicks "+ New Schedule" button
3. Fills out schedule creation form
4. Clicks "Save Schedule"
5. System validates input
6. System creates schedule in database
7. System schedules background job to check due date
8. User sees confirmation toast: "Maintenance schedule created successfully"
9. Calendar updates to show new scheduled maintenance

### Flow 2: Receiving and Acting on Maintenance Alert
1. Background job runs daily at 6 AM UTC
2. System identifies maintenance due within 24 hours
3. System sends email/SMS alerts to assigned users
4. User receives email notification
5. User clicks "Mark as Complete" link in email
6. System opens maintenance completion modal (requires login)
7. User fills in completion details and notes
8. User uploads optional photos
9. Clicks "Mark Complete"
10. System logs completion in maintenance_logs table
11. System updates next due date for recurring maintenance
12. User sees confirmation: "Maintenance logged successfully"

### Flow 3: Viewing Maintenance History
1. User navigates to Resource detail page
2. Clicks "Maintenance History" tab
3. System loads all maintenance logs for resource
4. User sees table of completed and scheduled maintenance
5. User clicks "View" on a specific log entry
6. System expands row to show full notes and metadata
7. User clicks photo thumbnail
8. System opens lightbox gallery to view photos

## Mobile Responsive Design

### Mobile Calendar View (< 768px)
- Calendar switches to list view instead of month grid
- Each list item shows: Date, Resource, Type, Status
- Filters collapse into hamburger menu
- "+ New Schedule" becomes floating action button (FAB)

### Mobile Form View
- Form fields stack vertically
- Date picker optimized for touch
- Photo upload uses native camera integration

## Accessibility

### Keyboard Navigation
- All interactive elements keyboard accessible
- Tab order follows logical flow
- Modal dialogs trap focus
- ESC key closes modals

### Screen Reader Support
- All form fields have proper labels
- Calendar events have descriptive aria-labels
- Status icons include text alternatives
- Loading states announced to screen readers

### Color and Contrast
- All text meets WCAG AA contrast ratios (4.5:1 for normal text, 3:1 for large text)
- Color is not the only indicator of status (icons + text used)
- Focus indicators clearly visible

## Design System Integration

### Typography
- Headers: Inter font, Semi-bold, 24px/28px
- Body text: Inter font, Regular, 16px/24px
- Small text: Inter font, Regular, 14px/20px

### Colors (from CommunityShare design system)
- Primary: #2563EB (Blue)
- Success: #16A34A (Green)
- Warning: #F59E0B (Orange)
- Error: #DC2626 (Red)
- Neutral: #6B7280 (Gray)

### Spacing
- Component padding: 16px
- Section spacing: 24px
- Form field spacing: 12px

### Components
- Buttons: Use existing CommunityShare button components
- Inputs: Use existing form field components
- Modals: Use existing modal wrapper component
- Calendar: FullCalendar.js with CommunityShare theme customization
