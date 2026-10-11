# Family Budget Manager — Project Plan & Architecture

## 1. Project Overview & Role
**Project Name:** Family Budget Manager  
**Target Platform:** ASP.NET Core 8.0 MVC (LTS)  
**Architecture:** Layered Clean / Onion Architecture with Dependency Injection  
**Primary Currency Default:** Indian Rupee (INR - ₹), with multi-currency support (USD $, EUR €, GBP £, etc.)  
**Security Model:** ASP.NET Core Identity, Cryptographic Tokens, Tenant/Family Data Isolation, CSRF Tokens, Anti-Forgery Validation.

---

## 2. Technology Stack & Hosting Architecture

### Core Stack
- **Framework:** .NET 8.0 (LTS) ASP.NET Core MVC
- **Data Access:** Entity Framework Core 8.0
- **Database Providers:**
  - **Development / Portable / Integration Testing:** SQLite (self-contained, zero-configuration, WAL mode enabled, foreign key constraints enforced).
  - **Production:** Microsoft SQL Server or PostgreSQL (supported via EF Core provider configuration in `appsettings.json`).
- **Identity & Security:** ASP.NET Core Identity with password hashing, lockout policies, cryptographic invitation hashing, and custom authorization policies.
- **Frontend & Styling:**
  - Semantic HTML5, CSS3, modern Vanilla JavaScript (no bloat).
  - Bootstrap 5.3 + Bootstrap Icons for responsive, accessible, mobile-first design.
  - Chart.js for responsive financial visualizations (income vs. expense, monthly trend, category doughnut, member breakdown).
- **Background Tasks:** `BackgroundService` hosted worker for recurring bill occurrence calculation and notification dispatch.
- **Testing:** xUnit, Moq, EF Core InMemory / SQLite for financial engine, recurring bill, and authorization test suites.

### Hosting Compatibility Decision (Cloudflare & .NET)
- Cloudflare Pages and Cloudflare Workers execute V8 edge isolates; they do **not** run the ASP.NET Core Kestrel MVC server process directly.
- **Architectural Solution:**
  - ASP.NET Core MVC runs on a compatible .NET 8 runtime host (Windows Server / IIS, Linux VM with Kestrel + systemd/Nginx, or Docker Container on Azure App Service / AWS / Render / Railway).
  - **Cloudflare** acts as the authoritative reverse proxy, providing DNS management, SSL/TLS edge termination, DDoS mitigation, Web Application Firewall (WAF), rate limiting, and caching of safe static assets (`/css/*`, `/js/*`, `/images/*`).
  - Dynamic application routes (`/Dashboard`, `/Expense/*`, `/Income/*`, `/Family/*`) bypass edge cache (`Cache-Control: private, no-store`) to ensure user financial privacy.

---

## 3. Layered Solution Architecture

```
e:\Budget
├── src/
│   ├── FamilyBudget.Domain/          # Core Domain Entities, Enums, Value Objects, Domain Exceptions
│   │   ├── Entities/
│   │   ├── Enums/
│   │   └── Common/
│   ├── FamilyBudget.Application/     # Interfaces, DTOs, Calculation Engine, Services, Authorization Handlers
│   │   ├── Common/
│   │   ├── DTOs/
│   │   ├── Interfaces/
│   │   └── Services/
│   ├── FamilyBudget.Infrastructure/  # EF Core DbContext, Configurations, Identity, Storage, Background Services
│   │   ├── Data/
│   │   ├── Identity/
│   │   ├── Services/
│   │   └── BackgroundJobs/
│   └── FamilyBudget.Web/             # Controllers, Razor Views, ViewModels, Filters, Static Assets, Program.cs
│       ├── Controllers/
│       ├── Models/
│       ├── Views/
│       ├── Filters/
│       └── wwwroot/
└── tests/
    └── FamilyBudget.Tests/           # Unit, Integration, Calculation, and Authorization Tests
```

---

## 4. Implementation Milestones

### Milestone 1: Architecture, Setup, Database Design, & UI Design System
- Establish 5-project solution and NuGet dependencies.
- Create `PROJECT_PLAN.md`, `DATABASE_DESIGN.md`, and initial `README.md`.
- Establish comprehensive domain entities, relationships, EF Core DbContext with precision decimal types for money (`decimal(18,2)`), unique constraints, and cascade delete rules.
- Build clean, accessible modern UI design system (custom CSS variables, typography, sidebar/topbar layout, responsive navigation, badges, toast notifications).

### Milestone 2: Authentication, Family Onboarding, Memberships, & Roles
- ASP.NET Core Identity integration with custom `ApplicationUser` entity.
- Registration, sign-in, sign-out, profile editing, password management.
- First-time onboarding wizard: Create Family or Join Family via secure invitation.
- Multi-family support with active family switcher saved in secure user session/cookie.
- Role-based authorization: **Family Owner**, **Family Admin**, **Family Member**, and **Viewer**.
- Secure invitation engine: cryptographically hashed tokens, expiration handling, acceptance and revocation flows.

### Milestone 3: Family Dashboard, Income, Expenses, Categories, & Member Tracking
- Pre-seeded default financial categories + custom family categories.
- Real-time family dashboard:
  - Metric summary cards: Total Income, Total Expenses, Net Cash Flow, Remaining Budget, Outstanding Debt, Upcoming Bills, Pending Obligations, Estimated Available Cash.
  - Interactive Chart.js charts: Income vs Expense, Spending by Category, Monthly Trend, Member Spending Comparison.
  - Period selector (Monthly, Date Range) with currency formatting (INR ₹ default).
- Expense Management: Quick Add modal, full CRUD, categories, payment methods, merchant tracking, receipt uploads, duplicate detection warning.
- Income Management: CRUD, sources (Salary, Freelance, Business, etc.), recipient member, payment methods, CSV export.
- Family Members directory: personal spending summaries, contributions, expense splitting attribution.

### Milestone 4: Budgets, Debts, Borrowing, Lending, Repayments, & Pending Obligations
- Budget Engine: Household monthly budgets, category-level allocations, spending thresholds (75%, 90%, 100%), warning badges, projected month-end forecast.
- Debts & Borrowing Module:
  - Money borrowed (liabilities) and money lent (receivables).
  - Principal, interest rate (optional), due dates, status tracking.
  - Repayment ledger: partial and full repayments with balance recalculation.
- Pending Payments & Reimbursements:
  - Unpaid bills, member reimbursements, owed amounts, settlement status.
  - Distinct separation between pending obligations and actual recorded cash expenses.

### Milestone 5: Recurring Bills, Scheduling, & Savings Goals
- Recurring Bills Module:
  - Bill templates (frequencies: Weekly, Monthly, Quarterly, Half-Yearly, Annually).
  - Occurrence generator preventing duplicates.
  - Mark-paid flow that creates corresponding payment/expense transactions.
- Automated `RecurringBillHostedService` background worker to evaluate due dates and generate occurrences.
- Savings Goals Module:
  - Goal target amount, target date, contribution ledger, progress visualizer.

### Milestone 6: Reports, Analytics, Data Export, Notifications, & Preferences
- Comprehensive reporting suite:
  - Monthly Income vs. Expenses, Category Breakdown, Member Breakdown, Cash Flow, Budget vs Actual, Debt Statement, Bill History.
  - CSV export for all transaction tables.
- In-App Notification Center:
  - Unread badge counter, notification drawer, deep links to relevant items, mark as read / mark all read.
  - Automatic notifications on overdue bills, high budget utilization, debt reminders.
- Family and User Preferences:
  - Currency selection, dark/light theme toggle, month start day, timezone.

### Milestone 7: Security Hardening, Automated Testing, & Quality Assurance
- Automated test suite in `FamilyBudget.Tests`:
  - Financial calculation tests (accurate cash flow, no double counting, split math).
  - Recurring bill occurrence generation and month-end boundary tests.
  - Tenant/family isolation security tests (prevent cross-family access).
  - Membership and permission authorization tests.
- Security audit: CSRF anti-forgery, parameterized queries, non-executable file uploads, tenant isolation filters.

### Milestone 8: Deployment Documentation, Backup Strategy, & Production Release
- Create `DEPLOYMENT.md` covering Windows IIS, Linux systemd, Docker, and Cloudflare reverse proxy configuration.
- Safe `.env.example` and `appsettings.json` templates without plaintext secrets.
- Database migration and automated backup/restore scripts.
- Verification of complete acceptance criteria.

---

## 5. Financial Calculation Principles & Anti-Double-Counting Rules
1. **Cash Flow vs. Accrual / Obligations:**
   - A pending bill or payment obligation is **not** an expense until marked paid.
   - Borrowing money is recorded as a liability, not income.
   - Lending money is recorded as a receivable/asset, not an expense.
2. **Repayments:**
   - Principal repayment reduces debt liability; it is not counted as household operating expense.
   - Interest portion of repayment is tracked separately as a financing expense.
3. **Internal Transfers & Reimbursements:**
   - Money transferred between family members does not increase total family income.
   - Reimbursement to a family member who paid on behalf of the family settles an internal debt rather than duplicating the original expense.
4. **Expense Splitting:**
   - The total split amount across beneficiaries must match the master expense total (`Sum(Splits) == Expense.Amount`).
5. **Precision & Rounding:**
   - All financial amounts utilize .NET `decimal` (mapped to `decimal(18,2)`) to eliminate floating-point precision errors.
