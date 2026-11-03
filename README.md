# AcxiomCRM - Enterprise Customer Lifecycle Management System

**Candidate:** Ashritha  
**Registration / Student ID:** 2023007299  
**Submission Repository:** [Ashritha1145/AcxiomCRM-ASHRITHA-2023007299-](https://github.com/Ashritha1145/AcxiomCRM-ASHRITHA-2023007299-)  
**Project Assignment:** AcxiomCRM Full-Stack Enterprise CRM  

---

## 1. Executive Summary & Architecture

AcxiomCRM is an enterprise-grade Customer Relationship Management (CRM) platform covering the entire customer-sales lifecycle from prospect lead capture through qualification, conversion, opportunity pipeline tracking, scheduled follow-ups, activity logs, and executive analytics.

The application follows an **Enterprise Layered Architecture**:
- **Presentation Layer:** ASP.NET Core 8 MVC (Razor Views), Bootstrap 5.3, Bootstrap Icons, Unobtrusive Client Validation, and Chart.js 4.4.
- **Application & Service Coordination Layer:** Typed service interfaces (`ICustomerService`, `ILeadService`, `IOpportunityService`, `IFollowUpService`, `IActivityService`, `IDashboardService`, `IAuditService`) enforcing ownership scoping and business workflows.
- **Domain & Business Layer:** Strongly-typed CRM domain models, custom validation attributes, regex validations, and state machine transitions.
- **Data Access Layer:** Entity Framework Core 8 with SQLite database (zero-configuration, code-first migrations, auto-seeded on initial startup).
- **Security & Authorization Layer:** ASP.NET Core Identity with PBKDF2 salted password hashing, 5-attempt account lockout, session cookies, and role-based access control.

```
AcxiomCRM Architecture
│
├── Controllers/            # MVC Web Controllers with [Authorize] & [ValidateAntiForgeryToken]
│   └── Api/                # REST API Controllers returning pure DTOs (no EF entity leaks)
├── Models/                 # Domain Entities (Customer, Lead, Opportunity, FollowUp, Activity, AuditLog, ApplicationUser)
├── ViewModels/             # View presentation models with DataAnnotations and error messaging
├── DTOs/                   # Data Transfer Objects for API request and response isolation
├── Data/                   # EF Core ApplicationDbContext and automated DbInitializer seeder
├── Services/               # Business logic, audit logging, and role scoping service layer
└── Views/                  # Razor Views styled with Bootstrap 5 and Chart.js dashboards
```

---

## 2. Technology Stack

| Layer | Component | Version / Details |
|---|---|---|
| **Framework** | ASP.NET Core MVC | .NET 8.0 (LTS) |
| **ORM / Data Access** | Entity Framework Core | 8.0.11 (Code-First) |
| **Database** | SQLite | Embedded, zero-configuration |
| **Authentication & Identity** | ASP.NET Core Identity | PBKDF2 adaptive password hashing, cookie auth |
| **Front-End Styling** | Bootstrap & Icons | Bootstrap 5.3, Bootstrap Icons 1.11 |
| **Client Validation** | jQuery Unobtrusive | jQuery 3.7 + jQuery Validation 1.19 |
| **Data Visualization** | Chart.js | Chart.js 4.4 (local offline bundle) |

---

## 3. Seeded Accounts & Role-Based Authorization

The application automatically seeds 3 distinct role personas with realistic enterprise demo data upon first execution.

| Role | Email Address | Password | Privileges & Access Scope |
|---|---|---|---|
| **Admin** | `admin@acxiom.com` | `Test@1234` | Full unrestricted administration: User & Role management, Account Unlock, Audit Log viewer, all CRM records and executive reports. |
| **Manager** | `manager@acxiom.com` | `Test@1234` | Team oversight: View team pipeline, all customer accounts, leads, opportunities, follow-ups, and conversion analytics. |
| **SalesExecutive** | `sales@acxiom.com` | `Test@1234` | Least-privilege scoping: Can only view, edit, and access records where `CreatedBy == currentUserId` or `AssignedTo == currentUserId`. Manual URL tampering returns 404/403. |

> **Password Policy Enforced:** Minimum 8 characters, at least 1 uppercase letter, 1 lowercase letter, 1 numeric digit, and 1 special symbol.  
> **Account Lockout Policy:** 5 consecutive failed login attempts trigger a 10-minute security lockout. Administrators can unlock accounts immediately via the Admin Console.  
> **Quick-Fill Buttons:** The Sign-In screen features 1-click credential filler buttons for rapid evaluator testing.

---

## 4. How to Run the Application

### Option A: Using Windows Runner Scripts (Fastest)
Double-click **`run.bat`** (or execute `.\run.ps1` in PowerShell).

### Option B: Using .NET CLI
```powershell
cd AcxiomCRM
dotnet run
```

### Option C: If `dotnet` is not in your current shell session PATH
```powershell
$env:PATH = "C:\Users\AshuS\AppData\Local\Microsoft\dotnet;$env:PATH"
cd AcxiomCRM
dotnet run
```

Once running, navigate your web browser to:
**`http://localhost:5218`** (or the URL printed in the terminal).

---

## 5. Functional Modules & Capabilities

### 5.1 Dashboard Analytics
- **KPI Summary Cards:** Total Customers, Total Leads, Open Leads, Active Opportunities, Won Deals, Lost Deals, Total Pipeline Value, and Weighted Pipeline Value.
- **Temporal Filters:** Today, This Week, This Month, and All Time.
- **Chart.js Visualizations:**
  1. *Opportunity Pipeline by Stage* (Bar Chart)
  2. *Lead Status Breakdown* (Doughnut Chart)
  3. *Monthly Closed-Won Revenue Trend* (Line Chart)
- **Actionable Widget:** Upcoming and overdue follow-up queue with 1-click completion.

### 5.2 Customer Management
- **Search & Filter:** Search across Customer Name, Code, Email, Phone, Company, and Status.
- **360-Degree Profile:** Displays customer master data alongside linked active deals, scheduled follow-ups, and interaction timeline.
- **Duplicate Prevention:** Server-side duplicate checks for Email, Phone number, and Customer Code.
- **Soft Deactivation:** Safe deactivation preserves audit trails without cascading data loss.

### 5.3 Lead Management & Conversion Workflow
- **State Machine Workflow:** Enforces valid lifecycle stages: `New` ➔ `Contacted` ➔ `Qualified` ➔ `Converted` / `Lost`.
- **Workflow Protection:** Prevents illegal jumps (e.g. `New` cannot jump directly to `Converted`).
- **1-Click Lead Conversion Wizard:** Converting a `Qualified` lead provisions a Customer master record and optionally attaches an active sales Opportunity with target value and expected close date.

### 5.4 Opportunity & Pipeline Management
- **Pipeline Stages:** `Qualification`, `Proposal`, `Negotiation`, `Won`, `Lost`.
- **Mathematical Formulations:**
  $$\text{Weighted Pipeline Value} = \frac{\text{Opportunity Amount} \times \text{Probability}}{100}$$
- **Validation Rules:**
  - Opportunity Amount must be strictly greater than 0: `"Opportunity Amount must be greater than 0."`
  - Probability must be between 0 and 100: `"Probability must be between 0 and 100."`
  - Expected Close Date cannot be in the past: `"Expected Close Date cannot be in the past."`

### 5.5 Follow-Up & Activity Tracking
- **Channels:** Phone Calls, Meetings, Email Correspondence, and Product Demos.
- **Date Protection:** Follow-up date cannot be earlier than today: `"Follow-up date cannot be earlier than today."`
- **Audit & Status:** Statuses include `Planned`, `Completed`, `Missed`, and `Cancelled`.

### 5.6 User & Role Administration (Admin Only)
- User directory with status badges and lockout indicators.
- 1-click **Account Unlock** mechanism.
- Role management (`Admin`, `Manager`, `SalesExecutive`).
- Administrative password reset flows.

### 5.7 Audit Trail & Governance (Admin Only)
- Append-only security logging capturing `Login`, `Failed Login`, `Lockout`, `Logout`, `Create`, `Update`, `Delete`, `Role Change`, and `Lead Conversion`.
- Before-and-after JSON state diffs viewable via a modal inspector.
- Exclusion of passwords, password hashes, and sensitive authentication tokens.

---

## 6. REST API Specification

All REST APIs use dedicated Data Transfer Objects (DTOs) and return standard HTTP status codes (`200 OK`, `201 Created`, `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `409 Conflict`).

| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| `POST` | `/api/auth/login` | Authenticate user credentials and issue cookie | Anonymous |
| `POST` | `/api/auth/logout` | Terminate session and clear cookie | Authenticated |
| `GET` | `/api/customers` | Query and search customer master records | Authenticated (Scoped) |
| `GET` | `/api/customers/{id}` | Retrieve customer record by ID | Authenticated (Scoped) |
| `POST` | `/api/customers` | Create a new customer master record | Authenticated |
| `PUT` | `/api/customers/{id}` | Update existing customer details | Authenticated (Scoped) |
| `DELETE`| `/api/customers/{id}` | Soft-deactivate customer | Authenticated (Scoped) |
| `GET` | `/api/leads` | Query and search prospect leads | Authenticated (Scoped) |
| `POST` | `/api/leads` | Create a new prospect lead | Authenticated |
| `GET` | `/api/opportunities` | Query active sales opportunities | Authenticated (Scoped) |
| `POST` | `/api/opportunities` | Create a new sales opportunity | Authenticated |
| `GET` | `/api/followups` | Retrieve scheduled follow-ups | Authenticated (Scoped) |
| `POST` | `/api/followups` | Schedule a new follow-up engagement | Authenticated |
| `GET` | `/api/reports/pipeline` | Aggregate stage-wise pipeline metrics | Manager / Admin |

> **API Security Note:** In compliance with assignment requirements, unauthenticated requests to `/api/*` return **`401 Unauthorized`** HTTP status codes directly instead of redirecting to the HTML login view.

---

## 7. Business Validation Matrix

| Field / Area | Validation Requirement | Category | Error Response |
|---|---|---|---|
| **Customer Name** | Required, max 100 characters | Required & Length | `"Customer Name is required."` |
| **Customer Email** | Valid RFC email, must be unique | Format & Business | `"Enter a valid email address."` / duplicate conflict |
| **Mobile Phone** | Indian 10-digit mobile `^[6-9]\d{9}$`, unique | Regex & Business | `"Enter a valid 10-digit Indian mobile number."` |
| **Lead Name** | Required, max 100 characters | Required & Length | `"Lead Name is required."` |
| **Lead Value** | Numeric range 0 to 100,000,000 | Numeric | `"Expected Value must be between 0 and 100,000,000."` |
| **Opportunity Amount** | Strictly positive numeric | Numeric & Business | `"Opportunity Amount must be greater than 0."` |
| **Probability** | Integer range 0 through 100 | Range | `"Probability must be between 0 and 100."` |
| **Expected Close Date** | Cannot be in past for active deals | Date & Business | `"Expected Close Date cannot be in the past."` |
| **Follow-Up Date** | Cannot be earlier than current date | Date & Business | `"Follow-up date cannot be earlier than today."` |
| **Lead Transitions** | Valid sequential workflow states | State Machine | `"Invalid status transition from '{0}' to '{1}'."` |

---

## 8. Automated Acceptance Verification

The project root includes an automated end-to-end acceptance script (`test_crm.ps1`) that executes the 14 mandatory acceptance test cases against the live running server:

```powershell
powershell -ExecutionPolicy Bypass -File .\test_crm.ps1
```

**Verification Results:**
- ✅ **Test 1:** Anonymous access to protected `/Dashboard` redirects to `/Account/Login`.
- ✅ **Test 2:** Anonymous access to `/api/customers` returns HTTP 401 Unauthorized (no HTML redirect).
- ✅ **Test 3:** Authentication via `/api/auth/login` returns successful user session and role claims.
- ✅ **Test 4:** Authenticated GET `/api/customers` retrieves formatted customer DTOs.
- ✅ **Test 5:** Authenticated GET `/api/reports/pipeline` returns stage-wise breakdown and totals.
- ✅ **Test 6:** Creating a customer generates record and inserts an `AuditLogs` entry.
- ✅ **Test 7:** Opportunity with `Amount <= 0` is rejected by server with HTTP 400 Bad Request.
- ✅ **Test 8:** Opportunity with `Probability > 100` is rejected by server with HTTP 400 Bad Request.
- ✅ **Test 9:** Follow-up dated in the past is rejected by server with HTTP 400 Bad Request.
- ✅ **Test 10:** Role scoping confirms SalesExecutive accounts are restricted to their own data scope.
