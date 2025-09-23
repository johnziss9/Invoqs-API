using Microsoft.AspNetCore.Mvc;
using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;

namespace Invoqs.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            _logger.LogInformation("Getting all jobs");
            var jobs = await _jobService.GetAllJobsAsync();
            return Ok(jobs);
        }

        /// <summary>
        /// Get job by ID with full details
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<JobDTO>> GetJob(int id)
        {
            _logger.LogInformation("Getting job with ID: {JobId}", id);

            var job = await _jobService.GetJobByIdAsync(id);
            if (job == null)
            {
                _logger.LogWarning("Job with ID {JobId} not found", id);
                return NotFound($"Job with ID {id} not found");
            }

            return Ok(job);
        }

        /// <summary>
        /// Get all jobs for a specific customer
        /// </summary>
        [HttpGet("customer/{customerId:int}")]
        public async Task<ActionResult<IEnumerable<JobDTO>>> GetJobsByCustomer(int customerId)
        {
            _logger.LogInformation("Getting jobs for customer ID: {CustomerId}", customerId);
            var jobs = await _jobService.GetJobsByCustomerIdAsync(customerId);
            return Ok(jobs);
        }

        /// <summary>
        /// Get completed uninvoiced jobs for a customer (for invoice creation)
        /// </summary>
        [HttpGet("customer/{customerId:int}/uninvoiced")]
        public async Task<ActionResult<IEnumerable<JobDTO>>> GetUninvoicedJobs(int customerId)
        {
            _logger.LogInformation("Getting uninvoiced jobs for customer ID: {CustomerId}", customerId);
            var jobs = await _jobService.GetCompletedUnInvoicedJobsAsync(customerId);
            return Ok(jobs);
        }

        /// <summary>
        /// Get jobs grouped by address for a customer (Blazor feature)
        /// </summary>
        [HttpGet("customer/{customerId:int}/grouped-by-address")]
        public async Task<ActionResult<Dictionary<string, IEnumerable<JobDTO>>>> GetJobsGroupedByAddress(int customerId)
        {
            _logger.LogInformation("Getting jobs grouped by address for customer ID: {CustomerId}", customerId);
            var jobGroups = await _jobService.GetJobsGroupedByAddressAsync(customerId);
            return Ok(jobGroups);
        }

        /// <summary>
        /// Get jobs for a specific invoice
        /// </summary>
        [HttpGet("invoice/{invoiceId:int}")]
        public async Task<ActionResult<IEnumerable<JobDTO>>> GetJobsByInvoice(int invoiceId)
        {
            _logger.LogInformation("Getting jobs for invoice ID: {InvoiceId}", invoiceId);
            var jobs = await _jobService.GetJobsByInvoiceIdAsync(invoiceId);
            return Ok(jobs);
        }

        /// <summary>
        /// Create a new job
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<JobDTO>> CreateJob(CreateJobDTO createJobDto)
        {
            _logger.LogInformation("Creating new job: {JobTitle} for customer ID: {CustomerId}",
                createJobDto.Title, createJobDto.CustomerId);

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
        }

        /// <summary>
        /// Update existing job
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<JobDTO>> UpdateJob(int id, UpdateJobDTO updateJobDto)
        {
            _logger.LogInformation("Updating job with ID: {JobId}", id);

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
        }

        /// <summary>
        /// Update job status only
        /// </summary>
        [HttpPut("{id:int}/status")]
        public async Task<ActionResult<JobDTO>> UpdateJobStatus(int id, UpdateJobStatusDTO statusDto)
        {
            _logger.LogInformation("Updating status for job ID: {JobId} to {Status}", id, statusDto.Status);

            try
            {
                var job = await _jobService.UpdateJobStatusAsync(id, statusDto);
                if (job == null)
                {
                    _logger.LogWarning("Job with ID {JobId} not found for status update", id);
                    return NotFound($"Job with ID {id} not found");
                }

                return Ok(job);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Job status update failed: {Error}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Delete job (soft delete)
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteJob(int id)
        {
            _logger.LogInformation("Deleting job with ID: {JobId}", id);

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
        }

        /// <summary>
        /// Mark jobs as invoiced (batch operation)
        /// </summary>
        [HttpPost("mark-as-invoiced")]
        public async Task<ActionResult> MarkJobsAsInvoiced(MarkJobsAsInvoicedDTO markJobsDto)
        {
            _logger.LogInformation("Marking {JobCount} jobs as invoiced for invoice ID: {InvoiceId}",
                markJobsDto.JobIds.Count(), markJobsDto.InvoiceId);

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
        }

        /// <summary>
        /// Remove jobs from invoice
        /// </summary>
        [HttpPost("remove-from-invoice")]
        public async Task<ActionResult> RemoveJobsFromInvoice(RemoveJobsFromInvoiceDTO removeJobsDto)
        {
            _logger.LogInformation("Removing {JobCount} jobs from invoice", removeJobsDto.JobIds.Count());

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
        }
    }
}