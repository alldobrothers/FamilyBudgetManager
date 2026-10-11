# Family Budget Manager (ASP.NET Core 8 MVC)

A modern, secure, and production-oriented household financial management web application built with C#, ASP.NET Core 8 MVC, Entity Framework Core, ASP.NET Core Identity, and Bootstrap 5.

Designed for multi-family household budgeting, collaborative expense tracking, debt & loan repayment ledgers, recurring bill schedules with automated occurrence generation, savings goals, and real-time interactive financial dashboards.

---

## 🌟 Key Features

- **Multi-Family & Multi-Tenant Isolation:** Users can create families, switch between multiple families, or accept cryptographically secure email invitations.
- **Role-Based Access Control:** Granular permissions across 4 roles:
  - **Family Owner:** Full administrative control, ownership transfer, member management, and family deletion.
  - **Family Admin:** Manage categories, budgets, bills, and permitted family settings.
  - **Family Member:** Record income, quick-add expenses, split expenses, record repayments, and view dashboards.
  - **Viewer:** Read-only access to authorized financial data.
- **Family Financial Dashboard:**
  - Real-time KPI summary cards: Total Income, Total Expenses, Net Cash Flow, Remaining Budget, Outstanding Debt, Upcoming Bills, Pending Obligations, and Estimated Available Cash.
  - Responsive Chart.js visualizations: Income vs. Expense, Monthly Spending Trend, Category Doughnut Breakdown, and Member-wise Spending Comparison.
- **Expense Management:**
  - Prominent "Quick Add Expense" workflow.
  - Multi-category support (default system categories + custom family categories).
  - Expense splitting (Equal, Fixed Amount, Percentage) with beneficiary tracking.
  - Duplicate-entry detection warnings, receipt image/document uploads, and search/filter pagination.
- **Income Management:**
  - Categorized income tracking (Salary, Business, Freelance, Rental, Gifts, etc.).
  - Recipient member assignment, payment method logging, and CSV export.
- **Debts, Borrowing & Lending Module:**
  - Track both money borrowed (liabilities) and money lent (receivables).
  - Principal, optional interest calculations, due dates, and repayment ledger.
  - Repayment tracking preventing overpayment and recalculating balances from immutable transaction history.
- **Recurring Monthly Bills & Scheduled Occurrences:**
  - Automated recurrence engine (Weekly, Monthly, Quarterly, Half-Yearly, Annually).
  - Background worker service (`RecurringBillHostedService`) automatically generates occurrences and tracks upcoming/overdue statuses.
  - "Mark as Paid" workflow that creates linked expense/payment records and advances to the next billing cycle.
- **Budgets & Savings Goals:**
  - Monthly household budget with category-level allocations.
  - Visual utilization progress bars with configurable warning thresholds (75%, 90%, 100%).
  - Family savings goals with contribution logs and milestone tracking.
- **Security & Privacy by Design:**
  - Full anti-forgery CSRF protection.
  - Strict tenant filtering prevents cross-family ID tampering.
  - Non-executable file upload filters with MIME validation.
  - Sensitive logging redaction and parameter-safe queries via EF Core.

---

## 📋 Prerequisites

- **.NET 8.0 SDK (LTS)** or higher (`dotnet --version`)
- **Operating System:** Windows 10/11 / Windows Server, Linux (Ubuntu/Debian/Alpine), or macOS
- **Database:**
  - **Default (Zero-Config Development):** SQLite (included out-of-the-box, no external server required).
  - **Production:** Microsoft SQL Server (2019+) or PostgreSQL (14+).

---

## 🚀 Quick Start & Local Setup

### 1. Clone or Open the Workspace
```bash
git clone <repository-url>
cd FamilyBudgetManager
```

### 2. Verify SDK & Build the Solution
```bash
dotnet --info
dotnet build /p:UseSharedCompilation=false
```

### 3. Run Automated Tests
```bash
dotnet test
```

### 4. Apply Database Migrations & Seed Default Data
The application automatically ensures the database is created and applies migrations and initial category seed data on first startup. To run migrations manually via CLI:
```bash
dotnet ef database update --project src/FamilyBudget.Infrastructure --startup-project src/FamilyBudget.Web
```

### 5. Launch the Web Application
```bash
dotnet run --project src/FamilyBudget.Web
```
Open your browser and navigate to:
```
https://localhost:5001  or  http://localhost:5000
```

---

## 🛡️ Default Development Credentials

For quick local evaluation, you can register a new account on `/Account/Register` or use the development seed account:
- **Email:** `demo@familybudget.local`
- **Password:** `Demo@123456`
- **Family:** "Sharma Household" (Currency: ₹ INR)

---

## 📂 Project Structure

```
FamilyBudgetManager/
├── src/
│   ├── FamilyBudget.Domain/          # Entities, Enums, Value Objects
│   ├── FamilyBudget.Application/     # Services, DTOs, Financial Calculation Engine
│   ├── FamilyBudget.Infrastructure/  # EF Core DbContext, Identity, Background Worker
│   └── FamilyBudget.Web/             # Controllers, Razor Views, ViewModels, UI Assets
├── tests/
│   └── FamilyBudget.Tests/           # Unit & Integration Tests (xUnit, Moq)
├── PROJECT_PLAN.md                   # Architecture, Milestones, and Decisions
├── DATABASE_DESIGN.md                # Entity Schemas, Relationships, and Data Dictionary
├── DEPLOYMENT.md                     # Production, Hosting, and Cloudflare Setup Guide
└── README.md                         # Setup and Overview
```

---

## 🧪 Testing

The solution includes a comprehensive test suite covering:
1. **Financial Calculations:** Cash flow totals, debt balance recalculation, and expense split validation.
2. **Double-Counting Prevention:** Verification that unpaid bills, debt repayments, and savings contributions do not distort household operating expenses.
3. **Recurring Bills:** Next due date calculations, leap-year handling, and idempotent occurrence generation.
4. **Tenant Isolation:** Cross-family access rejection and membership permissions.

To execute tests:
```bash
dotnet test --logger "console;verbosity=detailed"
```

---

## 🌐 Production & Cloudflare Hosting

Refer to [DEPLOYMENT.md](DEPLOYMENT.md) for full instructions on deploying ASP.NET Core 8 on Windows IIS or Linux systemd behind Cloudflare DNS, WAF, SSL, and edge proxy.
