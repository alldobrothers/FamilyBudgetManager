import { Hono } from 'hono';
import { cors } from 'hono/cors';

type Bindings = {
  DB: D1Database;
  ASSETS: Fetcher;
};

const app = new Hono<{ Bindings: Bindings }>();

// Enable CORS for all origins
app.use('*', cors({
  origin: '*',
  allowHeaders: ['Content-Type', 'Authorization', 'X-Family-Id'],
  allowMethods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS'],
}));

app.get('/api/health', (c) => c.json({ status: 'ok', timestamp: new Date().toISOString() }));

// 1. AUTHENTICATION
app.post('/api/auth/login', async (c) => {
  const { email, password } = await c.req.json();
  if (!email || !password) {
    return c.json({ error: 'Email and password are required' }, 400);
  }

  const user = await c.env.DB.prepare(
    'SELECT Id, Email, FullName, DisplayName, PasswordHash FROM Users WHERE Email = ?'
  ).bind(email.toLowerCase().trim()).first<{
    Id: string;
    Email: string;
    FullName: string;
    DisplayName: string;
    PasswordHash: string;
  }>();

  if (!user || user.PasswordHash !== password) {
    return c.json({ error: 'Invalid email or password' }, 401);
  }

  // Get user's active family membership
  const membership = await c.env.DB.prepare(
    `SELECT m.Id as MembershipId, m.FamilyId, m.Role, f.Name as FamilyName, f.CurrencySymbol, f.CurrencyCode, f.MonthlyBudget
     FROM FamilyMemberships m
     JOIN Families f ON m.FamilyId = f.Id
     WHERE m.UserId = ? AND m.IsActive = 1
     LIMIT 1`
  ).bind(user.Id).first<{
    MembershipId: string;
    FamilyId: string;
    Role: number;
    FamilyName: string;
    CurrencySymbol: string;
    CurrencyCode: string;
    MonthlyBudget: number;
  }>();

  return c.json({
    token: `token-${user.Id}-${Date.now()}`,
    user: {
      id: user.Id,
      email: user.Email,
      fullName: user.FullName,
      displayName: user.DisplayName
    },
    family: membership ? {
      id: membership.FamilyId,
      name: membership.FamilyName,
      role: membership.Role,
      currencySymbol: membership.CurrencySymbol,
      currencyCode: membership.CurrencyCode,
      monthlyBudget: membership.MonthlyBudget,
      membershipId: membership.MembershipId
    } : null
  });
});

// 2. DASHBOARD SUMMARY
app.get('/api/dashboard/summary', async (c) => {
  const familyId = c.req.header('X-Family-Id') || 'fam-sharma';

  const family = await c.env.DB.prepare(
    'SELECT * FROM Families WHERE Id = ?'
  ).bind(familyId).first<{
    Id: string;
    Name: string;
    CurrencySymbol: string;
    CurrencyCode: string;
    MonthlyBudget: number;
  }>();

  if (!family) {
    return c.json({ error: 'Family household not found' }, 404);
  }

  // Total Income
  const incomeResult = await c.env.DB.prepare(
    'SELECT COALESCE(SUM(Amount), 0) as total FROM IncomeTransactions WHERE FamilyId = ?'
  ).bind(familyId).first<{ total: number }>();
  const totalIncome = incomeResult?.total || 0;

  // Total Expenses
  const expenseResult = await c.env.DB.prepare(
    'SELECT COALESCE(SUM(Amount), 0) as total FROM ExpenseTransactions WHERE FamilyId = ?'
  ).bind(familyId).first<{ total: number }>();
  const totalExpenses = expenseResult?.total || 0;

  // Outstanding Debt (Borrowed)
  const debtResult = await c.env.DB.prepare(
    'SELECT COALESCE(SUM(CurrentBalance), 0) as total FROM Debts WHERE FamilyId = ? AND DebtType = 1 AND Status != 3'
  ).bind(familyId).first<{ total: number }>();
  const totalDebt = debtResult?.total || 0;

  // Upcoming Bills Total
  const billsResult = await c.env.DB.prepare(
    `SELECT COALESCE(SUM(ExpectedAmount), 0) as total 
     FROM BillOccurrences 
     WHERE FamilyId = ? AND Status = 1`
  ).bind(familyId).first<{ total: number }>();
  const upcomingBills = billsResult?.total || 0;

  // Recent Transactions (top 5 expenses)
  const recentExpenses = await c.env.DB.prepare(
    `SELECT e.Id, e.Title, e.Amount, e.ExpenseDate, e.PaymentMethod, e.MerchantOrShop,
            c.Name as CategoryName, c.ColorHex, c.IconClass,
            u.DisplayName as PaidByName
     FROM ExpenseTransactions e
     LEFT JOIN FinancialCategories c ON e.CategoryId = c.Id
     LEFT JOIN FamilyMemberships m ON e.PaidByMemberId = m.Id
     LEFT JOIN Users u ON m.UserId = u.Id
     WHERE e.FamilyId = ?
     ORDER BY e.ExpenseDate DESC
     LIMIT 5`
  ).bind(familyId).all();

  // Upcoming Bills List
  const billItems = await c.env.DB.prepare(
    `SELECT o.Id, o.DueDate, o.ExpectedAmount, t.BillName, t.Payee
     FROM BillOccurrences o
     JOIN BillTemplates t ON o.BillTemplateId = t.Id
     WHERE o.FamilyId = ? AND o.Status = 1
     ORDER BY o.DueDate ASC
     LIMIT 5`
  ).bind(familyId).all();

  // Category Breakdown for Charts
  const categoryBreakdown = await c.env.DB.prepare(
    `SELECT c.Name, c.ColorHex, SUM(e.Amount) as TotalAmount
     FROM ExpenseTransactions e
     JOIN FinancialCategories c ON e.CategoryId = c.Id
     WHERE e.FamilyId = ?
     GROUP BY c.Name, c.ColorHex
     ORDER BY TotalAmount DESC`
  ).bind(familyId).all();

  const netSavings = totalIncome - totalExpenses;
  const savingsRate = totalIncome > 0 ? ((netSavings / totalIncome) * 100).toFixed(1) : '0';
  const budgetUtilization = family.MonthlyBudget > 0 ? ((totalExpenses / family.MonthlyBudget) * 100).toFixed(1) : '0';

  return c.json({
    family,
    metrics: {
      totalIncome,
      totalExpenses,
      netSavings,
      savingsRate: Number(savingsRate),
      monthlyBudget: family.MonthlyBudget,
      remainingBudget: Math.max(0, family.MonthlyBudget - totalExpenses),
      budgetUtilization: Number(budgetUtilization),
      totalDebt,
      upcomingBills
    },
    recentExpenses: recentExpenses.results,
    upcomingBills: billItems.results,
    categoryBreakdown: categoryBreakdown.results
  });
});

// 3. CATEGORIES
app.get('/api/categories', async (c) => {
  const result = await c.env.DB.prepare(
    'SELECT * FROM FinancialCategories ORDER BY Type, Name'
  ).all();
  return c.json(result.results);
});

// 4. EXPENSES
app.get('/api/expenses', async (c) => {
  const familyId = c.req.header('X-Family-Id') || 'fam-sharma';
  const result = await c.env.DB.prepare(
    `SELECT e.*, c.Name as CategoryName, c.ColorHex, c.IconClass,
            u.DisplayName as PaidByName
     FROM ExpenseTransactions e
     LEFT JOIN FinancialCategories c ON e.CategoryId = c.Id
     LEFT JOIN FamilyMemberships m ON e.PaidByMemberId = m.Id
     LEFT JOIN Users u ON m.UserId = u.Id
     WHERE e.FamilyId = ?
     ORDER BY e.ExpenseDate DESC`
  ).bind(familyId).all();
  return c.json(result.results);
});

app.post('/api/expenses/create', async (c) => {
  const familyId = c.req.header('X-Family-Id') || 'fam-sharma';
  const body = await c.req.json();
  const id = `exp-${Date.now()}`;

  await c.env.DB.prepare(
    `INSERT INTO ExpenseTransactions (
      Id, FamilyId, Title, Amount, CategoryId, PaidByMemberId, ExpenseDate,
      PaymentMethod, MerchantOrShop, Notes, Status, NeedsApproval, IsApproved, CreatedByUserId
    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 1, 0, 1, ?)`
  ).bind(
    id,
    familyId,
    body.title,
    Number(body.amount),
    body.categoryId,
    body.paidByMemberId || 'mem-rajesh',
    body.expenseDate || new Date().toISOString(),
    body.paymentMethod || 'UPI',
    body.merchantOrShop || '',
    body.notes || '',
    'user-demo-1'
  ).run();

  return c.json({ success: true, id });
});

app.delete('/api/expenses/:id', async (c) => {
  const id = c.req.param('id');
  await c.env.DB.prepare('DELETE FROM ExpenseTransactions WHERE Id = ?').bind(id).run();
  return c.json({ success: true });
});

// 5. INCOMES
app.get('/api/incomes', async (c) => {
  const familyId = c.req.header('X-Family-Id') || 'fam-sharma';
  const result = await c.env.DB.prepare(
    `SELECT i.*, c.Name as CategoryName, u.DisplayName as RecipientName
     FROM IncomeTransactions i
     LEFT JOIN FinancialCategories c ON i.CategoryId = c.Id
     LEFT JOIN FamilyMemberships m ON i.RecipientMemberId = m.Id
     LEFT JOIN Users u ON m.UserId = u.Id
     WHERE i.FamilyId = ?
     ORDER BY i.DateReceived DESC`
  ).bind(familyId).all();
  return c.json(result.results);
});

app.post('/api/incomes/create', async (c) => {
  const familyId = c.req.header('X-Family-Id') || 'fam-sharma';
  const body = await c.req.json();
  const id = `inc-${Date.now()}`;

  await c.env.DB.prepare(
    `INSERT INTO IncomeTransactions (
      Id, FamilyId, Title, Amount, IncomeSource, RecipientMemberId,
      DateReceived, PaymentMethod, CategoryId, Notes, CreatedByUserId
    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`
  ).bind(
    id,
    familyId,
    body.title,
    Number(body.amount),
    body.incomeSource || 'Salary',
    body.recipientMemberId || 'mem-rajesh',
    body.dateReceived || new Date().toISOString(),
    body.paymentMethod || 'Bank Transfer',
    body.categoryId || 'cat-inc-1',
    body.notes || '',
    'user-demo-1'
  ).run();

  return c.json({ success: true, id });
});

// 6. DEBTS
app.get('/api/debts', async (c) => {
  const familyId = c.req.header('X-Family-Id') || 'fam-sharma';
  const debts = await c.env.DB.prepare(
    `SELECT d.*, u.DisplayName as LinkedMemberName
     FROM Debts d
     LEFT JOIN FamilyMemberships m ON d.LinkedMemberId = m.Id
     LEFT JOIN Users u ON m.UserId = u.Id
     WHERE d.FamilyId = ?
     ORDER BY d.CreatedAt DESC`
  ).bind(familyId).all();

  const repayments = await c.env.DB.prepare(
    `SELECT r.*, d.CreditorOrDebtorName, u.DisplayName as RepaidByName
     FROM DebtRepayments r
     JOIN Debts d ON r.DebtId = d.Id
     LEFT JOIN FamilyMemberships m ON r.RepaidByMemberId = m.Id
     LEFT JOIN Users u ON m.UserId = u.Id
     WHERE d.FamilyId = ?
     ORDER BY r.PaymentDate DESC`
  ).bind(familyId).all();

  return c.json({
    debts: debts.results,
    repayments: repayments.results
  });
});

app.post('/api/debts/create', async (c) => {
  const familyId = c.req.header('X-Family-Id') || 'fam-sharma';
  const body = await c.req.json();
  const id = `debt-${Date.now()}`;
  const amount = Number(body.originalPrincipal);

  await c.env.DB.prepare(
    `INSERT INTO Debts (
      Id, FamilyId, DebtType, CreditorOrDebtorName, LinkedMemberId,
      OriginalPrincipal, CurrentBalance, CurrencyCode, StartDate, DueDate,
      Notes, Status, CreatedByUserId
    ) VALUES (?, ?, ?, ?, ?, ?, ?, 'INR', ?, ?, ?, 1, 'user-demo-1')`
  ).bind(
    id,
    familyId,
    Number(body.debtType) || 1,
    body.creditorOrDebtorName,
    body.linkedMemberId || 'mem-rajesh',
    amount,
    amount,
    body.startDate || new Date().toISOString(),
    body.dueDate || null,
    body.notes || ''
  ).run();

  return c.json({ success: true, id });
});

app.post('/api/debts/repay', async (c) => {
  const body = await c.req.json();
  const id = `dr-${Date.now()}`;
  const amount = Number(body.amount);

  // Update debt balance
  const debt = await c.env.DB.prepare('SELECT CurrentBalance FROM Debts WHERE Id = ?').bind(body.debtId).first<{ CurrentBalance: number }>();
  if (!debt) return c.json({ error: 'Debt not found' }, 404);

  const newBalance = Math.max(0, debt.CurrentBalance - amount);
  const newStatus = newBalance === 0 ? 3 : 2; // 3: PaidOff, 2: PartiallyRepaid

  await c.env.DB.prepare('UPDATE Debts SET CurrentBalance = ?, Status = ? WHERE Id = ?')
    .bind(newBalance, newStatus, body.debtId).run();

  await c.env.DB.prepare(
    `INSERT INTO DebtRepayments (
      Id, DebtId, RepaidByMemberId, Amount, PrincipalPortion, InterestPortion,
      PaymentDate, PaymentMethod, ReferenceNumber, Notes
    ) VALUES (?, ?, ?, ?, ?, 0, ?, ?, ?, ?)`
  ).bind(
    id,
    body.debtId,
    body.repaidByMemberId || 'mem-rajesh',
    amount,
    amount,
    body.paymentDate || new Date().toISOString(),
    body.paymentMethod || 'UPI',
    body.referenceNumber || '',
    body.notes || ''
  ).run();

  return c.json({ success: true, newBalance });
});

// 7. BILLS
app.get('/api/bills', async (c) => {
  const familyId = c.req.header('X-Family-Id') || 'fam-sharma';
  const templates = await c.env.DB.prepare(
    `SELECT t.*, c.Name as CategoryName, u.DisplayName as ResponsibleMemberName
     FROM BillTemplates t
     LEFT JOIN FinancialCategories c ON t.CategoryId = c.Id
     LEFT JOIN FamilyMemberships m ON t.ResponsibleMemberId = m.Id
     LEFT JOIN Users u ON m.UserId = u.Id
     WHERE t.FamilyId = ? AND t.IsActive = 1
     ORDER BY t.NextDueDate ASC`
  ).bind(familyId).all();

  const occurrences = await c.env.DB.prepare(
    `SELECT o.*, t.BillName, t.Payee
     FROM BillOccurrences o
     JOIN BillTemplates t ON o.BillTemplateId = t.Id
     WHERE o.FamilyId = ?
     ORDER BY o.DueDate ASC`
  ).bind(familyId).all();

  return c.json({
    templates: templates.results,
    occurrences: occurrences.results
  });
});

app.post('/api/bills/mark-paid', async (c) => {
  const { occurrenceId, amountPaid, paymentMethod } = await c.req.json();
  const occ = await c.env.DB.prepare(
    'SELECT * FROM BillOccurrences WHERE Id = ?'
  ).bind(occurrenceId).first<{
    Id: string;
    BillTemplateId: string;
    FamilyId: string;
    ExpectedAmount: number;
  }>();

  if (!occ) return c.json({ error: 'Occurrence not found' }, 404);

  // Update occurrence status
  await c.env.DB.prepare(
    'UPDATE BillOccurrences SET Status = 2, ActualAmountPaid = ?, PaidDate = datetime(\'now\') WHERE Id = ?'
  ).bind(Number(amountPaid) || occ.ExpectedAmount, occurrenceId).run();

  // Create linked expense
  const expId = `exp-bill-${Date.now()}`;
  await c.env.DB.prepare(
    `INSERT INTO ExpenseTransactions (
      Id, FamilyId, Title, Amount, CategoryId, PaidByMemberId, ExpenseDate,
      PaymentMethod, Notes, Status, NeedsApproval, IsApproved, CreatedByUserId
    ) VALUES (?, ?, ?, ?, 'cat-exp-3', 'mem-rajesh', datetime('now'), ?, 'Bill payment settlement', 1, 0, 1, 'user-demo-1')`
  ).bind(
    expId,
    occ.FamilyId,
    'Electricity Bill Payment',
    Number(amountPaid) || occ.ExpectedAmount,
    paymentMethod || 'UPI'
  ).run();

  return c.json({ success: true, message: 'Bill marked as paid and expense logged' });
});

// 8. OBLIGATIONS
app.get('/api/obligations', async (c) => {
  const familyId = c.req.header('X-Family-Id') || 'fam-sharma';
  const result = await c.env.DB.prepare(
    `SELECT o.*, u.DisplayName as ResponsibleMemberName
     FROM PaymentObligations o
     LEFT JOIN FamilyMemberships m ON o.ResponsibleMemberId = m.Id
     LEFT JOIN Users u ON m.UserId = u.Id
     WHERE o.FamilyId = ?
     ORDER BY o.DueDate ASC`
  ).bind(familyId).all();
  return c.json(result.results);
});

// 9. REPORTS
app.get('/api/reports/summary', async (c) => {
  const familyId = c.req.header('X-Family-Id') || 'fam-sharma';

  const categorySpending = await c.env.DB.prepare(
    `SELECT c.Name, c.ColorHex, SUM(e.Amount) as TotalAmount, COUNT(e.Id) as TransactionCount
     FROM ExpenseTransactions e
     JOIN FinancialCategories c ON e.CategoryId = c.Id
     WHERE e.FamilyId = ?
     GROUP BY c.Name, c.ColorHex
     ORDER BY TotalAmount DESC`
  ).bind(familyId).all();

  const memberSpending = await c.env.DB.prepare(
    `SELECT u.DisplayName as MemberName, SUM(e.Amount) as TotalSpent, COUNT(e.Id) as TransactionCount
     FROM ExpenseTransactions e
     JOIN FamilyMemberships m ON e.PaidByMemberId = m.Id
     JOIN Users u ON m.UserId = u.Id
     WHERE e.FamilyId = ?
     GROUP BY u.DisplayName
     ORDER BY TotalSpent DESC`
  ).bind(familyId).all();

  const totalIncome = await c.env.DB.prepare(
    'SELECT COALESCE(SUM(Amount), 0) as total FROM IncomeTransactions WHERE FamilyId = ?'
  ).bind(familyId).first<{ total: number }>();

  const totalExpense = await c.env.DB.prepare(
    'SELECT COALESCE(SUM(Amount), 0) as total FROM ExpenseTransactions WHERE FamilyId = ?'
  ).bind(familyId).first<{ total: number }>();

  return c.json({
    totalIncome: totalIncome?.total || 0,
    totalExpense: totalExpense?.total || 0,
    netSavings: (totalIncome?.total || 0) - (totalExpense?.total || 0),
    categorySpending: categorySpending.results,
    memberSpending: memberSpending.results
  });
});

// 10. FAMILY MEMBERS
app.get('/api/family', async (c) => {
  const familyId = c.req.header('X-Family-Id') || 'fam-sharma';
  const family = await c.env.DB.prepare('SELECT * FROM Families WHERE Id = ?').bind(familyId).first();
  const members = await c.env.DB.prepare(
    `SELECT m.*, u.FullName, u.DisplayName, u.Email
     FROM FamilyMemberships m
     JOIN Users u ON m.UserId = u.Id
     WHERE m.FamilyId = ? AND m.IsActive = 1
     ORDER BY m.Role ASC`
  ).bind(familyId).all();

  return c.json({ family, members: members.results });
});

export default {
  async fetch(request: Request, env: Bindings, ctx: ExecutionContext) {
    const url = new URL(request.url);
    if (url.pathname.startsWith('/api')) {
      return app.fetch(request, env, ctx);
    }
    return env.ASSETS.fetch(request);
  }
};
