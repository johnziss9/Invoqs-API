namespace Invoqs.API.DTOs;

/// <summary>
/// Complete dashboard data for API responses
/// </summary>
public class DashboardDataDTO
{
    // Revenue metrics
    public decimal WeekRevenue { get; set; }
    public decimal RevenueGrowth { get; set; }

    // Job metrics
    public int ActiveJobs { get; set; }
    public int JobsScheduledToday { get; set; }
    public int SkipRentals { get; set; }
    public int SandDelivery { get; set; }
    public int FortCliffServices { get; set; }

    // Customer metrics
    public int TotalCustomers { get; set; }
    public int NewCustomersThisWeek { get; set; }

    // Invoice metrics
    public int PendingInvoices { get; set; }
    public decimal PendingAmount { get; set; }
    public int OverdueInvoices { get; set; }

    // Service breakdown percentages
    public decimal SkipRentalPercentage { get; set; }
    public decimal SandDeliveryPercentage { get; set; }
    public decimal FortCliffServicePercentage { get; set; }

    // Computed properties
    public int TotalActiveJobs => SkipRentals + SandDelivery + FortCliffServices;
    public decimal TotalServiceJobs => TotalActiveJobs;
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
    public int SkipRentalJobs { get; set; }
    public int SandDeliveryJobs { get; set; }
    public int FortCliffServiceJobs { get; set; }

    // Percentages
    public decimal SkipRentalPercentage { get; set; }
    public decimal SandDeliveryPercentage { get; set; }
    public decimal FortCliffServicePercentage { get; set; }
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