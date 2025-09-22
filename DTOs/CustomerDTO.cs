using System.ComponentModel.DataAnnotations;

namespace Invoqs.API.DTOs;

/// <summary>
/// Full customer data for API responses
/// </summary>
public class CustomerDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyRegistrationNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    // Computed properties from Blazor model
    public int ActiveJobs { get; set; }
    public int NewJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int CancelledJobs { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalJobs => ActiveJobs + NewJobs + CompletedJobs + CancelledJobs;
    public string Status => ActiveJobs > 0 ? "Active" : "Inactive";
}

/// <summary>
/// Basic customer info for dropdown lists and references
/// </summary>
public class CustomerSummaryDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalJobs { get; set; }
    public decimal TotalRevenue { get; set; }
}

/// <summary>
/// Data for creating new customers
/// </summary>
public class CreateCustomerDTO
{
    [Required(ErrorMessage = "Customer name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Customer name must be between 2 and 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(255, ErrorMessage = "Email address cannot exceed 255 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Phone number must be between 10 and 20 characters")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Company registration number cannot exceed 50 characters")]
    public string? CompanyRegistrationNumber { get; set; }

    [StringLength(20, ErrorMessage = "VAT number cannot exceed 20 characters")]
    public string? VatNumber { get; set; }

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }
}

/// <summary>
/// Data for updating existing customers
/// </summary>
public class UpdateCustomerDTO
{
    [Required(ErrorMessage = "Customer name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Customer name must be between 2 and 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(255, ErrorMessage = "Email address cannot exceed 255 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Phone number must be between 10 and 20 characters")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Company registration number cannot exceed 50 characters")]
    public string? CompanyRegistrationNumber { get; set; }

    [StringLength(20, ErrorMessage = "VAT number cannot exceed 20 characters")]
    public string? VatNumber { get; set; }

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }
}