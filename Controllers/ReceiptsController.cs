using Microsoft.AspNetCore.Mvc;
using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Invoqs.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReceiptsController : ControllerBase
    {
        private readonly IReceiptService _receiptService;
        private readonly IPdfService _pdfService;
        private readonly ILogger<ReceiptsController> _logger;

        public ReceiptsController(IReceiptService receiptService, IPdfService pdfService, ILogger<ReceiptsController> logger)
        {
            _receiptService = receiptService;
            _pdfService = pdfService;
            _logger = logger;
        }

        /// <summary>
        /// Get all receipts
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReceiptDTO>>> GetAllReceipts()
        {
            try
            {
                var receipts = await _receiptService.GetAllReceiptsAsync();
                return Ok(receipts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving receipts for user {UserId}", User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while retrieving receipts" });
            }
        }

        /// <summary>
        /// Get receipt by ID
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ReceiptDTO>> GetReceipt(int id)
        {
            try
            {
                var receipt = await _receiptService.GetReceiptByIdAsync(id);
                if (receipt == null)
                {
                    return NotFound($"Receipt with ID {id} not found");
                }

                return Ok(receipt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving receipt with ID: {ReceiptId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while retrieving the receipt" });
            }
        }

        /// <summary>
        /// Get all receipts for a specific customer
        /// </summary>
        [HttpGet("customer/{customerId:int}")]
        public async Task<ActionResult<IEnumerable<ReceiptDTO>>> GetReceiptsByCustomer(int customerId)
        {
            try
            {
                var receipts = await _receiptService.GetReceiptsByCustomerIdAsync(customerId);
                return Ok(receipts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving receipts for customer ID: {CustomerId} for user {UserId}", customerId, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while retrieving receipts for the customer" });
            }
        }

        /// <summary>
        /// Create a new receipt from paid invoices
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ReceiptDTO>> CreateReceipt(CreateReceiptDTO createReceiptDto)
        {
            try
            {
                var receipt = await _receiptService.CreateReceiptAsync(createReceiptDto);
                return CreatedAtAction(nameof(GetReceipt), new { id = receipt.Id }, receipt);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Receipt creation failed: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating receipt for user {UserId}", User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while creating the receipt" });
            }
        }

        /// <summary>
        /// Delete receipt
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteReceipt(int id)
        {
            try
            {
                var result = await _receiptService.DeleteReceiptAsync(id);
                if (!result)
                {
                    return NotFound($"Receipt with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting receipt with ID: {ReceiptId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while deleting the receipt" });
            }
        }

        /// <summary>
        /// Download receipt as PDF
        /// </summary>
        [HttpGet("{id:int}/pdf")]
        public async Task<IActionResult> GetReceiptPdf(int id)
        {
            try
            {
                var pdfBytes = await _pdfService.GenerateReceiptPdfAsync(id);

                // Get receipt to use receipt number in filename
                var receipt = await _receiptService.GetReceiptByIdAsync(id);
                var fileName = receipt != null
                    ? $"{receipt.ReceiptNumber}.pdf"
                    : $"Receipt-{id}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Receipt PDF generation failed: {Error}", ex.Message);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for receipt ID: {ReceiptId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while generating the receipt PDF" });
            }
        }

        /// <summary>
        /// Send receipt to customer via email
        /// </summary>
        [HttpPost("{id:int}/send")]
        public async Task<IActionResult> SendReceipt(int id)
        {
            try
            {
                var result = await _receiptService.SendReceiptAsync(id);

                if (!result)
                {
                    return NotFound(new { error = $"Receipt with ID {id} not found" });
                }

                return Ok(new { message = "Receipt sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending receipt with ID: {ReceiptId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while sending the receipt" });
            }
        }
    }
}