using Microsoft.EntityFrameworkCore;
using Invoqs.API.Data;
using Invoqs.API.Models;
using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;

namespace Invoqs.API.Services;

public class DashboardService : IDashboardService
{
    private readonly InvoqsDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(InvoqsDbContext context, ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardDataDTO> GetDashboardDataAsync()
    {
        _logger.LogInformation("Getting dashboard data");

        try
        {
            var today = DateTime.UtcNow.Date;
            var oneWeekAgo = today.AddDays(-7);
            var twoWeeksAgo = today.AddDays(-14);

            // Get all data in parallel for better performance
            var customersTask = GetCustomerMetricsAsync(oneWeekAgo);
            var jobsTask = GetJobMetricsAsync(today);
            var invoicesTask = GetInvoiceMetricsAsync();
            var revenueTask = GetRevenueMetricsAsync(oneWeekAgo, twoWeeksAgo);

            await Task.WhenAll(customersTask, jobsTask, invoicesTask, revenueTask);

            var dashboardData = new DashboardDataDTO
            {
                CustomerMetrics = await customersTask,
                JobMetrics = await jobsTask,
                InvoiceMetrics = await invoicesTask,
                RevenueMetrics = await revenueTask,
                LastUpdated = DateTime.UtcNow
            };

            _logger.LogInformation("Retrieved dashboard data: {CustomerCount} customers, {JobCount} active jobs, £{Revenue} weekly revenue",
                dashboardData.CustomerMetrics.TotalCustomers,
                dashboardData.JobMetrics.ActiveJobs,
                dashboardData.RevenueMetrics.WeeklyRevenue);

            return dashboardData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard data");
            throw;
        }
    }

    private async Task<CustomerMetricsDTO> GetCustomerMetricsAsync(DateTime oneWeekAgo)
    {
        try
        {
            var customers = await _context.Customers.ToListAsync();

            var totalCustomers = customers.Count;
            var newCustomersThisWeek = customers.Count(c => c.CreatedDate >= oneWeekAgo);

            // Calculate active customers (customers with active jobs)
            var activeCustomerIds = await _context.Jobs
                .Where(j => j.Status == JobStatus.Active)
                .Select(j => j.CustomerId)
                .Distinct()
                .CountAsync();

            return new CustomerMetricsDTO
            {
                TotalCustomers = totalCustomers,
                ActiveCustomers = activeCustomerIds,
                NewCustomersThisWeek = newCustomersThisWeek
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating customer metrics");
            return new CustomerMetricsDTO();
        }
    }

    private async Task<JobMetricsDTO> GetJobMetricsAsync(DateTime today)
    {
        try
        {
            var jobs = await _context.Jobs.ToListAsync();

            var activeJobs = jobs.Where(j => j.Status == JobStatus.Active).ToList();
            var jobsScheduledToday = jobs.Count(j => j.StartDate.Date == today && j.Status == JobStatus.New);

            // Job breakdowns by type
            var skipRentals = activeJobs.Count(j => j.Type == JobType.SkipRental);
            var sandDeliveries = activeJobs.Count(j => j.Type == JobType.SandDelivery);
            var forkLiftServices = activeJobs.Count(j => j.Type == JobType.ForkLiftService);

            return new JobMetricsDTO
            {
                ActiveJobs = activeJobs.Count,
                JobsScheduledToday = jobsScheduledToday,
                NewJobs = jobs.Count(j => j.Status == JobStatus.New),
                CompletedJobs = jobs.Count(j => j.Status == JobStatus.Completed),
                SkipRentals = skipRentals,
                SandDeliveries = sandDeliveries,
                ForkLiftServices = forkLiftServices
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating job metrics");
            return new JobMetricsDTO();
        }
    }

    private async Task<InvoiceMetricsDTO> GetInvoiceMetricsAsync()
    {
        try
        {
            var invoices = await _context.Invoices.ToListAsync();

            var pendingInvoices = invoices.Where(i =>
                i.Status == InvoiceStatus.Sent ||
                i.Status == InvoiceStatus.Overdue).ToList();

            var overdueInvoices = invoices.Where(i =>
                i.Status == InvoiceStatus.Sent &&
                i.DueDate.HasValue &&
                DateTime.UtcNow.Date > i.DueDate.Value.Date).ToList();

            var totalOutstanding = pendingInvoices.Sum(i => i.Total);

            return new InvoiceMetricsDTO
            {
                PendingInvoices = pendingInvoices.Count,
                OverdueInvoices = overdueInvoices.Count,
                DraftInvoices = invoices.Count(i => i.Status == InvoiceStatus.Draft),
                PaidInvoices = invoices.Count(i => i.Status == InvoiceStatus.Paid),
                TotalOutstanding = totalOutstanding
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating invoice metrics");
            return new InvoiceMetricsDTO();
        }
    }

    private async Task<RevenueMetricsDTO> GetRevenueMetricsAsync(DateTime oneWeekAgo, DateTime twoWeeksAgo)
    {
        try
        {
            // This week's revenue from completed jobs
            var thisWeekJobs = await _context.Jobs
                .Where(j => j.Status == JobStatus.Completed && j.EndDate >= oneWeekAgo)
                .ToListAsync();

            var thisWeekRevenue = thisWeekJobs.Sum(j => j.Price);

            // Previous week's revenue for comparison
            var previousWeekJobs = await _context.Jobs
                .Where(j => j.Status == JobStatus.Completed &&
                           j.EndDate >= twoWeeksAgo &&
                           j.EndDate < oneWeekAgo)
                .ToListAsync();

            var previousWeekRevenue = previousWeekJobs.Sum(j => j.Price);

            // Calculate growth percentage
            decimal revenueGrowth = 0;
            if (previousWeekRevenue > 0)
            {
                revenueGrowth = Math.Round(((thisWeekRevenue - previousWeekRevenue) / previousWeekRevenue) * 100, 1);
            }
            else if (thisWeekRevenue > 0)
            {
                revenueGrowth = 100; // 100% growth from 0
            }

            // Monthly revenue
            var oneMonthAgo = DateTime.UtcNow.Date.AddDays(-30);
            var monthlyRevenue = await _context.Jobs
                .Where(j => j.Status == JobStatus.Completed && j.EndDate >= oneMonthAgo)
                .SumAsync(j => j.Price);

            // Yearly revenue
            var oneYearAgo = DateTime.UtcNow.Date.AddDays(-365);
            var yearlyRevenue = await _context.Jobs
                .Where(j => j.Status == JobStatus.Completed && j.EndDate >= oneYearAgo)
                .SumAsync(j => j.Price);

            return new RevenueMetricsDTO
            {
                WeeklyRevenue = thisWeekRevenue,
                WeekOverWeekGrowth = revenueGrowth,
                MonthlyRevenue = monthlyRevenue,
                YearlyRevenue = yearlyRevenue
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating revenue metrics");
            return new RevenueMetricsDTO();
        }
    }
}