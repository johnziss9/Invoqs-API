using Microsoft.AspNetCore.Mvc;
using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Invoqs.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ICustomerService _customerService;
        private readonly ILogger<EmailController> _logger;

        public EmailController(IEmailService emailService, ICustomerService customerService, ILogger<EmailController> logger)
        {
            _emailService = emailService;
            _customerService = customerService;
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

            _logger.LogInformation("Sending bulk email to {Count} customers for user {UserId}",
                request.CustomerIds.Count, User.Identity?.Name);

            var result = new BulkEmailResultDTO();

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
                    continue;
                }

                var emailResult = await _emailService.SendCustomEmailAsync(
                    primaryEmail.Email, customer.Name, request.Subject, request.Body, request.Language);

                if (emailResult.Success)
                {
                    result.SentCount++;
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
                }
            }

            _logger.LogInformation("Bulk email completed: {Sent} sent, {Failed} failed for user {UserId}",
                result.SentCount, result.FailedCount, User.Identity?.Name);

            return Ok(result);
        }
    }
}
