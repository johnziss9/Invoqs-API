using Microsoft.EntityFrameworkCore;
using Invoqs.API.Data;
using Invoqs.API.Models;
using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;
using Invoqs.API.Helpers;

namespace Invoqs.API.Services;

public class CustomerStatementService : ICustomerStatementService
{
    private readonly InvoqsDbContext _context;
    private readonly ILogger<CustomerStatementService> _logger;
    private readonly IEmailService _emailService;
    private readonly IPdfService _pdfService;

    public CustomerStatementService(
        InvoqsDbContext context,
        ILogger<CustomerStatementService> logger,
        IEmailService emailService,
        IPdfService pdfService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _pdfService = pdfService;
    }

    public async Task<IEnumerable<CustomerStatementDTO>> GetAllCustomerStatementsAsync()
    {
        try
        {
            var statements = await _context.CustomerStatements
                .Include(s => s.Customer)
                    .ThenInclude(c => c.Emails)
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();

            var dtos = new List<CustomerStatementDTO>();
            foreach (var s in statements)
            {
                dtos.Add(await MapToDTO(s));
            }
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all customer statements");
            throw;
        }
    }

    public async Task<IEnumerable<CustomerStatementDTO>> GetCustomerStatementsAsync(int customerId)
    {
        try
        {
            var statements = await _context.CustomerStatements
                .Include(s => s.Customer)
                    .ThenInclude(c => c.Emails)
                .Where(s => !s.IsDeleted && s.CustomerId == customerId)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();

            var dtos = new List<CustomerStatementDTO>();
            foreach (var s in statements)
            {
                dtos.Add(await MapToDTO(s));
            }
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer statements for customer ID: {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<CustomerStatementDTO?> GetCustomerStatementByIdAsync(int id)
    {
        try
        {
            var statement = await _context.CustomerStatements
                .Include(s => s.Customer)
                    .ThenInclude(c => c.Emails)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (statement == null) return null;

            return await MapToDTO(statement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer statement ID: {Id}", id);
            throw;
        }
    }

    public async Task<CustomerStatementDTO> CreateCustomerStatementAsync(CreateCustomerStatementDTO createDTO)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (createDTO.EndDate < createDTO.StartDate)
                throw new InvalidOperationException("End date must be after or equal to start date");

            var customer = await _context.Customers
                .Include(c => c.Emails)
                .FirstOrDefaultAsync(c => c.Id == createDTO.CustomerId);

            if (customer == null)
                throw new InvalidOperationException($"Customer with ID {createDTO.CustomerId} not found");

            var startDateUtc = createDTO.StartDate.ToUniversalTime();
            var endDateUtc = createDTO.EndDate.Date.AddDays(1).AddTicks(-1).ToUniversalTime();

            var allInvoices = await _context.Invoices
                .Where(i => !i.IsDeleted &&
                            i.CustomerId == createDTO.CustomerId &&
                            i.Status != InvoiceStatus.Draft &&
                            i.CreatedDate >= startDateUtc &&
                            i.CreatedDate <= endDateUtc)
                .OrderBy(i => i.CreatedDate)
                .ToListAsync();

            if (!allInvoices.Any())
                throw new InvalidOperationException("No invoices found for this customer in the specified date range");

            var activeInvoices = allInvoices.Where(i => i.Status != InvoiceStatus.Cancelled).ToList();
            var cancelledInvoices = allInvoices.Where(i => i.Status == InvoiceStatus.Cancelled).ToList();

            var paidInvoices = activeInvoices.Where(i => i.Status == InvoiceStatus.Paid).ToList();
            var partiallyPaidInvoices = activeInvoices.Where(i => i.Status == InvoiceStatus.PartiallyPaid).ToList();
            var outstandingInvoices = activeInvoices.Where(i =>
                i.Status == InvoiceStatus.Sent ||
                i.Status == InvoiceStatus.Delivered ||
                i.Status == InvoiceStatus.Overdue ||
                i.Status == InvoiceStatus.PartiallyPaid).ToList();

            var statement = new CustomerStatement
            {
                StatementNumber = await GenerateStatementNumberAsync(),
                CustomerId = createDTO.CustomerId,
                StartDate = startDateUtc,
                EndDate = endDateUtc,
                TotalInvoiced = activeInvoices.Sum(i => i.Total),
                TotalVatAmount = activeInvoices.Sum(i => i.VatAmount),
                TotalPaid = paidInvoices.Sum(i => i.Total),
                TotalPartiallyPaid = partiallyPaidInvoices.Sum(i => i.Total),
                TotalCancelled = cancelledInvoices.Sum(i => i.Total),
                OutstandingBalance = outstandingInvoices.Sum(i => i.Total),
                InvoiceCount = activeInvoices.Count,
                CancelledInvoiceCount = cancelledInvoices.Count,
                CreatedDate = DateTime.UtcNow
            };

            _context.CustomerStatements.Add(statement);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (await GetCustomerStatementByIdAsync(statement.Id))!;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating customer statement for customer ID: {CustomerId}", createDTO.CustomerId);
            throw;
        }
    }

    public async Task<bool> DeleteCustomerStatementAsync(int id)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var statement = await _context.CustomerStatements.FirstOrDefaultAsync(s => s.Id == id);
            if (statement == null) return false;

            statement.IsDeleted = true;
            statement.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error deleting customer statement ID: {Id}", id);
            throw;
        }
    }

    public async Task<bool> SendCustomerStatementAsync(int statementId, List<string> recipientEmails)
    {
        try
        {
            var statement = await _context.CustomerStatements
                .FirstOrDefaultAsync(s => s.Id == statementId && !s.IsDeleted);

            if (statement == null) return false;

            var statementDTO = await GetCustomerStatementByIdAsync(statementId);
            if (statementDTO == null) return false;

            var pdfData = await _pdfService.GenerateCustomerStatementPdfAsync(statementId);

            var emailResult = await _emailService.SendCustomerStatementEmailAsync(statementDTO, pdfData, recipientEmails);

            if (!emailResult.Success)
            {
                _logger.LogError("Failed to send customer statement email for ID: {StatementId}. Error: {Error}",
                    statementId, emailResult.ErrorMessage);
                return false;
            }

            statement.IsSent = true;
            statement.SentDate = DateTime.UtcNow;
            statement.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending customer statement ID: {StatementId}", statementId);
            throw;
        }
    }

    public async Task<bool> MarkCustomerStatementAsDeliveredAsync(int statementId)
    {
        try
        {
            var statement = await _context.CustomerStatements
                .FirstOrDefaultAsync(s => s.Id == statementId && !s.IsDeleted);

            if (statement == null) return false;

            statement.IsDelivered = true;
            statement.DeliveredDate = DateTime.UtcNow;
            statement.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking customer statement ID: {StatementId} as delivered", statementId);
            throw;
        }
    }

    private async Task<string> GenerateStatementNumberAsync()
    {
        var currentYear = DateTime.UtcNow.Year;

        var allStatements = await _context.CustomerStatements
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync();

        var maxSequence = allStatements
            .Where(s => CustomerStatementNumberGenerator.GetYearFromStatementNumber(s.StatementNumber) == currentYear)
            .Select(s => CustomerStatementNumberGenerator.GetSequenceFromStatementNumber(s.StatementNumber))
            .DefaultIfEmpty(0)
            .Max();

        return CustomerStatementNumberGenerator.Generate(maxSequence + 1);
    }

    private async Task<CustomerStatementDTO> MapToDTO(CustomerStatement statement)
    {
        var allInvoices = await _context.Invoices
            .Where(i => !i.IsDeleted &&
                        i.CustomerId == statement.CustomerId &&
                        i.Status != InvoiceStatus.Draft &&
                        i.CreatedDate >= statement.StartDate &&
                        i.CreatedDate <= statement.EndDate)
            .OrderBy(i => i.CreatedDate)
            .ToListAsync();

        var invoiceIds = allInvoices.Select(i => i.Id).ToList();
        var jobAddresses = await _context.Jobs
            .IgnoreQueryFilters()
            .Where(j => j.InvoiceId.HasValue && invoiceIds.Contains(j.InvoiceId.Value))
            .Select(j => new { j.InvoiceId, j.Address })
            .ToListAsync();
        var addressByInvoiceId = jobAddresses
            .GroupBy(j => j.InvoiceId!.Value)
            .ToDictionary(g => g.Key, g => g.First().Address);

        var activeInvoices = allInvoices
            .Where(i => i.Status != InvoiceStatus.Cancelled)
            .Select(i => new CustomerStatementInvoiceDTO
            {
                InvoiceId = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.CreatedDate,
                Subtotal = i.Subtotal,
                VatAmount = i.VatAmount,
                Total = i.Total,
                Status = i.Status,
                PaymentMethod = i.PaymentMethod,
                PaymentReference = i.PaymentReference,
                JobAddress = addressByInvoiceId.TryGetValue(i.Id, out var addr) ? addr : null
            })
            .ToList();

        var cancelledInvoices = allInvoices
            .Where(i => i.Status == InvoiceStatus.Cancelled)
            .Select(i => new CustomerStatementInvoiceDTO
            {
                InvoiceId = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.CreatedDate,
                Subtotal = i.Subtotal,
                VatAmount = i.VatAmount,
                Total = i.Total,
                Status = i.Status,
                PaymentMethod = i.PaymentMethod,
                PaymentReference = i.PaymentReference,
                JobAddress = addressByInvoiceId.TryGetValue(i.Id, out var addr2) ? addr2 : null
            })
            .ToList();

        return new CustomerStatementDTO
        {
            Id = statement.Id,
            StatementNumber = statement.StatementNumber,
            CustomerId = statement.CustomerId,
            CustomerName = statement.Customer.Name,
            CustomerPhone = statement.Customer.Phone,
            CustomerEmails = statement.Customer.Emails.Select(e => e.Email).ToList(),
            CustomerVatNumber = statement.Customer.VatNumber,
            CustomerCompanyRegistrationNumber = statement.Customer.CompanyRegistrationNumber,
            StartDate = statement.StartDate,
            EndDate = statement.EndDate,
            TotalInvoiced = statement.TotalInvoiced,
            TotalVatAmount = statement.TotalVatAmount,
            TotalPaid = statement.TotalPaid,
            TotalPartiallyPaid = statement.TotalPartiallyPaid,
            TotalCancelled = statement.TotalCancelled,
            OutstandingBalance = statement.OutstandingBalance,
            InvoiceCount = statement.InvoiceCount,
            CancelledInvoiceCount = statement.CancelledInvoiceCount,
            CreatedDate = statement.CreatedDate,
            IsSent = statement.IsSent,
            SentDate = statement.SentDate,
            IsDelivered = statement.IsDelivered,
            DeliveredDate = statement.DeliveredDate,
            Invoices = activeInvoices,
            CancelledInvoices = cancelledInvoices
        };
    }
}
