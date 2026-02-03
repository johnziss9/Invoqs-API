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

            _logger.LogInformation("Retrieved dashboard data: {CustomerCount} customers, {JobCount} total jobs, £{Revenue} weekly revenue",
                dashboardData.CustomerMetrics.TotalCustomers,
                dashboardData.JobMetrics.TotalJobs,
                dashboardData.RevenueMetrics.WeeklyRevenue);

            return dashboardData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard data");
            throw;
        }
    }

    public async Task<CustomerMetricsDTO> GetCustomerMetricsAsync(DateTime oneWeekAgo)
    {
        try
        {
            var customers = await _context.Customers.ToListAsync();

            var totalCustomers = customers.Count;
            var newCustomersThisWeek = customers.Count(c => c.CreatedDate >= oneWeekAgo);

            // Calculate customers with uninvoiced work
            var customersWithUninvoicedJobs = await _context.Jobs
                .Where(j => !j.IsInvoiced)
                .Select(j => j.CustomerId)
                .Distinct()
                .CountAsync();

            return new CustomerMetricsDTO
            {
                TotalCustomers = totalCustomers,
                ActiveCustomers = customersWithUninvoicedJobs,
                NewCustomersThisWeek = newCustomersThisWeek
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating customer metrics");
            return new CustomerMetricsDTO();
        }
    }

    public async Task<JobMetricsDTO> GetJobMetricsAsync(DateTime today)
    {
        try
        {
            var jobs = await _context.Jobs.ToListAsync();

            var uninvoicedJobs = jobs.Where(j => !j.IsInvoiced).ToList();

            // Job breakdowns by type for all jobs
            var skipRentals = jobs.Count(j => j.Type == JobType.SkipRental);
            var sandDeliveries = jobs.Count(j => j.Type == JobType.SandDelivery);
            var forkLiftServices = jobs.Count(j => j.Type == JobType.ForkLiftService);
            var transfers = jobs.Count(j => j.Type == JobType.Transfer);

            return new JobMetricsDTO
            {
                TotalJobs = jobs.Count,
                UninvoicedJobs = uninvoicedJobs.Count,
                SkipRentals = skipRentals,
                SandDeliveries = sandDeliveries,
                ForkLiftServices = forkLiftServices,
                Transfers = transfers
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating job metrics");
            return new JobMetricsDTO();
        }
    }

    public async Task<InvoiceMetricsDTO> GetInvoiceMetricsAsync()
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

    public async Task<RevenueMetricsDTO> GetRevenueMetricsAsync(DateTime oneWeekAgo, DateTime twoWeeksAgo)
    {
        try
        {
            // This week's revenue from jobs
            var thisWeekJobs = await _context.Jobs
                .Where(j => j.JobDate >= oneWeekAgo)
                .ToListAsync();

            var thisWeekRevenue = thisWeekJobs.Sum(j => j.Price);

            // Previous week's revenue for comparison
            var previousWeekJobs = await _context.Jobs
                .Where(j => j.JobDate >= twoWeeksAgo &&
                           j.JobDate < oneWeekAgo)
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
                .Where(j => j.JobDate >= oneMonthAgo)
                .SumAsync(j => j.Price);

            // Yearly revenue
            var oneYearAgo = DateTime.UtcNow.Date.AddDays(-365);
            var yearlyRevenue = await _context.Jobs
                .Where(j => j.JobDate >= oneYearAgo)
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