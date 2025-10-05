using Microsoft.EntityFrameworkCore;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Invoqs.API.Data;
using Invoqs.API.Models;
using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;

namespace Invoqs.API.Services;

public class InvoiceService : IInvoiceService
{
    private readonly InvoqsDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(InvoqsDbContext context, IMapper mapper, ILogger<InvoiceService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<InvoiceDTO>> GetAllInvoicesAsync()
    {
        _logger.LogInformation("Getting all invoices");

        try
        {
            var invoices = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.LineItems)
                    .ThenInclude(li => li.Job)
                .OrderByDescending(i => i.CreatedDate)
                .ToListAsync();

            var invoiceDTOs = _mapper.Map<List<InvoiceDTO>>(invoices);

            // Calculate computed properties
            foreach (var invoiceDTO in invoiceDTOs)
            {
                CalculateInvoiceProperties(invoiceDTO);
            }

            _logger.LogInformation("Retrieved {Count} invoices", invoiceDTOs.Count);
            return invoiceDTOs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all invoices");
            throw;
        }
    }

    public async Task<InvoiceDTO?> GetInvoiceByIdAsync(int id)
    {
        _logger.LogInformation("Getting invoice by ID: {Id}", id);

        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.LineItems)
                    .ThenInclude(li => li.Job)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice not found with ID: {Id}", id);
                return null;
            }

            var invoiceDTO = _mapper.Map<InvoiceDTO>(invoice);
            CalculateInvoiceProperties(invoiceDTO);

            _logger.LogInformation("Retrieved invoice: {InvoiceNumber}", invoice.InvoiceNumber);
            return invoiceDTO;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice with ID: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<InvoiceDTO>> GetInvoicesByCustomerIdAsync(int customerId)
    {
        _logger.LogInformation("Getting invoices for customer ID: {CustomerId}", customerId);

        try
        {
            var invoices = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.LineItems)
                    .ThenInclude(li => li.Job)
                .Where(i => i.CustomerId == customerId)
                .OrderByDescending(i => i.CreatedDate)
                .ToListAsync();

            var invoiceDTOs = _mapper.Map<List<InvoiceDTO>>(invoices);

            // Calculate computed properties
            foreach (var invoiceDTO in invoiceDTOs)
            {
                CalculateInvoiceProperties(invoiceDTO);
            }

            _logger.LogInformation("Retrieved {Count} invoices for customer {CustomerId}", invoiceDTOs.Count, customerId);
            return invoiceDTOs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices for customer ID: {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<InvoiceDTO> CreateInvoiceAsync(CreateInvoiceDTO createDTO)
    {
        _logger.LogInformation("Creating invoice for customer {CustomerId} with {JobCount} jobs",
            createDTO.CustomerId, createDTO.JobIds.Count);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Verify customer exists
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == createDTO.CustomerId);
            if (customer == null)
            {
                throw new InvalidOperationException($"Customer with ID {createDTO.CustomerId} does not exist");
            }

            // Verify all jobs exist, are completed, uninvoiced, and belong to the customer
            var jobs = await _context.Jobs
                .Where(j => createDTO.JobIds.Contains(j.Id))
                .ToListAsync();

            if (jobs.Count != createDTO.JobIds.Count)
            {
                throw new InvalidOperationException("One or more jobs not found");
            }

            var invalidJobs = jobs.Where(j =>
                j.CustomerId != createDTO.CustomerId ||
                j.Status != JobStatus.Completed ||
                j.InvoiceId.HasValue).ToList();

            if (invalidJobs.Any())
            {
                throw new InvalidOperationException("All jobs must be completed, uninvoiced, and belong to the specified customer");
            }

            // Create invoice
            var invoice = new Invoice
            {
                InvoiceNumber = await GenerateInvoiceNumberAsync(),
                CustomerId = createDTO.CustomerId,
                Status = InvoiceStatus.Draft,
                PaymentTermsDays = createDTO.PaymentTermsDays,
                Notes = createDTO.Notes,
                CreatedDate = DateTime.UtcNow,
                VatRate = createDTO.VatRate
            };

            // Calculate due date
            invoice.DueDate = invoice.CreatedDate.AddDays(invoice.PaymentTermsDays);

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync(); // Save to get invoice ID

            // Create line items
            decimal subtotal = 0;
            foreach (var job in jobs)
            {
                var lineItem = new InvoiceLineItem
                {
                    InvoiceId = invoice.Id,
                    JobId = job.Id,
                    Description = GetJobInvoiceDescription(job),
                    Quantity = 1,
                    UnitPrice = job.Price,
                    LineTotal = job.Price
                };

                _context.InvoiceLineItems.Add(lineItem);
                subtotal += job.Price;

                // Mark job as invoiced
                job.InvoiceId = invoice.Id;
                job.InvoicedDate = DateTime.UtcNow;
                job.UpdatedDate = DateTime.UtcNow;
            }

            // Calculate totals
            invoice.Subtotal = subtotal;
            invoice.VatAmount = Math.Round(subtotal * (invoice.VatRate / 100), 2);
            invoice.Total = invoice.Subtotal + invoice.VatAmount;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Load the created invoice with all includes
            var createdInvoice = await GetInvoiceByIdAsync(invoice.Id);

            _logger.LogInformation("Created invoice {InvoiceNumber} with total £{Total}",
                invoice.InvoiceNumber, invoice.Total);

            return createdInvoice!;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating invoice for customer {CustomerId}", createDTO.CustomerId);
            throw;
        }
    }

    public async Task<InvoiceDTO?> UpdateInvoiceAsync(int id, UpdateInvoiceDTO updateDTO)
    {
        _logger.LogInformation("Updating invoice ID: {Id}", id);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.LineItems)
                    .ThenInclude(li => li.Job)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice not found for update with ID: {Id}", id);
                return null;
            }

            // Only allow updates to draft invoices
            if (invoice.Status != InvoiceStatus.Draft)
            {
                throw new InvalidOperationException("Only draft invoices can be updated");
            }

            // Handle job changes if provided
            if (updateDTO.JobIds != null && updateDTO.JobIds.Any())
            {
                // Remove existing line items and clear job references
                var existingJobIds = invoice.LineItems.Select(li => li.JobId).ToList();

                // Clear existing job references
                var existingJobs = await _context.Jobs
                    .Where(j => existingJobIds.Contains(j.Id))
                    .ToListAsync();

                foreach (var job in existingJobs)
                {
                    job.InvoiceId = null;
                    job.InvoicedDate = null;
                    job.UpdatedDate = DateTime.UtcNow;
                }

                // Remove existing line items
                _context.InvoiceLineItems.RemoveRange(invoice.LineItems);

                // Verify new jobs
                var newJobs = await _context.Jobs
                    .Where(j => updateDTO.JobIds.Contains(j.Id))
                    .ToListAsync();

                if (newJobs.Count != updateDTO.JobIds.Count)
                {
                    throw new InvalidOperationException("One or more jobs not found");
                }

                var invalidJobs = newJobs.Where(j =>
                    j.CustomerId != invoice.CustomerId ||
                    j.Status != JobStatus.Completed ||
                    j.InvoiceId.HasValue).ToList();

                if (invalidJobs.Any())
                {
                    throw new InvalidOperationException("All jobs must be completed, uninvoiced, and belong to the invoice customer");
                }

                // Create new line items
                decimal subtotal = 0;
                foreach (var job in newJobs)
                {
                    var lineItem = new InvoiceLineItem
                    {
                        InvoiceId = invoice.Id,
                        JobId = job.Id,
                        Description = GetJobInvoiceDescription(job),
                        Quantity = 1,
                        UnitPrice = job.Price,
                        LineTotal = job.Price
                    };

                    _context.InvoiceLineItems.Add(lineItem);
                    subtotal += job.Price;

                    // Mark job as invoiced
                    job.InvoiceId = invoice.Id;
                    job.InvoicedDate = DateTime.UtcNow;
                    job.UpdatedDate = DateTime.UtcNow;
                }

                // Recalculate totals
                invoice.Subtotal = subtotal;
                invoice.VatRate = updateDTO.VatRate;
                invoice.VatAmount = Math.Round(subtotal * (invoice.VatRate / 100), 2);
                invoice.Total = invoice.Subtotal + invoice.VatAmount;
            }
            else
            {
                // Update only invoice properties
                if (updateDTO.VatRate > 0)
                {
                    invoice.VatRate = updateDTO.VatRate;
                    invoice.VatAmount = Math.Round(invoice.Subtotal * (invoice.VatRate / 100), 2);
                    invoice.Total = invoice.Subtotal + invoice.VatAmount;
                }

                if (updateDTO.PaymentTermsDays > 0)
                {
                    invoice.PaymentTermsDays = updateDTO.PaymentTermsDays;
                    invoice.DueDate = invoice.CreatedDate.AddDays(invoice.PaymentTermsDays);
                }

                if (!string.IsNullOrEmpty(updateDTO.Notes))
                {
                    invoice.Notes = updateDTO.Notes;
                }
            }

            invoice.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var updatedInvoice = await GetInvoiceByIdAsync(invoice.Id);

            _logger.LogInformation("Updated invoice: {InvoiceNumber}", invoice.InvoiceNumber);
            return updatedInvoice;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating invoice ID: {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteInvoiceAsync(int id)
    {
        _logger.LogInformation("Soft deleting invoice ID: {Id}", id);

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.LineItems)
                    .ThenInclude(li => li.Job)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice not found for deletion with ID: {Id}", id);
                return false;
            }

            // Only allow deletion of draft invoices
            if (invoice.Status != InvoiceStatus.Draft)
            {
                throw new InvalidOperationException("Only draft invoices can be deleted");
            }

            // Clear job invoice references
            foreach (var lineItem in invoice.LineItems)
            {
                if (lineItem.Job != null)
                {
                    lineItem.Job.InvoiceId = null;
                    lineItem.Job.InvoicedDate = null;
                    lineItem.Job.UpdatedDate = DateTime.UtcNow;
                }
            }

            // Soft delete invoice and line items
            invoice.IsDeleted = true;
            invoice.UpdatedDate = DateTime.UtcNow;

            foreach (var lineItem in invoice.LineItems)
            {
                lineItem.IsDeleted = true;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Soft deleted invoice: {InvoiceNumber}", invoice.InvoiceNumber);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error soft deleting invoice ID: {Id}", id);
            throw;
        }
    }

    public async Task<InvoiceDTO?> MarkInvoiceAsSentAsync(int id)
    {
        _logger.LogInformation("Marking invoice as sent ID: {Id}", id);

        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.LineItems)
                    .ThenInclude(li => li.Job)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice not found for marking as sent with ID: {Id}", id);
                return null;
            }

            if (invoice.Status != InvoiceStatus.Draft)
            {
                throw new InvalidOperationException("Only draft invoices can be marked as sent");
            }

            invoice.Status = InvoiceStatus.Sent;
            invoice.SentDate = DateTime.UtcNow;
            invoice.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var invoiceDTO = await GetInvoiceByIdAsync(invoice.Id);

            _logger.LogInformation("Marked invoice as sent: {InvoiceNumber}", invoice.InvoiceNumber);
            return invoiceDTO;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking invoice as sent ID: {Id}", id);
            throw;
        }
    }

    public async Task<InvoiceDTO?> MarkInvoiceAsPaidAsync(int id, MarkInvoiceAsPaidDTO paymentDTO)
    {
        _logger.LogInformation("Marking invoice as paid with invoice ID: {Id}", id);

        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.LineItems)
                    .ThenInclude(li => li.Job)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice not found to mark as paid with ID: {Id}", id);
                return null;
            }

            if (invoice.Status != InvoiceStatus.Sent && invoice.Status != InvoiceStatus.Overdue)
            {
                throw new InvalidOperationException("Only sent or overdue invoices can be marked as paid");
            }

            invoice.Status = InvoiceStatus.Paid;
            invoice.PaymentDate = paymentDTO.PaymentDate;
            invoice.PaymentMethod = paymentDTO.PaymentMethod;
            invoice.PaymentReference = paymentDTO.PaymentReference;
            invoice.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var invoiceDTO = await GetInvoiceByIdAsync(invoice.Id);

            _logger.LogInformation("Marked invoice as paid with Invoice ID: {InvoiceNumber}", invoice.InvoiceNumber);
            return invoiceDTO;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking invoice as paid with invoice ID: {Id}", id);
            throw;
        }
    }

    public async Task<InvoiceDTO?> CancelInvoiceAsync(int id)
    {
        _logger.LogInformation("Cancelling invoice ID: {Id}", id);

        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.LineItems)
                    .ThenInclude(li => li.Job)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice not found for cancellation with ID: {Id}", id);
                return null;
            }

            if (invoice.Status == InvoiceStatus.Paid)
            {
                throw new InvalidOperationException("Paid invoices cannot be cancelled");
            }

            invoice.Status = InvoiceStatus.Cancelled;
            invoice.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var invoiceDTO = await GetInvoiceByIdAsync(invoice.Id);

            _logger.LogInformation("Cancelled invoice: {InvoiceNumber}", invoice.InvoiceNumber);
            return invoiceDTO;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling invoice ID: {Id}", id);
            throw;
        }
    }

    public async Task<decimal> GetTotalOutstandingAsync()
    {
        try
        {
            return await _context.Invoices
                .Where(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue)
                .SumAsync(i => i.Total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating total outstanding amount");
            throw;
        }
    }

    // public async Task<InvoiceStatisticsDTO> GetInvoiceStatisticsAsync()
    // {
    //     _logger.LogInformation("Getting invoice statistics");

    //     try
    //     {
    //         var invoices = await _context.Invoices.ToListAsync();

    //         var statistics = new InvoiceStatisticsDTO
    //         {
    //             TotalInvoices = invoices.Count,
    //             DraftInvoices = invoices.Count(i => i.Status == InvoiceStatus.Draft),
    //             SentInvoices = invoices.Count(i => i.Status == InvoiceStatus.Sent),
    //             PaidInvoices = invoices.Count(i => i.Status == InvoiceStatus.Paid),
    //             OverdueInvoices = invoices.Count(i => i.Status == InvoiceStatus.Overdue),
    //             CancelledInvoices = invoices.Count(i => i.Status == InvoiceStatus.Cancelled),
    //             TotalOutstanding = invoices
    //                 .Where(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue)
    //                 .Sum(i => i.Total),
    //             TotalPaid = invoices
    //                 .Where(i => i.Status == InvoiceStatus.Paid)
    //                 .Sum(i => i.Total)
    //         };

    //         _logger.LogInformation("Retrieved invoice statistics: {TotalCount} total, £{Outstanding} outstanding",
    //             statistics.TotalInvoices, statistics.TotalOutstanding);

    //         return statistics;
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error retrieving invoice statistics");
    //         throw;
    //     }
    // }

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"INV-{year}-";

        var lastInvoice = await _context.Invoices
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastInvoice != null)
        {
            var numberPart = lastInvoice.InvoiceNumber.Substring(prefix.Length);
            if (int.TryParse(numberPart, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D4}";
    }

    private static decimal CalculateDefaultVatRate(IEnumerable<Job> jobs)
    {
        // 5% for skip rentals, 19% for others
        var hasSkipRental = jobs.Any(j => j.Type == JobType.SkipRental);
        var hasOtherTypes = jobs.Any(j => j.Type != JobType.SkipRental);

        if (hasSkipRental && !hasOtherTypes)
            return 5.0m;

        if (hasOtherTypes && !hasSkipRental)
            return 19.0m;

        // Mixed types - use higher rate
        return 19.0m;
    }

    private static string GetJobInvoiceDescription(Job job)
    {
        var typeDisplay = job.Type switch
        {
            JobType.SkipRental => "Skip Rental",
            JobType.SandDelivery => "Sand Delivery",
            JobType.ForkLiftService => "Fork Lift Service",
            _ => job.Type.ToString()
        };

        return $"{typeDisplay} - {job.Title} at {job.Address}";
    }

    private static void CalculateInvoiceProperties(InvoiceDTO invoiceDTO)
    {
        // Calculate overdue status
        if (invoiceDTO.Status == InvoiceStatus.Sent && invoiceDTO.DueDate.HasValue)
        {
            if (DateTime.UtcNow.Date > invoiceDTO.DueDate.Value.Date)
            {
                invoiceDTO.Status = InvoiceStatus.Overdue;
            }
        }

        // Calculate days until due (for sent invoices)
        if (invoiceDTO.Status == InvoiceStatus.Sent && invoiceDTO.DueDate.HasValue)
        {
            var daysUntilDue = (invoiceDTO.DueDate.Value.Date - DateTime.UtcNow.Date).Days;
        }
    }
}