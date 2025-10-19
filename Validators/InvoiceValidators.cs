using FluentValidation;
using Invoqs.API.DTOs;
using Invoqs.API.Data;
using Invoqs.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Invoqs.API.Validators;

/// <summary>
/// Validation rules for creating new invoices
/// </summary>
public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceDTO>
{
    private readonly InvoqsDbContext _context;

    public CreateInvoiceValidator(InvoqsDbContext context)
    {
        _context = context;

        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer is required")
            .MustAsync(BeValidCustomer).WithMessage("Selected customer does not exist");

        RuleFor(x => x.JobIds)
            .NotEmpty().WithMessage("At least one job must be selected")
            .Must(x => x.Count <= 50).WithMessage("Cannot include more than 50 jobs in one invoice")
            .MustAsync(BeValidJobs).WithMessage("One or more selected jobs do not exist")
            .MustAsync(BeCompletedJobs).WithMessage("All jobs must be completed before invoicing")
            .MustAsync(BeUninvoicedJobs).WithMessage("One or more jobs are already invoiced")
            .MustAsync(BelongToCustomer).WithMessage("All jobs must belong to the selected customer");

        RuleFor(x => x.VatRate)
            .InclusiveBetween(0m, 1m).WithMessage("VAT rate must be between 0% and 100%")
            .PrecisionScale(6, 4, false).WithMessage("VAT rate cannot have more than 4 decimal places");

        RuleFor(x => x.PaymentTermsDays)
            .InclusiveBetween(1, 365).WithMessage("Payment terms must be between 1 and 365 days");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }

    private async Task<bool> BeValidCustomer(int customerId, CancellationToken cancellationToken)
    {
        return await _context.Customers
            .AnyAsync(c => c.Id == customerId, cancellationToken);
    }

    private async Task<bool> BeValidJobs(List<int> jobIds, CancellationToken cancellationToken)
    {
        var existingCount = await _context.Jobs
            .CountAsync(j => jobIds.Contains(j.Id), cancellationToken);
        return existingCount == jobIds.Count;
    }

    private async Task<bool> BeCompletedJobs(List<int> jobIds, CancellationToken cancellationToken)
    {
        var completedCount = await _context.Jobs
            .Where(j => jobIds.Contains(j.Id))
            .CountAsync(j => j.Status == JobStatus.Completed, cancellationToken);
        return completedCount == jobIds.Count;
    }

    private async Task<bool> BeUninvoicedJobs(List<int> jobIds, CancellationToken cancellationToken)
    {
        var uninvoicedCount = await _context.Jobs
            .Where(j => jobIds.Contains(j.Id))
            .CountAsync(j => j.InvoiceId == null, cancellationToken);
        return uninvoicedCount == jobIds.Count;
    }

    private async Task<bool> BelongToCustomer(CreateInvoiceDTO dto, List<int> jobIds, CancellationToken cancellationToken)
    {
        var customerJobCount = await _context.Jobs
            .Where(j => jobIds.Contains(j.Id))
            .CountAsync(j => j.CustomerId == dto.CustomerId, cancellationToken);
        return customerJobCount == jobIds.Count;
    }
}

/// <summary>
/// Validation rules for updating existing invoices (draft only)
/// </summary>
public class UpdateInvoiceValidator : AbstractValidator<UpdateInvoiceDTO>
{
    private readonly InvoqsDbContext _context;
    private int _invoiceId;

    public UpdateInvoiceValidator(InvoqsDbContext context)
    {
        _context = context;

        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer is required")
            .MustAsync(BeValidCustomer).WithMessage("Selected customer does not exist")
            .MustAsync(NotChangeCustomerIfSent).WithMessage("Cannot change customer after invoice is sent");

        RuleFor(x => x.JobIds)
            .NotEmpty().WithMessage("At least one job must be selected")
            .Must(x => x.Count <= 50).WithMessage("Cannot include more than 50 jobs in one invoice")
            .MustAsync(BeValidJobs).WithMessage("One or more selected jobs do not exist")
            .MustAsync(BeCompletedJobs).WithMessage("All jobs must be completed before invoicing")
            .MustAsync(BeAvailableJobs).WithMessage("One or more jobs are invoiced elsewhere")
            .MustAsync(BelongToCustomer).WithMessage("All jobs must belong to the selected customer");

        RuleFor(x => x.VatRate)
            .InclusiveBetween(0m, 1m).WithMessage("VAT rate must be between 0% and 100%")
            .PrecisionScale(6, 4, false).WithMessage("VAT rate cannot have more than 4 decimal places");

        RuleFor(x => x.PaymentTermsDays)
            .InclusiveBetween(1, 365).WithMessage("Payment terms must be between 1 and 365 days");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));

        // Business rule: Only draft invoices can be updated
        RuleFor(x => x)
            .MustAsync(BeInDraftStatus)
            .WithMessage("Only draft invoices can be updated")
            .WithName("Invoice");
    }

    public void SetInvoiceIdForUpdate(int invoiceId)
    {
        _invoiceId = invoiceId;
    }

    private async Task<bool> BeValidCustomer(int customerId, CancellationToken cancellationToken)
    {
        return await _context.Customers
            .AnyAsync(c => c.Id == customerId, cancellationToken);
    }

    private async Task<bool> NotChangeCustomerIfSent(int customerId, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == _invoiceId, cancellationToken);

        if (invoice == null) return true;

        // If invoice is not draft, customer cannot be changed
        if (invoice.Status != InvoiceStatus.Draft)
        {
            return invoice.CustomerId == customerId;
        }

        return true;
    }

    private async Task<bool> BeValidJobs(List<int> jobIds, CancellationToken cancellationToken)
    {
        var existingCount = await _context.Jobs
            .CountAsync(j => jobIds.Contains(j.Id), cancellationToken);
        return existingCount == jobIds.Count;
    }

    private async Task<bool> BeCompletedJobs(List<int> jobIds, CancellationToken cancellationToken)
    {
        var completedCount = await _context.Jobs
            .Where(j => jobIds.Contains(j.Id))
            .CountAsync(j => j.Status == JobStatus.Completed, cancellationToken);
        return completedCount == jobIds.Count;
    }

    private async Task<bool> BeAvailableJobs(List<int> jobIds, CancellationToken cancellationToken)
    {
        // Jobs can be uninvoiced OR invoiced by the current invoice being updated
        var availableCount = await _context.Jobs
            .Where(j => jobIds.Contains(j.Id))
            .CountAsync(j => !j.IsInvoiced || j.InvoiceId == _invoiceId, cancellationToken);
        return availableCount == jobIds.Count;
    }

    private async Task<bool> BelongToCustomer(UpdateInvoiceDTO dto, List<int> jobIds, CancellationToken cancellationToken)
    {
        var customerJobCount = await _context.Jobs
            .Where(j => jobIds.Contains(j.Id))
            .CountAsync(j => j.CustomerId == dto.CustomerId, cancellationToken);
        return customerJobCount == jobIds.Count;
    }

    private async Task<bool> BeInDraftStatus(UpdateInvoiceDTO dto, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == _invoiceId, cancellationToken);
        return invoice?.Status == InvoiceStatus.Draft;
    }
}

/// <summary>
/// Validation rules for marking invoice as sent
/// </summary>
public class MarkInvoiceAsSentValidator : AbstractValidator<MarkInvoiceAsSentDTO>
{
    private readonly InvoqsDbContext _context;
    private int _invoiceId;

    public MarkInvoiceAsSentValidator(InvoqsDbContext context)
    {
        _context = context;

        RuleFor(x => x.SentDate)
            .NotNull().WithMessage("Sent date is required")
            .Must(sentDate =>
            {
                var result = sentDate <= DateTime.UtcNow.Date;
                return result;
            })
            .WithMessage("Sent date cannot be in the future")
            .Must(sentDate =>
            {
                var thirtyDaysAgo = DateTime.UtcNow.Date.AddDays(-30);
                var result = sentDate >= thirtyDaysAgo;

                return result;
            })
            .WithMessage("Sent date cannot be more than 30 days in the past");

        RuleFor(x => x)
            .MustAsync(BeInDraftStatus)
            .WithMessage("Only draft invoices can be marked as sent")
            .WithName("Invoice");
    }

    public void SetInvoiceIdForSent(int invoiceId)
    {
        _invoiceId = invoiceId;
    }

    private async Task<bool> BeInDraftStatus(MarkInvoiceAsSentDTO dto, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == _invoiceId, cancellationToken);
        return invoice?.Status == InvoiceStatus.Draft;
    }
}

/// <summary>
/// Validation rules for marking invoice as paid
/// </summary>
public class MarkInvoiceAsPaidValidator : AbstractValidator<MarkInvoiceAsPaidDTO>
{
    private readonly InvoqsDbContext _context;
    private int _invoiceId;

    public MarkInvoiceAsPaidValidator(InvoqsDbContext context)
    {
        _context = context;

        RuleFor(x => x.PaymentDate)
            .NotNull().WithMessage("Payment date is required")
            .Must(paymentDate =>
            {
                var result = paymentDate <= DateTime.UtcNow.Date;

                return result;
            })
            .WithMessage("Payment date cannot be in the future")
            .Must(paymentDate =>
            {
                var oneYearAgo = DateTime.UtcNow.Date.AddYears(-1);
                var result = paymentDate >= oneYearAgo;

                return result;
            })
            .WithMessage("Payment date cannot be more than 1 year in the past");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Payment method is required")
            .MaximumLength(50).WithMessage("Payment method cannot exceed 50 characters")
            .Must(BeValidPaymentMethod).WithMessage("Please select a valid payment method");

        RuleFor(x => x.PaymentReference)
            .MaximumLength(100).WithMessage("Payment reference cannot exceed 100 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.PaymentReference));

        RuleFor(x => x)
            .MustAsync(BeInSentOrOverdueStatus)
            .WithMessage("Only sent or overdue invoices can be marked as paid")
            .WithName("Invoice");

        RuleFor(x => x.PaymentDate)
            .MustAsync(BeAfterSentDate)
            .WithMessage("Payment date cannot be before the invoice was sent");
    }

    public void SetInvoiceIdForPayment(int invoiceId)
    {
        _invoiceId = invoiceId;
    }

    private bool BeValidPaymentMethod(string paymentMethod)
    {
        var validMethods = new[]
        {
            "Bank Transfer", "Credit Card", "Debit Card", "Cash", "Cheque",
            "PayPal", "Stripe", "Direct Debit", "BACS", "Other"
        };
        return validMethods.Contains(paymentMethod, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<bool> BeInSentOrOverdueStatus(MarkInvoiceAsPaidDTO dto, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == _invoiceId, cancellationToken);
        return invoice?.Status is InvoiceStatus.Sent or InvoiceStatus.Overdue;
    }

    private async Task<bool> BeAfterSentDate(DateTime paymentDate, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == _invoiceId, cancellationToken);

        if (invoice?.SentDate == null) return true;

        return paymentDate >= invoice.SentDate.Value.Date;
    }
}