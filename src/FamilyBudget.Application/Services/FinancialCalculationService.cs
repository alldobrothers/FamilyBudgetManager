using FamilyBudget.Application.DTOs;
using FamilyBudget.Application.Interfaces;
using FamilyBudget.Domain.Entities;
using FamilyBudget.Domain.Enums;

namespace FamilyBudget.Application.Services;

public class FinancialCalculationService : IFinancialCalculationService
{
    public DashboardSummaryDto CalculateDashboardMetrics(
        IEnumerable<IncomeTransaction> incomes,
        IEnumerable<ExpenseTransaction> expenses,
        Budget? currentBudget,
        IEnumerable<Debt> debts,
        IEnumerable<BillOccurrence> upcomingBills,
        IEnumerable<PaymentObligation> pendingObligations,
        IEnumerable<FamilyMembership> members,
        string currencyCode,
        string currencySymbol,
        DateTime periodStart,
        DateTime periodEnd)
    {
        var incomeList = incomes.ToList();
        var expenseList = expenses.ToList();
        var debtList = debts.ToList();
        var billList = upcomingBills.ToList();
        var obligationList = pendingObligations.ToList();
        var memberList = members.ToList();

        // 1. Calculate confirmed Period Totals
        var totalIncome = CalculateTotalIncome(incomeList);
        var totalExpenses = CalculateTotalExpenses(expenseList);
        var netCashFlow = CalculateNetCashFlow(totalIncome, totalExpenses);

        // 2. Budget Calculations
        var plannedBudget = currentBudget?.TotalPlannedBudget ?? 0.0m;
        var remainingBudget = CalculateRemainingBudget(plannedBudget, totalExpenses);
        var budgetUtilization = CalculateBudgetUtilization(plannedBudget, totalExpenses);

        // 3. Outstanding Debt (Only borrowed money where status is Active or PartiallyRepaid)
        var outstandingDebt = CalculateOutstandingDebtBalance(debtList.Where(d => d.DebtType == DebtType.Borrowed));

        // 4. Upcoming Bills Total (Within next 30 days and not yet paid)
        var upcomingBillsTotal = billList
            .Where(b => b.Status == BillOccurrenceStatus.Pending || b.Status == BillOccurrenceStatus.Overdue)
            .Sum(b => Math.Max(0.0m, b.ExpectedAmount - b.ActualAmountPaid));

        // 5. Pending Obligations Total (Unsettled obligations)
        var pendingObligationsTotal = obligationList
            .Where(o => o.Status == ObligationStatus.Pending || o.Status == ObligationStatus.PartiallyPaid || o.Status == ObligationStatus.Overdue)
            .Sum(o => o.RemainingBalance);

        // 6. Estimated Available Cash: opening estimate + cash inflows - cash outflows
        // Note: Disclose clearly that this is an estimate based on recorded cash flows
        var estimatedCash = Math.Max(0.0m, totalIncome - totalExpenses);

        // 7. Category Expense Breakdown
        var categoryExpenses = expenseList
            .Where(e => e.Status == TransactionStatus.Confirmed && e.Category != null)
            .GroupBy(e => new { e.Category!.Name, e.Category.ColorHex, e.Category.IconClass })
            .Select(g => new CategoryExpenseShareDto
            {
                CategoryName = g.Key.Name,
                ColorHex = g.Key.ColorHex,
                IconClass = g.Key.IconClass,
                Amount = g.Sum(x => x.Amount),
                Percentage = totalExpenses > 0 ? Math.Round((g.Sum(x => x.Amount) / totalExpenses) * 100.0m, 1) : 0
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        // 8. Member Spending Share
        var memberShares = CalculateMemberBreakdown(expenseList, incomeList, memberList);

        // 9. Budget Category Progress Bars
        var budgetBars = new List<BudgetUtilizationBarDto>();
        if (currentBudget != null && currentBudget.CategoryAllocations.Any())
        {
            foreach (var alloc in currentBudget.CategoryAllocations)
            {
                var catName = alloc.Category?.Name ?? "Category";
                var catColor = alloc.Category?.ColorHex ?? "#3B82F6";
                var spentInCat = expenseList
                    .Where(e => e.CategoryId == alloc.CategoryId && e.Status == TransactionStatus.Confirmed)
                    .Sum(e => e.Amount);

                budgetBars.Add(new BudgetUtilizationBarDto
                {
                    CategoryName = catName,
                    ColorHex = catColor,
                    PlannedAmount = alloc.PlannedAmount,
                    SpentAmount = spentInCat
                });
            }
        }

        // 10. Recent Transactions (merged list of latest expenses and incomes)
        var recentTransactions = expenseList
            .Select(e => new RecentTransactionDto
            {
                Id = e.Id,
                Title = e.Title,
                Amount = e.Amount,
                Type = "Expense",
                CategoryName = e.Category?.Name ?? "Uncategorized",
                CategoryColor = e.Category?.ColorHex ?? "#EF4444",
                PaidByOrRecipient = e.PaidByMember?.User?.DisplayName ?? e.PaidByMember?.User?.FullName ?? "Member",
                Date = e.ExpenseDate,
                PaymentMethod = e.PaymentMethod
            })
            .Concat(incomeList.Select(i => new RecentTransactionDto
            {
                Id = i.Id,
                Title = i.Title,
                Amount = i.Amount,
                Type = "Income",
                CategoryName = i.Category?.Name ?? i.IncomeSource,
                CategoryColor = i.Category?.ColorHex ?? "#10B981",
                PaidByOrRecipient = i.RecipientMember?.User?.DisplayName ?? i.RecipientMember?.User?.FullName ?? "Member",
                Date = i.DateReceived,
                PaymentMethod = i.PaymentMethod
            }))
            .OrderByDescending(t => t.Date)
            .Take(8)
            .ToList();

        // 11. Upcoming Bills DTOs
        var now = DateTime.UtcNow;
        var upcomingBillDtos = billList
            .Where(b => b.Status != BillOccurrenceStatus.Paid && b.Status != BillOccurrenceStatus.Skipped)
            .OrderBy(b => b.DueDate)
            .Take(5)
            .Select(b => new UpcomingBillDto
            {
                OccurrenceId = b.Id,
                BillName = b.BillTemplate?.BillName ?? "Scheduled Bill",
                Payee = b.BillTemplate?.Payee ?? "Payee",
                Amount = b.ExpectedAmount - b.ActualAmountPaid,
                DueDate = b.DueDate,
                DaysRemaining = (int)Math.Ceiling((b.DueDate.Date - now.Date).TotalDays),
                Status = b.Status.ToString()
            })
            .ToList();

        // 12. Pending Obligations DTOs
        var pendingObligationDtos = obligationList
            .Where(o => o.Status != ObligationStatus.Paid && o.Status != ObligationStatus.Cancelled)
            .OrderBy(o => o.DueDate)
            .Take(5)
            .Select(o => new PendingObligationDto
            {
                Id = o.Id,
                Title = o.Title,
                PayeeOrBeneficiary = o.PayeeName,
                AmountDue = o.AmountDue,
                RemainingBalance = o.RemainingBalance,
                DueDate = o.DueDate,
                Priority = o.Priority.ToString(),
                Status = o.Status.ToString()
            })
            .ToList();

        // 13. System Alerts
        var alerts = new List<DashboardAlertDto>();
        if (plannedBudget > 0 && budgetUtilization >= 100.0m)
        {
            alerts.Add(new DashboardAlertDto
            {
                Type = "danger",
                Title = "Budget Exceeded",
                Message = $"Household spending has reached {budgetUtilization:F1}% of the planned monthly budget ({currencySymbol}{plannedBudget:N0}).",
                ActionUrl = "/Budgets",
                ActionText = "Review Budgets"
            });
        }
        else if (plannedBudget > 0 && budgetUtilization >= 90.0m)
        {
            alerts.Add(new DashboardAlertDto
            {
                Type = "warning",
                Title = "High Budget Utilization",
                Message = $"You have consumed {budgetUtilization:F1}% of this month's budget. Only {currencySymbol}{remainingBudget:N0} remains.",
                ActionUrl = "/Budgets",
                ActionText = "View Allocation"
            });
        }

        var overdueBillsCount = upcomingBillDtos.Count(b => b.IsOverdue);
        if (overdueBillsCount > 0)
        {
            alerts.Add(new DashboardAlertDto
            {
                Type = "danger",
                Title = "Overdue Bills",
                Message = $"You have {overdueBillsCount} bill(s) past due date that need immediate payment.",
                ActionUrl = "/RecurringBills",
                ActionText = "View Bills"
            });
        }

        return new DashboardSummaryDto
        {
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            NetCashFlow = netCashFlow,
            TotalBudget = plannedBudget,
            RemainingBudget = remainingBudget,
            BudgetUtilizationPercentage = budgetUtilization,
            OutstandingDebt = outstandingDebt,
            UpcomingBillsTotal = upcomingBillsTotal,
            PendingPaymentsTotal = pendingObligationsTotal,
            EstimatedAvailableCash = estimatedCash,
            CurrencyCode = currencyCode,
            CurrencySymbol = currencySymbol,
            CategoryExpenses = categoryExpenses,
            MemberExpenses = memberShares,
            BudgetProgressBars = budgetBars,
            RecentTransactions = recentTransactions,
            UpcomingBillsList = upcomingBillDtos,
            PendingObligationsList = pendingObligationDtos,
            Alerts = alerts
        };
    }

    public decimal CalculateTotalIncome(IEnumerable<IncomeTransaction> incomes)
    {
        return incomes
            .Where(i => i.Status == TransactionStatus.Confirmed)
            .Sum(i => i.Amount);
    }

    public decimal CalculateTotalExpenses(IEnumerable<ExpenseTransaction> expenses)
    {
        return expenses
            .Where(e => e.Status == TransactionStatus.Confirmed && (!e.NeedsApproval || e.IsApproved))
            .Sum(e => e.Amount);
    }

    public decimal CalculateNetCashFlow(decimal confirmedIncome, decimal confirmedExpenses)
    {
        return confirmedIncome - confirmedExpenses;
    }

    public decimal CalculateRemainingBudget(decimal plannedBudget, decimal actualExpenses)
    {
        return Math.Max(0.0m, plannedBudget - actualExpenses);
    }

    public decimal CalculateBudgetUtilization(decimal plannedBudget, decimal actualExpenses)
    {
        if (plannedBudget <= 0.0m) return 0.0m;
        return Math.Round((actualExpenses / plannedBudget) * 100.0m, 1);
    }

    public decimal CalculateOutstandingDebtBalance(IEnumerable<Debt> debts)
    {
        return debts
            .Where(d => d.Status == DebtStatus.Active || d.Status == DebtStatus.PartiallyRepaid || d.Status == DebtStatus.Overdue)
            .Sum(d => CalculateRecalculatedDebtBalance(d.OriginalPrincipal, d.Repayments));
    }

    public decimal CalculateRecalculatedDebtBalance(decimal originalPrincipal, IEnumerable<DebtRepayment> repayments)
    {
        var principalRepaid = repayments.Sum(r => r.PrincipalPortion);
        return Math.Max(0.0m, originalPrincipal - principalRepaid);
    }

    public DateTime CalculateNextBillDueDate(DateTime currentDueDate, BillFrequency frequency)
    {
        return frequency switch
        {
            BillFrequency.Weekly => currentDueDate.AddDays(7),
            BillFrequency.Monthly => AddMonthsSafely(currentDueDate, 1),
            BillFrequency.Quarterly => AddMonthsSafely(currentDueDate, 3),
            BillFrequency.HalfYearly => AddMonthsSafely(currentDueDate, 6),
            BillFrequency.Annually => AddMonthsSafely(currentDueDate, 12),
            _ => currentDueDate.AddMonths(1)
        };
    }

    private static DateTime AddMonthsSafely(DateTime date, int monthsToAdd)
    {
        int targetYear = date.Year;
        int targetMonth = date.Month + monthsToAdd;

        while (targetMonth > 12)
        {
            targetMonth -= 12;
            targetYear++;
        }
        while (targetMonth < 1)
        {
            targetMonth += 12;
            targetYear--;
        }

        // Clamp day to max days in target month (e.g. Jan 31 -> Feb 28 or 29)
        int daysInTargetMonth = DateTime.DaysInMonth(targetYear, targetMonth);
        int targetDay = Math.Min(date.Day, daysInTargetMonth);

        return new DateTime(targetYear, targetMonth, targetDay, date.Hour, date.Minute, date.Second, date.Kind);
    }

    public bool ValidateExpenseSplits(decimal totalExpenseAmount, IEnumerable<ExpenseSplit> splits)
    {
        var splitList = splits.ToList();
        if (!splitList.Any()) return true;

        var sum = splitList.Sum(s => s.SplitAmount);
        return Math.Abs(totalExpenseAmount - sum) <= 0.01m; // 1 cent/paise rounding tolerance
    }

    public List<MemberSpendingShareDto> CalculateMemberBreakdown(
        IEnumerable<ExpenseTransaction> expenses,
        IEnumerable<IncomeTransaction> incomes,
        IEnumerable<FamilyMembership> members)
    {
        var expenseList = expenses.Where(e => e.Status == TransactionStatus.Confirmed).ToList();
        var incomeList = incomes.Where(i => i.Status == TransactionStatus.Confirmed).ToList();
        var result = new List<MemberSpendingShareDto>();

        foreach (var member in members.Where(m => m.IsActive))
        {
            var paid = expenseList.Where(e => e.PaidByMemberId == member.Id).Sum(e => e.Amount);
            var income = incomeList.Where(i => i.RecipientMemberId == member.Id).Sum(i => i.Amount);
            var name = member.User?.DisplayName ?? member.User?.FullName ?? "Family Member";

            result.Add(new MemberSpendingShareDto
            {
                MemberId = member.Id,
                MemberName = name,
                Role = member.Role.ToString(),
                TotalPaid = paid,
                TotalIncomeContributed = income
            });
        }

        return result;
    }
}
