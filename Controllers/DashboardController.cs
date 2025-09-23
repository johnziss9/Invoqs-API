using Microsoft.AspNetCore.Mvc;
using Invoqs.API.Services;
using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;

namespace Invoqs.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Get complete dashboard data with all metrics
        /// </summary>
        [HttpGet("data")]
        public async Task<ActionResult<DashboardDataDTO>> GetDashboardData()
        {
            _logger.LogInformation("Getting dashboard data");
            var dashboardData = await _dashboardService.GetDashboardDataAsync();
            return Ok(dashboardData);
        }

        /// <summary>
        /// Get customer metrics only
        /// </summary>
        [HttpGet("customers")]
        public async Task<ActionResult<CustomerMetricsDTO>> GetCustomerMetrics()
        {
            _logger.LogInformation("Getting customer metrics");
            var oneWeekAgo = DateTime.UtcNow.Date.AddDays(-7);
            var metrics = await _dashboardService.GetCustomerMetricsAsync(oneWeekAgo);
            return Ok(metrics);
        }

        /// <summary>
        /// Get job metrics only
        /// </summary>
        [HttpGet("jobs")]
        public async Task<ActionResult<JobMetricsDTO>> GetJobMetrics()
        {
            _logger.LogInformation("Getting job metrics");
            var today = DateTime.UtcNow.Date;
            var metrics = await _dashboardService.GetJobMetricsAsync(today);
            return Ok(metrics);
        }

        /// <summary>
        /// Get invoice metrics only
        /// </summary>
        [HttpGet("invoices")]
        public async Task<ActionResult<InvoiceMetricsDTO>> GetInvoiceMetrics()
        {
            _logger.LogInformation("Getting invoice metrics");
            var metrics = await _dashboardService.GetInvoiceMetricsAsync();
            return Ok(metrics);
        }

        /// <summary>
        /// Get revenue metrics only
        /// </summary>
        [HttpGet("revenue")]
        public async Task<ActionResult<RevenueMetricsDTO>> GetRevenueMetrics()
        {
            _logger.LogInformation("Getting revenue metrics");
            var oneWeekAgo = DateTime.UtcNow.Date.AddDays(-7);
            var twoWeeksAgo = DateTime.UtcNow.Date.AddDays(-14);
            var metrics = await _dashboardService.GetRevenueMetricsAsync(oneWeekAgo, twoWeeksAgo);
            return Ok(metrics);
        }
    }
}