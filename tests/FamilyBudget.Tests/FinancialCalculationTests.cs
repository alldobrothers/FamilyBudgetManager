using FamilyBudget.Application.Services;
using FamilyBudget.Domain.Entities;
using FamilyBudget.Domain.Enums;
using Xunit;

namespace FamilyBudget.Tests;

public class FinancialCalculationTests
{
    private readonly FinancialCalculationService _calcService = new();

    [Fact]
    public void CalculateTotalIncome_OnlyIncludesConfirmedTransactions()
    {
        var incomes = new List<IncomeTransaction>
        {
            new() { Amount = 50000m, Status = TransactionStatus.Confirmed },
            new() { Amount = 15000m, Status = TransactionStatus.Confirmed },
            new() { Amount = 8000m, Status = TransactionStatus.PendingApproval },
            new() { Amount = 5000m, Status = TransactionStatus.Reversed }
        };

        var total = _calcService.CalculateTotalIncome(incomes);

        Assert.Equal(65000m, total);
    }

    [Fact]
    public void CalculateTotalExpenses_OnlyIncludesConfirmedAndApprovedExpenses()
    {
        var expenses = new List<ExpenseTransaction>
        {
            new() { Amount = 12000m, Status = TransactionStatus.Confirmed, NeedsApproval = false, IsApproved = true },
            new() { Amount = 3500m, Status = TransactionStatus.Confirmed, NeedsApproval = true, IsApproved = true },
            new() { Amount = 9000m, Status = TransactionStatus.Confirmed, NeedsApproval = true, IsApproved = false },
            new() { Amount = 4000m, Status = TransactionStatus.Reversed, NeedsApproval = false, IsApproved = true }
        };

        var total = _calcService.CalculateTotalExpenses(expenses);

        Assert.Equal(15500m, total);
    }

    [Fact]
    public void CalculateNetCashFlow_CorrectlySubtractsOutflowsFromInflows()
    {
        decimal income = 85000m;
        decimal expenses = 42350m;

        var netCashFlow = _calcService.CalculateNetCashFlow(income, expenses);

        Assert.Equal(42650m, netCashFlow);
    }

    [Fact]
    public void CalculateRemainingBudget_ClampsAtZeroWhenOverspent()
    {
        decimal planned = 50000m;
        decimal spentUnder = 35000m;
        decimal spentOver = 54000m;

        var remainingUnder = _calcService.CalculateRemainingBudget(planned, spentUnder);
        var remainingOver = _calcService.CalculateRemainingBudget(planned, spentOver);

        Assert.Equal(15000m, remainingUnder);
        Assert.Equal(0m, remainingOver);
    }

    [Fact]
    public void CalculateBudgetUtilization_ComputesAccuratePercentage()
    {
        decimal planned = 40000m;
        decimal spent = 30000m;

        var util = _calcService.CalculateBudgetUtilization(planned, spent);

        Assert.Equal(75.0m, util);
    }

    [Fact]
    public void CalculateRecalculatedDebtBalance_SubtractsPrincipalRepayments()
    {
        decimal original = 50000m;
        var repayments = new List<DebtRepayment>
        {
            new() { PrincipalPortion = 15000m, InterestPortion = 1200m },
            new() { PrincipalPortion = 10000m, InterestPortion = 800m }
        };

        var balance = _calcService.CalculateRecalculatedDebtBalance(original, repayments);

        Assert.Equal(25000m, balance);
    }

    [Fact]
    public void CalculateRecalculatedDebtBalance_ClampsAtZeroIfRepaymentsMeetOrExceedPrincipal()
    {
        decimal original = 20000m;
        var repayments = new List<DebtRepayment>
        {
            new() { PrincipalPortion = 25000m, InterestPortion = 0m }
        };

        var balance = _calcService.CalculateRecalculatedDebtBalance(original, repayments);

        Assert.Equal(0m, balance);
    }

    [Fact]
    public void CalculateNextBillDueDate_SafelyHandlesMonthEndLeapYearBoundaries()
    {
        // Jan 31 -> Feb 28 (non leap year 2025)
        var jan31_2025 = new DateTime(2025, 1, 31);
        var nextDate2025 = _calcService.CalculateNextBillDueDate(jan31_2025, BillFrequency.Monthly);
        Assert.Equal(new DateTime(2025, 2, 28), nextDate2025);

        // Jan 31 -> Feb 29 (leap year 2024)
        var jan31_2024 = new DateTime(2024, 1, 31);
        var nextDate2024 = _calcService.CalculateNextBillDueDate(jan31_2024, BillFrequency.Monthly);
        Assert.Equal(new DateTime(2024, 2, 29), nextDate2024);

        // Quarterly: Nov 30 -> Feb 28
        var nov30 = new DateTime(2024, 11, 30);
        var quarterlyNext = _calcService.CalculateNextBillDueDate(nov30, BillFrequency.Quarterly);
        Assert.Equal(new DateTime(2025, 2, 28), quarterlyNext);
    }

    [Fact]
    public void ValidateExpenseSplits_ValidatesCorrectSumsAndFlagsDiscrepancies()
    {
        decimal total = 1000m;
        var validSplits = new List<ExpenseSplit>
        {
            new() { SplitAmount = 500m },
            new() { SplitAmount = 300m },
            new() { SplitAmount = 200m }
        };

        var invalidSplits = new List<ExpenseSplit>
        {
            new() { SplitAmount = 500m },
            new() { SplitAmount = 300m } // Sum = 800 != 1000
        };

        Assert.True(_calcService.ValidateExpenseSplits(total, validSplits));
        Assert.False(_calcService.ValidateExpenseSplits(total, invalidSplits));
    }

    [Fact]
    public void CalculateMemberBreakdown_AttributesExpensesAndIncomeToCorrectMembers()
    {
        var member1 = new FamilyMembership { Id = "m1", User = new ApplicationUser { DisplayName = "Member 1" } };
        var member2 = new FamilyMembership { Id = "m2", User = new ApplicationUser { DisplayName = "Member 2" } };
        var members = new List<FamilyMembership> { member1, member2 };

        var expenses = new List<ExpenseTransaction>
        {
            new() { PaidByMemberId = "m1", Amount = 1000m, Status = TransactionStatus.Confirmed },
            new() { PaidByMemberId = "m2", Amount = 400m, Status = TransactionStatus.Confirmed }
        };

        var incomes = new List<IncomeTransaction>
        {
            new() { RecipientMemberId = "m1", Amount = 5000m, Status = TransactionStatus.Confirmed },
            new() { RecipientMemberId = "m2", Amount = 3000m, Status = TransactionStatus.Confirmed }
        };

        var breakdown = _calcService.CalculateMemberBreakdown(expenses, incomes, members);

        var m1Summary = breakdown.First(m => m.MemberId == "m1");
        var m2Summary = breakdown.First(m => m.MemberId == "m2");

        Assert.Equal(1000m, m1Summary.TotalPaid);
        Assert.Equal(5000m, m1Summary.TotalIncomeContributed);
        Assert.Equal(4000m, m1Summary.NetBalance);

        Assert.Equal(400m, m2Summary.TotalPaid);
        Assert.Equal(3000m, m2Summary.TotalIncomeContributed);
        Assert.Equal(2600m, m2Summary.NetBalance);
    }
}
