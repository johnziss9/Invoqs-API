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
            .Matches(@"^[\p{L}\p{N}\s\-\.\,\&\']+$")
            .WithMessage("Customer name contains invalid characters. Only letters, numbers, spaces, hyphens, periods, commas, ampersands, and apostrophes are allowed");

        // Emails validation
        RuleFor(x => x.Emails)
            .NotEmpty().WithMessage("At least one email address is required")
            .Must(emails => emails != null && emails.Count > 0)
            .WithMessage("At least one email address is required");

        RuleForEach(x => x.Emails)
            .NotEmpty().WithMessage("Email address cannot be empty")
            .EmailAddress().WithMessage("Please enter a valid email address")
            .MaximumLength(255).WithMessage("Email address cannot exceed 255 characters");

        RuleFor(x => x.Emails)
            .Must(HaveUniqueEmails).WithMessage("Duplicate email addresses are not allowed");

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

        // VAT number validation (optional, Cyprus format)
        RuleFor(x => x.VatNumber)
            .MaximumLength(20).WithMessage("VAT number cannot exceed 20 characters")
            .Matches(@"^(CY)?\d{8}[A-Z]$")
            .WithMessage("VAT number format is invalid. Expected format: 12345678L or CY12345678L (8 digits followed by 1 letter)")
            .When(x => !string.IsNullOrWhiteSpace(x.VatNumber));

        // Notes validation (optional)
        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }

    /// <summary>
    /// Checks that there are no duplicate emails in the list
    /// </summary>
    private bool HaveUniqueEmails(List<string>? emails)
    {
        if (emails == null || emails.Count == 0)
            return true;

        var normalizedEmails = emails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim().ToLower())
            .ToList();

        return normalizedEmails.Count == normalizedEmails.Distinct().Count();
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
            .Matches(@"^[\p{L}\p{N}\s\-\.\,\&\']+$")
            .WithMessage("Customer name contains invalid characters. Only letters, numbers, spaces, hyphens, periods, commas, ampersands, and apostrophes are allowed");

        // Emails validation
        RuleFor(x => x.Emails)
            .NotEmpty().WithMessage("At least one email address is required")
            .Must(emails => emails != null && emails.Count > 0)
            .WithMessage("At least one email address is required");

        RuleForEach(x => x.Emails)
            .NotEmpty().WithMessage("Email address cannot be empty")
            .EmailAddress().WithMessage("Please enter a valid email address")
            .MaximumLength(255).WithMessage("Email address cannot exceed 255 characters");

        RuleFor(x => x.Emails)
            .Must(HaveUniqueEmails).WithMessage("Duplicate email addresses are not allowed");

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
            .Matches(@"^(CY)?\d{8}[A-Z]$")
            .WithMessage("VAT number format is invalid. Expected format: 12345678L or CY12345678L (8 digits followed by 1 letter)")
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
    /// Checks that there are no duplicate emails in the list
    /// </summary>
    private bool HaveUniqueEmails(List<string>? emails)
    {
        if (emails == null || emails.Count == 0)
            return true;

        var normalizedEmails = emails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim().ToLower())
            .ToList();

        return normalizedEmails.Count == normalizedEmails.Distinct().Count();
    }
}