using Invoqs.API.DTOs;

namespace Invoqs.API.Interfaces;

public interface IJobService
{
    Task<IEnumerable<JobDTO>> GetAllJobsAsync();
    Task<JobDTO?> GetJobByIdAsync(int id);
    Task<IEnumerable<JobDTO>> GetJobsByCustomerIdAsync(int customerId);
    Task<IEnumerable<JobDTO>> GetCompletedUnInvoicedJobsAsync(int customerId);
    Task<Dictionary<string, IEnumerable<JobDTO>>> GetJobsGroupedByAddressAsync(int customerId);
    Task<IEnumerable<JobDTO>> GetJobsByInvoiceIdAsync(int invoiceId);
    Task<JobDTO> CreateJobAsync(CreateJobDTO createDTO);
    Task<JobDTO?> UpdateJobAsync(int id, UpdateJobDTO updateDTO);
    Task<JobDTO?> UpdateJobStatusAsync(int id, UpdateJobStatusDTO statusDTO);
    Task<bool> DeleteJobAsync(int id);
    Task<bool> MarkJobsAsInvoicedAsync(IEnumerable<int> jobIds, int invoiceId);
    Task<bool> RemoveJobsFromInvoiceAsync(IEnumerable<int> jobIds);
    Task<IEnumerable<string>> SearchAddressesAsync(string query);
}