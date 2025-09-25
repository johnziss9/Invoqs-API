using Microsoft.AspNetCore.Mvc;
using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Invoqs.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
        {
            _customerService = customerService;
            _logger = logger;
        }

        /// <summary>
        /// Get all customers with computed statistics
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerDTO>>> GetAllCustomers()
        {
            try
            {
                _logger.LogInformation("Getting all customers for user {UserId}", User.Identity?.Name);
                var customers = await _customerService.GetAllCustomersAsync();
                return Ok(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customers");
                return StatusCode(500, new { error = "An error occurred while retrieving customers" });
            }
        }

        /// <summary>
        /// Get customer by ID with full details
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CustomerDTO>> GetCustomer(int id)
        {
            try
            {
                _logger.LogInformation("Getting customer with ID: {CustomerId} for user {UserId}", id, User.Identity?.Name);
                var customer = await _customerService.GetCustomerByIdAsync(id);
                if (customer == null)
                {
                    _logger.LogWarning("Customer with ID {CustomerId} not found", id);
                    return NotFound($"Customer with ID {id} not found");
                }

                return Ok(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer with ID: {CustomerId}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving customer with ID {customerId}", id});
            }
        }

        /// <summary>
        /// Create a new customer
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CustomerDTO>> CreateCustomer(CreateCustomerDTO createCustomerDto)
        {
            _logger.LogInformation("Creating new customer: {CustomerName} for user {UserId}", createCustomerDto.Name, User.Identity?.Name);

            try
            {
                var customer = await _customerService.CreateCustomerAsync(createCustomerDto);
                return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("email"))
            {
                _logger.LogWarning("Customer creation failed - email already exists: {Email}", createCustomerDto.Email);
                return Conflict(new { error = "Email address already exists", email = createCustomerDto.Email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer for user {UserId}", User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while creating the customer" });
            }
        }

        /// <summary>
        /// Update existing customer
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<CustomerDTO>> UpdateCustomer(int id, UpdateCustomerDTO updateCustomerDto)
        {
            _logger.LogInformation("Updating customer with ID: {CustomerId} for user {UserId}", id, User.Identity?.Name);

            try
            {
                var customer = await _customerService.UpdateCustomerAsync(id, updateCustomerDto);
                if (customer == null)
                {
                    _logger.LogWarning("Customer with ID {CustomerId} not found for update", id);
                    return NotFound($"Customer with ID {id} not found");
                }

                return Ok(customer);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("email"))
            {
                _logger.LogWarning("Customer update failed - email already exists: {Email}", updateCustomerDto.Email);
                return Conflict(new { error = "Email address already exists", email = updateCustomerDto.Email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer with ID: {CustomerId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while updating the customer" });
            }
        }

        /// <summary>
        /// Delete customer (soft delete)
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteCustomer(int id)
        {
            _logger.LogInformation("Deleting customer with ID: {CustomerId} for user {UserId}", id, User.Identity?.Name);

            try
            {
                var result = await _customerService.DeleteCustomerAsync(id);
                if (!result)
                {
                    _logger.LogWarning("Customer with ID {CustomerId} not found for deletion", id);
                    return NotFound($"Customer with ID {id} not found");
                }

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Customer deletion failed: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer with ID: {CustomerId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while deleting the customer" });
            }
        }

        /// <summary>
        /// Search customers by name, email, or phone
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<CustomerDTO>>> SearchCustomers([FromQuery] string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return BadRequest("Search term is required");
            }

            try
            {
                _logger.LogInformation("Searching customers with term: {SearchTerm} for user {UserId}", term, User.Identity?.Name);
                var customers = await _customerService.SearchCustomersAsync(term);
                return Ok(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching customers with term: {SearchTerm} for user {UserId}", term, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while searching for customers" });
            }
        }

        /// <summary>
        /// Check if customer exists (used by job creation)
        /// </summary>
        [HttpGet("{id:int}/exists")]
        public async Task<ActionResult<bool>> CustomerExists(int id)
        {
            try
            {
                _logger.LogInformation("Checking if customer exists with ID: {CustomerId} for user {UserId}", id, User.Identity?.Name);
                var exists = await _customerService.CustomerExistsAsync(id);
                return Ok(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking customer existence with ID: {CustomerId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while checking if the customer exists" });
            }
        }

        /// <summary>
        /// Get lightweight customer summaries for dropdowns
        /// </summary>
        [HttpGet("summaries")]
        public async Task<ActionResult<IEnumerable<CustomerSummaryDTO>>> GetCustomerSummaries()
        {
            try
            {
                _logger.LogInformation("Getting customer summaries for user {UserId}", User.Identity?.Name);
                var summaries = await _customerService.GetCustomerSummariesAsync();
                return Ok(summaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer summaries for user {UserId}", User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while retrieving customer summaries" });
            }
        }
    }
}