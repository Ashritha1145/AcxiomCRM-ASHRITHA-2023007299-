# AcxiomCRM: Antigravity Build Plan
Deadline: 12:00 AM. Start: ~8:10 PM. Hard stop for features: **11:10 PM**. Last 50 min = test, fix, submit.

## 0. Locked decisions (put in Antigravity rules / first prompt)
- **Stack:** ASP.NET Core 8 MVC (Razor Views), EF Core, **ASP.NET Core Identity** (no custom password table), Bootstrap 5, jQuery Validation Unobtrusive, Chart.js.
- **DB:** SQL Server LocalDB. If it isn't installed, switch to SQLite right now (same EF code, no setup risk).
- **API auth:** same Identity cookie; for `/api/*` return 401/403 instead of redirects.
- **Folders:** `Controllers/`, `Controllers/Api/`, `Models/`, `ViewModels/`, `DTOs/`, `Data/`, `Services/`, `Views/`.
- **Roles:** `Admin`, `Manager`, `SalesExecutive`.
- **Never:** raw SQL concatenation, password hashes in views/DTOs/logs, EF entities returned from APIs.

## 1. Must-have vs cut line
**Must (this is what gets graded):** Identity login/register/logout + lockout + password policy; 3 roles with server-side `[Authorize]` + ownership scoping; CRUD for Customer, Lead, Opportunity, FollowUp, Activity; client + server validation; all business rules; dashboard cards + 3 Chart.js charts; search/filter on every list; API for Customers/Leads/Opportunities; AuditLog; anti-forgery on POSTs.
**Cut if late:** Reports module, Permissions screen, export, pagination polish, API for follow-ups/reports, global search.

## 2. Timeline
| Time | Phase | Output |
|---|---|---|
| 8:10-8:40 | **P0 Foundation** | Solution, entities, DbContext, Identity, seeded roles + 3 test users, migration, layout with role-based menu |
| 8:40-9:30 | **P1 Customers (template module)** | Full CRUD + validation + search + AuditService + `CustomersApiController` + DTOs |
| 9:30-10:30 | **P2 Leads, Opportunities, Follow-Ups, Activities** | Run as 2-3 parallel agents, each copying the Customers pattern |
| 10:30-11:10 | **P3 Dashboard, Users/Roles, Audit viewer** | KPI cards, charts, admin screens |
| 11:10-11:50 | **P4 Test and fix** | Walk the 14-step acceptance scenario |
| 11:50-12:00 | **P5 Submit** | README, zip/push, submit early |

Tip: use Antigravity's Manager view to run parallel agents in P2 and P3. Do P0 and P1 sequentially so the pattern is set before cloning it.

## 3. Prompts (paste in order)

### Prompt 0: Foundation
```
Create an ASP.NET Core 8 MVC app named AcxiomCRM with ASP.NET Core Identity (IdentityUser extended with FullName, IsActive). Do NOT create a custom password table.
Folders: Controllers, Controllers/Api, Models, ViewModels, DTOs, Data, Services, Views.
Entities (EF Core, code-first):
- Customer: CustomerId, CustomerCode, CustomerName, Email, Phone, CompanyName, Address, City, State, Status, CreatedDate, CreatedBy
- Lead: LeadId, LeadCode, LeadName, Email, Phone, CompanyName, Source, Status, ExpectedValue, CreatedDate, AssignedTo
- Opportunity: OpportunityId, OpportunityName, CustomerId, LeadId?, Amount, Stage, Probability, ExpectedCloseDate, Status, CreatedDate, AssignedTo
- FollowUp: FollowUpId, CustomerId?, LeadId?, FollowUpDate, FollowUpType, Remarks, Status, AssignedTo
- Activity: ActivityId, ActivityType(Call/Meeting/Email/Task), Subject, Description, ActivityDate, CustomerId?, LeadId?, AssignedTo, Status
- AuditLog: AuditLogId, UserId, Action, EntityName, RecordId, OldValue, NewValue, CreatedDate, IpAddress
Add unique indexes on Customer.Email and Customer.Phone. Configure Identity: password min 8 with upper/lower/digit/symbol, lockout 5 failed attempts / 10 min, secure cookie, redirect unauthenticated to /Account/Login, 401/403 (no redirect) for /api paths.
Seed roles Admin, Manager, SalesExecutive and users admin@acxiom.com, manager@acxiom.com, sales@acxiom.com (password Test@1234). Add Register (assigns SalesExecutive), Login, Logout. After login redirect to /Dashboard. Global [Authorize] fallback policy. Layout: Bootstrap 5 responsive navbar whose menu items depend on role. Run migrations and confirm the app starts.
```

### Prompt 1: Customers + shared patterns (template module)
```
Implement Customer management end-to-end as the reference pattern: Index (search by name/email/phone/company, pagination), Create, Edit, Details, Delete (soft delete via Status=Inactive, with confirm dialog).
- ViewModels with DataAnnotations: Required, StringLength, EmailAddress, Phone regex (10-digit Indian mobile ^[6-9]\d{9}$). Show asp-validation messages; include _ValidationScriptsPartial for client-side validation.
- Server side: check ModelState, check duplicate Email/Phone/CustomerCode and return friendly errors, never trust client.
- All POST actions use [ValidateAntiForgeryToken].
- Scope: SalesExecutive sees only customers where CreatedBy == current user; Manager and Admin see all. Enforce in a service layer (CustomerService), not just in the UI. Direct URL access to another user's record returns 403/404.
- Create IAuditService/AuditService (logs UserId, Action, EntityName, RecordId, OldValue/NewValue as JSON, CreatedDate, IpAddress) and call it on login, failed login, lockout, create, update, delete.
- Add Api/CustomersApiController: GET list, GET {id}, POST, PUT {id}, DELETE {id} using CustomerDto/CreateCustomerDto (no entity leaks), [ApiController] validation, status codes 200/201/400/401/403/404/409.
```

### Prompt 2a: Leads (parallel agent)
```
Following the exact Customers pattern (service layer, ViewModels, validation, anti-forgery, audit, role scoping by AssignedTo), implement Lead management: Create/Edit/Details/Delete, search by name/company/status/assigned user.
Rules: LeadName required; ExpectedValue range 0-100000000; Status must be from the valid set New, Contacted, Qualified, Unqualified, Converted, Lost (validate on server against the set); email/phone same rules as customers; invalid status transitions rejected (e.g. New cannot jump to Converted).
Lead Conversion: action on Qualified leads that creates a Customer (and optionally an Opportunity), sets Lead.Status=Converted, and writes an audit entry. Add Api/LeadsApiController (GET list, POST) with DTOs.
```

### Prompt 2b: Opportunities (parallel agent)
```
Following the Customers pattern, implement Opportunity management with search by name/customer/stage/status and a simple pipeline view grouped by stage (Qualification, Proposal, Negotiation, Won, Lost).
Server AND client validation: Amount > 0 for active opportunities (never negative), Probability 0-100, ExpectedCloseDate cannot be in the past for active opportunities. Exact messages: "Opportunity Amount must be greater than 0.", "Probability must be between 0 and 100.", "Expected Close Date cannot be in the past." Customer FK must exist. Audit all changes, including stage changes. Add Api/OpportunitiesApiController (GET list, POST) with DTOs.
```

### Prompt 2c: Follow-Ups + Activities (parallel agent)
```
Implement FollowUp (schedule, complete, pending list, edit) and Activity (Call/Meeting/Email/Task CRUD) with search/filter (date, status, assigned user, related customer/lead; activity type/date/status).
Rule: new/planned follow-up date cannot be earlier than today (server + client), message "Follow-up date cannot be earlier than today." Statuses Planned, Completed, Missed, Cancelled. Complete action sets status and audits it. SalesExecutive sees only own items. Anti-forgery + audit on all writes.
```

### Prompt 3a: Dashboard
```
Build /Dashboard scoped by role (SalesExecutive = own records, Manager/Admin = all). Cards: Total Customers, Total Leads, Open Leads, Total Opportunities, Open Opportunities, Won, Lost, Total Pipeline Value (sum of open opportunity amounts). Chart.js charts: Lead Status (New/Contacted/Qualified/Lost/Converted) doughnut, Opportunity Pipeline by stage bar, Monthly Sales line/bar. Data comes from a server-side DashboardService that applies the role scope, fetched via JSON endpoint.
```

### Prompt 3b: Admin screens (parallel agent)
```
Admin-only: Users list/search, create, edit, activate/deactivate, change role (single role), unlock account; Roles list. Audit every role change. Audit Log viewer (Admin only) with filters by user, action, entity, date range and pagination. Never display password hashes. Manager gets read-only user list only if trivial; otherwise Admin only.
```

### Prompt 4: Verify
```
Walk through this scenario and fix every failure: (1) anonymous access to CRM pages is blocked; (2) register/login lands on Dashboard; (3) invalid email/phone blocked client-side; (4) same data via crafted POST (curl/disable JS) rejected server-side; (5) Opportunity amount<=0, probability>100, past close date all rejected; (6) past follow-up rejected; (7) SalesExecutive cannot open another user's record by changing the URL or API id; (8) Manager sees team data; (9) Admin sees users/roles/audit; (10) create/update/delete each write an AuditLog row; (11) GET /api/customers returns JSON when logged in and 401 otherwise; (12) 5 wrong passwords lock the account; (13) dashboard cards and charts render. Then write README.md.
```

## 4. Submission checklist
- [ ] `dotnet run` works on a clean clone (migrations apply on startup or documented)
- [ ] README: stack, run steps, test credentials (admin/manager/sales @acxiom.com, Test@1234), feature list, API endpoint table
- [ ] No connection strings with real secrets; passwords only via Identity
- [ ] `[ValidateAntiForgeryToken]` on every MVC POST
- [ ] Screenshots of: validation errors, dashboard, audit log, API JSON
- [ ] Zip without `bin/` and `obj/`, or push to GitHub, and **submit by 11:50 PM**

## 5. Risk notes
- If any phase runs over by 20+ minutes, drop from the cut list in section 1 rather than squeezing validation or security. Those are graded as mandatory.
- Review each agent's diff for server-side scoping; agents often add UI filters but forget service-level checks.
- Commit after every phase so a bad agent run can be rolled back.
