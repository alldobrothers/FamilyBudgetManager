using FamilyBudget.Domain.Entities;
using FamilyBudget.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamilyBudget.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        FamilyBudgetDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger logger,
        bool isDevelopment = false)
    {
        try
        {
            await context.Database.EnsureCreatedAsync();

            // 1. Seed System Default Categories
            if (!await context.FinancialCategories.AnyAsync(c => c.IsSystemDefault))
            {
                logger.LogInformation("Seeding default financial categories...");
                var defaultCategories = GetDefaultCategories();
                await context.FinancialCategories.AddRangeAsync(defaultCategories);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} default financial categories.", defaultCategories.Count);
            }

            // 2. Seed Identity Roles if not exist
            string[] systemRoles = ["Owner", "Admin", "Member", "Viewer"];
            foreach (var role in systemRoles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 3. Seed Realistic Demo Data only in Development
            if (isDevelopment && !await userManager.Users.AnyAsync())
            {
                logger.LogInformation("Seeding development demo account and sample household...");
                await SeedDevelopmentDataAsync(context, userManager, logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }

    public static List<FinancialCategory> GetDefaultCategories()
    {
        return new List<FinancialCategory>
        {
            // Expense categories
            new() { Name = "Groceries", Description = "Food, vegetables, supermarkets", ColorHex = "#10B981", IconClass = "bi-cart4", Type = CategoryType.Expense, IsSystemDefault = true },
            new() { Name = "Rent & Housing", Description = "Monthly rent, home EMI, society dues", ColorHex = "#3B82F6", IconClass = "bi-house-door", Type = CategoryType.Expense, IsSystemDefault = true },
            new() { Name = "Electricity & Power", Description = "Electricity bills", ColorHex = "#F59E0B", IconClass = "bi-lightning-charge", Type = CategoryType.Expense, IsSystemDefault = true },
            new() { Name = "Water Supply", Description = "Water utility bills", ColorHex = "#06B6D4", IconClass = "bi-droplet", Type = CategoryType.Expense, IsSystemDefault = true },
            new() { Name = "Gas & Cooking", Description = "LPG cylinders, piped gas", ColorHex = "#EF4444", IconClass = "bi-fire", Type = CategoryType.Expense, IsSystemDefault = true },
            new() { Name = "Internet & Mobile", Description = "Broadband, cellular mobile recharges", ColorHex = "#6366F1", IconClass = "bi-wifi", Type = CategoryType.Expense, IsSystemDefault = true },
            new() { Name = "Transportation", Description = "Metro, cab, auto, bus fares", ColorHex = "#8B5CF6", IconClass = "bi-car-front", Type = CategoryType.Expense, IsSystemDefault = true },
            new() { Name = "Fuel & Petrol", Description = "Petrol, diesel, vehicle fuel", ColorHex = "#EC4899", IconClass = "bi-fuel-pump", Type = CategoryType.Expense, IsSystemDefault = true },
            new() { Name = "Education & Tuition", Description = "School fees, tuition, books", ColorHex = "#14B8A6", IconClass = "bi-mortarboard", Type = CategoryType.Expense, IsSystemDefault = true },
            new() { Name = "Medical & Health", Description = "Doctor visits, pharmacy, medicines", ColorHex = "#F43F5E", IconClass = "bi-heart-pulse", Type = CategoryType.Expense, IsSystemDefault = true },
            new() { Name = "Insurance", Description = "Health, vehicle, life insurance", ColorHex = "#64748B", IconClass = "bi-shield-check", Type = CategoryType.Expense, IsSystemDefault = true },
            new() { Name = "Household Supplies", Description = "Cleaning items, cookware, repairs", ColorHex = "#84CC16", IconClass = "bi-basket", Type = CategoryType.Expense, IsSystemDefault = true },
            new() { Name = "Shopping & Apparel", Description = "Clothes, footwear, electronics", ColorHex = "#D946EF", IconClass = "bi-bag", Type = CategoryType.Expense, IsSystemDefault = true },
            new() { Name = "Dining Out", Description = "Restaurants, cafes, food delivery", ColorHex = "#F97316", IconClass = "bi-cup-hot", Type = CategoryType.Expense, IsSystemDefault = true },
            new() { Name = "Entertainment", Description = "Movies, events, outings", ColorHex = "#A855F7", IconClass = "bi-film", Type = CategoryType.Expense, IsSystemDefault = true },
            new() { Name = "Subscriptions", Description = "OTT (Netflix, Prime), software", ColorHex = "#6366F1", IconClass = "bi-bell", Type = CategoryType.Expense, IsSystemDefault = true },
            new() { Name = "Childcare", Description = "Daycare, baby supplies", ColorHex = "#0EA5E9", IconClass = "bi-emoji-smile", Type = CategoryType.Expense, IsSystemDefault = true },
            new() { Name = "Family Events & Gifts", Description = "Festivals, birthdays, gifts", ColorHex = "#F472B6", IconClass = "bi-gift", Type = CategoryType.Expense, IsSystemDefault = true },
            new() { Name = "Travel & Vacation", Description = "Trips, hotel bookings, flights", ColorHex = "#0284C7", IconClass = "bi-airplane", Type = CategoryType.Expense, IsSystemDefault = true },
            new() { Name = "Miscellaneous", Description = "Other general household expenses", ColorHex = "#94A3B8", IconClass = "bi-three-dots", Type = CategoryType.Expense, IsSystemDefault = true },

            // Income categories
            new() { Name = "Salary & Wages", Description = "Primary employer salary", ColorHex = "#10B981", IconClass = "bi-cash-stack", Type = CategoryType.Income, IsSystemDefault = true },
            new() { Name = "Business Income", Description = "Business revenue or profit", ColorHex = "#3B82F6", IconClass = "bi-briefcase", Type = CategoryType.Income, IsSystemDefault = true },
            new() { Name = "Freelance & Consulting", Description = "Independent client projects", ColorHex = "#8B5CF6", IconClass = "bi-laptop", Type = CategoryType.Income, IsSystemDefault = true },
            new() { Name = "Rental Income", Description = "Property tenant rental income", ColorHex = "#F59E0B", IconClass = "bi-building", Type = CategoryType.Income, IsSystemDefault = true },
            new() { Name = "Investments & Dividends", Description = "Mutual funds, stock dividends, interest", ColorHex = "#06B6D4", IconClass = "bi-graph-up-arrow", Type = CategoryType.Income, IsSystemDefault = true },
            new() { Name = "Gifts & Grants", Description = "Monetary gifts from relatives", ColorHex = "#EC4899", IconClass = "bi-gift", Type = CategoryType.Income, IsSystemDefault = true },
            new() { Name = "Other Income", Description = "Cashbacks, tax refunds, miscellaneous", ColorHex = "#64748B", IconClass = "bi-wallet2", Type = CategoryType.Income, IsSystemDefault = true }
        };
    }

    private static async Task SeedDevelopmentDataAsync(
        FamilyBudgetDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        // Primary demo user (Parent / Family Owner)
        var ownerUser = new ApplicationUser
        {
            UserName = "demo@familybudget.local",
            Email = "demo@familybudget.local",
            FullName = "Rajesh Sharma",
            DisplayName = "Rajesh",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };
        var result1 = await userManager.CreateAsync(ownerUser, "Demo@123456");

        // Secondary demo user (Spouse / Family Admin)
        var memberUser = new ApplicationUser
        {
            UserName = "priya@familybudget.local",
            Email = "priya@familybudget.local",
            FullName = "Priya Sharma",
            DisplayName = "Priya",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };
        var result2 = await userManager.CreateAsync(memberUser, "Demo@123456");

        if (!result1.Succeeded || !result2.Succeeded)
        {
            logger.LogWarning("Failed to create demo users.");
            return;
        }

        // Create Family
        var family = new Family
        {
            Name = "Sharma Household",
            CurrencyCode = "INR",
            CurrencySymbol = "₹",
            Country = "India",
            TimeZoneId = "India Standard Time",
            MonthStartDay = 1,
            MonthlyBudget = 75000.0m,
            Description = "Our primary household family budget",
            CreatedByUserId = ownerUser.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Families.Add(family);
        await context.SaveChangesAsync();

        // Family Settings
        var settings = new FamilySettings
        {
            FamilyId = family.Id,
            EnableExpenseApprovals = false,
            BudgetWarningThreshold1 = 75.0m,
            BudgetWarningThreshold2 = 90.0m,
            NotifyOnOverdueBills = true,
            DarkModeDefault = false
        };
        context.FamilySettings.Add(settings);

        // Memberships
        var ownerMembership = new FamilyMembership
        {
            FamilyId = family.Id,
            UserId = ownerUser.Id,
            Role = FamilyRole.Owner,
            JoinedAt = DateTime.UtcNow,
            CanApproveExpenses = true,
            CanManageBills = true,
            CanViewDebts = true,
            CanExportReports = true
        };

        var partnerMembership = new FamilyMembership
        {
            FamilyId = family.Id,
            UserId = memberUser.Id,
            Role = FamilyRole.Admin,
            JoinedAt = DateTime.UtcNow,
            CanApproveExpenses = true,
            CanManageBills = true,
            CanViewDebts = true,
            CanExportReports = true
        };

        context.FamilyMemberships.AddRange(ownerMembership, partnerMembership);
        await context.SaveChangesAsync();

        // User Preferences
        context.UserPreferences.AddRange(
            new UserPreference { UserId = ownerUser.Id, DefaultFamilyId = family.Id, PreferredCurrency = "INR", PreferredTheme = "light" },
            new UserPreference { UserId = memberUser.Id, DefaultFamilyId = family.Id, PreferredCurrency = "INR", PreferredTheme = "light" }
        );

        // Fetch categories
        var groceryCat = await context.FinancialCategories.FirstAsync(c => c.Name == "Groceries");
        var rentCat = await context.FinancialCategories.FirstAsync(c => c.Name == "Rent & Housing");
        var electricityCat = await context.FinancialCategories.FirstAsync(c => c.Name == "Electricity & Power");
        var diningCat = await context.FinancialCategories.FirstAsync(c => c.Name == "Dining Out");
        var salaryCat = await context.FinancialCategories.FirstAsync(c => c.Name == "Salary & Wages");

        // Seed current month Budget
        var currentYear = DateTime.UtcNow.Year;
        var currentMonth = DateTime.UtcNow.Month;
        var budget = new Budget
        {
            FamilyId = family.Id,
            PeriodYear = currentYear,
            PeriodMonth = currentMonth,
            TotalPlannedBudget = 75000.0m,
            RolloverAmount = 2500.0m,
            Notes = "Budget for household monthly necessities"
        };
        context.Budgets.Add(budget);
        await context.SaveChangesAsync();

        context.BudgetCategories.AddRange(
            new BudgetCategory { BudgetId = budget.Id, CategoryId = groceryCat.Id, PlannedAmount = 15000.0m },
            new BudgetCategory { BudgetId = budget.Id, CategoryId = rentCat.Id, PlannedAmount = 25000.0m },
            new BudgetCategory { BudgetId = budget.Id, CategoryId = electricityCat.Id, PlannedAmount = 4000.0m },
            new BudgetCategory { BudgetId = budget.Id, CategoryId = diningCat.Id, PlannedAmount = 6000.0m }
        );

        // Seed Income for Rajesh & Priya
        context.IncomeTransactions.AddRange(
            new IncomeTransaction
            {
                FamilyId = family.Id,
                Title = "Rajesh Tech Salary",
                Amount = 85000.0m,
                IncomeSource = "Salary",
                RecipientMemberId = ownerMembership.Id,
                DateReceived = DateTime.UtcNow.AddDays(-10),
                PaymentMethod = "Bank Transfer",
                CategoryId = salaryCat.Id,
                CreatedByUserId = ownerUser.Id
            },
            new IncomeTransaction
            {
                FamilyId = family.Id,
                Title = "Priya Consulting Project",
                Amount = 35000.0m,
                IncomeSource = "Freelance & Consulting",
                RecipientMemberId = partnerMembership.Id,
                DateReceived = DateTime.UtcNow.AddDays(-6),
                PaymentMethod = "UPI",
                CategoryId = salaryCat.Id,
                CreatedByUserId = memberUser.Id
            }
        );

        // Seed Expenses
        var exp1 = new ExpenseTransaction
        {
            FamilyId = family.Id,
            Title = "Monthly Apartment Rent",
            Amount = 25000.0m,
            CategoryId = rentCat.Id,
            PaidByMemberId = ownerMembership.Id,
            ExpenseDate = DateTime.UtcNow.AddDays(-9),
            PaymentMethod = "Net Banking",
            MerchantOrShop = "Landlord Mr. Verma",
            CreatedByUserId = ownerUser.Id
        };

        var exp2 = new ExpenseTransaction
        {
            FamilyId = family.Id,
            Title = "Supermarket Grocery Restock",
            Amount = 6450.0m,
            CategoryId = groceryCat.Id,
            PaidByMemberId = partnerMembership.Id,
            ExpenseDate = DateTime.UtcNow.AddDays(-5),
            PaymentMethod = "UPI",
            MerchantOrShop = "Nature's Basket",
            CreatedByUserId = memberUser.Id
        };

        var exp3 = new ExpenseTransaction
        {
            FamilyId = family.Id,
            Title = "Family Weekend Dinner",
            Amount = 2850.0m,
            CategoryId = diningCat.Id,
            PaidByMemberId = ownerMembership.Id,
            ExpenseDate = DateTime.UtcNow.AddDays(-2),
            PaymentMethod = "Credit Card",
            MerchantOrShop = "Barbeque Nation",
            CreatedByUserId = ownerUser.Id
        };

        context.ExpenseTransactions.AddRange(exp1, exp2, exp3);

        // Seed Recurring Bill
        var bill = new BillTemplate
        {
            FamilyId = family.Id,
            BillName = "BSES Delhi Electricity Bill",
            CategoryId = electricityCat.Id,
            Payee = "BSES Yamuna Power Ltd",
            ExpectedAmount = 3450.0m,
            Frequency = BillFrequency.Monthly,
            BillingStartDate = DateTime.UtcNow.AddMonths(-1),
            NextDueDate = DateTime.UtcNow.AddDays(5),
            ResponsibleMemberId = ownerMembership.Id,
            Notes = "Consumer CA: 100984729"
        };
        context.BillTemplates.Add(bill);
        await context.SaveChangesAsync();

        // Bill Occurrence
        context.BillOccurrences.Add(new BillOccurrence
        {
            BillTemplateId = bill.Id,
            FamilyId = family.Id,
            DueDate = DateTime.UtcNow.AddDays(5),
            ExpectedAmount = 3450.0m,
            Status = BillOccurrenceStatus.Pending
        });

        // Seed Debt
        var debt = new Debt
        {
            FamilyId = family.Id,
            DebtType = DebtType.Borrowed,
            CreditorOrDebtorName = "Uncle Suresh (Home Renovation)",
            LinkedMemberId = ownerMembership.Id,
            OriginalPrincipal = 50000.0m,
            CurrentBalance = 35000.0m,
            CurrencyCode = "INR",
            StartDate = DateTime.UtcNow.AddMonths(-2),
            DueDate = DateTime.UtcNow.AddMonths(4),
            Status = DebtStatus.PartiallyRepaid,
            CreatedByUserId = ownerUser.Id,
            Notes = "Borrowed for bathroom renovation work"
        };
        context.Debts.Add(debt);
        await context.SaveChangesAsync();

        context.DebtRepayments.Add(new DebtRepayment
        {
            DebtId = debt.Id,
            RepaidByMemberId = ownerMembership.Id,
            Amount = 15000.0m,
            PrincipalPortion = 15000.0m,
            InterestPortion = 0.0m,
            PaymentDate = DateTime.UtcNow.AddDays(-15),
            PaymentMethod = "UPI",
            ReferenceNumber = "UPI/2026/0981729",
            Notes = "First installment repaid via UPI"
        });

        // Seed Savings Goal
        var savingsGoal = new SavingsGoal
        {
            FamilyId = family.Id,
            Title = "Emergency Fund (6 Months)",
            TargetAmount = 200000.0m,
            CurrentAmount = 85000.0m,
            TargetDate = DateTime.UtcNow.AddMonths(8),
            Category = "Emergency",
            Status = SavingsGoalStatus.Active,
            CreatedByUserId = ownerUser.Id
        };
        context.SavingsGoals.Add(savingsGoal);
        await context.SaveChangesAsync();

        context.SavingsContributions.Add(new SavingsContribution
        {
            SavingsGoalId = savingsGoal.Id,
            MemberId = ownerMembership.Id,
            Amount = 10000.0m,
            ContributionDate = DateTime.UtcNow.AddDays(-8),
            Notes = "Monthly automated transfer to liquid fund"
        });

        // Seed Pending Payment Obligation
        context.PaymentObligations.Add(new PaymentObligation
        {
            FamilyId = family.Id,
            Title = "Society Maintenance Quarterly Dues",
            AmountDue = 4500.0m,
            DueDate = DateTime.UtcNow.AddDays(12),
            PayeeName = "Emerald Heights RWA",
            ObligationType = ObligationType.HouseholdExpense,
            ResponsibleMemberId = partnerMembership.Id,
            Priority = ObligationPriority.High,
            Status = ObligationStatus.Pending
        });

        // Seed In-App Notification
        context.InAppNotifications.Add(new InAppNotification
        {
            UserId = ownerUser.Id,
            FamilyId = family.Id,
            Title = "Upcoming Bill: Electricity",
            Message = "BSES Delhi Electricity Bill of ₹3,450 is due in 5 days.",
            NotificationType = NotificationType.BillReminder,
            DeepLinkUrl = "/RecurringBills",
            IsRead = false
        });

        await context.SaveChangesAsync();
        logger.LogInformation("Successfully completed seeding development demo data.");
    }
}
