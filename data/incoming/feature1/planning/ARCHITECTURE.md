# Architecture Document: Maintenance Scheduling & Alert System

## System Overview

The Maintenance Scheduling & Alert System extends the existing CommunityShare platform with automated maintenance tracking, scheduling, and notification capabilities. This feature integrates with the Resource Management system (v1.3+) and Notification Service (v2.3+).

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         Client Layer                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────┐  │
│  │ Maintenance      │  │ Schedule         │  │ Maintenance  │  │
│  │ Calendar         │  │ Form             │  │ Log Table    │  │
│  │ (FullCalendar)   │  │ (React)          │  │ (React)      │  │
│  └────────┬─────────┘  └────────┬─────────┘  └──────┬───────┘  │
│           │                     │                     │           │
│           └─────────────────────┼─────────────────────┘           │
│                                 │                                 │
└─────────────────────────────────┼─────────────────────────────────┘
                                  │
                           REST API (HTTPS)
                                  │
┌─────────────────────────────────┼─────────────────────────────────┐
│                      Application Layer                            │
├─────────────────────────────────┴─────────────────────────────────┤
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              Express.js API Server                        │  │
│  │                                                            │  │
│  │  ┌──────────────────────────────────────────────────┐   │  │
│  │  │  Maintenance Routes (/api/maintenance/*)         │   │  │
│  │  │                                                    │   │  │
│  │  │  • POST   /schedules      (Create schedule)      │   │  │
│  │  │  • GET    /schedules      (List schedules)       │   │  │
│  │  │  • GET    /schedules/:id  (Get schedule)         │   │  │
│  │  │  • PATCH  /schedules/:id  (Update schedule)      │   │  │
│  │  │  • DELETE /schedules/:id  (Delete schedule)      │   │  │
│  │  │  • POST   /logs           (Log completion)       │   │  │
│  │  │  • GET    /logs           (Get logs)             │   │  │
│  │  └────────────────────────────────────────────────────┘   │  │
│  │                                                            │  │
│  │  ┌──────────────────────────────────────────────────┐   │  │
│  │  │  Middleware Stack                                 │   │  │
│  │  │  • Authentication (JWT validation)                │   │  │
│  │  │  • Authorization (Role-based access)              │   │  │
│  │  │  • Rate Limiting (100 req/min per user)           │   │  │
│  │  │  • Request Validation (Joi schemas)               │   │  │
│  │  │  • Error Handling (Centralized)                   │   │  │
│  │  └────────────────────────────────────────────────────┘   │  │
│  │                                                            │  │
│  │  ┌──────────────────────────────────────────────────┐   │  │
│  │  │  Controllers                                      │   │  │
│  │  │  • MaintenanceScheduleController                 │   │  │
│  │  │  • MaintenanceLogController                      │   │  │
│  │  └────────────────────────────────────────────────────┘   │  │
│  │                                                            │  │
│  │  ┌──────────────────────────────────────────────────┐   │  │
│  │  │  Services                                         │   │  │
│  │  │  • MaintenanceScheduleService                    │   │  │
│  │  │  • MaintenanceLogService                         │   │  │
│  │  │  • MaintenanceAlertService                       │   │  │
│  │  └────────────────────────────────────────────────────┘   │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │         Background Job Scheduler (Node-cron)          │  │
│  │                                                            │  │
│  │  ┌──────────────────────────────────────────────────┐   │  │
│  │  │  Daily Maintenance Checker Job                   │   │  │
│  │  │  • Cron: "0 6 * * *" (Daily at 6 AM UTC)         │   │  │
│  │  │  • Queries due/overdue maintenance                │   │  │
│  │  │  • Generates alerts                                │   │  │
│  │  │  • Sends to Notification Service                  │   │  │
│  │  │  • Logs execution metrics                         │   │  │
│  │  └────────────────────────────────────────────────────┘   │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                   │
└───────────────────────────────┬───────────────────────────────────┘
                                │
                                │
┌───────────────────────────────┼───────────────────────────────────┐
│                       Data Layer                                  │
├───────────────────────────────┴───────────────────────────────────┤
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                    PostgreSQL Database                    │  │
│  │                                                            │  │
│  │  ┌──────────────────────────────────────────────────┐   │  │
│  │  │  maintenance_schedules                            │   │  │
│  │  │  ─────────────────────────────────────            │   │  │
│  │  │  • id (UUID, PK)                                  │   │  │
│  │  │  • resource_id (UUID, FK → resources)            │   │  │
│  │  │  • schedule_type (VARCHAR)                        │   │  │
│  │  │  • frequency (ENUM)                               │   │  │
│  │  │  • start_date (TIMESTAMP)                         │   │  │
│  │  │  • next_due_date (TIMESTAMP)                      │   │  │
│  │  │  • description (TEXT)                             │   │  │
│  │  │  • assigned_to (UUID, FK → users)                │   │  │
│  │  │  • created_at (TIMESTAMP)                         │   │  │
│  │  │  • updated_at (TIMESTAMP)                         │   │  │
│  │  │  • deleted_at (TIMESTAMP, nullable)               │   │  │
│  │  │                                                    │   │  │
│  │  │  Indexes:                                          │   │  │
│  │  │  • idx_resource_id                                 │   │  │
│  │  │  • idx_next_due_date                               │   │  │
│  │  │  • idx_assigned_to                                 │   │  │
│  │  └────────────────────────────────────────────────────┘   │  │
│  │                                                            │  │
│  │  ┌──────────────────────────────────────────────────┐   │  │
│  │  │  maintenance_logs                                 │   │  │
│  │  │  ─────────────────────────────────────            │   │  │
│  │  │  • id (UUID, PK)                                  │   │  │
│  │  │  • schedule_id (UUID, FK → maintenance_schedules)│   │  │
│  │  │  • resource_id (UUID, FK → resources)            │   │  │
│  │  │  • performed_by (UUID, FK → users)               │   │  │
│  │  │  • performed_at (TIMESTAMP)                       │   │  │
│  │  │  • notes (TEXT)                                   │   │  │
│  │  │  • photos (JSONB) [array of URLs]                │   │  │
│  │  │  • issues_found (ENUM: none, minor, major)       │   │  │
│  │  │  • created_at (TIMESTAMP)                         │   │  │
│  │  │  • updated_at (TIMESTAMP)                         │   │  │
│  │  │                                                    │   │  │
│  │  │  Indexes:                                          │   │  │
│  │  │  • idx_schedule_id                                 │   │  │
│  │  │  • idx_resource_id                                 │   │  │
│  │  │  • idx_performed_at                                │   │  │
│  │  └────────────────────────────────────────────────────┘   │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                   │
└───────────────────────────────────────────────────────────────────┘

                                │
                                │
┌───────────────────────────────┼───────────────────────────────────┐
│                   External Services                               │
├───────────────────────────────┴───────────────────────────────────┤
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │        Notification Service v2.3 (Existing)              │  │
│  │                                                            │  │
│  │  • Email notifications (SendGrid)                         │  │
│  │  • SMS notifications (Twilio) - Premium only              │  │
│  │  • In-app notifications                                    │  │
│  │  • Notification preferences management                     │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │    Resource Management Service v1.3 (Existing)           │  │
│  │                                                            │  │
│  │  • Resource CRUD operations                                │  │
│  │  • Resource ownership/permissions                          │  │
│  │  • Resource metadata                                        │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                   │
└───────────────────────────────────────────────────────────────────┘
```

## Component Descriptions

### Client Layer

#### Maintenance Calendar Component
- **Technology**: React + FullCalendar.js
- **Responsibilities**:
  - Display monthly calendar view of scheduled/completed maintenance
  - Color-code events by status (scheduled, completed, overdue)
  - Handle user interactions (click, hover)
  - Filter events by resource, type, date range
- **State Management**: React hooks (useState, useEffect)
- **API Calls**: GET /api/maintenance/schedules

#### Schedule Form Component
- **Technology**: React + Formik (form state) + Yup (validation)
- **Responsibilities**:
  - Create/edit maintenance schedules
  - Form validation
  - Date picker integration
  - User assignment dropdown
- **API Calls**: POST /api/maintenance/schedules, PATCH /api/maintenance/schedules/:id

#### Maintenance Log Table Component
- **Technology**: React + React Table
- **Responsibilities**:
  - Display maintenance history
  - Expandable rows for detailed notes
  - Photo gallery integration
  - CSV export
- **API Calls**: GET /api/maintenance/logs

### Application Layer

#### Express.js API Server
- **Port**: 3000 (behind NGINX reverse proxy)
- **Authentication**: JWT tokens validated via existing auth middleware
- **Authorization**: Role-based access control (admin, maintenance_coordinator)
- **Rate Limiting**: 100 requests/minute per user using express-rate-limit
- **Request Validation**: Joi schemas for all incoming requests

#### Controllers
**MaintenanceScheduleController**:
- `createSchedule(req, res)`: Create new maintenance schedule
- `getSchedules(req, res)`: List schedules with filtering
- `getSchedule(req, res)`: Get single schedule details
- `updateSchedule(req, res)`: Update schedule
- `deleteSchedule(req, res)`: Soft delete schedule

**MaintenanceLogController**:
- `createLog(req, res)`: Log completed maintenance
- `getLogs(req, res)`: Get maintenance logs with filtering
- `getLog(req, res)`: Get single log details

#### Services
**MaintenanceScheduleService**:
- Business logic for schedule management
- Calculates next due dates based on frequency
- Validates resource ownership before operations

**MaintenanceLogService**:
- Business logic for logging maintenance
- Updates schedule next_due_date when maintenance completed
- Handles photo upload to S3

**MaintenanceAlertService**:
- Generates alert payloads
- Determines recipients based on assignment and resource ownership
- Interfaces with Notification Service

#### Background Job Scheduler
- **Technology**: Node-cron
- **Job**: Daily maintenance due date checker
- **Schedule**: Runs daily at 6:00 AM UTC
- **Process**:
  1. Query all schedules where next_due_date <= today + 1 day
  2. For each schedule, generate alert
  3. Send alerts via Notification Service
  4. Log execution metrics (schedules checked, alerts sent, failures)
- **Error Handling**:
  - Retries up to 3 times with exponential backoff (1min, 5min, 15min)
  - If notification fails: Store in `pending_notifications` table for later retry
  - If schedule query fails: Alert ops team via PagerDuty, abort execution
  - Partial failures: Log failed items but don't block remaining schedules
  - Notification delivery tracked per-alert for retry logic
- **Monitoring**: Logs to CloudWatch, emits metrics

### Data Layer

#### PostgreSQL Database
- **Version**: 14+
- **Connection Pooling**: pg-pool with max 20 connections
- **Migrations**: Managed by Sequelize migrations

**maintenance_schedules Table**:
- Stores all maintenance schedules
- Soft deletes (deleted_at column)
- Indexed on resource_id, next_due_date, assigned_to for query performance

**maintenance_logs Table**:
- Immutable records of completed maintenance
- JSONB column for flexible photo metadata storage
- Indexed on resource_id and performed_at for history queries

**pending_notifications Table**:
- Stores failed notifications for retry processing
- Fields: id, schedule_id, notification_type, recipient_id, payload (JSONB), retry_count, last_attempt_at, created_at
- Indexed on retry_count and last_attempt_at for retry job efficiency
- Records deleted after successful delivery or 24-hour expiry

### External Services

#### Notification Service v2.3
- **Existing Service**: Used by multiple platform features
- **Integration Method**: REST API calls
- **Endpoints Used**:
  - POST /api/notifications/email
  - POST /api/notifications/sms (Premium only)
  - POST /api/notifications/in-app
- **Error Handling**:
  - On 5xx errors: Queue notification in `pending_notifications` table for retry
  - On 4xx errors: Log validation error, alert assigned user via alternate notification channel
  - Timeout after 5s with fallback to queued retry
  - Success rate SLA: 95% within 5 minutes (including retries)
  - Circuit breaker: After 5 consecutive failures, pause for 1 minute before retry

#### Resource Management Service v1.3
- **Existing Service**: Core platform service
- **Integration Method**: Direct database foreign keys
- **Dependencies**:
  - resources table exists
  - Resource ownership/permissions model

## Data Flow

### Creating a Maintenance Schedule
```
1. User submits schedule form
   ↓
2. Frontend validates form (Yup schema)
   ↓
3. POST /api/maintenance/schedules
   ↓
4. Auth middleware validates JWT
   ↓
5. Authorization middleware checks user role
   ↓
6. Rate limiting middleware checks limits
   ↓
7. Request validation middleware (Joi)
   ↓
8. MaintenanceScheduleController.createSchedule()
   ↓
9. MaintenanceScheduleService.createSchedule()
   - Validate resource exists
   - Calculate next_due_date
   - Insert into maintenance_schedules
   ↓
10. Return 201 Created with schedule object
   ↓
11. Frontend updates calendar view

Error Scenarios:
- Resource not found → 404 with error.code="RESOURCE_NOT_FOUND"
- Invalid frequency value → 400 with error.code="VALIDATION_ERROR", error.details.frequency
- Database connection timeout → 503 with error.code="SERVICE_UNAVAILABLE", include correlation ID
- User lacks permission for resource → 403 with error.code="FORBIDDEN"
- Rate limit exceeded → 429 with error.code="RATE_LIMIT_EXCEEDED", include retry-after header
```

### Daily Maintenance Alert Generation
```
1. Cron job triggers at 6:00 AM UTC
   ↓
2. MaintenanceCheckerJob.run()
   ↓
3. Query maintenance_schedules
   WHERE next_due_date <= NOW() + INTERVAL '1 day'
   AND deleted_at IS NULL
   ↓
4. For each schedule:
   a. Determine recipients (assigned_to or resource owner)
   b. Generate alert payload
   c. Call MaintenanceAlertService.sendAlert()
   ↓
5. MaintenanceAlertService.sendAlert()
   a. POST /api/notifications/email (Notification Service)
   b. POST /api/notifications/in-app (Notification Service)
   ↓
6. Log execution metrics
   - Total schedules checked
   - Alerts sent successfully
   - Failures
   ↓
7. Emit CloudWatch metrics

Failure Handling:
- Query failure (database down) → Alert ops team via PagerDuty, abort job execution, retry job in 15 minutes
- Individual notification failure → Store in `pending_notifications` table with retry_count=0
- Notification Service timeout → Queue notification for retry, continue processing other alerts
- Retry job runs hourly for pending notifications (max 24 retries over 24 hours)
- After 24 hours, failed notifications trigger ops alert for manual investigation
- Partial success: Job completes successfully if >80% of notifications sent
```

### Logging Completed Maintenance
```
1. User submits completion form
   ↓
2. POST /api/maintenance/logs
   ↓
3. Auth & authorization middleware
   ↓
4. MaintenanceLogController.createLog()
   ↓
5. MaintenanceLogService.createLog()
   a. Upload photos to S3 (if provided)
   b. Insert into maintenance_logs
   c. Update schedule.next_due_date (if recurring)
   ↓
6. Return 201 Created with log object
   ↓
7. Frontend updates maintenance history table
```

## Error Handling Strategy

### API Error Response Format
All errors return consistent JSON structure:
- `error.code`: Machine-readable error code (e.g., "RESOURCE_NOT_FOUND", "VALIDATION_ERROR")
- `error.message`: Human-readable message
- `error.details`: Optional context object with field-specific validation errors
- HTTP status codes:
  - 400 (validation errors)
  - 403 (unauthorized access)
  - 404 (resource not found)
  - 500 (internal server error)
  - 503 (service unavailable)

### Database Failure Handling
- **Connection failures**: Return 503 Service Unavailable, retry with exponential backoff (max 3 attempts, 1s/2s/4s delays)
- **Transaction failures**: Automatic rollback, log error with correlation ID, return 500 with correlation ID for debugging
- **Deadlocks**: Automatic retry up to 2 times with jitter (random delay 100-500ms)
- **Query timeouts**: 30-second timeout for complex queries, log slow queries for optimization

### External Service Failures
- **Notification Service down**: Queue alerts in database table `pending_notifications` for retry
- **Resource Service down**: Cache last-known resource data for validation, graceful degradation for up to 5 minutes
- **Timeout handling**:
  - 5s timeout for Notification Service calls
  - 3s timeout for Resource Service validation
  - Log timeout events for monitoring

### Background Job Failures
- **Job execution failure**: Log to CloudWatch with full stack trace, retry 3x with exponential backoff (1min, 5min, 15min)
- **Persistent failures**: Alert ops team via PagerDuty after 3 consecutive failures
- **Partial failures**: Continue processing remaining schedules, log failed items separately for manual review
- **Notification delivery failures**: Store failed notifications in `pending_notifications` table with retry metadata

### Validation Error Handling
- **Client-side validation**: Prevent invalid requests before API call
- **Server-side validation**: Comprehensive validation with detailed error messages
- **Field-level errors**: Return specific field names and constraint violations in `error.details`

## Security Considerations

### Authentication
- All API endpoints require valid JWT token
- Tokens expire after 24 hours
- Refresh tokens supported

### Authorization
- Role-based access control:
  - **admin**: Full CRUD on all schedules and logs
  - **maintenance_coordinator**: Can create logs, read schedules assigned to them
  - **regular user**: Can only read schedules for resources they own

### Data Protection
- Sensitive data encrypted at rest (PostgreSQL encryption)
- API communications over HTTPS only
- SQL injection prevention via parameterized queries
- XSS prevention via input sanitization

### Rate Limiting
- 100 requests/minute per user on schedule/log endpoints
- 10 requests/minute on alert generation endpoint
- 429 Too Many Requests response when exceeded

## Performance Considerations

### Database Optimization
- Indexes on frequently queried columns
- Connection pooling to reduce overhead
- Query optimization (avoid N+1 queries)

### Caching Strategy
- No caching for schedule data (must be real-time)
- Redis cache for resource metadata (1 hour TTL)
- Frontend caches calendar data for 5 minutes

### Load Testing Results
- 500 concurrent users sustained
- p95 response time < 200ms for all endpoints
- Background job processes 10,000 schedules in < 2 minutes

## Monitoring and Observability

### Metrics
- CloudWatch metrics:
  - API request count, latency, error rate
  - Background job execution time, success rate
  - Alert delivery success rate

### Logging
- Application logs: Winston logger
- Log levels: ERROR, WARN, INFO, DEBUG
- Centralized logging: CloudWatch Logs
- Structured logging with correlation IDs

### Alerting
- PagerDuty alerts for:
  - Background job failures
  - API error rate > 5%
  - Alert delivery rate < 90%

## Deployment Architecture

### Infrastructure
- AWS ECS Fargate for containerized application
- RDS PostgreSQL for database
- S3 for photo storage
- CloudFront for static asset delivery
- Application Load Balancer for traffic distribution

### Scaling
- Auto-scaling based on CPU/memory utilization
- Min instances: 2
- Max instances: 10
- Target CPU utilization: 70%

## Rollback Strategy

### Feature Flag
- Feature controlled by `maintenance_scheduling_enabled` flag
- Default: false (disabled)
- Can be toggled via admin panel without deployment

### Database Rollback
- Migrations are reversible
- Rollback command: `npx sequelize-cli db:migrate:undo`
- Data preserved during rollback

### Monitoring Post-Deployment
- Watch error rates for 24 hours
- Monitor alert delivery success rate
- Check background job execution metrics
