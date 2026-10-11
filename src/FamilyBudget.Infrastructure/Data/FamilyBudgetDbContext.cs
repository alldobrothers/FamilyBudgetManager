using FamilyBudget.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FamilyBudget.Infrastructure.Data;

public class FamilyBudgetDbContext : IdentityDbContext<ApplicationUser>
{
    public FamilyBudgetDbContext(DbContextOptions<FamilyBudgetDbContext> options)
        : base(options)
    {
    }

    public DbSet<Family> Families => Set<Family>();
    public DbSet<FamilyMembership> FamilyMemberships => Set<FamilyMembership>();
    public DbSet<FamilyInvitation> FamilyInvitations => Set<FamilyInvitation>();
    public DbSet<FamilySettings> FamilySettings => Set<FamilySettings>();
    public DbSet<FinancialCategory> FinancialCategories => Set<FinancialCategory>();
    public DbSet<ExpenseTransaction> ExpenseTransactions => Set<ExpenseTransaction>();
    public DbSet<ExpenseSplit> ExpenseSplits => Set<ExpenseSplit>();
    public DbSet<IncomeTransaction> IncomeTransactions => Set<IncomeTransaction>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetCategory> BudgetCategories => Set<BudgetCategory>();
    public DbSet<SavingsGoal> SavingsGoals => Set<SavingsGoal>();
    public DbSet<SavingsContribution> SavingsContributions => Set<SavingsContribution>();
    public DbSet<Debt> Debts => Set<Debt>();
    public DbSet<DebtRepayment> DebtRepayments => Set<DebtRepayment>();
    public DbSet<BillTemplate> BillTemplates => Set<BillTemplate>();
    public DbSet<BillOccurrence> BillOccurrences => Set<BillOccurrence>();
    public DbSet<PaymentObligation> PaymentObligations => Set<PaymentObligation>();
    public DbSet<InAppNotification> InAppNotifications => Set<InAppNotification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 1. Family
        builder.Entity<Family>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(120);
            entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(10);
            entity.Property(e => e.CurrencySymbol).IsRequired().HasMaxLength(5);
            entity.Property(e => e.Country).HasMaxLength(80);
            entity.Property(e => e.TimeZoneId).HasMaxLength(80);
            entity.Property(e => e.MonthlyBudget).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Settings)
                  .WithOne(e => e.Family)
                  .HasForeignKey<FamilySettings>(e => e.FamilyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 2. FamilyMembership
        builder.Entity<FamilyMembership>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AllocatedPersonalBudget).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => new { e.FamilyId, e.UserId }).IsUnique();

            entity.HasOne(e => e.Family)
                  .WithMany(f => f.Memberships)
                  .HasForeignKey(e => e.FamilyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Memberships)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 3. FamilyInvitation
        builder.Entity<FamilyInvitation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvitedEmail).IsRequired().HasMaxLength(256);
            entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(128);
            entity.HasIndex(e => e.TokenHash);

            entity.HasOne(e => e.Family)
                  .WithMany(f => f.Invitations)
                  .HasForeignKey(e => e.FamilyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.InvitedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.InvitedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AcceptedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.AcceptedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 4. FamilySettings
        builder.Entity<FamilySettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BudgetWarningThreshold1).HasColumnType("decimal(5,2)");
            entity.Property(e => e.BudgetWarningThreshold2).HasColumnType("decimal(5,2)");
        });

        // 5. FinancialCategory
        builder.Entity<FinancialCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ColorHex).IsRequired().HasMaxLength(20);
            entity.Property(e => e.IconClass).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(250);

            entity.HasOne(e => e.Family)
                  .WithMany(f => f.CustomCategories)
                  .HasForeignKey(e => e.FamilyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.FamilyId, e.Name, e.Type });
        });

        // 6. ExpenseTransaction
        builder.Entity<ExpenseTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Amount).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(50);
            entity.Property(e => e.MerchantOrShop).HasMaxLength(120);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.ReceiptPath).HasMaxLength(500);
            entity.Property(e => e.Tags).HasMaxLength(200);

            entity.HasOne(e => e.Family)
                  .WithMany(f => f.Expenses)
                  .HasForeignKey(e => e.FamilyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Expenses)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PaidByMember)
                  .WithMany(m => m.PaidExpenses)
                  .HasForeignKey(e => e.PaidByMemberId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ApprovedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.ApprovedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.FamilyId, e.ExpenseDate });
            entity.HasIndex(e => new { e.FamilyId, e.CategoryId });
            entity.HasIndex(e => e.PaidByMemberId);
        });

        // 7. ExpenseSplit
        builder.Entity<ExpenseSplit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SplitAmount).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.SplitPercentage).HasColumnType("decimal(5,2)");
            entity.Property(e => e.Notes).HasMaxLength(200);

            entity.HasOne(e => e.ExpenseTransaction)
                  .WithMany(t => t.Splits)
                  .HasForeignKey(e => e.ExpenseTransactionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.BeneficiaryMember)
                  .WithMany(m => m.ExpenseSplits)
                  .HasForeignKey(e => e.BeneficiaryMemberId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 8. IncomeTransaction
        builder.Entity<IncomeTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Amount).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.IncomeSource).IsRequired().HasMaxLength(60);
            entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AccountOrSource).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.AttachmentPath).HasMaxLength(500);

            entity.HasOne(e => e.Family)
                  .WithMany(f => f.Incomes)
                  .HasForeignKey(e => e.FamilyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Incomes)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.RecipientMember)
                  .WithMany(m => m.ReceivedIncomes)
                  .HasForeignKey(e => e.RecipientMemberId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.FamilyId, e.DateReceived });
            entity.HasIndex(e => e.RecipientMemberId);
        });

        // 9. Budget
        builder.Entity<Budget>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalPlannedBudget).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.RolloverAmount).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasIndex(e => new { e.FamilyId, e.PeriodYear, e.PeriodMonth }).IsUnique();

            entity.HasOne(e => e.Family)
                  .WithMany(f => f.Budgets)
                  .HasForeignKey(e => e.FamilyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 10. BudgetCategory
        builder.Entity<BudgetCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PlannedAmount).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.HasIndex(e => new { e.BudgetId, e.CategoryId }).IsUnique();

            entity.HasOne(e => e.Budget)
                  .WithMany(b => b.CategoryAllocations)
                  .HasForeignKey(e => e.BudgetId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                  .WithMany()
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 11. SavingsGoal
        builder.Entity<SavingsGoal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(120);
            entity.Property(e => e.TargetAmount).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.CurrentAmount).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.Category).HasMaxLength(60);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.Family)
                  .WithMany(f => f.SavingsGoals)
                  .HasForeignKey(e => e.FamilyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 12. SavingsContribution
        builder.Entity<SavingsContribution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.Notes).HasMaxLength(300);

            entity.HasOne(e => e.SavingsGoal)
                  .WithMany(g => g.Contributions)
                  .HasForeignKey(e => e.SavingsGoalId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Member)
                  .WithMany()
                  .HasForeignKey(e => e.MemberId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 13. Debt
        builder.Entity<Debt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreditorOrDebtorName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.OriginalPrincipal).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.CurrentBalance).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(10);
            entity.Property(e => e.InterestRate).HasColumnType("decimal(6,3)");
            entity.Property(e => e.RepaymentFrequency).HasMaxLength(30);
            entity.Property(e => e.InstallmentAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.DocumentPath).HasMaxLength(500);

            entity.HasOne(e => e.Family)
                  .WithMany(f => f.Debts)
                  .HasForeignKey(e => e.FamilyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.LinkedMember)
                  .WithMany()
                  .HasForeignKey(e => e.LinkedMemberId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.FamilyId, e.Status });
        });

        // 14. DebtRepayment
        builder.Entity<DebtRepayment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.PrincipalPortion).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.InterestPortion).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.Debt)
                  .WithMany(d => d.Repayments)
                  .HasForeignKey(e => e.DebtId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.RepaidByMember)
                  .WithMany()
                  .HasForeignKey(e => e.RepaidByMemberId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // 15. BillTemplate
        builder.Entity<BillTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BillName).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Payee).IsRequired().HasMaxLength(120);
            entity.Property(e => e.ExpectedAmount).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.Family)
                  .WithMany(f => f.BillTemplates)
                  .HasForeignKey(e => e.FamilyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                  .WithMany()
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ResponsibleMember)
                  .WithMany()
                  .HasForeignKey(e => e.ResponsibleMemberId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // 16. BillOccurrence
        builder.Entity<BillOccurrence>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExpectedAmount).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.ActualAmountPaid).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.Notes).HasMaxLength(300);

            entity.HasIndex(e => new { e.BillTemplateId, e.DueDate }).IsUnique();
            entity.HasIndex(e => new { e.FamilyId, e.DueDate, e.Status });

            entity.HasOne(e => e.BillTemplate)
                  .WithMany(b => b.Occurrences)
                  .HasForeignKey(e => e.BillTemplateId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Family)
                  .WithMany()
                  .HasForeignKey(e => e.FamilyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ExpenseTransaction)
                  .WithMany()
                  .HasForeignKey(e => e.ExpenseTransactionId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // 17. PaymentObligation
        builder.Entity<PaymentObligation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(150);
            entity.Property(e => e.AmountDue).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.AmountPaid).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.PayeeName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.Family)
                  .WithMany(f => f.Obligations)
                  .HasForeignKey(e => e.FamilyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ResponsibleMember)
                  .WithMany()
                  .HasForeignKey(e => e.ResponsibleMemberId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.RelatedBillOccurrence)
                  .WithMany()
                  .HasForeignKey(e => e.RelatedBillOccurrenceId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.RelatedExpense)
                  .WithMany()
                  .HasForeignKey(e => e.RelatedExpenseId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.RelatedDebt)
                  .WithMany()
                  .HasForeignKey(e => e.RelatedDebtId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.FamilyId, e.DueDate, e.Status });
        });

        // 18. InAppNotification
        builder.Entity<InAppNotification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(500);
            entity.Property(e => e.DeepLinkUrl).HasMaxLength(250);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Notifications)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Family)
                  .WithMany()
                  .HasForeignKey(e => e.FamilyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.IsRead });
        });

        // 19. AuditLog
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ActionType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EntityName).IsRequired().HasMaxLength(60);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(45);

            entity.HasOne(e => e.Family)
                  .WithMany()
                  .HasForeignKey(e => e.FamilyId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.FamilyId, e.Timestamp });
        });

        // 20. UserPreference
        builder.Entity<UserPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PreferredTheme).IsRequired().HasMaxLength(20);
            entity.Property(e => e.PreferredCurrency).IsRequired().HasMaxLength(10);
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.HasOne(e => e.User)
                  .WithOne(u => u.Preference)
                  .HasForeignKey<UserPreference>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
