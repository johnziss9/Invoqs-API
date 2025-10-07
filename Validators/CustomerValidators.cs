using FluentValidation;
using Invoqs.API.DTOs;
using Invoqs.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Invoqs.API.Validators;

/// <summary>
/// Validation rules for creating new customers
/// </summary>
public class CreateCustomerValidator : AbstractValidator<CreateCustomerDTO>
{
    private readonly InvoqsDbContext _context;

    public CreateCustomerValidator(InvoqsDbContext context)
    {
        _context = context;

        // Customer name validation
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Customer name is required")
            .Length(2, 200).WithMessage("Customer name must be between 2 and 200 characters")
            .Matches(@"^[a-zA-Z0-9\s\-\.\,\&\']+$")
            .WithMessage("Customer name contains invalid characters. Only letters, numbers, spaces, hyphens, periods, commas, ampersands, and apostrophes are allowed");

        // Email validation with uniqueness check
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required")
            .EmailAddress().WithMessage("Please enter a valid email address")
            .MaximumLength(255).WithMessage("Email address cannot exceed 255 characters")
            .MustAsync(BeUniqueEmail).WithMessage("This email address is already registered");

        // Phone number validation
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required")
            .Length(6, 20).WithMessage("Phone number must be between 6 and 20 characters")
            .Matches(@"^[\+]?[\d\s\-\(\)]+$")
            .WithMessage("Please enter a valid phone number. Only numbers, spaces, hyphens, parentheses, and + are allowed");

        // Company registration number (optional)
        RuleFor(x => x.CompanyRegistrationNumber)
            .MaximumLength(50).WithMessage("Company registration number cannot exceed 50 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.CompanyRegistrationNumber));

        // VAT number validation (optional, UK format)
        RuleFor(x => x.VatNumber)
            .MaximumLength(20).WithMessage("VAT number cannot exceed 20 characters")
            .Matches(@"^[A-Z]{2}\d{8,12}$")
            .WithMessage("VAT number format is invalid. Expected format: GB123456789 (2 letters followed by 8-12 digits)")
            .When(x => !string.IsNullOrWhiteSpace(x.VatNumber));

        // Notes validation (optional)
        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }

    /// <summary>
    /// Checks if the email address is unique across all customers
    /// </summary>
    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return true; // Let the NotEmpty validator handle empty emails

        return !await _context.Customers
            .AnyAsync(c => c.Email.ToLower() == email.ToLower(), cancellationToken);
    }
}

/// <summary>
/// Validation rules for updating existing customers
/// </summary>
public class UpdateCustomerValidator : AbstractValidator<UpdateCustomerDTO>
{
    private readonly InvoqsDbContext _context;
    private int _customerId;

    public UpdateCustomerValidator(InvoqsDbContext context)
    {
        _context = context;

        // Customer name validation
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Customer name is required")
            .Length(2, 200).WithMessage("Customer name must be between 2 and 200 characters")
            .Matches(@"^[a-zA-Z0-9\s\-\.\,\&\']+$")
            .WithMessage("Customer name contains invalid characters. Only letters, numbers, spaces, hyphens, periods, commas, ampersands, and apostrophes are allowed");

        // Email validation with uniqueness check (excluding current customer)
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required")
            .EmailAddress().WithMessage("Please enter a valid email address")
            .MaximumLength(255).WithMessage("Email address cannot exceed 255 characters")
            .MustAsync(BeUniqueEmailForUpdate).WithMessage("This email address is already registered to another customer");

        // Phone number validation
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required")
            .Length(6, 20).WithMessage("Phone number must be between 6 and 20 characters")
            .Matches(@"^[\+]?[\d\s\-\(\)]+$")
            .WithMessage("Please enter a valid phone number. Only numbers, spaces, hyphens, parentheses, and + are allowed");

        // Company registration number (optional)
        RuleFor(x => x.CompanyRegistrationNumber)
            .MaximumLength(50).WithMessage("Company registration number cannot exceed 50 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.CompanyRegistrationNumber));

        // VAT number validation (optional, UK format)
        RuleFor(x => x.VatNumber)
            .MaximumLength(20).WithMessage("VAT number cannot exceed 20 characters")
            .Matches(@"^[A-Z]{2}\d{8,12}$")
            .WithMessage("VAT number format is invalid. Expected format: GB123456789 (2 letters followed by 8-12 digits)")
            .When(x => !string.IsNullOrWhiteSpace(x.VatNumber));

        // Notes validation (optional)
        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }

    /// <summary>
    /// Set the customer ID for update validation
    /// This allows the customer to keep their current email address during updates
    /// </summary>
    public void SetCustomerIdForUpdate(int customerId)
    {
        _customerId = customerId;
    }

    /// <summary>
    /// Checks if the email address is unique across all customers except the current one being updated
    /// </summary>
    private async Task<bool> BeUniqueEmailForUpdate(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return true; // Let the NotEmpty validator handle empty emails

        return !await _context.Customers
            .AnyAsync(c => c.Email.ToLower() == email.ToLower() && c.Id != _customerId, cancellationToken);
    }
}