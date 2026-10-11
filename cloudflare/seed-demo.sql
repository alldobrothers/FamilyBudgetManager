-- Seed Demo Account & Household for Cloudflare D1

INSERT OR IGNORE INTO Users (Id, Email, FullName, DisplayName, PasswordHash, CreatedAt) VALUES
('user-demo-1', 'demo@familybudget.local', 'Rajesh Sharma', 'Rajesh', 'Demo@123456', datetime('now')),
('user-demo-2', 'priya@familybudget.local', 'Priya Sharma', 'Priya', 'Demo@123456', datetime('now'));

INSERT OR IGNORE INTO Families (Id, Name, CurrencyCode, CurrencySymbol, Country, TimeZoneId, MonthStartDay, MonthlyBudget, Description, CreatedByUserId, CreatedAt) VALUES
('fam-sharma', 'Sharma Household', 'INR', '₹', 'India', 'India Standard Time', 1, 75000.0, 'Our primary household family budget', 'user-demo-1', datetime('now'));

INSERT OR IGNORE INTO FamilySettings (FamilyId, EnableExpenseApprovals, BudgetWarningThreshold1, BudgetWarningThreshold2, NotifyOnOverdueBills, DarkModeDefault, UpdatedAt) VALUES
('fam-sharma', 0, 75.0, 90.0, 1, 0, datetime('now'));

INSERT OR IGNORE INTO FamilyMemberships (Id, FamilyId, UserId, Role, JoinedAt, IsActive, CanApproveExpenses, CanManageBills, CanViewDebts, CanExportReports) VALUES
('mem-rajesh', 'fam-sharma', 'user-demo-1', 1, datetime('now'), 1, 1, 1, 1, 1),
('mem-priya', 'fam-sharma', 'user-demo-2', 2, datetime('now'), 1, 1, 1, 1, 1);

INSERT OR IGNORE INTO Budgets (Id, FamilyId, PeriodYear, PeriodMonth, TotalPlannedBudget, RolloverAmount, Notes, CreatedAt) VALUES
('bgt-curr', 'fam-sharma', CAST(strftime('%Y', 'now') AS INTEGER), CAST(strftime('%m', 'now') AS INTEGER), 75000.0, 2500.0, 'Monthly household budget', datetime('now'));

INSERT OR IGNORE INTO BudgetCategories (Id, BudgetId, CategoryId, PlannedAmount) VALUES
('bc-1', 'bgt-curr', 'cat-exp-1', 15000.0),
('bc-2', 'bgt-curr', 'cat-exp-2', 25000.0),
('bc-3', 'bgt-curr', 'cat-exp-3', 4000.0),
('bc-4', 'bgt-curr', 'cat-exp-14', 6000.0);

INSERT OR IGNORE INTO IncomeTransactions (Id, FamilyId, Title, Amount, IncomeSource, RecipientMemberId, DateReceived, PaymentMethod, CategoryId, CreatedByUserId, CreatedAt) VALUES
('inc-1', 'fam-sharma', 'Rajesh Tech Salary', 85000.0, 'Salary', 'mem-rajesh', datetime('now', '-10 days'), 'Bank Transfer', 'cat-inc-1', 'user-demo-1', datetime('now')),
('inc-2', 'fam-sharma', 'Priya Consulting Project', 35000.0, 'Freelance & Consulting', 'mem-priya', datetime('now', '-6 days'), 'UPI', 'cat-inc-3', 'user-demo-2', datetime('now'));

INSERT OR IGNORE INTO ExpenseTransactions (Id, FamilyId, Title, Amount, CategoryId, PaidByMemberId, ExpenseDate, PaymentMethod, MerchantOrShop, Status, NeedsApproval, IsApproved, CreatedByUserId, CreatedAt) VALUES
('exp-1', 'fam-sharma', 'Monthly Apartment Rent', 25000.0, 'cat-exp-2', 'mem-rajesh', datetime('now', '-9 days'), 'Net Banking', 'Landlord Mr. Verma', 1, 0, 1, 'user-demo-1', datetime('now')),
('exp-2', 'fam-sharma', 'Supermarket Grocery Restock', 6450.0, 'cat-exp-1', 'mem-priya', datetime('now', '-5 days'), 'UPI', 'Nature''s Basket', 1, 0, 1, 'user-demo-2', datetime('now')),
('exp-3', 'fam-sharma', 'Family Weekend Dinner', 2850.0, 'cat-exp-14', 'mem-rajesh', datetime('now', '-2 days'), 'Credit Card', 'Barbeque Nation', 1, 0, 1, 'user-demo-1', datetime('now'));

INSERT OR IGNORE INTO BillTemplates (Id, FamilyId, BillName, CategoryId, Payee, ExpectedAmount, Frequency, BillingStartDate, NextDueDate, ResponsibleMemberId, Notes, IsActive, CreatedAt) VALUES
('bill-1', 'fam-sharma', 'BSES Delhi Electricity Bill', 'cat-exp-3', 'BSES Yamuna Power Ltd', 3450.0, 2, datetime('now', '-1 month'), datetime('now', '+5 days'), 'mem-rajesh', 'Consumer CA: 100984729', 1, datetime('now'));

INSERT OR IGNORE INTO BillOccurrences (Id, BillTemplateId, FamilyId, DueDate, ExpectedAmount, ActualAmountPaid, Status) VALUES
('occ-1', 'bill-1', 'fam-sharma', datetime('now', '+5 days'), 3450.0, 0, 1);

INSERT OR IGNORE INTO Debts (Id, FamilyId, DebtType, CreditorOrDebtorName, LinkedMemberId, OriginalPrincipal, CurrentBalance, CurrencyCode, StartDate, DueDate, Status, CreatedByUserId, Notes, CreatedAt) VALUES
('debt-1', 'fam-sharma', 1, 'Uncle Suresh (Home Renovation)', 'mem-rajesh', 50000.0, 35000.0, 'INR', datetime('now', '-2 months'), datetime('now', '+4 months'), 2, 'user-demo-1', 'Borrowed for bathroom renovation work', datetime('now'));

INSERT OR IGNORE INTO DebtRepayments (Id, DebtId, RepaidByMemberId, Amount, PrincipalPortion, InterestPortion, PaymentDate, PaymentMethod, ReferenceNumber, Notes, CreatedAt) VALUES
('dr-1', 'debt-1', 'mem-rajesh', 15000.0, 15000.0, 0, datetime('now', '-15 days'), 'UPI', 'UPI/2026/0981729', 'First installment repaid via UPI', datetime('now'));

INSERT OR IGNORE INTO PaymentObligations (Id, FamilyId, Title, AmountDue, AmountPaid, DueDate, ResponsibleMemberId, PayeeName, ObligationType, Priority, Status, Notes, CreatedAt) VALUES
('obl-1', 'fam-sharma', 'Society Maintenance Quarterly Dues', 4500.0, 0, datetime('now', '+12 days'), 'mem-priya', 'Emerald Heights RWA', 4, 3, 1, 'Quarterly maintenance dues', datetime('now'));

INSERT OR IGNORE INTO SavingsGoals (Id, FamilyId, Title, TargetAmount, CurrentAmount, TargetDate, Category, Status, CreatedByUserId, CreatedAt) VALUES
('sav-1', 'fam-sharma', 'Emergency Fund (6 Months)', 200000.0, 85000.0, datetime('now', '+8 months'), 'Emergency', 1, 'user-demo-1', datetime('now'));

INSERT OR IGNORE INTO SavingsContributions (Id, SavingsGoalId, MemberId, Amount, ContributionDate, Notes) VALUES
('sc-1', 'sav-1', 'mem-rajesh', 10000.0, datetime('now', '-8 days'), 'Monthly automated transfer to liquid fund');

INSERT OR IGNORE INTO InAppNotifications (Id, UserId, FamilyId, Title, Message, NotificationType, DeepLinkUrl, IsRead, CreatedAt) VALUES
('notif-1', 'user-demo-1', 'fam-sharma', 'Upcoming Bill: Electricity', 'BSES Delhi Electricity Bill of ₹3,450 is due in 5 days.', 1, '/bills', 0, datetime('now'));
