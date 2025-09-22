using FluentValidation;
using Invoqs.API.DTOs;
using Invoqs.API.Data;
using Invoqs.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Invoqs.API.Validators;

/// <summary>
/// Validation rules for creating new jobs
/// </summary>
public class CreateJobValidator : AbstractValidator<CreateJobDTO>
{
    private readonly InvoqsDbContext _context;

    public CreateJobValidator(InvoqsDbContext context)
    {
        _context = context;

        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer is required")
            .MustAsync(BeValidCustomer).WithMessage("Selected customer does not exist");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Job title is required")
            .Length(3, 200).WithMessage("Job title must be between 3 and 200 characters")
            .Matches(@"^[a-zA-Z0-9\s\-\.\,\&\'\(\)\/]+$")
            .WithMessage("Job title contains invalid characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Service address is required")
            .Length(10, 500).WithMessage("Address must be between 10 and 500 characters");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Please select a valid job type");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Please select a valid job status");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than £0")
            .LessThanOrEqualTo(999999.99m).WithMessage("Price cannot exceed £999,999.99")
            .PrecisionScale(8, 2, false).WithMessage("Price cannot have more than 2 decimal places");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required")
            .GreaterThanOrEqualTo(DateTime.Today.AddDays(-30))
            .WithMessage("Start date cannot be more than 30 days in the past");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be on or after the start date")
            .When(x => x.EndDate.HasValue);

        // Business rule validations
        RuleFor(x => x)
            .Must(HaveEndDateWhenCompleted)
            .WithMessage("End date is required when job status is Completed")
            .WithName("EndDate");
    }

    private async Task<bool> BeValidCustomer(int customerId, CancellationToken cancellationToken)
    {
        return await _context.Customers
            .AnyAsync(c => c.Id == customerId, cancellationToken);
    }

    private bool HaveEndDateWhenCompleted(CreateJobDTO job)
    {
        if (job.Status == JobStatus.Completed)
        {
            return job.EndDate.HasValue;
        }
        return true;
    }
}

/// <summary>
/// Validation rules for updating existing jobs
/// </summary>
public class UpdateJobValidator : AbstractValidator<UpdateJobDTO>
{
    private readonly InvoqsDbContext _context;
    private int _jobId;

    public UpdateJobValidator(InvoqsDbContext context)
    {
        _context = context;

        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer is required")
            .MustAsync(BeValidCustomer).WithMessage("Selected customer does not exist");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Job title is required")
            .Length(3, 200).WithMessage("Job title must be between 3 and 200 characters")
            .Matches(@"^[a-zA-Z0-9\s\-\.\,\&\'\(\)\/]+$")
            .WithMessage("Job title contains invalid characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Service address is required")
            .Length(10, 500).WithMessage("Address must be between 10 and 500 characters");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Please select a valid job type");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Please select a valid job status")
            .MustAsync(BeValidStatusTransition).WithMessage("Invalid status transition for this job");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than £0")
            .LessThanOrEqualTo(999999.99m).WithMessage("Price cannot exceed £999,999.99")
            .PrecisionScale(8, 2, false).WithMessage("Price cannot have more than 2 decimal places")
            .MustAsync(BeEditableIfInvoiced).WithMessage("Cannot change price of invoiced job");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required")
            .GreaterThanOrEqualTo(DateTime.Today.AddDays(-30))
            .WithMessage("Start date cannot be more than 30 days in the past");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be on or after the start date")
            .When(x => x.EndDate.HasValue);

        // Business rule validations
        RuleFor(x => x)
            .Must(HaveEndDateWhenCompleted)
            .WithMessage("End date is required when job status is Completed")
            .WithName("EndDate");

        RuleFor(x => x)
            .MustAsync(NotBeInvoicedWhenChangingCriticalFields)
            .WithMessage("Cannot modify invoiced job details")
            .WithName("Job");
    }

    public void SetJobIdForUpdate(int jobId)
    {
        _jobId = jobId;
    }

    private async Task<bool> BeValidCustomer(int customerId, CancellationToken cancellationToken)
    {
        return await _context.Customers
            .AnyAsync(c => c.Id == customerId, cancellationToken);
    }

    private async Task<bool> BeValidStatusTransition(JobStatus newStatus, CancellationToken cancellationToken)
    {
        var currentJob = await _context.Jobs
            .FirstOrDefaultAsync(j => j.Id == _jobId, cancellationToken);

        if (currentJob == null) return false;

        // Define valid status transitions
        return currentJob.Status switch
        {
            JobStatus.New => newStatus is JobStatus.New or JobStatus.Active or JobStatus.Cancelled,
            JobStatus.Active => newStatus is JobStatus.Active or JobStatus.Completed or JobStatus.Cancelled,
            JobStatus.Completed => newStatus is JobStatus.Completed or JobStatus.Active, // Allow reopening
            JobStatus.Cancelled => newStatus is JobStatus.Cancelled or JobStatus.New, // Allow reactivation
            _ => false
        };
    }

    private async Task<bool> BeEditableIfInvoiced(decimal price, CancellationToken cancellationToken)
    {
        var currentJob = await _context.Jobs
            .FirstOrDefaultAsync(j => j.Id == _jobId, cancellationToken);

        if (currentJob == null || !currentJob.IsInvoiced) return true;

        // If job is invoiced, price cannot be changed
        return currentJob.Price == price;
    }

    private bool HaveEndDateWhenCompleted(UpdateJobDTO job)
    {
        if (job.Status == JobStatus.Completed)
        {
            return job.EndDate.HasValue;
        }
        return true;
    }

    private async Task<bool> NotBeInvoicedWhenChangingCriticalFields(UpdateJobDTO jobDTO, CancellationToken cancellationToken)
    {
        var currentJob = await _context.Jobs
            .FirstOrDefaultAsync(j => j.Id == _jobId, cancellationToken);

        if (currentJob == null || !currentJob.IsInvoiced) return true;

        // If job is invoiced, only allow status and date changes
        return currentJob.Title == jobDTO.Title &&
               currentJob.Description == jobDTO.Description &&
               currentJob.Address == jobDTO.Address &&
               currentJob.Type == jobDTO.Type &&
               currentJob.Price == jobDTO.Price;
    }
}

/// <summary>
/// Validation rules for updating job status only
/// </summary>
public class UpdateJobStatusValidator : AbstractValidator<UpdateJobStatusDTO>
{
    public UpdateJobStatusValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Please select a valid job status");

        RuleFor(x => x.EndDate)
            .NotNull().WithMessage("End date is required when marking job as completed")
            .When(x => x.Status == JobStatus.Completed);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(DateTime.Today.AddDays(-7))
            .WithMessage("End date cannot be more than 7 days in the past")
            .When(x => x.EndDate.HasValue);
    }
}

/// <summary>
/// Validation rules for marking jobs as invoiced
/// </summary>
public class MarkJobsAsInvoicedValidator : AbstractValidator<MarkJobsAsInvoicedDTO>
{
    private readonly InvoqsDbContext _context;

    public MarkJobsAsInvoicedValidator(InvoqsDbContext context)
    {
        _context = context;

        RuleFor(x => x.JobIds)
            .NotEmpty().WithMessage("At least one job must be selected")
            .Must(x => x.Count <= 50).WithMessage("Cannot process more than 50 jobs at once")
            .MustAsync(BeValidJobs).WithMessage("One or more selected jobs do not exist");

        RuleFor(x => x.InvoiceId)
            .GreaterThan(0).WithMessage("Valid invoice is required")
            .MustAsync(BeValidInvoice).WithMessage("Selected invoice does not exist");

        RuleFor(x => x.JobIds)
            .MustAsync(BeCompletedAndUninvoiced).WithMessage("All jobs must be completed and not already invoiced");
    }

    private async Task<bool> BeValidJobs(List<int> jobIds, CancellationToken cancellationToken)
    {
        var existingCount = await _context.Jobs
            .CountAsync(j => jobIds.Contains(j.Id), cancellationToken);
        return existingCount == jobIds.Count;
    }

    private async Task<bool> BeValidInvoice(int invoiceId, CancellationToken cancellationToken)
    {
        return await _context.Invoices
            .AnyAsync(i => i.Id == invoiceId, cancellationToken);
    }

    private async Task<bool> BeCompletedAndUninvoiced(List<int> jobIds, CancellationToken cancellationToken)
    {
        var invalidJobs = await _context.Jobs
            .Where(j => jobIds.Contains(j.Id))
            .Where(j => j.Status != JobStatus.Completed || j.IsInvoiced)
            .CountAsync(cancellationToken);

        return invalidJobs == 0;
    }
}

/// <summary>
/// Validation rules for removing jobs from invoice
/// </summary>
public class RemoveJobsFromInvoiceValidator : AbstractValidator<RemoveJobsFromInvoiceDTO>
{
    private readonly InvoqsDbContext _context;

    public RemoveJobsFromInvoiceValidator(InvoqsDbContext context)
    {
        _context = context;

        RuleFor(x => x.JobIds)
            .NotEmpty().WithMessage("At least one job must be selected")
            .Must(x => x.Count <= 50).WithMessage("Cannot process more than 50 jobs at once")
            .MustAsync(BeValidInvoicedJobs).WithMessage("One or more jobs are not invoiced or do not exist");
    }

    private async Task<bool> BeValidInvoicedJobs(List<int> jobIds, CancellationToken cancellationToken)
    {
        var validCount = await _context.Jobs
            .CountAsync(j => jobIds.Contains(j.Id) && j.IsInvoiced, cancellationToken);
        return validCount == jobIds.Count;
    }
}