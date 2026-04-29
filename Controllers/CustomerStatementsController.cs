using Microsoft.AspNetCore.Mvc;
using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Invoqs.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomerStatementsController : ControllerBase
{
    private readonly ICustomerStatementService _customerStatementService;
    private readonly IPdfService _pdfService;
    private readonly ILogger<CustomerStatementsController> _logger;

    public CustomerStatementsController(
        ICustomerStatementService customerStatementService,
        IPdfService pdfService,
        ILogger<CustomerStatementsController> logger)
    {
        _customerStatementService = customerStatementService;
        _pdfService = pdfService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerStatementDTO>>> GetAll()
    {
        try
        {
            var statements = await _customerStatementService.GetAllCustomerStatementsAsync();
            return Ok(statements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer statements");
            return StatusCode(500, new { error = "An error occurred while retrieving customer statements" });
        }
    }

    [HttpGet("customer/{customerId:int}")]
    public async Task<ActionResult<IEnumerable<CustomerStatementDTO>>> GetByCustomer(int customerId)
    {
        try
        {
            var statements = await _customerStatementService.GetCustomerStatementsAsync(customerId);
            return Ok(statements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer statements for customer ID: {CustomerId}", customerId);
            return StatusCode(500, new { error = "An error occurred while retrieving customer statements" });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerStatementDTO>> GetById(int id)
    {
        try
        {
            var statement = await _customerStatementService.GetCustomerStatementByIdAsync(id);
            if (statement == null)
                return NotFound($"Customer statement with ID {id} not found");

            return Ok(statement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer statement ID: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the customer statement" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<CustomerStatementDTO>> Create(CreateCustomerStatementDTO createDTO)
    {
        try
        {
            var statement = await _customerStatementService.CreateCustomerStatementAsync(createDTO);
            return CreatedAtAction(nameof(GetById), new { id = statement.Id }, statement);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Customer statement creation failed: {Error}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer statement");
            return StatusCode(500, new { error = "An error occurred while creating the customer statement" });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var result = await _customerStatementService.DeleteCustomerStatementAsync(id);
            if (!result)
                return NotFound($"Customer statement with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer statement ID: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the customer statement" });
        }
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> GetPdf(int id)
    {
        try
        {
            var pdfBytes = await _pdfService.GenerateCustomerStatementPdfAsync(id);
            var statement = await _customerStatementService.GetCustomerStatementByIdAsync(id);
            var fileName = statement != null
                ? $"{statement.StatementNumber}.pdf"
                : $"CustomerStatement-{id}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Customer statement PDF generation failed: {Error}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for customer statement ID: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while generating the PDF" });
        }
    }

    [HttpPost("{id:int}/send")]
    public async Task<IActionResult> Send(int id, [FromBody] SendCustomerStatementRequestDTO request)
    {
        try
        {
            if (request == null || request.RecipientEmails == null || !request.RecipientEmails.Any())
                return BadRequest(new { error = "Recipient emails are required" });

            var result = await _customerStatementService.SendCustomerStatementAsync(id, request.RecipientEmails);
            if (!result)
                return NotFound(new { error = $"Customer statement with ID {id} not found" });

            return Ok(new { message = "Customer statement sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending customer statement ID: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while sending the customer statement" });
        }
    }

    [HttpPut("{id:int}/mark-as-delivered")]
    public async Task<IActionResult> MarkAsDelivered(int id)
    {
        try
        {
            var result = await _customerStatementService.MarkCustomerStatementAsDeliveredAsync(id);
            if (!result)
                return NotFound(new { error = $"Customer statement with ID {id} not found" });

            return Ok(new { message = "Customer statement marked as delivered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking customer statement ID: {Id} as delivered", id);
            return StatusCode(500, new { error = "An error occurred while marking the customer statement as delivered" });
        }
    }
}
