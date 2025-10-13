using Microsoft.AspNetCore.Mvc;
using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;

namespace Invoqs.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class JobsController : ControllerBase
    {
        private readonly IJobService _jobService;
        private readonly ILogger<JobsController> _logger;

        public JobsController(IJobService jobService, ILogger<JobsController> logger)
        {
            _jobService = jobService;
            _logger = logger;
        }

        /// <summary>
        /// Get all jobs with customer information
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<JobDTO>>> GetAllJobs()
        {
            try
            {
                _logger.LogInformation("Getting all jobs for user {UserId}", User.Identity?.Name);
                var jobs = await _jobService.GetAllJobsAsync();
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving jobs for user {UserId}", User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while retrieving jobs" });
            }
        }

        /// <summary>
        /// Get job by ID with full details
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<JobDTO>> GetJob(int id)
        {
            try
            {
                _logger.LogInformation("Getting job with ID: {JobId} for user {UserId}", id, User.Identity?.Name);

                var job = await _jobService.GetJobByIdAsync(id);
                if (job == null)
                {
                    _logger.LogWarning("Job with ID {JobId} not found", id);
                    return NotFound($"Job with ID {id} not found");
                }

                return Ok(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job with ID: {JobId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while retrieving the job" });
            }
        }

        /// <summary>
        /// Get all jobs for a specific customer
        /// </summary>
        [HttpGet("customer/{customerId:int}")]
        public async Task<ActionResult<IEnumerable<JobDTO>>> GetJobsByCustomer(int customerId)
        {
            try
            {
                _logger.LogInformation("Getting jobs for customer ID: {CustomerId} for user {UserId}", customerId, User.Identity?.Name);
                var jobs = await _jobService.GetJobsByCustomerIdAsync(customerId);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving jobs for customer ID: {CustomerId} for user {UserId}", customerId, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while retrieving jobs for the customer" });
            }
        }

        /// <summary>
        /// Get completed uninvoiced jobs for a customer (for invoice creation)
        /// </summary>
        [HttpGet("customer/{customerId:int}/uninvoiced")]
        public async Task<ActionResult<IEnumerable<JobDTO>>> GetUninvoicedJobs(int customerId)
        {
            try
            {
                _logger.LogInformation("Getting uninvoiced jobs for customer ID: {CustomerId} for user {UserId}", customerId, User.Identity?.Name);
                var jobs = await _jobService.GetCompletedUnInvoicedJobsAsync(customerId);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving uninvoiced jobs for customer ID: {CustomerId} for user {UserId}", customerId, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while retrieving uninvoiced jobs" });
            }
        }

        /// <summary>
        /// Get jobs grouped by address for a customer (Blazor feature)
        /// </summary>
        [HttpGet("customer/{customerId:int}/grouped-by-address")]
        public async Task<ActionResult<Dictionary<string, IEnumerable<JobDTO>>>> GetJobsGroupedByAddress(int customerId)
        {
            try
            {
                _logger.LogInformation("Getting jobs grouped by address for customer ID: {CustomerId} for user {UserId}", customerId, User.Identity?.Name);
                var jobGroups = await _jobService.GetJobsGroupedByAddressAsync(customerId);
                return Ok(jobGroups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving jobs grouped by address for customer ID: {CustomerId} for user {UserId}", customerId, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while retrieving jobs grouped by address" });
            }
        }

        /// <summary>
        /// Get jobs for a specific invoice
        /// </summary>
        [HttpGet("invoice/{invoiceId:int}")]
        public async Task<ActionResult<IEnumerable<JobDTO>>> GetJobsByInvoice(int invoiceId)
        {
            try
            {
                _logger.LogInformation("Getting jobs for invoice ID: {InvoiceId} for user {UserId}", invoiceId, User.Identity?.Name);
                var jobs = await _jobService.GetJobsByInvoiceIdAsync(invoiceId);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving jobs for invoice ID: {InvoiceId} for user {UserId}", invoiceId, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while retrieving jobs for the invoice" });
            }
        }

        /// <summary>
        /// Create a new job
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<JobDTO>> CreateJob(
            CreateJobDTO createJobDto, 
            [FromServices] IValidator<CreateJobDTO> validator)
        {
            // Manually validate with async support
            var validationResult = await validator.ValidateAsync(createJobDto);
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

            _logger.LogInformation("Creating new job: {JobTitle} for customer ID: {CustomerId} for user {UserId}",
                createJobDto.Title, createJobDto.CustomerId, User.Identity?.Name);

            try
            {
                var job = await _jobService.CreateJobAsync(createJobDto);
                return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Job creation failed: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job for user {UserId}", User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while creating the job" });
            }
        }

        /// <summary>
        /// Update existing job
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<JobDTO>> UpdateJob(
            int id, 
            UpdateJobDTO updateJobDto, 
            [FromServices] IValidator<UpdateJobDTO> validator)
        {
            // Manually validate with async support
            var validationResult = await validator.ValidateAsync(updateJobDto);
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

            _logger.LogInformation("Updating job with ID: {JobId} for user {UserId}", id, User.Identity?.Name);

            try
            {
                var job = await _jobService.UpdateJobAsync(id, updateJobDto);
                if (job == null)
                {
                    _logger.LogWarning("Job with ID {JobId} not found for update", id);
                    return NotFound($"Job with ID {id} not found");
                }

                return Ok(job);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Job update failed: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job with ID: {JobId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while updating the job" });
            }
        }

        // /// <summary>
        // /// Update job status only
        // /// </summary>
        // [HttpPut("{id:int}/status")]
        // public async Task<ActionResult<JobDTO>> UpdateJobStatus(int id, UpdateJobStatusDTO statusDto)
        // {
        //     _logger.LogInformation("Updating status for job ID: {JobId} to {Status} for user {UserId}", id, statusDto.Status, User.Identity?.Name);

        //     try
        //     {
        //         var job = await _jobService.UpdateJobStatusAsync(id, statusDto);
        //         if (job == null)
        //         {
        //             _logger.LogWarning("Job with ID {JobId} not found for status update", id);
        //             return NotFound($"Job with ID {id} not found");
        //         }

        //         return Ok(job);
        //     }
        //     catch (InvalidOperationException ex)
        //     {
        //         _logger.LogWarning("Job status update failed: {Error}", ex.Message);
        //         return BadRequest(new { error = ex.Message });
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error updating job status for ID: {JobId} for user {UserId}", id, User.Identity?.Name);
        //         return StatusCode(500, new { error = "An error occurred while updating the job status" });
        //     }
        // }

        /// <summary>
        /// Delete job (soft delete)
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteJob(int id)
        {
            _logger.LogInformation("Deleting job with ID: {JobId} for user {UserId}", id, User.Identity?.Name);

            try
            {
                var result = await _jobService.DeleteJobAsync(id);
                if (!result)
                {
                    _logger.LogWarning("Job with ID {JobId} not found for deletion", id);
                    return NotFound($"Job with ID {id} not found");
                }

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Job deletion failed: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job with ID: {JobId} for user {UserId}", id, User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while deleting the job" });
            }
        }

        /// <summary>
        /// Mark jobs as invoiced (batch operation)
        /// </summary>
        [HttpPost("mark-as-invoiced")]
        public async Task<ActionResult> MarkJobsAsInvoiced(
            MarkJobsAsInvoicedDTO markJobsDto,
            [FromServices] IValidator<MarkJobsAsInvoicedDTO> validator)
        {
            // Manually validate with async support
            var validationResult = await validator.ValidateAsync(markJobsDto);
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

            _logger.LogInformation("Marking {JobCount} jobs as invoiced for invoice ID: {InvoiceId} for user {UserId}",
                markJobsDto.JobIds.Count(), markJobsDto.InvoiceId, User.Identity?.Name);

            try
            {
                var result = await _jobService.MarkJobsAsInvoicedAsync(markJobsDto.JobIds, markJobsDto.InvoiceId);
                if (!result)
                {
                    return BadRequest("Failed to mark jobs as invoiced");
                }

                return Ok(new { message = "Jobs marked as invoiced successfully" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Failed to mark jobs as invoiced: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking jobs as invoiced for user {UserId}", User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while marking jobs as invoiced" });
            }
        }

        /// <summary>
        /// Remove jobs from invoice
        /// </summary>
        [HttpPost("remove-from-invoice")]
        public async Task<ActionResult> RemoveJobsFromInvoice(
            RemoveJobsFromInvoiceDTO removeJobsDto,
            [FromServices] IValidator<RemoveJobsFromInvoiceDTO> validator)
        {
            // Manually validate with async support
            var validationResult = await validator.ValidateAsync(removeJobsDto);
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

            _logger.LogInformation("Removing {JobCount} jobs from invoice for user {UserId}", removeJobsDto.JobIds.Count(), User.Identity?.Name);

            try
            {
                var result = await _jobService.RemoveJobsFromInvoiceAsync(removeJobsDto.JobIds);
                if (!result)
                {
                    return BadRequest("Failed to remove jobs from invoice");
                }

                return Ok(new { message = "Jobs removed from invoice successfully" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Failed to remove jobs from invoice: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing jobs from invoice for user {UserId}", User.Identity?.Name);
                return StatusCode(500, new { error = "An error occurred while removing jobs from invoice" });
            }
        }
    }
}