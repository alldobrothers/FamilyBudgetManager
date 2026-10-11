namespace FamilyBudget.Domain.Enums;

public enum FamilyRole
{
    Owner = 1,
    Admin = 2,
    Member = 3,
    Viewer = 4
}

public enum CategoryType
{
    Expense = 1,
    Income = 2
}

public enum TransactionStatus
{
    Confirmed = 1,
    PendingApproval = 2,
    Rejected = 3,
    Reversed = 4
}

public enum SplitType
{
    Equal = 1,
    FixedAmount = 2,
    Percentage = 3
}

public enum DebtType
{
    Borrowed = 1, // Liability: Money family or member owes to an external lender/member
    Lent = 2     // Receivable: Money family or member gave out and expects back
}

public enum DebtStatus
{
    Active = 1,
    PartiallyRepaid = 2,
    PaidOff = 3,
    Overdue = 4,
    Cancelled = 5
}

public enum InterestType
{
    None = 0,
    SimpleInterest = 1
}

public enum BillFrequency
{
    Weekly = 1,
    Monthly = 2,
    Quarterly = 3,
    HalfYearly = 4,
    Annually = 5
}

public enum BillOccurrenceStatus
{
    Pending = 1,
    Paid = 2,
    PartiallyPaid = 3,
    Skipped = 4,
    Overdue = 5
}

public enum ObligationType
{
    Bill = 1,
    MemberReimbursement = 2,
    PersonalDebt = 3,
    HouseholdExpense = 4,
    Other = 5
}

public enum ObligationPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4
}

public enum ObligationStatus
{
    Pending = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overdue = 4,
    Cancelled = 5
}

public enum SavingsGoalStatus
{
    Active = 1,
    Achieved = 2,
    Paused = 3
}

public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Expired = 2,
    Revoked = 3
}

public enum NotificationType
{
    BillReminder = 1,
    BudgetAlert = 2,
    DebtDue = 3,
    Invitation = 4,
    ExpenseAdded = 5,
    System = 6
}
