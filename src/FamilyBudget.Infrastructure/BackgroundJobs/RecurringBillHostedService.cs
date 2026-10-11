using FamilyBudget.Application.Interfaces;
using FamilyBudget.Domain.Entities;
using FamilyBudget.Domain.Enums;
using FamilyBudget.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyBudget.Infrastructure.BackgroundJobs;

public class RecurringBillHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecurringBillHostedService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);

    public RecurringBillHostedService(
        IServiceProvider serviceProvider,
        ILogger<RecurringBillHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RecurringBillHostedService background worker initialized.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRecurringBillsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unhandled error during recurring bills background check.");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("RecurringBillHostedService background worker stopped.");
    }

    public async Task ProcessRecurringBillsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FamilyBudgetDbContext>();
        var calcService = scope.ServiceProvider.GetRequiredService<IFinancialCalculationService>();

        var today = DateTime.UtcNow.Date;
        var horizon = today.AddDays(30); // Look ahead 30 days

        var activeBills = await context.BillTemplates
            .Include(b => b.Occurrences)
            .Include(b => b.Family)
            .Where(b => b.IsActive)
            .ToListAsync(cancellationToken);

        int occurrencesGenerated = 0;

        foreach (var bill in activeBills)
        {
            var dueDate = bill.NextDueDate.Date;

            // Generate occurrence if within 30-day horizon and doesn't already exist
            if (dueDate <= horizon)
            {
                bool alreadyExists = bill.Occurrences.Any(o => o.DueDate.Date == dueDate);
                if (!alreadyExists)
                {
                    var occurrence = new BillOccurrence
                    {
                        BillTemplateId = bill.Id,
                        FamilyId = bill.FamilyId,
                        DueDate = dueDate,
                        ExpectedAmount = bill.ExpectedAmount,
                        ActualAmountPaid = 0.0m,
                        Status = dueDate < today ? BillOccurrenceStatus.Overdue : BillOccurrenceStatus.Pending
                    };

                    context.BillOccurrences.Add(occurrence);
                    occurrencesGenerated++;

                    // Advance template's NextDueDate to subsequent cycle
                    bill.NextDueDate = calcService.CalculateNextBillDueDate(dueDate, bill.Frequency);
                }
            }

            // Mark existing pending occurrences past due as Overdue
            foreach (var occ in bill.Occurrences.Where(o => o.Status == BillOccurrenceStatus.Pending && o.DueDate.Date < today))
            {
                occ.Status = BillOccurrenceStatus.Overdue;
            }
        }

        if (occurrencesGenerated > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Generated {Count} upcoming recurring bill occurrences.", occurrencesGenerated);
        }
    }
}
