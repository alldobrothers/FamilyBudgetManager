using FamilyBudget.Application.DTOs;
using FamilyBudget.Domain.Entities;
using FamilyBudget.Domain.Enums;

namespace FamilyBudget.Application.Interfaces;

public interface IFinancialCalculationService
{
    DashboardSummaryDto CalculateDashboardMetrics(
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
        DateTime periodEnd);

    decimal CalculateTotalIncome(IEnumerable<IncomeTransaction> incomes);
    decimal CalculateTotalExpenses(IEnumerable<ExpenseTransaction> expenses);
    decimal CalculateNetCashFlow(decimal confirmedIncome, decimal confirmedExpenses);
    decimal CalculateRemainingBudget(decimal plannedBudget, decimal actualExpenses);
    decimal CalculateBudgetUtilization(decimal plannedBudget, decimal actualExpenses);

    decimal CalculateOutstandingDebtBalance(IEnumerable<Debt> debts);
    decimal CalculateRecalculatedDebtBalance(decimal originalPrincipal, IEnumerable<DebtRepayment> repayments);

    DateTime CalculateNextBillDueDate(DateTime currentDueDate, BillFrequency frequency);
    bool ValidateExpenseSplits(decimal totalExpenseAmount, IEnumerable<ExpenseSplit> splits);

    List<MemberSpendingShareDto> CalculateMemberBreakdown(
        IEnumerable<ExpenseTransaction> expenses,
        IEnumerable<IncomeTransaction> incomes,
        IEnumerable<FamilyMembership> members);
}
