# AcxiomCRM - Enterprise Customer Lifecycle Management System

An enterprise-grade, role-based CRM full-stack web application built according to the **AcxiomCRM Project Assignment Specification**.

---

## 1. Technology Stack

- **Framework:** ASP.NET Core 8.0 MVC (Razor Views)
- **Data Access:** Entity Framework Core 8.0 (Code-First)
- **Database:** SQLite (zero-configuration, self-contained, auto-seeded on first run)
- **Security & Authentication:** ASP.NET Core Identity (framework-managed PBKDF2 password hashing, secure auth cookies, account lockout, global `[Authorize]` fallback policy)
- **UI & UX:** Bootstrap 5.3, Bootstrap Icons, jQuery Validation & Unobtrusive Client Validation
- **Analytics & Data Visualizations:** Chart.js 4.4 (Pipeline by Stage bar chart, Lead Status doughnut chart, Monthly Sales line chart)
- **Architecture:** Enterprise 4-Layer Architecture (Presentation, Application Services, Domain/Business Logic, Data Access)

---

## 2. Default Seeded Credentials

The database automatically initializes and seeds all 3 primary role profiles with realistic enterprise data on application startup:

| Role | Email Address | Password | Access Scope |
|---|---|---|---|
| **Admin** | `admin@acxiom.com` | `Test@1234` | Full system administration, user management, role reassignment, account unlocks, complete audit log viewer, all CRM records and reports. |
| **Manager** | `manager@acxiom.com` | `Test@1234` | Team-wide customer, lead, opportunity, follow-up management and executive reports. |
| **SalesExecutive** | `sales@acxiom.com` | `Test@1234` | Scoped strictly to own assigned leads, customers, opportunities, follow-ups, and activities. |

*(Password Policy: Minimum 8 characters, at least 1 uppercase letter, 1 lowercase letter, 1 digit, and 1 special character).*

---

## 3. How to Run the Application

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Execution Steps
From the root directory or inside `AcxiomCRM`:

```bash
cd AcxiomCRM
dotnet run
```

The application will launch on `http://localhost:5218` (or `http://localhost:5000` / HTTPS if configured).

On the first launch:
1. SQLite database `AcxiomCRM.db` is created automatically.
2. Roles (`Admin`, `Manager`, `SalesExecutive`) and default users are seeded.
3. Realistic enterprise customers, leads, opportunities, follow-ups, activities, and initial audit logs are populated.

---

## 4. Key Functional Modules

```
AcxiomCRM
│
├── Authentication & Identity
│   ├── Login (with quick-fill credential buttons for evaluators)
│   ├── Register (provisions new SalesExecutive accounts)
│   ├── Logout (safe session termination + audit log)
│   ├── Lockout (triggered after 5 failed attempts, 10 min window)
│   └── 401/403 status code handlers for REST API requests
│
├── Dashboard
│   ├── Role-scoped KPI Cards (Total Customers, Total Leads, Open Leads, Total Deals, Pipeline Value, Weighted Value)
│   ├── Date Filtering (All Time, This Month, This Week, Today)
│   ├── Chart.js Lead Status Doughnut Chart
│   ├── Chart.js Opportunity Pipeline by Stage Bar Chart
│   ├── Chart.js Closed Won Monthly Sales Line Chart
│   └── Quick Follow-Up Actions & Completion
│
├── Customer Management
│   ├── Search (name, code, company, email, phone) & status filters
│   ├── Customer 360 Details view (linked deals, follow-ups, activities)
│   ├── Client & server-side duplicate checks (email, phone, code)
│   ├── Soft-delete deactivation with confirmation modal
│   └── Server-side role scoping (SalesExecutive only views assigned)
│
├── Lead Management & Conversion Workflow
│   ├── Lead status workflow: New ➔ Contacted ➔ Qualified ➔ Converted / Lost
│   ├── Transition protection: Invalid jumps (e.g. New directly to Converted) blocked
│   └── Lead Conversion: 1-click wizard converts Qualified lead to Customer and creates linked Opportunity
│
├── Opportunity & Pipeline Management
│   ├── Pipeline stages: Qualification, Proposal, Negotiation, Won, Lost
│   ├── Inline stage progression menu with instant recalculation
│   ├── Weighted Pipeline calculation: (Amount × Probability) / 100
│   └── Strict validation: Amount > 0, Probability 0-100%, Close date cannot be in the past
│
├── Follow-Up Management
│   ├── Channels: Call, Meeting, Email, Demo
│   ├── Rule: New follow-up date cannot be earlier than today
│   ├── Overdue indicators & 1-click "Complete" action
│   └── Status: Planned, Completed, Missed, Cancelled
│
├── Activity Logging
│   ├── Call, Meeting, Email, and Task logs
│   ├── Linked to Customer and/or Lead
│   └── Search and filter by activity type and status
│
├── Reports & Analytics
│   ├── Stage-wise Pipeline Report
│   ├── Lead Conversion & Win/Loss Outcome Report
│   ├── Customer Master Directory
│   ├── Follow-Up Execution Report
│   └── Print / PDF Export Friendly Layout
│
├── User & Access Administration (Admin Only)
│   ├── User listing with account & lockout status
│   ├── Manual 1-click Account Unlock for locked out users
│   ├── Role changes (Admin / Manager / SalesExecutive)
│   ├── Account activation / deactivation toggles
│   └── Administrative password resets
│
└── Security & Audit Logs (Admin Only)
    ├── Append-only event history (Login, Failed Login, Lockout, Logout, Create, Update, Delete, Role Change)
    ├── Filters: User, Entity Name, Action, Date Range
    ├── Modal viewer displaying before-and-after JSON state diffs
    └── Sensitive data filtering (passwords and hashes never logged)
```

---

## 5. Security Architecture

1. **Authentication:** ASP.NET Core Identity with PBKDF2-based salted hashing. Plain-text passwords are never stored or displayed.
2. **Account Lockout:** Configured for 5 failed attempts (`MaxFailedAccessAttempts = 5`), applying a 10-minute lockout window (`DefaultLockoutTimeSpan = 10 min`). Administrators can immediately unlock accounts from the Admin console.
3. **Authorization:** Global `FallbackPolicy` requiring authentication across all controller actions except explicit `[AllowAnonymous]` login and register endpoints. Role-based restrictions (`[Authorize(Roles = "Admin")]`) and service-layer ownership scoping (`CreatedBy == currentUserId` / `AssignedTo == currentUserId`).
4. **Anti-Forgery:** State-changing MVC forms are protected with `@Html.AntiForgeryToken()` and validated server-side with `[ValidateAntiForgeryToken]`.
5. **REST API Security:** Protected by Identity cookies. Unauthenticated requests to `/api/*` receive HTTP `401 Unauthorized` (rather than a redirect to the HTML login page). Unauthorized requests return HTTP `403 Forbidden`.
6. **Data Integrity & SQL Injection Protection:** Built using EF Core parameterized queries and LINQ APIs; no raw dynamic string concatenation.
7. **DTO Isolation:** Database entity models are never directly exposed via APIs. Custom DTOs are used for all REST endpoints.

---

## 6. REST API Specification

| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| `POST` | `/api/auth/login` | Authenticate user and receive session | Public |
| `POST` | `/api/auth/logout` | Terminate session | Authenticated |
| `GET` | `/api/customers` | Scoped list and search of customers | Authenticated |
| `GET` | `/api/customers/{id}` | Retrieve customer by ID | Authenticated |
| `POST` | `/api/customers` | Create new customer master record | Authenticated |
| `PUT` | `/api/customers/{id}` | Update existing customer record | Authenticated |
| `DELETE`| `/api/customers/{id}` | Soft-deactivate customer | Authenticated |
| `GET` | `/api/leads` | Scoped list and search of leads | Authenticated |
| `POST` | `/api/leads` | Create new lead record | Authenticated |
| `GET` | `/api/opportunities` | Scoped list of sales opportunities | Authenticated |
| `POST` | `/api/opportunities` | Create new opportunity | Authenticated |
| `GET` | `/api/followups` | Scoped list of planned follow-ups | Authenticated |
| `POST` | `/api/followups` | Schedule new follow-up | Authenticated |
| `GET` | `/api/reports/pipeline` | Aggregated stage-wise pipeline metrics | Manager / Admin |

---

## 7. Business Validation Rules

| Rule | Validation Category | Enforced Message / Behavior |
|---|---|---|
| **Opportunity Amount** | Numeric & Business | Must be > 0. Rejects with `"Opportunity Amount must be greater than 0."` |
| **Probability** | Numeric & Business | Must be 0–100. Rejects with `"Probability must be between 0 and 100."` |
| **Expected Close Date** | Date & Business | Cannot be in past for active deals: `"Expected Close Date cannot be in the past."` |
| **Follow-Up Date** | Date & Business | Cannot be earlier than today: `"Follow-up date cannot be earlier than today."` |
| **Phone Number** | Phone & Format | 10-digit Indian mobile regex: `^[6-9]\d{9}$` |
| **Customer Email/Phone/Code** | Uniqueness | Server-side duplicate checks return user-friendly conflict errors |
| **Lead Status Workflow** | State Machine | Prevents illegal state transitions (e.g. New cannot jump to Converted) |

---

## 8. Verification & Acceptance Testing

An automated verification test script is included at the project root (`test_crm.ps1`). It boots the compiled server and automatically tests:
- Protected page redirection for anonymous users
- HTTP 401 response for unauthenticated `/api/*` requests
- User authentication and session cookie establishment
- Role scoping between Admin and SalesExecutive accounts
- Server-side rejection of negative Opportunity amounts
- Server-side rejection of Probability > 100
- Server-side rejection of past Follow-up dates
- Automatic generation of AuditLog entries upon CRUD operations
