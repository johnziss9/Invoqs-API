using Microsoft.EntityFrameworkCore;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Invoqs.API.Data;
using Invoqs.API.Models;
using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;

namespace Invoqs.API.Services;

public class JobService : IJobService
{
    private readonly InvoqsDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<JobService> _logger;

    public JobService(InvoqsDbContext context, IMapper mapper, ILogger<JobService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<JobDTO>> GetAllJobsAsync()
    {
        try
        {
            var jobs = await _context.Jobs
                .Include(j => j.Customer)
                .Include(j => j.Invoice)
                .IgnoreQueryFilters()
                .Where(j => !j.IsDeleted)
                .OrderByDescending(j => j.CreatedDate)
                .ToListAsync();

            // Map to DTOs after loading from database
            var jobDtos = _mapper.Map<List<JobDTO>>(jobs);

            return jobDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all jobs");
            throw;
        }
    }

    public async Task<JobDTO?> GetJobByIdAsync(int id)
    {
        try
        {
            var job = await _context.Jobs
                .IgnoreQueryFilters()
                .Include(j => j.Customer)
                .Include(j => j.Invoice)
                .Where(j => j.Id == id && !j.IsDeleted)
                .FirstOrDefaultAsync();

            if (job == null)
            {
                _logger.LogWarning("Job not found with ID: {Id}", id);
                return null;
            }

            var jobDTO = _mapper.Map<JobDTO>(job);
            return jobDTO;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job with ID: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<JobDTO>> GetJobsByCustomerIdAsync(int customerId)
    {
        try
        {
            var jobs = await _context.Jobs
                .IgnoreQueryFilters()
                .Include(j => j.Customer)
                .Include(j => j.Invoice)
                .Where(j => j.CustomerId == customerId && !j.IsDeleted)
                .OrderByDescending(j => j.CreatedDate)
                .ToListAsync();

            var jobDTOs = _mapper.Map<List<JobDTO>>(jobs);

            return jobDTOs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jobs for customer ID: {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<IEnumerable<JobDTO>> GetCompletedUnInvoicedJobsAsync(int customerId)
    {
        _logger.LogInformation("Getting uninvoiced jobs for customer ID: {CustomerId}", customerId);

        try
        {
            var jobs = await _context.Jobs
                .Include(j => j.Customer)
                .Where(j => j.CustomerId == customerId
                         && j.InvoiceId == null)
                .OrderBy(j => j.JobDate)
                .ProjectTo<JobDTO>(_mapper.ConfigurationProvider)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} uninvoiced jobs for customer {CustomerId}", jobs.Count, customerId);
            return jobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving uninvoiced jobs for customer ID: {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<Dictionary<string, IEnumerable<JobDTO>>> GetJobsGroupedByAddressAsync(int customerId)
    {
        _logger.LogInformation("Getting jobs grouped by address for customer ID: {CustomerId}", customerId);

        try
        {
            var jobs = await _context.Jobs
                .Include(j => j.Customer)
                .Include(j => j.Invoice)
                .Where(j => j.CustomerId == customerId)
                .OrderByDescending(j => j.CreatedDate)
                .ToListAsync();

            var jobDTOs = _mapper.Map<List<JobDTO>>(jobs);

            var groupedJobs = jobDTOs
                .GroupBy(j => j.Address.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(j => j.CreatedDate).AsEnumerable()
                );

            _logger.LogInformation("Grouped {Count} jobs into {GroupCount} addresses for customer {CustomerId}",
                jobDTOs.Count, groupedJobs.Count, customerId);

            return groupedJobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error grouping jobs by address for customer ID: {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<JobDTO> CreateJobAsync(CreateJobDTO createDTO)
    {
        _logger.LogInformation("Creating new job: {Title} for customer {CustomerId}", createDTO.Title, createDTO.CustomerId);

        try
        {
            // Verify customer exists
            var customerExists = await _context.Customers.AnyAsync(c => c.Id == createDTO.CustomerId);
            if (!customerExists)
            {
                throw new InvalidOperationException($"Customer with ID {createDTO.CustomerId} does not exist");
            }

            var job = _mapper.Map<Job>(createDTO);
            job.CreatedDate = DateTime.UtcNow;

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            // Load the job with includes for DTO mapping
            var createdJob = await _context.Jobs
                .Include(j => j.Customer)
                .FirstAsync(j => j.Id == job.Id);

            var jobDTO = _mapper.Map<JobDTO>(createdJob);

            _logger.LogInformation("Created job with ID: {Id}", job.Id);
            return jobDTO;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job: {Title}", createDTO.Title);
            throw;
        }
    }

    public async Task<JobDTO?> UpdateJobAsync(int id, UpdateJobDTO updateDTO)
    {
        _logger.LogInformation("Updating job ID: {Id}", id);

        try
        {
            var job = await _context.Jobs
                .Include(j => j.Customer)
                .Include(j => j.Invoice)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                _logger.LogWarning("Job not found for update with ID: {Id}", id);
                return null;
            }

            // Map all updates
            _mapper.Map(updateDTO, job);
            job.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var jobDTO = _mapper.Map<JobDTO>(job);

            _logger.LogInformation("Updated job: {Title}", job.Title);
            return jobDTO;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job ID: {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteJobAsync(int id)
    {
        _logger.LogInformation("Soft deleting job ID: {Id}", id);

        try
        {
            var job = await _context.Jobs
                .Include(j => j.Invoice)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                _logger.LogWarning("Job not found for deletion with ID: {Id}", id);
                return false;
            }

            // Check if job is on an invoice
            if (job.InvoiceId.HasValue && job.Invoice != null && !job.Invoice.IsDeleted)
            {
                throw new InvalidOperationException($"Cannot delete job that is on invoice {job.Invoice.InvoiceNumber}");
            }

            job.IsDeleted = true;
            job.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Soft deleted job: {Title}", job.Title);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft deleting job ID: {Id}", id);
            throw;
        }
    }

    public async Task<bool> MarkJobsAsInvoicedAsync(IEnumerable<int> jobIds, int invoiceId)
    {
        _logger.LogInformation("Marking {Count} jobs as invoiced for invoice {InvoiceId}", jobIds.Count(), invoiceId);

        try
        {
            var jobs = await _context.Jobs
                .Where(j => jobIds.Contains(j.Id))
                .ToListAsync();

            if (jobs.Count != jobIds.Count())
            {
                throw new InvalidOperationException("One or more jobs not found");
            }

            // Validate all jobs are not already invoiced
            var invalidJobs = jobs.Where(j => j.InvoiceId.HasValue).ToList();
            if (invalidJobs.Any())
            {
                throw new InvalidOperationException("All jobs must not be already invoiced");
            }

            foreach (var job in jobs)
            {
                job.InvoiceId = invoiceId;
                job.InvoicedDate = DateTime.UtcNow;
                job.UpdatedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Marked {Count} jobs as invoiced", jobs.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking jobs as invoiced for invoice {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<bool> RemoveJobsFromInvoiceAsync(IEnumerable<int> jobIds)
    {
        _logger.LogInformation("Removing {Count} jobs from invoice", jobIds.Count());

        try
        {
            var jobs = await _context.Jobs
                .Where(j => jobIds.Contains(j.Id))
                .ToListAsync();

            if (jobs.Count != jobIds.Count())
            {
                throw new InvalidOperationException("One or more jobs not found");
            }

            foreach (var job in jobs)
            {
                job.InvoiceId = null;
                job.InvoicedDate = null;
                job.UpdatedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Removed {Count} jobs from invoice", jobs.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing jobs from invoice");
            throw;
        }
    }

    public async Task<IEnumerable<JobDTO>> GetJobsByInvoiceIdAsync(int invoiceId)
    {
        _logger.LogInformation("Getting jobs for invoice ID: {InvoiceId}", invoiceId);

        try
        {
            var jobs = await _context.Jobs
                .Include(j => j.Customer)
                .Include(j => j.Invoice)
                .Where(j => j.InvoiceId == invoiceId)
                .OrderBy(j => j.Title)
                .ProjectTo<JobDTO>(_mapper.ConfigurationProvider)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} jobs for invoice {InvoiceId}", jobs.Count, invoiceId);
            return jobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jobs for invoice ID: {InvoiceId}", invoiceId);
            throw;
        }
    }

    public async Task<IEnumerable<string>> SearchAddressesAsync(string query, int? customerId = null)
    {
        try
        {
            // Start with base query
            var jobQuery = _context.Jobs.Where(j => !j.IsDeleted && !string.IsNullOrEmpty(j.Address));

            // Filter by customer if provided
            if (customerId.HasValue && customerId.Value > 0)
            {
                jobQuery = jobQuery.Where(j => j.CustomerId == customerId.Value);
                
                // When customer filter is applied, allow filtering with ANY query length
                if (!string.IsNullOrWhiteSpace(query))
                {
                    jobQuery = jobQuery.Where(j => j.Address.ToLower().Contains(query.ToLower()));
                }
            }
            else
            {
                // No customer filter - require at least 2 characters
                if (!string.IsNullOrWhiteSpace(query) && query.Length >= 2)
                {
                    jobQuery = jobQuery.Where(j => j.Address.ToLower().Contains(query.ToLower()));
                }
                else
                {
                    // No query and no customer filter - return empty
                    return Enumerable.Empty<string>();
                }
            }

            var addresses = await jobQuery
                .Select(j => j.Address.Trim())
                .Distinct()
                .OrderBy(a => a)
                .Take(10)
                .ToListAsync();

            return addresses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching addresses with query: {Query}, CustomerId: {CustomerId}", query, customerId);
            throw;
        }
    }
}