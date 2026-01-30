using Microsoft.EntityFrameworkCore;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Invoqs.API.Data;
using Invoqs.API.Models;
using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;

namespace Invoqs.API.Services;

public class CustomerService : ICustomerService
{
    private readonly InvoqsDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(InvoqsDbContext context, IMapper mapper, ILogger<CustomerService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<CustomerDTO>> GetAllCustomersAsync()
    {
        _logger.LogInformation("Getting all customers");

        try
        {
            var customers = await _context.Customers
                .Include(c => c.Emails)
                .Include(c => c.Jobs.Where(j => !j.IsDeleted))
                .Include(c => c.Invoices.Where(i => !i.IsDeleted))
                .OrderBy(c => c.Name)
                .ToListAsync();

            var customerDTOs = new List<CustomerDTO>();

            foreach (var customer in customers)
            {
                var customerDTO = _mapper.Map<CustomerDTO>(customer);
                CalculateCustomerStatistics(customerDTO, customer);
                customerDTOs.Add(customerDTO);
            }

            _logger.LogInformation("Retrieved {Count} customers", customerDTOs.Count);
            return customerDTOs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all customers");
            throw;
        }
    }

    public async Task<CustomerDTO?> GetCustomerByIdAsync(int id)
    {
        _logger.LogInformation("Getting customer by ID: {Id}", id);

        try
        {
            var customer = await _context.Customers
                .Include(c => c.Emails)
                .Include(c => c.Jobs.Where(j => !j.IsDeleted))
                .Include(c => c.Invoices.Where(i => !i.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                _logger.LogWarning("Customer not found with ID: {Id}", id);
                return null;
            }

            var customerDTO = _mapper.Map<CustomerDTO>(customer);
            CalculateCustomerStatistics(customerDTO, customer);

            _logger.LogInformation("Retrieved customer: {Name}", customer.Name);
            return customerDTO;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer with ID: {Id}", id);
            throw;
        }
    }

    public async Task<CustomerDTO> CreateCustomerAsync(CreateCustomerDTO createDTO)
    {
        _logger.LogInformation("Creating new customer: {Name}", createDTO.Name);

        try
        {
            var customer = _mapper.Map<Customer>(createDTO);
            customer.CreatedDate = DateTime.UtcNow;

            // Add emails to the customer
            foreach (var email in createDTO.Emails)
            {
                customer.Emails.Add(new CustomerEmail
                {
                    Email = email.Trim(),
                    CreatedDate = DateTime.UtcNow
                });
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            // Reload customer with emails to ensure proper mapping
            var createdCustomer = await _context.Customers
                .Include(c => c.Emails)
                .Include(c => c.Jobs.Where(j => !j.IsDeleted))
                .Include(c => c.Invoices.Where(i => !i.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == customer.Id);

            var customerDTO = _mapper.Map<CustomerDTO>(createdCustomer!);
            CalculateCustomerStatistics(customerDTO, createdCustomer!);

            _logger.LogInformation("Created customer with ID: {Id}", customer.Id);
            return customerDTO;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer: {Name}", createDTO.Name);
            throw;
        }
    }

    public async Task<CustomerDTO?> UpdateCustomerAsync(int id, UpdateCustomerDTO updateDTO)
    {
        _logger.LogInformation("Updating customer ID: {Id}", id);

        try
        {
            var customer = await _context.Customers
                .Include(c => c.Emails)
                .Include(c => c.Jobs.Where(j => !j.IsDeleted))
                .Include(c => c.Invoices.Where(i => !i.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                _logger.LogWarning("Customer not found for update with ID: {Id}", id);
                return null;
            }

            // Update basic fields (excluding emails)
            _mapper.Map(updateDTO, customer);
            customer.UpdatedDate = DateTime.UtcNow;

            // Update emails collection
            // Remove all existing emails
            _context.CustomerEmails.RemoveRange(customer.Emails);

            // Add new emails
            foreach (var email in updateDTO.Emails)
            {
                customer.Emails.Add(new CustomerEmail
                {
                    Email = email.Trim(),
                    CustomerId = customer.Id,
                    CreatedDate = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            var customerDTO = _mapper.Map<CustomerDTO>(customer);
            CalculateCustomerStatistics(customerDTO, customer);

            _logger.LogInformation("Updated customer: {Name}", customer.Name);
            return customerDTO;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer ID: {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteCustomerAsync(int id)
    {
        _logger.LogInformation("Soft deleting customer ID: {Id}", id);

        try
        {
            var customer = await _context.Customers
                .Include(c => c.Emails)
                .Include(c => c.Jobs.Where(j => !j.IsDeleted))
                .Include(c => c.Invoices.Where(i => !i.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                _logger.LogWarning("Customer not found for deletion with ID: {Id}", id);
                return false;
            }

            // Get all non-deleted jobs
            var jobs = customer.Jobs.ToList();

            // If customer has jobs, they must ALL be invoiced and paid
            if (jobs.Any())
            {
                // Check for jobs that are NOT invoiced
                var uninvoicedJobs = jobs.Where(j => !j.IsInvoiced).ToList();
                if (uninvoicedJobs.Any())
                {
                    throw new InvalidOperationException(
                        $"Cannot delete customer with {uninvoicedJobs.Count} uninvoiced job(s). " +
                        "All jobs must be invoiced and paid before customer deletion.");
                }

                // Get all unique invoice IDs from the customer's jobs
                var jobInvoiceIds = jobs.Where(j => j.InvoiceId.HasValue)
                                       .Select(j => j.InvoiceId!.Value)
                                       .Distinct()
                                       .ToList();

                // Check if all invoices are paid
                var unpaidInvoices = customer.Invoices
                    .Where(i => jobInvoiceIds.Contains(i.Id) &&
                               i.Status != InvoiceStatus.Paid)
                    .ToList();

                if (unpaidInvoices.Any())
                {
                    var invoiceStatuses = string.Join(", ", unpaidInvoices.Select(i => $"Invoice #{i.InvoiceNumber} ({i.Status})"));
                    throw new InvalidOperationException(
                        $"Cannot delete customer with {unpaidInvoices.Count} unpaid invoice(s): {invoiceStatuses}. " +
                        "All invoices must be paid before customer deletion.");
                }
            }

            // Also check for any draft invoices not associated with jobs
            var draftInvoices = customer.Invoices
                .Where(i => i.Status == InvoiceStatus.Draft)
                .ToList();

            if (draftInvoices.Any())
            {
                throw new InvalidOperationException(
                    $"Cannot delete customer with {draftInvoices.Count} draft invoice(s). " +
                    "Please delete or finalize all draft invoices first.");
            }

            // All validations passed - safe to delete
            customer.IsDeleted = true;
            customer.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft deleting customer ID: {Id}", id);
            throw;
        }
    }

    public async Task<bool> CustomerExistsAsync(int id)
    {
        try
        {
            return await _context.Customers.AnyAsync(c => c.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if customer exists with ID: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<CustomerDTO>> SearchCustomersAsync(string searchTerm)
    {
        _logger.LogInformation("Searching customers with term: {SearchTerm}", searchTerm);

        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllCustomersAsync();
            }

            var searchLower = searchTerm.ToLower();

            var customers = await _context.Customers
                .Include(c => c.Emails)
                .Include(c => c.Jobs.Where(j => !j.IsDeleted))
                .Include(c => c.Invoices.Where(i => !i.IsDeleted))
                .Where(c => c.Name.ToLower().Contains(searchLower)
                         || c.Emails.Any(e => e.Email.ToLower().Contains(searchLower))
                         || (c.Phone != null && c.Phone.Contains(searchTerm)))
                .OrderBy(c => c.Name)
                .ToListAsync();

            var customerDTOs = new List<CustomerDTO>();

            foreach (var customer in customers)
            {
                var customerDTO = _mapper.Map<CustomerDTO>(customer);
                CalculateCustomerStatistics(customerDTO, customer);
                customerDTOs.Add(customerDTO);
            }

            _logger.LogInformation("Found {Count} customers matching '{SearchTerm}'", customerDTOs.Count, searchTerm);
            return customerDTOs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching customers with term: {SearchTerm}", searchTerm);
            throw;
        }
    }

    // public async Task<IEnumerable<CustomerSummaryDTO>> GetCustomerSummariesAsync()
    // {
    //     _logger.LogInformation("Getting customer summaries for dropdowns");

    //     try
    //     {
    //         var customers = await _context.Customers
    //             .OrderBy(c => c.Name)
    //             .ProjectTo<CustomerSummaryDTO>(_mapper.ConfigurationProvider)
    //             .ToListAsync();

    //         _logger.LogInformation("Retrieved {Count} customer summaries", customers.Count);
    //         return customers;
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error retrieving customer summaries");
    //         throw;
    //     }
    // }

    private void CalculateCustomerStatistics(CustomerDTO customerDTO, Customer customer)
    {
        try
        {
            // Calculate job statistics
            var jobs = customer.Jobs.Where(j => !j.IsDeleted).ToList();

            customerDTO.TotalJobs = jobs.Count;
            customerDTO.UninvoicedJobs = jobs.Count(j => !j.IsInvoiced);
            customerDTO.TotalRevenue = jobs.Sum(j => j.Price);

            // Get invoices for this customer
            var invoices = customer.Invoices.Where(i => !i.IsDeleted).ToList();

            customerDTO.UnpaidInvoices = invoices.Count(i => 
                i.Status == InvoiceStatus.Sent || 
                i.Status == InvoiceStatus.Overdue);

            customerDTO.OutstandingAmount = invoices
                .Where(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue)
                .Sum(i => i.Total);

            _logger.LogDebug("Calculated statistics for customer {Id}: {TotalJobs} total jobs, {UninvoicedJobs} uninvoiced, {UnpaidInvoices} unpaid, £{Revenue} revenue",
                customer.Id, customerDTO.TotalJobs, customerDTO.UninvoicedJobs, customerDTO.UnpaidInvoices, customerDTO.TotalRevenue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating statistics for customer {Id}", customer.Id);
            // Set default values
            customerDTO.TotalJobs = 0;
            customerDTO.UninvoicedJobs = 0;
            customerDTO.TotalRevenue = 0;
            customerDTO.UnpaidInvoices = 0;
            customerDTO.OutstandingAmount = 0;
        }
    }

    public async Task<List<DuplicateCustomerDTO>> CheckEmailDuplicatesAsync(List<string> emails, int? excludeCustomerId = null)
    {
        _logger.LogInformation("Checking for duplicate emails across customers");

        try
        {
            // Normalize emails for case-insensitive comparison
            var normalizedEmails = emails.Select(e => e.ToLower().Trim()).ToList();

            // Find customers with matching emails
            var customersWithDuplicates = await _context.Customers
                .Include(c => c.Emails)
                .Include(c => c.Jobs.Where(j => !j.IsDeleted))
                .Where(c => c.Id != excludeCustomerId && // Exclude current customer if editing
                           c.Emails.Any(e => normalizedEmails.Contains(e.Email.ToLower())))
                .ToListAsync();

            var duplicateCustomers = new List<DuplicateCustomerDTO>();

            foreach (var customer in customersWithDuplicates)
            {
                // Find which emails match
                var matchingEmails = customer.Emails
                    .Where(e => normalizedEmails.Contains(e.Email.ToLower()))
                    .Select(e => e.Email)
                    .ToList();

                duplicateCustomers.Add(new DuplicateCustomerDTO
                {
                    Id = customer.Id,
                    Name = customer.Name,
                    Phone = customer.Phone ?? string.Empty,
                    TotalJobs = customer.Jobs.Count,
                    CreatedDate = customer.CreatedDate,
                    MatchingEmails = matchingEmails
                });
            }

            _logger.LogInformation("Found {Count} customers with duplicate emails", duplicateCustomers.Count);
            return duplicateCustomers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for duplicate emails");
            throw;
        }
    }
}