using Microsoft.AspNetCore.Mvc;
using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Invoqs.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StatementsController : ControllerBase
    {
        private readonly IStatementService _statementService;
        private readonly IPdfService _pdfService;
        private readonly ILogger<StatementsController> _logger;

        public StatementsController(IStatementService statementService, IPdfService pdfService, ILogger<StatementsController> logger)
        {
            _statementService = statementService;
            _pdfService = pdfService;
            _logger = logger;
        }

        /// <summary>
        /// Get all statements
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StatementDTO>>> GetAllStatements()
        {
            try
            {
                var statements = await _statementService.GetAllStatementsAsync();
                return Ok(statements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving statements for user {UserId}", User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while retrieving statements" });
            }
        }

        /// <summary>
        /// Get statement by ID
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<StatementDTO>> GetStatement(int id)
        {
            try
            {
                var statement = await _statementService.GetStatementByIdAsync(id);
                if (statement == null)
                {
                    return NotFound($"Statement with ID {id} not found");
                }

                return Ok(statement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving statement with ID: {StatementId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while retrieving the statement" });
            }
        }

        /// <summary>
        /// Create a new statement for a date range
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<StatementDTO>> CreateStatement(CreateStatementDTO createStatementDto)
        {
            try
            {
                var statement = await _statementService.CreateStatementAsync(createStatementDto);
                return CreatedAtAction(nameof(GetStatement), new { id = statement.Id }, statement);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Statement creation failed: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating statement for user {UserId}", User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while creating the statement" });
            }
        }

        /// <summary>
        /// Delete statement
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteStatement(int id)
        {
            try
            {
                var result = await _statementService.DeleteStatementAsync(id);
                if (!result)
                {
                    return NotFound($"Statement with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting statement with ID: {StatementId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while deleting the statement" });
            }
        }

        /// <summary>
        /// Download statement as PDF
        /// </summary>
        [HttpGet("{id:int}/pdf")]
        public async Task<IActionResult> GetStatementPdf(int id)
        {
            try
            {
                var pdfBytes = await _pdfService.GenerateStatementPdfAsync(id);
                var statement = await _statementService.GetStatementByIdAsync(id);
                var fileName = statement != null
                    ? $"{statement.StatementNumber}.pdf"
                    : $"Statement-{id}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Statement PDF generation failed: {Error}", ex.Message);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for statement ID: {StatementId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while generating the statement PDF" });
            }
        }

        /// <summary>
        /// Send statement via email
        /// </summary>
        [HttpPost("{id:int}/send")]
        public async Task<IActionResult> SendStatement(int id, [FromBody] SendStatementRequestDTO request)
        {
            try
            {
                if (request == null || request.RecipientEmails == null || !request.RecipientEmails.Any())
                {
                    return BadRequest(new { error = "Recipient emails are required" });
                }

                var result = await _statementService.SendStatementAsync(id, request.RecipientEmails);

                if (!result)
                {
                    return NotFound(new { error = $"Statement with ID {id} not found" });
                }

                return Ok(new { message = "Statement sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending statement with ID: {StatementId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while sending the statement" });
            }
        }

        /// <summary>
        /// Mark statement as delivered (without sending email - for manually handed statements)
        /// </summary>
        [HttpPut("{id:int}/mark-as-delivered")]
        public async Task<IActionResult> MarkStatementAsDelivered(int id)
        {
            try
            {
                var result = await _statementService.MarkStatementAsDeliveredAsync(id);

                if (!result)
                {
                    return NotFound(new { error = $"Statement with ID {id} not found" });
                }

                return Ok(new { message = "Statement marked as delivered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking statement with ID: {StatementId} as delivered for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while marking the statement as delivered" });
            }
        }
    }
}
