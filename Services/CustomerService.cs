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
                .Include(c => c.Jobs.Where(j => !j.IsDeleted))
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
                .Include(c => c.Jobs.Where(j => !j.IsDeleted))
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
            // Check if email already exists
            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email.ToLower() == createDTO.Email.ToLower());

            if (existingCustomer != null)
            {
                throw new InvalidOperationException($"Customer with email '{createDTO.Email}' already exists");
            }

            var customer = _mapper.Map<Customer>(createDTO);
            customer.CreatedDate = DateTime.UtcNow;

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var customerDTO = _mapper.Map<CustomerDTO>(customer);
            CalculateCustomerStatistics(customerDTO, customer);

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
                .Include(c => c.Jobs.Where(j => !j.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                _logger.LogWarning("Customer not found for update with ID: {Id}", id);
                return null;
            }

            // Check if email already exists (excluding current customer)
            if (updateDTO.Email.ToLower() != customer.Email.ToLower())
            {
                var existingCustomer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email.ToLower() == updateDTO.Email.ToLower() && c.Id != id);

                if (existingCustomer != null)
                {
                    throw new InvalidOperationException($"Customer with email '{updateDTO.Email}' already exists");
                }
            }

            // Update fields
            _mapper.Map(updateDTO, customer);
            customer.UpdatedDate = DateTime.UtcNow;

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
                .Include(c => c.Jobs.Where(j => !j.IsDeleted))
                .Include(c => c.Invoices.Where(i => !i.IsDeleted))
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                _logger.LogWarning("Customer not found for deletion with ID: {Id}", id);
                return false;
            }

            // Check for active jobs
            var activeJobs = customer.Jobs.Where(j => j.Status == JobStatus.Active).ToList();
            if (activeJobs.Any())
            {
                throw new InvalidOperationException($"Cannot delete customer with {activeJobs.Count} active jobs");
            }

            // Check for unsent invoices
            var unsentInvoices = customer.Invoices.Where(i => i.Status == InvoiceStatus.Draft).ToList();
            if (unsentInvoices.Any())
            {
                throw new InvalidOperationException($"Cannot delete customer with {unsentInvoices.Count} unsent invoices");
            }

            customer.IsDeleted = true;
            customer.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Soft deleted customer: {Name}", customer.Name);
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
                .Include(c => c.Jobs.Where(j => !j.IsDeleted))
                .Where(c => c.Name.ToLower().Contains(searchLower)
                         || c.Email.ToLower().Contains(searchLower)
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

    public async Task<IEnumerable<CustomerSummaryDTO>> GetCustomerSummariesAsync()
    {
        _logger.LogInformation("Getting customer summaries for dropdowns");

        try
        {
            var customers = await _context.Customers
                .OrderBy(c => c.Name)
                .ProjectTo<CustomerSummaryDTO>(_mapper.ConfigurationProvider)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} customer summaries", customers.Count);
            return customers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer summaries");
            throw;
        }
    }

    private void CalculateCustomerStatistics(CustomerDTO customerDTO, Customer customer)
    {
        try
        {
            // Calculate job statistics
            var jobs = customer.Jobs.Where(j => !j.IsDeleted).ToList();
    
            customerDTO.ActiveJobs = jobs.Count(j => j.Status == JobStatus.Active);
            customerDTO.NewJobs = jobs.Count(j => j.Status == JobStatus.New);
            customerDTO.CompletedJobs = jobs.Count(j => j.Status == JobStatus.Completed);
            customerDTO.CancelledJobs = jobs.Count(j => j.Status == JobStatus.Cancelled);
            customerDTO.TotalRevenue = jobs
                .Where(j => j.Status == JobStatus.Completed)
                .Sum(j => j.Price);

            _logger.LogDebug("Calculated statistics for customer {Id}: {ActiveJobs} active, £{Revenue} revenue",
                customer.Id, customerDTO.ActiveJobs, customerDTO.TotalRevenue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating statistics for customer {Id}", customer.Id);
            // Set default values
            customerDTO.ActiveJobs = 0;
            customerDTO.NewJobs = 0;
            customerDTO.CompletedJobs = 0;
            customerDTO.CancelledJobs = 0;
            customerDTO.TotalRevenue = 0;
        }
    }
}