using Microsoft.AspNetCore.Mvc;
using Invoqs.API.Services;
using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;

namespace Invoqs.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(IInvoiceService invoiceService, ILogger<InvoicesController> logger)
        {
            _invoiceService = invoiceService;
            _logger = logger;
        }

        /// <summary>
        /// Get all invoices with line items and customer info
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvoiceDTO>>> GetAllInvoices()
        {
            _logger.LogInformation("Getting all invoices");
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            return Ok(invoices);
        }

        /// <summary>
        /// Get invoice by ID with full details
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<InvoiceDTO>> GetInvoice(int id)
        {
            _logger.LogInformation("Getting invoice with ID: {InvoiceId}", id);

            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
            {
                _logger.LogWarning("Invoice with ID {InvoiceId} not found", id);
                return NotFound($"Invoice with ID {id} not found");
            }

            return Ok(invoice);
        }

        /// <summary>
        /// Get all invoices for a specific customer
        /// </summary>
        [HttpGet("customer/{customerId:int}")]
        public async Task<ActionResult<IEnumerable<InvoiceDTO>>> GetInvoicesByCustomer(int customerId)
        {
            _logger.LogInformation("Getting invoices for customer ID: {CustomerId}", customerId);
            var invoices = await _invoiceService.GetInvoicesByCustomerIdAsync(customerId);
            return Ok(invoices);
        }

        /// <summary>
        /// Create a new invoice from completed jobs
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<InvoiceDTO>> CreateInvoice(CreateInvoiceDTO createInvoiceDto)
        {
            _logger.LogInformation("Creating new invoice for customer ID: {CustomerId} with {JobCount} jobs",
                createInvoiceDto.CustomerId, createInvoiceDto.JobIds.Count());

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
        }

        /// <summary>
        /// Update existing invoice (draft only)
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<InvoiceDTO>> UpdateInvoice(int id, UpdateInvoiceDTO updateInvoiceDto)
        {
            _logger.LogInformation("Updating invoice with ID: {InvoiceId}", id);

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
        }

        /// <summary>
        /// Delete invoice (draft only, soft delete)
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteInvoice(int id)
        {
            _logger.LogInformation("Deleting invoice with ID: {InvoiceId}", id);

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
        }

        /// <summary>
        /// Mark invoice as sent
        /// </summary>
        [HttpPost("{id:int}/send")]
        public async Task<ActionResult<InvoiceDTO>> MarkInvoiceAsSent(int id, MarkInvoiceAsSentDTO sentDto)
        {
            _logger.LogInformation("Marking invoice ID: {InvoiceId} as sent", id);

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
        }

        /// <summary>
        /// Record payment for invoice
        /// </summary>
        [HttpPost("{id:int}/payment")]
        public async Task<ActionResult<InvoiceDTO>> RecordPayment(int id, MarkInvoiceAsPaidDTO paymentDto)
        {
            _logger.LogInformation("Recording payment for invoice ID: {InvoiceId}", id);

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
                _logger.LogWarning("Payment recording failed: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Cancel invoice
        /// </summary>
        [HttpPost("{id:int}/cancel")]
        public async Task<ActionResult<InvoiceDTO>> CancelInvoice(int id)
        {
            _logger.LogInformation("Cancelling invoice ID: {InvoiceId}", id);

            try
            {
                var invoice = await _invoiceService.CancelInvoiceAsync(id);
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
        }

        /// <summary>
        /// Get invoice statistics for dashboard
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<InvoiceStatisticsDTO>> GetInvoiceStatistics()
        {
            _logger.LogInformation("Getting invoice statistics");
            var statistics = await _invoiceService.GetInvoiceStatisticsAsync();
            return Ok(statistics);
        }

        /// <summary>
        /// Get total outstanding amount
        /// </summary>
        [HttpGet("outstanding")]
        public async Task<ActionResult<decimal>> GetTotalOutstanding()
        {
            _logger.LogInformation("Getting total outstanding amount");
            var outstanding = await _invoiceService.GetTotalOutstandingAsync();
            return Ok(outstanding);
        }
    }
}