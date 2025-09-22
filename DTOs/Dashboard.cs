namespace Invoqs.API.DTOs;

/// <summary>
/// Complete dashboard data for API responses
/// </summary>
public class DashboardDataDTO
{
    public CustomerMetricsDTO CustomerMetrics { get; set; } = new();
    public JobMetricsDTO JobMetrics { get; set; } = new();
    public RevenueMetricsDTO RevenueMetrics { get; set; } = new();
    public InvoiceMetricsDTO InvoiceMetrics { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Revenue metrics for dashboard charts
/// </summary>
public class RevenueMetricsDTO
{
    public decimal WeeklyRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal YearlyRevenue { get; set; }
    public decimal WeekOverWeekGrowth { get; set; }
    public decimal MonthOverMonthGrowth { get; set; }
    public decimal YearOverYearGrowth { get; set; }
}

/// <summary>
/// Job metrics for dashboard
/// </summary>
public class JobMetricsDTO
{
    public int TotalJobs { get; set; }
    public int ActiveJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int NewJobs { get; set; }
    public int CancelledJobs { get; set; }
    public int JobsScheduledToday { get; set; }
    public int JobsScheduledThisWeek { get; set; }

    // Job type breakdown
    public int SkipRentals { get; set; }
    public int SandDeliveries { get; set; }
    public int ForkLiftServices { get; set; }

    // Percentages
    public decimal SkipRentalPercentage { get; set; }
    public decimal SandDeliveryPercentage { get; set; }
    public decimal ForkLiftServicePercentage { get; set; }
}

/// <summary>
/// Customer metrics for dashboard
/// </summary>
public class CustomerMetricsDTO
{
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int InactiveCustomers { get; set; }
    public int NewCustomersThisWeek { get; set; }
    public int NewCustomersThisMonth { get; set; }
    public decimal AverageJobsPerCustomer { get; set; }
    public decimal AverageRevenuePerCustomer { get; set; }
}

/// <summary>
/// Invoice metrics for dashboard
/// </summary>
public class InvoiceMetricsDTO
{
    public int PendingInvoices { get; set; }
    public int OverdueInvoices { get; set; }
    public int DraftInvoices { get; set; }
    public int PaidInvoices { get; set; }
    public decimal TotalOutstanding { get; set; }
}