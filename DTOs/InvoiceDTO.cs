using System.ComponentModel.DataAnnotations;
using Invoqs.API.Models;

namespace Invoqs.API.DTOs;

/// <summary>
/// Full invoice data for API responses
/// </summary>
public class InvoiceDTO
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal Total { get; set; }
    public InvoiceStatus Status { get; set; }
    public int PaymentTermsDays { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public DateTime? SentDate { get; set; }
    public DateTime? DueDate { get; set; }

    // Customer information (included in responses)
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    // Line items
    public List<InvoiceLineItemDTO> LineItems { get; set; } = new();

    // Computed properties from Blazor model
    public string StatusBadgeClass => Status switch
    {
        InvoiceStatus.Draft => "bg-secondary",
        InvoiceStatus.Sent => "bg-primary",
        InvoiceStatus.Paid => "bg-success",
        InvoiceStatus.Overdue => "bg-danger",
        InvoiceStatus.Cancelled => "bg-dark",
        _ => "bg-secondary"
    };

    public string StatusIcon => Status switch
    {
        InvoiceStatus.Draft => "bi-pencil-square",
        InvoiceStatus.Sent => "bi-send",
        InvoiceStatus.Paid => "bi-check-circle",
        InvoiceStatus.Overdue => "bi-exclamation-triangle",
        InvoiceStatus.Cancelled => "bi-x-circle",
        _ => "bi-file-text"
    };

    public bool IsOverdue
    {
        get
        {
            if (Status == InvoiceStatus.Paid || Status == InvoiceStatus.Cancelled || !DueDate.HasValue)
                return false;
            return DateTime.Today > DueDate.Value;
        }
    }

    public int? DaysUntilDue
    {
        get
        {
            if (!DueDate.HasValue || Status == InvoiceStatus.Paid || Status == InvoiceStatus.Cancelled)
                return null;
            return (int)(DueDate.Value - DateTime.Today).TotalDays;
        }
    }
}

/// <summary>
/// Basic invoice info for lists and references
/// </summary>
public class InvoiceSummaryDTO
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public InvoiceStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsOverdue { get; set; }
}

/// <summary>
/// Invoice line item data
/// </summary>
public class InvoiceLineItemDTO
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int JobId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    // Job information (for display)
    public string JobTitle { get; set; } = string.Empty;
    public JobType JobType { get; set; }
    public string JobAddress { get; set; } = string.Empty;
}

/// <summary>
/// Data for creating new invoices
/// </summary>
public class CreateInvoiceDTO
{
    [Required(ErrorMessage = "Customer is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid customer")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "At least one job is required")]
    [MinLength(1, ErrorMessage = "Please select at least one job")]
    public List<int> JobIds { get; set; } = new();

    [Range(0, 1, ErrorMessage = "VAT rate must be between 0% and 100%")]
    public decimal VatRate { get; set; } = 0.19m; // 19% default

    [Range(1, 365, ErrorMessage = "Payment terms must be between 1 and 365 days")]
    public int PaymentTermsDays { get; set; } = 30;

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }
}

/// <summary>
/// Data for updating existing invoices (draft only)
/// </summary>
public class UpdateInvoiceDTO
{
    [Required(ErrorMessage = "Customer is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid customer")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "At least one job is required")]
    [MinLength(1, ErrorMessage = "Please select at least one job")]
    public List<int> JobIds { get; set; } = new();

    [Range(0, 1, ErrorMessage = "VAT rate must be between 0% and 100%")]
    public decimal VatRate { get; set; }

    [Range(1, 365, ErrorMessage = "Payment terms must be between 1 and 365 days")]
    public int PaymentTermsDays { get; set; }

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }
}

/// <summary>
/// Data for marking invoice as sent
/// </summary>
public class MarkInvoiceAsSentDTO
{
    public DateTime? SentDate { get; set; } = DateTime.Today;
}

/// <summary>
/// Data for marking invoice as paid
/// </summary>
public class MarkInvoiceAsPaidDTO
{
    [Required(ErrorMessage = "Payment date is required")]
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Payment method is required")]
    [StringLength(50, ErrorMessage = "Payment method cannot exceed 50 characters")]
    public string PaymentMethod { get; set; } = "Bank Transfer";

    [StringLength(100, ErrorMessage = "Payment reference cannot exceed 100 characters")]
    public string? PaymentReference { get; set; }
}

/// <summary>
/// Invoice statistics for dashboard
/// </summary>
public class InvoiceStatisticsDTO
{
    public int TotalInvoices { get; set; }
    public int DraftInvoices { get; set; }
    public int SentInvoices { get; set; }
    public int PaidInvoices { get; set; }
    public int OverdueInvoices { get; set; }
    public int CancelledInvoices { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal WeeklyRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal YearlyRevenue { get; set; }
}