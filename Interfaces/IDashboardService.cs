using Invoqs.API.DTOs;

namespace Invoqs.API.Interfaces;

public interface IDashboardService
{
    Task<DashboardDataDTO> GetDashboardDataAsync();
    Task<CustomerMetricsDTO> GetCustomerMetricsAsync(DateTime oneWeekAgo);
    Task<JobMetricsDTO> GetJobMetricsAsync(DateTime today);
    Task<InvoiceMetricsDTO> GetInvoiceMetricsAsync();
    Task<RevenueMetricsDTO> GetRevenueMetricsAsync(DateTime oneWeekAgo, DateTime twoWeeksAgo);
}