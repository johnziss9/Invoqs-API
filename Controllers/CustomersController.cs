using Microsoft.AspNetCore.Mvc;
using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;

namespace Invoqs.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            _logger.LogInformation("Getting all customers");
            var customers = await _customerService.GetAllCustomersAsync();
            return Ok(customers);
        }

        /// <summary>
        /// Get customer by ID with full details
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CustomerDTO>> GetCustomer(int id)
        {
            _logger.LogInformation("Getting customer with ID: {CustomerId}", id);

            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
            {
                _logger.LogWarning("Customer with ID {CustomerId} not found", id);
                return NotFound($"Customer with ID {id} not found");
            }

            return Ok(customer);
        }

        /// <summary>
        /// Create a new customer
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CustomerDTO>> CreateCustomer(CreateCustomerDTO createCustomerDto)
        {
            _logger.LogInformation("Creating new customer: {CustomerName}", createCustomerDto.Name);

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
        }

        /// <summary>
        /// Update existing customer
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<CustomerDTO>> UpdateCustomer(int id, UpdateCustomerDTO updateCustomerDto)
        {
            _logger.LogInformation("Updating customer with ID: {CustomerId}", id);

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
        }

        /// <summary>
        /// Delete customer (soft delete)
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteCustomer(int id)
        {
            _logger.LogInformation("Deleting customer with ID: {CustomerId}", id);

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

            _logger.LogInformation("Searching customers with term: {SearchTerm}", term);
            var customers = await _customerService.SearchCustomersAsync(term);
            return Ok(customers);
        }

        /// <summary>
        /// Check if customer exists (used by job creation)
        /// </summary>
        [HttpGet("{id:int}/exists")]
        public async Task<ActionResult<bool>> CustomerExists(int id)
        {
            var exists = await _customerService.CustomerExistsAsync(id);
            return Ok(exists);
        }

        /// <summary>
        /// Get lightweight customer summaries for dropdowns
        /// </summary>
        [HttpGet("summaries")]
        public async Task<ActionResult<IEnumerable<CustomerSummaryDTO>>> GetCustomerSummaries()
        {
            _logger.LogInformation("Getting customer summaries");
            var summaries = await _customerService.GetCustomerSummariesAsync();
            return Ok(summaries);
        }
    }
}