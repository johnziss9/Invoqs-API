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
    public int UninvoicedJobs { get; set; }
    public int UnpaidInvoices { get; set; }
    public decimal OutstandingAmount { get; set; }
    public int TotalJobs => ActiveJobs + NewJobs + CompletedJobs + CancelledJobs;
    public bool HasActiveWork => ActiveJobs > 0 || NewJobs > 0;
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
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string? CompanyRegistrationNumber { get; set; }

    public string? VatNumber { get; set; }

    public string? Notes { get; set; }
}

/// <summary>
/// Data for updating existing customers
/// </summary>
public class UpdateCustomerDTO
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string? CompanyRegistrationNumber { get; set; }

    public string? VatNumber { get; set; }

    public string? Notes { get; set; }
}