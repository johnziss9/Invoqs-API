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
    public bool IsSent { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public bool IsDelivered { get; set; }
    public DateTime? DueDate { get; set; }
    public bool HasReceipt { get; set; }


    // Customer information (included in responses)
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public bool CustomerIsDeleted { get; set; }
    public DateTime CustomerCreatedDate { get; set; }
    public DateTime? CustomerUpdatedDate { get; set; }

    public string Address { get; set; } = string.Empty; // Primary address or comma-separated if multiple
    public List<string> Addresses { get; set; } = new(); // All unique addresses from line items

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
    public string Address { get; set; } = string.Empty;
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
    public int CustomerId { get; set; }

    public List<int> JobIds { get; set; } = new();

    public decimal VatRate { get; set; } = 0.19m; // 19% default

    public int PaymentTermsDays { get; set; } = 30;

    public string? Notes { get; set; }
}

/// <summary>
/// Data for updating existing invoices (draft only)
/// </summary>
public class UpdateInvoiceDTO
{
    public int CustomerId { get; set; }

    public List<int> JobIds { get; set; } = new();

    public decimal VatRate { get; set; }

    public int PaymentTermsDays { get; set; }

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
/// Data for marking invoice as delivered
/// </summary>
public class MarkInvoiceAsDeliveredDTO
{
    public DateTime? DeliveredDate { get; set; } = DateTime.Today;
}

/// <summary>
/// Data for marking invoice as paid
/// </summary>
public class MarkInvoiceAsPaidDTO
{
    public DateTime PaymentDate { get; set; } = DateTime.Today;

    public string PaymentMethod { get; set; } = "Bank Transfer";

    public string? PaymentReference { get; set; }
}

// /// <summary>
// /// Invoice statistics for dashboard
// /// </summary>
// public class InvoiceStatisticsDTO
// {
//     public int TotalInvoices { get; set; }
//     public int DraftInvoices { get; set; }
//     public int SentInvoices { get; set; }
//     public int PaidInvoices { get; set; }
//     public int OverdueInvoices { get; set; }
//     public int CancelledInvoices { get; set; }
//     public decimal TotalOutstanding { get; set; }
//     public decimal TotalPaid { get; set; }
//     public decimal WeeklyRevenue { get; set; }
//     public decimal MonthlyRevenue { get; set; }
//     public decimal YearlyRevenue { get; set; }
// }