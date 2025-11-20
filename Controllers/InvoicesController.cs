using Microsoft.AspNetCore.Mvc;
using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using Invoqs.API.Validators;
using static Invoqs.API.Validators.MarkInvoiceAsPaidValidator;

namespace Invoqs.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IPdfService _pdfService;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(IInvoiceService invoiceService, IPdfService pdfService, ILogger<InvoicesController> logger)
        {
            _invoiceService = invoiceService;
            _pdfService = pdfService;
            _logger = logger;
        }

        /// <summary>
        /// Get all invoices with line items and customer info
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvoiceDTO>>> GetAllInvoices()
        {
            try
            {
                _logger.LogInformation("Getting all invoices for user {UserId}", User.Identity?.Name);
                var invoices = await _invoiceService.GetAllInvoicesAsync();
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices for user {UserId}", User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while retrieving invoices" });
            }
        }

        /// <summary>
        /// Get invoice by ID with full details
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<InvoiceDTO>> GetInvoice(int id)
        {
            try
            {
                _logger.LogInformation("Getting invoice with ID: {InvoiceId} for user {UserId}", id, User.Identity?.Name);

                var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice with ID {InvoiceId} not found", id);
                    return NotFound($"Invoice with ID {id} not found");
                }

                return Ok(invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice with ID: {InvoiceId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while retrieving the invoice" });
            }
        }

        /// <summary>
        /// Get all invoices for a specific customer
        /// </summary>
        [HttpGet("customer/{customerId:int}")]
        public async Task<ActionResult<IEnumerable<InvoiceDTO>>> GetInvoicesByCustomer(int customerId)
        {
            try
            {
                _logger.LogInformation("Getting invoices for customer ID: {CustomerId} for user {UserId}", customerId, User.Identity?.Name);
                var invoices = await _invoiceService.GetInvoicesByCustomerIdAsync(customerId);
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices for customer ID: {CustomerId} for user {UserId}", customerId, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while retrieving invoices for the customer" });
            }
        }

        /// <summary>
        /// Create a new invoice from completed jobs
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<InvoiceDTO>> CreateInvoice(
            CreateInvoiceDTO createInvoiceDto,
            [FromServices] IValidator<CreateInvoiceDTO> validator)
        {
            // Manually validate with async support
            var validationResult = await validator.ValidateAsync(createInvoiceDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    errors = validationResult.Errors.Select(e => new
                    {
                        field = e.PropertyName,
                        message = e.ErrorMessage
                    })
                });
            }

            _logger.LogInformation("Creating new invoice for customer ID: {CustomerId} with {JobCount} jobs for user {UserId}",
                createInvoiceDto.CustomerId, createInvoiceDto.JobIds.Count(), User.Identity?.Name);

            try
            {
                var invoice = await _invoiceService.CreateInvoiceAsync(createInvoiceDto);
                return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invoice creation failed: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice for user {UserId}", User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while creating the invoice" });
            }
        }

        /// <summary>
        /// Update existing invoice (draft only)
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<InvoiceDTO>> UpdateInvoice(
            int id,
            UpdateInvoiceDTO updateInvoiceDto,
            [FromServices] IValidator<UpdateInvoiceDTO> validator)
        {
            if (validator is UpdateInvoiceValidator typedValidator)
            {
                typedValidator.SetInvoiceIdForUpdate(id);
            }
    
            // Manually validate with async support
            var validationResult = await validator.ValidateAsync(updateInvoiceDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    errors = validationResult.Errors.Select(e => new
                    {
                        field = e.PropertyName,
                        message = e.ErrorMessage
                    })
                });
            }

            _logger.LogInformation("Updating invoice with ID: {InvoiceId} for user {UserId}", id, User.Identity?.Name);

            try
            {
                var invoice = await _invoiceService.UpdateInvoiceAsync(id, updateInvoiceDto);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice with ID {InvoiceId} not found for update", id);
                    return NotFound($"Invoice with ID {id} not found");
                }

                return Ok(invoice);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invoice update failed: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice with ID: {InvoiceId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while updating the invoice" });
            }
        }

        /// <summary>
        /// Delete invoice (draft only, soft delete)
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteInvoice(int id)
        {
            _logger.LogInformation("Deleting invoice with ID: {InvoiceId} for user {UserId}", id, User.Identity?.Name);

            try
            {
                var result = await _invoiceService.DeleteInvoiceAsync(id);
                if (!result)
                {
                    _logger.LogWarning("Invoice with ID {InvoiceId} not found for deletion", id);
                    return NotFound($"Invoice with ID {id} not found");
                }

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invoice deletion failed: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice with ID: {InvoiceId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while deleting the invoice" });
            }
        }

        /// <summary>
        /// Mark invoice as sent
        /// </summary>
        [HttpPost("{id:int}/send")]
        public async Task<ActionResult<InvoiceDTO>> MarkInvoiceAsSent(
            int id,
            MarkInvoiceAsSentDTO sentDto,
            [FromServices] IValidator<MarkInvoiceAsSentDTO> validator)
        {
            if (validator is MarkInvoiceAsSentValidator typedValidator)
            {
                typedValidator.SetInvoiceIdForSent(id);
            }

            // Manually validate with async support
            var validationResult = await validator.ValidateAsync(sentDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    errors = validationResult.Errors.Select(e => new
                    {
                        field = e.PropertyName,
                        message = e.ErrorMessage
                    })
                });
            }

            _logger.LogInformation("Marking invoice ID: {InvoiceId} as sent for user {UserId}", id, User.Identity?.Name);

            try
            {
                var invoice = await _invoiceService.MarkInvoiceAsSentAsync(id);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice with ID {InvoiceId} not found for sending", id);
                    return NotFound($"Invoice with ID {id} not found");
                }

                return Ok(invoice);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invoice send failed: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking invoice as sent for ID: {InvoiceId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while marking the invoice as sent" });
            }
        }

        /// <summary>
        /// Mark invoice as delivered (hand-delivered to customer)
        /// </summary>
        [HttpPost("{id:int}/deliver")]
        public async Task<ActionResult<InvoiceDTO>> MarkInvoiceAsDelivered(
            int id,
            MarkInvoiceAsDeliveredDTO deliveredDto,
            [FromServices] IValidator<MarkInvoiceAsDeliveredDTO> validator)
        {
            if (validator is MarkInvoiceAsDeliveredValidator typedValidator)
            {
                typedValidator.SetInvoiceIdForDelivered(id);
            }

            // Manually validate with async support
            var validationResult = await validator.ValidateAsync(deliveredDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    errors = validationResult.Errors.Select(e => new
                    {
                        field = e.PropertyName,
                        message = e.ErrorMessage
                    })
                });
            }

            try
            {
                var invoice = await _invoiceService.MarkInvoiceAsDeliveredAsync(id);
                if (invoice == null)
                {
                    return NotFound($"Invoice with ID {id} not found");
                }

                return Ok(invoice);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invoice delivery marking failed: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking invoice as delivered for ID: {InvoiceId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while marking the invoice as delivered" });
            }
        }

        /// <summary>
        /// Mark invoice as paid
        /// </summary>
        [HttpPost("{id:int}/payment")]
        public async Task<ActionResult<InvoiceDTO>> MarkInvoiceAsPaid(
            int id,
            MarkInvoiceAsPaidDTO paymentDto,
            [FromServices] IValidator<MarkInvoiceAsPaidDTO> validator)
        {
            if (validator is MarkInvoiceAsPaidValidator typedValidator)
            {
                typedValidator.SetInvoiceIdForPayment(id);
            }

            // Manually validate with async support
            var validationResult = await validator.ValidateAsync(paymentDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    errors = validationResult.Errors.Select(e => new
                    {
                        field = e.PropertyName,
                        message = e.ErrorMessage
                    })
                });
            }

            _logger.LogInformation("Marking invoice as paid with invoice ID: {InvoiceId} for user {UserId}", id, User.Identity?.Name);

            try
            {
                var invoice = await _invoiceService.MarkInvoiceAsPaidAsync(id, paymentDto);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice with ID {InvoiceId} not found for payment", id);
                    return NotFound($"Invoice with ID {id} not found");
                }

                return Ok(invoice);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Marking invoice as paid failed: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking invoice as paid for ID: {InvoiceId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while marking the invoice as paid" });
            }
        }

        /// <summary>
        /// Cancel invoice
        /// </summary>
        [HttpPost("{id:int}/cancel")]
        public async Task<ActionResult<InvoiceDTO>> CancelInvoice(int id, [FromBody] CancelInvoiceDTO? cancelDTO = null)
        {
            _logger.LogInformation("Cancelling invoice ID: {InvoiceId} for user {UserId}", id, User.Identity?.Name);

            try
            {
                var invoice = await _invoiceService.CancelInvoiceAsync(id, cancelDTO);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice with ID {InvoiceId} not found for cancellation", id);
                    return NotFound($"Invoice with ID {id} not found");
                }

                return Ok(invoice);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invoice cancellation failed: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling invoice with ID: {InvoiceId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while cancelling the invoice" });
            }
        }

        // /// <summary>
        // /// Get invoice statistics for dashboard
        // /// </summary>
        // [HttpGet("statistics")]
        // public async Task<ActionResult<InvoiceStatisticsDTO>> GetInvoiceStatistics()
        // {
        //     try
        //     {
        //         _logger.LogInformation("Getting invoice statistics for user {UserId}", User.Identity?.Name);
        //         var statistics = await _invoiceService.GetInvoiceStatisticsAsync();
        //         return Ok(statistics);
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error retrieving invoice statistics for user {UserId}", User.Identity?.Name);
        //         return StatusCode(500, new { error = "An error occurred while retrieving invoice statistics" });
        //     }
        // }

        /// <summary>
        /// Get total outstanding amount
        /// </summary>
        [HttpGet("outstanding")]
        public async Task<ActionResult<decimal>> GetTotalOutstanding()
        {
            try
            {
                _logger.LogInformation("Getting total outstanding amount for user {UserId}", User.Identity?.Name);
                var outstanding = await _invoiceService.GetTotalOutstandingAsync();
                return Ok(outstanding);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving total outstanding amount for user {UserId}", User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while retrieving the total outstanding amount" });
            }
        }

        /// <summary>
        /// Download invoice as PDF
        /// </summary>
        [HttpGet("{id:int}/pdf")]
        public async Task<IActionResult> GetInvoicePdf(int id)
        {
            try
            {
                _logger.LogInformation("Generating PDF for invoice ID: {InvoiceId} for user {UserId}", id, User.Identity?.Name);

                var pdfBytes = await _pdfService.GenerateInvoicePdfAsync(id);

                // Get invoice to use invoice number in filename
                var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
                var fileName = invoice != null
                    ? $"{invoice.InvoiceNumber}.pdf"
                    : $"Invoice-{id}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invoice PDF generation failed: {Error}", ex.Message);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for invoice ID: {InvoiceId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while generating the invoice PDF" });
            }
        }
    }
}