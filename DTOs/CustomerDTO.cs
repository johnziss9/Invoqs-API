namespace Invoqs.API.DTOs;

/// <summary>
/// Full customer data for API responses
/// </summary>
public class CustomerDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<CustomerEmailDTO> Emails { get; set; } = new();
    public string Phone { get; set; } = string.Empty;
    public string? CompanyRegistrationNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    // Job and revenue statistics
    public int TotalJobs { get; set; }
    public int UninvoicedJobs { get; set; }
    public decimal TotalRevenue { get; set; }
    
    // Financial tracking
    public int UnpaidInvoices { get; set; }
    public decimal OutstandingAmount { get; set; }
}

/// <summary>
/// Basic customer info for dropdown lists and references
/// </summary>
public class CustomerSummaryDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Emails { get; set; } = new();
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

    public List<string> Emails { get; set; } = new();

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

    public List<string> Emails { get; set; } = new();

    public string Phone { get; set; } = string.Empty;

    public string? CompanyRegistrationNumber { get; set; }

    public string? VatNumber { get; set; }

    public string? Notes { get; set; }
}