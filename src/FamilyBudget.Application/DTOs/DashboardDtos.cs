namespace FamilyBudget.Application.DTOs;

public class DashboardSummaryDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetCashFlow { get; set; }
    public decimal TotalBudget { get; set; }
    public decimal RemainingBudget { get; set; }
    public decimal BudgetUtilizationPercentage { get; set; }
    public decimal OutstandingDebt { get; set; }
    public decimal UpcomingBillsTotal { get; set; }
    public decimal PendingPaymentsTotal { get; set; }
    public decimal EstimatedAvailableCash { get; set; }
    public string CurrencyCode { get; set; } = "INR";
    public string CurrencySymbol { get; set; } = "₹";

    // Chart Data structures
    public List<MonthlyTrendPointDto> MonthlySpendingTrend { get; set; } = new();
    public List<CategoryExpenseShareDto> CategoryExpenses { get; set; } = new();
    public List<MemberSpendingShareDto> MemberExpenses { get; set; } = new();
    public List<RecentTransactionDto> RecentTransactions { get; set; } = new();
    public List<UpcomingBillDto> UpcomingBillsList { get; set; } = new();
    public List<PendingObligationDto> PendingObligationsList { get; set; } = new();
    public List<BudgetUtilizationBarDto> BudgetProgressBars { get; set; } = new();
    public List<DashboardAlertDto> Alerts { get; set; } = new();
}

public class MonthlyTrendPointDto
{
    public string MonthName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal NetSavings => Income - Expenses;
}

public class CategoryExpenseShareDto
{
    public string CategoryName { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#3B82F6";
    public string IconClass { get; set; } = "bi-tag";
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}

public class MemberSpendingShareDto
{
    public string MemberId { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public decimal TotalPaid { get; set; }
    public decimal TotalIncomeContributed { get; set; }
    public decimal NetBalance => TotalIncomeContributed - TotalPaid;
}

public class RecentTransactionDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = "Expense"; // "Expense" or "Income"
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = "#3B82F6";
    public string PaidByOrRecipient { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}

public class UpcomingBillDto
{
    public string OccurrenceId { get; set; } = string.Empty;
    public string BillName { get; set; } = string.Empty;
    public string Payee { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public int DaysRemaining { get; set; }
    public bool IsOverdue => DaysRemaining < 0;
    public string Status { get; set; } = "Pending";
}

public class PendingObligationDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PayeeOrBeneficiary { get; set; } = string.Empty;
    public decimal AmountDue { get; set; }
    public decimal RemainingBalance { get; set; }
    public DateTime DueDate { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Pending";
}

public class BudgetUtilizationBarDto
{
    public string CategoryName { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#3B82F6";
    public decimal PlannedAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal RemainingAmount => Math.Max(0.0m, PlannedAmount - SpentAmount);
    public decimal PercentageUsed => PlannedAmount > 0 ? Math.Round((SpentAmount / PlannedAmount) * 100.0m, 1) : 0;
    public bool IsOverBudget => SpentAmount > PlannedAmount;
}

public class DashboardAlertDto
{
    public string Type { get; set; } = "warning"; // "warning", "danger", "info"
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public string? ActionText { get; set; }
}
