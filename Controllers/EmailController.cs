using Microsoft.AspNetCore.Mvc;
using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;
using Invoqs.API.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Invoqs.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ICustomerService _customerService;
        private readonly IBulkEmailLogService _bulkEmailLogService;
        private readonly ILogger<EmailController> _logger;

        public EmailController(
            IEmailService emailService,
            ICustomerService customerService,
            IBulkEmailLogService bulkEmailLogService,
            ILogger<EmailController> logger)
        {
            _emailService = emailService;
            _customerService = customerService;
            _bulkEmailLogService = bulkEmailLogService;
            _logger = logger;
        }

        /// <summary>
        /// Send a custom email to multiple customers
        /// </summary>
        [HttpPost("bulk")]
        public async Task<ActionResult<BulkEmailResultDTO>> SendBulkEmail(BulkEmailRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Body))
                return BadRequest(new { error = "Subject and body are required" });

            if (request.CustomerIds == null || !request.CustomerIds.Any())
                return BadRequest(new { error = "At least one customer must be selected" });

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            _logger.LogInformation("Sending bulk email to {Count} customers for user {UserId}",
                request.CustomerIds.Count, userId);

            var result = new BulkEmailResultDTO();
            var recipients = new List<BulkEmailRecipient>();

            foreach (var customerId in request.CustomerIds)
            {
                var customer = await _customerService.GetCustomerByIdAsync(customerId);
                if (customer == null)
                {
                    result.FailedCount++;
                    result.Failures.Add(new BulkEmailFailureDTO
                    {
                        CustomerName = $"Customer {customerId}",
                        Email = "",
                        Error = "Customer not found"
                    });
                    recipients.Add(new BulkEmailRecipient
                    {
                        CustomerId = customerId,
                        CustomerName = $"Customer {customerId}",
                        Email = "",
                        Success = false,
                        Error = "Customer not found"
                    });
                    continue;
                }

                var primaryEmail = customer.Emails.FirstOrDefault();
                if (primaryEmail == null)
                {
                    result.FailedCount++;
                    result.Failures.Add(new BulkEmailFailureDTO
                    {
                        CustomerName = customer.Name,
                        Email = "",
                        Error = "No email address"
                    });
                    recipients.Add(new BulkEmailRecipient
                    {
                        CustomerId = customerId,
                        CustomerName = customer.Name,
                        Email = "",
                        Success = false,
                        Error = "No email address"
                    });
                    continue;
                }

                var emailResult = await _emailService.SendCustomEmailAsync(
                    primaryEmail.Email, customer.Name, request.Subject, request.Body, request.Language);

                if (emailResult.Success)
                {
                    result.SentCount++;
                    recipients.Add(new BulkEmailRecipient
                    {
                        CustomerId = customerId,
                        CustomerName = customer.Name,
                        Email = primaryEmail.Email,
                        Success = true
                    });
                }
                else
                {
                    result.FailedCount++;
                    result.Failures.Add(new BulkEmailFailureDTO
                    {
                        CustomerName = customer.Name,
                        Email = primaryEmail.Email,
                        Error = emailResult.ErrorMessage ?? "Unknown error"
                    });
                    recipients.Add(new BulkEmailRecipient
                    {
                        CustomerId = customerId,
                        CustomerName = customer.Name,
                        Email = primaryEmail.Email,
                        Success = false,
                        Error = emailResult.ErrorMessage ?? "Unknown error"
                    });
                }
            }

            // Save log
            var log = new BulkEmailLog
            {
                SentDate = DateTime.UtcNow,
                Subject = request.Subject,
                Body = request.Body,
                Language = request.Language,
                SentByUserId = userId,
                TotalRecipients = recipients.Count,
                SentCount = result.SentCount,
                FailedCount = result.FailedCount,
                Recipients = recipients
            };
            await _bulkEmailLogService.SaveLogAsync(log);

            _logger.LogInformation("Bulk email completed: {Sent} sent, {Failed} failed for user {UserId}",
                result.SentCount, result.FailedCount, userId);

            return Ok(result);
        }

        /// <summary>
        /// Get bulk email send history
        /// </summary>
        [HttpGet("bulk/history")]
        public async Task<ActionResult<IEnumerable<BulkEmailLogDTO>>> GetBulkEmailHistory()
        {
            try
            {
                var logs = await _bulkEmailLogService.GetAllLogsAsync();
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bulk email history");
                return StatusCode(500, new { error = "An error occurred while retrieving email history" });
            }
        }

        /// <summary>
        /// Get bulk email log details including per-recipient breakdown
        /// </summary>
        [HttpGet("bulk/history/{id:int}")]
        public async Task<ActionResult<BulkEmailLogDTO>> GetBulkEmailLogDetail(int id)
        {
            try
            {
                var log = await _bulkEmailLogService.GetLogByIdAsync(id);
                if (log == null)
                    return NotFound($"Email log with ID {id} not found");

                return Ok(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bulk email log {LogId}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving email log details" });
            }
        }
    }
}
