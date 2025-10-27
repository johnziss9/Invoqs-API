using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Invoqs.API.Data;
using Invoqs.API.Models;
using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;
using Invoqs.API.Helpers;

namespace Invoqs.API.Services;

public class ReceiptService : IReceiptService
{
    private readonly InvoqsDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ReceiptService> _logger;
    private readonly IEmailService _emailService;
    private readonly IPdfService _pdfService;

    public ReceiptService(InvoqsDbContext context, IMapper mapper, ILogger<ReceiptService> logger, IEmailService emailService, IPdfService pdfService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _emailService = emailService;
        _pdfService = pdfService;
    }

    public async Task<IEnumerable<ReceiptDTO>> GetAllReceiptsAsync()
    {
        try
        {
            var receipts = await _context.Receipts
                .IgnoreQueryFilters()
                .Include(r => r.Customer)
                .Include(r => r.ReceiptInvoices)
                    .ThenInclude(ri => ri.Invoice)
                .Where(r => !r.IsDeleted)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            var receiptDTOs = _mapper.Map<List<ReceiptDTO>>(receipts);

            return receiptDTOs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all receipts");
            throw;
        }
    }

    public async Task<ReceiptDTO?> GetReceiptByIdAsync(int id)
    {
        try
        {
            var receipt = await _context.Receipts
                .IgnoreQueryFilters()
                .Include(r => r.Customer)
                .Include(r => r.ReceiptInvoices)
                    .ThenInclude(ri => ri.Invoice)
                .Where(r => !r.IsDeleted)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

            if (receipt == null)
            {
                return null;
            }

            var receiptDTO = _mapper.Map<ReceiptDTO>(receipt);

            return receiptDTO;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving receipt with ID: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<ReceiptDTO>> GetReceiptsByCustomerIdAsync(int customerId)
    {
        try
        {
            var receipts = await _context.Receipts
                .Include(r => r.Customer)
                .Include(r => r.ReceiptInvoices)
                    .ThenInclude(ri => ri.Invoice)
                .Where(r => r.CustomerId == customerId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            var receiptDTOs = _mapper.Map<List<ReceiptDTO>>(receipts);

            return receiptDTOs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving receipts for customer ID: {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<ReceiptDTO> CreateReceiptAsync(CreateReceiptDTO createDTO)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Verify customer exists
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == createDTO.CustomerId);
            if (customer == null)
            {
                throw new InvalidOperationException($"Customer with ID {createDTO.CustomerId} does not exist");
            }

            // Verify all invoices exist, are paid, and belong to the customer
            var invoices = await _context.Invoices
                .Where(i => createDTO.InvoiceIds.Contains(i.Id))
                .ToListAsync();

            if (invoices.Count != createDTO.InvoiceIds.Count)
            {
                throw new InvalidOperationException("One or more invoices not found");
            }

            var invalidInvoices = invoices.Where(i =>
                i.CustomerId != createDTO.CustomerId ||
                i.Status != InvoiceStatus.Paid).ToList();

            if (invalidInvoices.Any())
            {
                throw new InvalidOperationException("All invoices must be paid and belong to the specified customer");
            }

            // Create receipt
            var receipt = new Receipt
            {
                ReceiptNumber = await GenerateReceiptNumberAsync(),
                CustomerId = createDTO.CustomerId,
                CreatedDate = DateTime.UtcNow
            };

            // Add receipt first to get the ID
            _context.Receipts.Add(receipt);
            await _context.SaveChangesAsync(); 

            // Calculate total and create receipt invoices
            decimal totalAmount = 0;
            foreach (var invoice in invoices)
            {
                var receiptInvoice = new ReceiptInvoice
                {
                    ReceiptId = receipt.Id,
                    InvoiceId = invoice.Id,
                    AllocatedAmount = invoice.Total,
                    CreatedDate = DateTime.UtcNow
                };

                _context.ReceiptInvoices.Add(receiptInvoice);
                totalAmount += invoice.Total;
            }

            receipt.TotalAmount = totalAmount;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Load the created receipt with all includes
            var createdReceipt = await GetReceiptByIdAsync(receipt.Id);

            return createdReceipt!;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating receipt for customer {CustomerId}", createDTO.CustomerId);
            throw;
        }
    }

    public async Task<bool> DeleteReceiptAsync(int id)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var receipt = await _context.Receipts
                .Include(r => r.ReceiptInvoices)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receipt == null)
            {
                return false;
            }

            // Soft delete receipt
            receipt.IsDeleted = true;
            receipt.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error soft deleting receipt ID: {Id}", id);
            throw;
        }
    }

    public async Task<bool> SendReceiptAsync(int receiptId)
    {
        try
        {
            var receipt = await _context.Receipts
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.Id == receiptId && !r.IsDeleted);

            if (receipt == null)
            {
                return false;
            }

            // Fetch receipt DTO
            var receiptDTO = await GetReceiptByIdAsync(receiptId);
            if (receiptDTO == null)
            {
                return false;
            }

            // Generate PDF
            var pdfData = await _pdfService.GenerateReceiptPdfAsync(receiptId);

            // Send email BEFORE updating database
            _logger.LogInformation("Attempting to send receipt email for Receipt ID: {ReceiptId}", receiptId);
            var emailResult = await _emailService.SendReceiptEmailAsync(receiptDTO, pdfData);

            if (!emailResult.Success)
            {
                _logger.LogError("Failed to send receipt email for Receipt ID: {ReceiptId}. Error: {Error}",
                    receiptId, emailResult.ErrorMessage);
                return false;
            }

            _logger.LogInformation("Receipt email sent successfully for Receipt ID: {ReceiptId}, MessageId: {MessageId}",
                receiptId, emailResult.MessageId);

            // Update receipt status
            receipt.IsSent = true;
            receipt.SentDate = DateTime.UtcNow;
            receipt.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Receipt {ReceiptNumber} marked as sent to {CustomerEmail}",
                receipt.ReceiptNumber, receipt.Customer.Email);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending receipt ID: {ReceiptId}", receiptId);
            throw;
        }
    }

    private async Task<string> GenerateReceiptNumberAsync()
    {
        var currentYear = DateTime.UtcNow.Year;

        // Get the maximum sequence number for this year using a direct database query
        var receiptsThisYear = await _context.Receipts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync(); // Get all, then filter in memory to avoid EF translation issues

        // Filter to current year and find max sequence
        var maxSequence = receiptsThisYear
            .Where(r => ReceiptNumberGenerator.GetYearFromReceiptNumber(r.ReceiptNumber) == currentYear)
            .Select(r => ReceiptNumberGenerator.GetSequenceFromReceiptNumber(r.ReceiptNumber))
            .DefaultIfEmpty(0)
            .Max();

        var nextSequence = maxSequence + 1;
        var newReceiptNumber = ReceiptNumberGenerator.Generate(nextSequence);

        return newReceiptNumber;
    }
}