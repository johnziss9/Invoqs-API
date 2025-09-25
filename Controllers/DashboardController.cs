using Microsoft.AspNetCore.Mvc;
using Invoqs.API.Services;
using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Invoqs.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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
            try
            {
                _logger.LogInformation("Getting dashboard data for user {UserId}", User.Identity?.Name);
                var dashboardData = await _dashboardService.GetDashboardDataAsync();
                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard data for user {UserId}", User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while retrieving dashboard data" });
            }
        }

        /// <summary>
        /// Get customer metrics only
        /// </summary>
        [HttpGet("customers")]
        public async Task<ActionResult<CustomerMetricsDTO>> GetCustomerMetrics()
        {
            try
            {
                _logger.LogInformation("Getting customer metrics for user {UserId}", User.Identity?.Name);
                var oneWeekAgo = DateTime.UtcNow.Date.AddDays(-7);
                var metrics = await _dashboardService.GetCustomerMetricsAsync(oneWeekAgo);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer metrics for user {UserId}", User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while retrieving customer metrics" });
            }
        }

        /// <summary>
        /// Get job metrics only
        /// </summary>
        [HttpGet("jobs")]
        public async Task<ActionResult<JobMetricsDTO>> GetJobMetrics()
        {
            try
            {
                _logger.LogInformation("Getting job metrics for user {UserId}", User.Identity?.Name);
                var today = DateTime.UtcNow.Date;
                var metrics = await _dashboardService.GetJobMetricsAsync(today);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job metrics for user {UserId}", User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while retrieving job metrics" });
            }
        }

        /// <summary>
        /// Get invoice metrics only
        /// </summary>
        [HttpGet("invoices")]
        public async Task<ActionResult<InvoiceMetricsDTO>> GetInvoiceMetrics()
        {
            try
            {
                _logger.LogInformation("Getting invoice metrics for user {UserId}", User.Identity?.Name);
                var metrics = await _dashboardService.GetInvoiceMetricsAsync();
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice metrics for user {UserId}", User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while retrieving invoice metrics" });
            }
        }

        /// <summary>
        /// Get revenue metrics only
        /// </summary>
        [HttpGet("revenue")]
        public async Task<ActionResult<RevenueMetricsDTO>> GetRevenueMetrics()
        {
            try
            {
                _logger.LogInformation("Getting revenue metrics for user {UserId}", User.Identity?.Name);
                var oneWeekAgo = DateTime.UtcNow.Date.AddDays(-7);
                var twoWeeksAgo = DateTime.UtcNow.Date.AddDays(-14);
                var metrics = await _dashboardService.GetRevenueMetricsAsync(oneWeekAgo, twoWeeksAgo);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving revenue metrics for user {UserId}", User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while retrieving revenue metrics" });
            }
        }
    }
}