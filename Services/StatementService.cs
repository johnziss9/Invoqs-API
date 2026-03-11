using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Invoqs.API.Data;
using Invoqs.API.Models;
using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;
using Invoqs.API.Helpers;

namespace Invoqs.API.Services;

public class StatementService : IStatementService
{
    private readonly InvoqsDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<StatementService> _logger;
    private readonly IEmailService _emailService;
    private readonly IPdfService _pdfService;

    public StatementService(
        InvoqsDbContext context,
        IMapper mapper,
        ILogger<StatementService> logger,
        IEmailService emailService,
        IPdfService pdfService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _emailService = emailService;
        _pdfService = pdfService;
    }

    public async Task<IEnumerable<StatementDTO>> GetAllStatementsAsync()
    {
        try
        {
            var statements = await _context.Statements
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();

            var statementDTOs = new List<StatementDTO>();

            foreach (var statement in statements)
            {
                var dto = await MapStatementToDTO(statement);
                statementDTOs.Add(dto);
            }

            return statementDTOs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all statements");
            throw;
        }
    }

    public async Task<StatementDTO?> GetStatementByIdAsync(int id)
    {
        try
        {
            var statement = await _context.Statements
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (statement == null)
            {
                return null;
            }

            return await MapStatementToDTO(statement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statement with ID: {Id}", id);
            throw;
        }
    }

    public async Task<StatementDTO> CreateStatementAsync(CreateStatementDTO createDTO)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Validate date range
            if (createDTO.EndDate < createDTO.StartDate)
            {
                throw new InvalidOperationException("End date must be after or equal to start date");
            }

            // Get all invoices in the date range (based on invoice created date)
            var startDateUtc = createDTO.StartDate.ToUniversalTime();
            var endDateUtc = createDTO.EndDate.Date.AddDays(1).AddTicks(-1).ToUniversalTime();

            var allInvoices = await _context.Invoices
                .Include(i => i.Customer)
                .Where(i => !i.IsDeleted &&
                           i.CreatedDate >= startDateUtc &&
                           i.CreatedDate <= endDateUtc)
                .OrderBy(i => i.CreatedDate)
                .ToListAsync();

            if (!allInvoices.Any())
            {
                throw new InvalidOperationException("No invoices found in the specified date range");
            }

            // Separate active and cancelled invoices
            var activeInvoices = allInvoices.Where(i => i.Status != InvoiceStatus.Cancelled).ToList();
            var cancelledInvoices = allInvoices.Where(i => i.Status == InvoiceStatus.Cancelled).ToList();

            // Calculate totals
            var totalAmount = activeInvoices.Sum(i => i.Total);
            var totalVatAmount = activeInvoices.Sum(i => i.VatAmount);
            var cancelledAmount = cancelledInvoices.Sum(i => i.Total);
            var cancelledVatAmount = cancelledInvoices.Sum(i => i.VatAmount);

            // Create statement
            var statement = new Statement
            {
                StatementNumber = await GenerateStatementNumberAsync(),
                StartDate = startDateUtc,
                EndDate = endDateUtc,
                TotalAmount = totalAmount,
                TotalVatAmount = totalVatAmount,
                CancelledAmount = cancelledAmount,
                CancelledVatAmount = cancelledVatAmount,
                InvoiceCount = activeInvoices.Count,
                CancelledInvoiceCount = cancelledInvoices.Count,
                CreatedDate = DateTime.UtcNow
            };

            _context.Statements.Add(statement);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Load the created statement
            var createdStatement = await GetStatementByIdAsync(statement.Id);

            return createdStatement!;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating statement for date range {StartDate} to {EndDate}",
                createDTO.StartDate, createDTO.EndDate);
            throw;
        }
    }

    public async Task<bool> DeleteStatementAsync(int id)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var statement = await _context.Statements
                .FirstOrDefaultAsync(s => s.Id == id);

            if (statement == null)
            {
                return false;
            }

            // Soft delete statement
            statement.IsDeleted = true;
            statement.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error soft deleting statement ID: {Id}", id);
            throw;
        }
    }

    public async Task<bool> SendStatementAsync(int statementId, List<string> recipientEmails)
    {
        try
        {
            var statement = await _context.Statements
                .FirstOrDefaultAsync(s => s.Id == statementId && !s.IsDeleted);

            if (statement == null)
            {
                return false;
            }

            // Fetch statement DTO
            var statementDTO = await GetStatementByIdAsync(statementId);
            if (statementDTO == null)
            {
                return false;
            }

            // Generate PDF
            var pdfData = await _pdfService.GenerateStatementPdfAsync(statementId);

            // Send email BEFORE updating database
            _logger.LogInformation("Attempting to send statement email for Statement ID: {StatementId}", statementId);
            var emailResult = await _emailService.SendStatementEmailAsync(statementDTO, pdfData, recipientEmails);

            if (!emailResult.Success)
            {
                _logger.LogError("Failed to send statement email for Statement ID: {StatementId}. Error: {Error}",
                    statementId, emailResult.ErrorMessage);
                return false;
            }

            _logger.LogInformation("Statement email sent successfully for Statement ID: {StatementId}, MessageId: {MessageId}",
                statementId, emailResult.MessageId);

            // Update statement status
            statement.IsSent = true;
            statement.SentDate = DateTime.UtcNow;
            statement.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Statement {StatementNumber} marked as sent",
                statement.StatementNumber);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending statement ID: {StatementId}", statementId);
            throw;
        }
    }

    public async Task<bool> MarkStatementAsDeliveredAsync(int statementId)
    {
        try
        {
            var statement = await _context.Statements
                .FirstOrDefaultAsync(s => s.Id == statementId && !s.IsDeleted);

            if (statement == null)
            {
                return false;
            }

            // Update statement status - mark as delivered (manually handed over)
            statement.IsDelivered = true;
            statement.DeliveredDate = DateTime.UtcNow;
            statement.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Statement {StatementNumber} manually marked as delivered", statement.StatementNumber);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking statement ID: {StatementId} as delivered", statementId);
            throw;
        }
    }

    private async Task<string> GenerateStatementNumberAsync()
    {
        var currentYear = DateTime.UtcNow.Year;

        var statementsThisYear = await _context.Statements
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync();

        var maxSequence = statementsThisYear
            .Where(s => StatementNumberGenerator.GetYearFromStatementNumber(s.StatementNumber) == currentYear)
            .Select(s => StatementNumberGenerator.GetSequenceFromStatementNumber(s.StatementNumber))
            .DefaultIfEmpty(0)
            .Max();

        var nextSequence = maxSequence + 1;
        var newStatementNumber = StatementNumberGenerator.Generate(nextSequence);

        return newStatementNumber;
    }

    private async Task<StatementDTO> MapStatementToDTO(Statement statement)
    {
        // Get all invoices in the date range
        var allInvoices = await _context.Invoices
            .Include(i => i.Customer)
            .Where(i => !i.IsDeleted &&
                       i.CreatedDate >= statement.StartDate &&
                       i.CreatedDate <= statement.EndDate)
            .OrderBy(i => i.CreatedDate)
            .ToListAsync();

        // Separate active and cancelled invoices
        var activeInvoices = allInvoices
            .Where(i => i.Status != InvoiceStatus.Cancelled)
            .Select(i => new StatementInvoiceDTO
            {
                InvoiceId = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.CreatedDate,
                CustomerName = i.Customer.Name,
                Total = i.Total,
                VatAmount = i.VatAmount,
                Status = i.Status
            })
            .ToList();

        var cancelledInvoices = allInvoices
            .Where(i => i.Status == InvoiceStatus.Cancelled)
            .Select(i => new StatementInvoiceDTO
            {
                InvoiceId = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.CreatedDate,
                CustomerName = i.Customer.Name,
                Total = i.Total,
                VatAmount = i.VatAmount,
                Status = i.Status
            })
            .ToList();

        return new StatementDTO
        {
            Id = statement.Id,
            StatementNumber = statement.StatementNumber,
            StartDate = statement.StartDate,
            EndDate = statement.EndDate,
            TotalAmount = statement.TotalAmount,
            TotalVatAmount = statement.TotalVatAmount,
            CancelledAmount = statement.CancelledAmount,
            CancelledVatAmount = statement.CancelledVatAmount,
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
