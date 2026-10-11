-- Cloudflare D1 Schema for Family Budget Manager

-- 1. Users & Auth
CREATE TABLE IF NOT EXISTS Users (
    Id TEXT PRIMARY KEY,
    Email TEXT NOT NULL UNIQUE,
    FullName TEXT NOT NULL,
    DisplayName TEXT NOT NULL,
    PasswordHash TEXT NOT NULL,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
);

-- 2. Families & Settings
CREATE TABLE IF NOT EXISTS Families (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    CurrencyCode TEXT NOT NULL DEFAULT 'INR',
    CurrencySymbol TEXT NOT NULL DEFAULT '₹',
    Country TEXT DEFAULT 'India',
    TimeZoneId TEXT DEFAULT 'India Standard Time',
    MonthStartDay INTEGER NOT NULL DEFAULT 1,
    MonthlyBudget REAL NOT NULL DEFAULT 0,
    Description TEXT,
    CreatedByUserId TEXT NOT NULL,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS FamilyMemberships (
    Id TEXT PRIMARY KEY,
    FamilyId TEXT NOT NULL,
    UserId TEXT NOT NULL,
    Role INTEGER NOT NULL DEFAULT 3, -- 1: Owner, 2: Admin, 3: Member, 4: Viewer
    JoinedAt TEXT NOT NULL DEFAULT (datetime('now')),
    IsActive INTEGER NOT NULL DEFAULT 1,
    CanApproveExpenses INTEGER NOT NULL DEFAULT 0,
    CanManageBills INTEGER NOT NULL DEFAULT 1,
    CanViewDebts INTEGER NOT NULL DEFAULT 1,
    CanExportReports INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY(FamilyId) REFERENCES Families(Id),
    FOREIGN KEY(UserId) REFERENCES Users(Id)
);

CREATE TABLE IF NOT EXISTS FamilySettings (
    FamilyId TEXT PRIMARY KEY,
    EnableExpenseApprovals INTEGER NOT NULL DEFAULT 0,
    BudgetWarningThreshold1 REAL NOT NULL DEFAULT 75.0,
    BudgetWarningThreshold2 REAL NOT NULL DEFAULT 90.0,
    NotifyOnOverdueBills INTEGER NOT NULL DEFAULT 1,
    DarkModeDefault INTEGER NOT NULL DEFAULT 0,
    UpdatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY(FamilyId) REFERENCES Families(Id)
);

CREATE TABLE IF NOT EXISTS FamilyInvitations (
    Id TEXT PRIMARY KEY,
    FamilyId TEXT NOT NULL,
    InvitedEmail TEXT NOT NULL,
    InvitedRole INTEGER NOT NULL DEFAULT 3,
    TokenHash TEXT NOT NULL,
    InvitedByUserId TEXT NOT NULL,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    ExpiresAt TEXT NOT NULL,
    Status INTEGER NOT NULL DEFAULT 0, -- 0: Pending, 1: Accepted, 2: Expired, 3: Revoked
    AcceptedByUserId TEXT,
    AcceptedAt TEXT,
    FOREIGN KEY(FamilyId) REFERENCES Families(Id)
);

-- 3. Categories
CREATE TABLE IF NOT EXISTS FinancialCategories (
    Id TEXT PRIMARY KEY,
    FamilyId TEXT,
    Name TEXT NOT NULL,
    Description TEXT,
    ColorHex TEXT NOT NULL DEFAULT '#6366f1',
    IconClass TEXT NOT NULL DEFAULT 'bi-tag',
    Type INTEGER NOT NULL, -- 1: Expense, 2: Income
    IsSystemDefault INTEGER NOT NULL DEFAULT 0
);

-- 4. Budgets
CREATE TABLE IF NOT EXISTS Budgets (
    Id TEXT PRIMARY KEY,
    FamilyId TEXT NOT NULL,
    PeriodYear INTEGER NOT NULL,
    PeriodMonth INTEGER NOT NULL,
    TotalPlannedBudget REAL NOT NULL DEFAULT 0,
    RolloverAmount REAL NOT NULL DEFAULT 0,
    Notes TEXT,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY(FamilyId) REFERENCES Families(Id)
);

CREATE TABLE IF NOT EXISTS BudgetCategories (
    Id TEXT PRIMARY KEY,
    BudgetId TEXT NOT NULL,
    CategoryId TEXT NOT NULL,
    PlannedAmount REAL NOT NULL DEFAULT 0,
    FOREIGN KEY(BudgetId) REFERENCES Budgets(Id),
    FOREIGN KEY(CategoryId) REFERENCES FinancialCategories(Id)
);

-- 5. Expenses & Splits
CREATE TABLE IF NOT EXISTS ExpenseTransactions (
    Id TEXT PRIMARY KEY,
    FamilyId TEXT NOT NULL,
    Title TEXT NOT NULL,
    Amount REAL NOT NULL,
    CategoryId TEXT NOT NULL,
    PaidByMemberId TEXT NOT NULL,
    ExpenseDate TEXT NOT NULL DEFAULT (datetime('now')),
    PaymentMethod TEXT NOT NULL DEFAULT 'UPI',
    MerchantOrShop TEXT,
    Notes TEXT,
    ReceiptPath TEXT,
    Tags TEXT,
    Status INTEGER NOT NULL DEFAULT 1, -- 1: Confirmed, 2: PendingApproval, 3: Rejected
    NeedsApproval INTEGER NOT NULL DEFAULT 0,
    IsApproved INTEGER NOT NULL DEFAULT 1,
    CreatedByUserId TEXT NOT NULL,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY(FamilyId) REFERENCES Families(Id),
    FOREIGN KEY(CategoryId) REFERENCES FinancialCategories(Id),
    FOREIGN KEY(PaidByMemberId) REFERENCES FamilyMemberships(Id)
);

CREATE TABLE IF NOT EXISTS ExpenseSplits (
    Id TEXT PRIMARY KEY,
    ExpenseTransactionId TEXT NOT NULL,
    BeneficiaryMemberId TEXT NOT NULL,
    SplitType INTEGER NOT NULL DEFAULT 1, -- 1: Equal, 2: Fixed, 3: Percentage
    SplitAmount REAL NOT NULL,
    SplitPercentage REAL,
    Notes TEXT,
    FOREIGN KEY(ExpenseTransactionId) REFERENCES ExpenseTransactions(Id),
    FOREIGN KEY(BeneficiaryMemberId) REFERENCES FamilyMemberships(Id)
);

-- 6. Income
CREATE TABLE IF NOT EXISTS IncomeTransactions (
    Id TEXT PRIMARY KEY,
    FamilyId TEXT NOT NULL,
    Title TEXT NOT NULL,
    Amount REAL NOT NULL,
    IncomeSource TEXT NOT NULL DEFAULT 'Salary',
    RecipientMemberId TEXT NOT NULL,
    DateReceived TEXT NOT NULL DEFAULT (datetime('now')),
    PaymentMethod TEXT NOT NULL DEFAULT 'Bank Transfer',
    AccountOrSource TEXT,
    CategoryId TEXT,
    Notes TEXT,
    IsRecurring INTEGER NOT NULL DEFAULT 0,
    CreatedByUserId TEXT NOT NULL,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY(FamilyId) REFERENCES Families(Id),
    FOREIGN KEY(RecipientMemberId) REFERENCES FamilyMemberships(Id)
);

-- 7. Debts & Repayments
CREATE TABLE IF NOT EXISTS Debts (
    Id TEXT PRIMARY KEY,
    FamilyId TEXT NOT NULL,
    DebtType INTEGER NOT NULL DEFAULT 1, -- 1: Borrowed, 2: Lent
    CreditorOrDebtorName TEXT NOT NULL,
    LinkedMemberId TEXT,
    OriginalPrincipal REAL NOT NULL,
    CurrentBalance REAL NOT NULL,
    CurrencyCode TEXT NOT NULL DEFAULT 'INR',
    StartDate TEXT NOT NULL,
    DueDate TEXT,
    InterestRate REAL,
    InterestType INTEGER NOT NULL DEFAULT 0,
    RepaymentFrequency TEXT DEFAULT 'Monthly',
    InstallmentAmount REAL,
    Notes TEXT,
    Status INTEGER NOT NULL DEFAULT 1, -- 1: Active, 2: PartiallyRepaid, 3: PaidOff
    CreatedByUserId TEXT NOT NULL,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY(FamilyId) REFERENCES Families(Id)
);

CREATE TABLE IF NOT EXISTS DebtRepayments (
    Id TEXT PRIMARY KEY,
    DebtId TEXT NOT NULL,
    RepaidByMemberId TEXT,
    Amount REAL NOT NULL,
    PrincipalPortion REAL NOT NULL,
    InterestPortion REAL NOT NULL DEFAULT 0,
    PaymentDate TEXT NOT NULL DEFAULT (datetime('now')),
    PaymentMethod TEXT NOT NULL DEFAULT 'UPI',
    ReferenceNumber TEXT,
    Notes TEXT,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY(DebtId) REFERENCES Debts(Id)
);

-- 8. Recurring Bills & Occurrences
CREATE TABLE IF NOT EXISTS BillTemplates (
    Id TEXT PRIMARY KEY,
    FamilyId TEXT NOT NULL,
    BillName TEXT NOT NULL,
    CategoryId TEXT NOT NULL,
    Payee TEXT NOT NULL,
    ExpectedAmount REAL NOT NULL,
    IsVariableAmount INTEGER NOT NULL DEFAULT 0,
    Frequency INTEGER NOT NULL DEFAULT 2, -- 1: Weekly, 2: Monthly, 3: Quarterly, 4: HalfYearly, 5: Annually
    BillingStartDate TEXT NOT NULL,
    NextDueDate TEXT NOT NULL,
    EndDate TEXT,
    ResponsibleMemberId TEXT,
    AutopayEnabled INTEGER NOT NULL DEFAULT 0,
    Notes TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY(FamilyId) REFERENCES Families(Id)
);

CREATE TABLE IF NOT EXISTS BillOccurrences (
    Id TEXT PRIMARY KEY,
    BillTemplateId TEXT NOT NULL,
    FamilyId TEXT NOT NULL,
    DueDate TEXT NOT NULL,
    ExpectedAmount REAL NOT NULL,
    ActualAmountPaid REAL NOT NULL DEFAULT 0,
    Status INTEGER NOT NULL DEFAULT 1, -- 1: Pending, 2: Paid, 3: PartiallyPaid, 4: Skipped, 5: Overdue
    PaidDate TEXT,
    ExpenseTransactionId TEXT,
    Notes TEXT,
    FOREIGN KEY(BillTemplateId) REFERENCES BillTemplates(Id),
    FOREIGN KEY(FamilyId) REFERENCES Families(Id)
);

-- 9. Obligations
CREATE TABLE IF NOT EXISTS PaymentObligations (
    Id TEXT PRIMARY KEY,
    FamilyId TEXT NOT NULL,
    Title TEXT NOT NULL,
    AmountDue REAL NOT NULL,
    AmountPaid REAL NOT NULL DEFAULT 0,
    DueDate TEXT NOT NULL,
    ResponsibleMemberId TEXT,
    PayeeName TEXT NOT NULL,
    ObligationType INTEGER NOT NULL DEFAULT 4,
    Priority INTEGER NOT NULL DEFAULT 2, -- 1: Low, 2: Medium, 3: High, 4: Urgent
    Status INTEGER NOT NULL DEFAULT 1, -- 1: Pending, 2: PartiallyPaid, 3: Paid, 4: Overdue
    Notes TEXT,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY(FamilyId) REFERENCES Families(Id)
);

-- 10. Savings Goals & Contributions
CREATE TABLE IF NOT EXISTS SavingsGoals (
    Id TEXT PRIMARY KEY,
    FamilyId TEXT NOT NULL,
    Title TEXT NOT NULL,
    TargetAmount REAL NOT NULL,
    CurrentAmount REAL NOT NULL DEFAULT 0,
    TargetDate TEXT,
    Category TEXT,
    Status INTEGER NOT NULL DEFAULT 1,
    CreatedByUserId TEXT NOT NULL,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY(FamilyId) REFERENCES Families(Id)
);

CREATE TABLE IF NOT EXISTS SavingsContributions (
    Id TEXT PRIMARY KEY,
    SavingsGoalId TEXT NOT NULL,
    MemberId TEXT NOT NULL,
    Amount REAL NOT NULL,
    ContributionDate TEXT NOT NULL DEFAULT (datetime('now')),
    Notes TEXT,
    FOREIGN KEY(SavingsGoalId) REFERENCES SavingsGoals(Id)
);

-- 11. Notifications
CREATE TABLE IF NOT EXISTS InAppNotifications (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    FamilyId TEXT NOT NULL,
    Title TEXT NOT NULL,
    Message TEXT NOT NULL,
    NotificationType INTEGER NOT NULL DEFAULT 1,
    DeepLinkUrl TEXT,
    IsRead INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
);

-- SEED DEFAULT SYSTEM CATEGORIES
INSERT OR IGNORE INTO FinancialCategories (Id, FamilyId, Name, Description, ColorHex, IconClass, Type, IsSystemDefault) VALUES
('cat-exp-1', NULL, 'Groceries', 'Food, vegetables, supermarkets', '#10B981', 'bi-cart4', 1, 1),
('cat-exp-2', NULL, 'Rent & Housing', 'Monthly rent, home EMI, society dues', '#3B82F6', 'bi-house-door', 1, 1),
('cat-exp-3', NULL, 'Electricity & Power', 'Electricity bills', '#F59E0B', 'bi-lightning-charge', 1, 1),
('cat-exp-4', NULL, 'Water Supply', 'Water utility bills', '#06B6D4', 'bi-droplet', 1, 1),
('cat-exp-5', NULL, 'Gas & Cooking', 'LPG cylinders, piped gas', '#EF4444', 'bi-fire', 1, 1),
('cat-exp-6', NULL, 'Internet & Mobile', 'Broadband, cellular mobile recharges', '#6366F1', 'bi-wifi', 1, 1),
('cat-exp-7', NULL, 'Transportation', 'Metro, cab, auto, bus fares', '#8B5CF6', 'bi-car-front', 1, 1),
('cat-exp-8', NULL, 'Fuel & Petrol', 'Petrol, diesel, vehicle fuel', '#EC4899', 'bi-fuel-pump', 1, 1),
('cat-exp-9', NULL, 'Education & Tuition', 'School fees, tuition, books', '#14B8A6', 'bi-mortarboard', 1, 1),
('cat-exp-10', NULL, 'Medical & Health', 'Doctor visits, pharmacy, medicines', '#F43F5E', 'bi-heart-pulse', 1, 1),
('cat-exp-11', NULL, 'Insurance', 'Health, vehicle, life insurance', '#64748B', 'bi-shield-check', 1, 1),
('cat-exp-12', NULL, 'Household Supplies', 'Cleaning items, cookware, repairs', '#84CC16', 'bi-basket', 1, 1),
('cat-exp-13', NULL, 'Shopping & Apparel', 'Clothes, footwear, electronics', '#D946EF', 'bi-bag', 1, 1),
('cat-exp-14', NULL, 'Dining Out', 'Restaurants, cafes, food delivery', '#F97316', 'bi-cup-hot', 1, 1),
('cat-exp-15', NULL, 'Entertainment', 'Movies, events, outings', '#A855F7', 'bi-film', 1, 1),
('cat-exp-16', NULL, 'Subscriptions', 'OTT (Netflix, Prime), software', '#6366F1', 'bi-bell', 1, 1),
('cat-exp-17', NULL, 'Childcare', 'Daycare, baby supplies', '#0EA5E9', 'bi-emoji-smile', 1, 1),
('cat-exp-18', NULL, 'Family Events & Gifts', 'Festivals, birthdays, gifts', '#F472B6', 'bi-gift', 1, 1),
('cat-exp-19', NULL, 'Travel & Vacation', 'Trips, hotel bookings, flights', '#0284C7', 'bi-airplane', 1, 1),
('cat-exp-20', NULL, 'Miscellaneous', 'Other general household expenses', '#94A3B8', 'bi-three-dots', 1, 1),
('cat-inc-1', NULL, 'Salary & Wages', 'Primary employer salary', '#10B981', 'bi-cash-stack', 2, 1),
('cat-inc-2', NULL, 'Business Income', 'Business revenue or profit', '#3B82F6', 'bi-briefcase', 2, 1),
('cat-inc-3', NULL, 'Freelance & Consulting', 'Independent client projects', '#8B5CF6', 'bi-laptop', 2, 1),
('cat-inc-4', NULL, 'Rental Income', 'Property tenant rental income', '#F59E0B', 'bi-building', 2, 1),
('cat-inc-5', NULL, 'Investments & Dividends', 'Mutual funds, stock dividends, interest', '#06B6D4', 'bi-graph-up-arrow', 2, 1),
('cat-inc-6', NULL, 'Gifts & Grants', 'Monetary gifts from relatives', '#EC4899', 'bi-gift', 2, 1),
('cat-inc-7', NULL, 'Other Income', 'Cashbacks, tax refunds, miscellaneous', '#64748B', 'bi-wallet2', 2, 1);
