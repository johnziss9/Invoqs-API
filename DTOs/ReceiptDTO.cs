using Invoqs.API.Models;

namespace Invoqs.API.DTOs;

/// <summary>
/// Full receipt data for API responses
/// </summary>
public class ReceiptDTO
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public bool CustomerIsDeleted { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsSent { get; set; }
    public DateTime? SentDate { get; set; }

    // Invoices included in this receipt
    public List<ReceiptInvoiceDTO> Invoices { get; set; } = new();
}

/// <summary>
/// Receipt summary for lists
/// </summary>
public class ReceiptSummaryDTO
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int InvoiceCount { get; set; }
    public DateTime CreatedDate { get; set; }
}

/// <summary>
/// Invoice info within a receipt
/// </summary>
public class ReceiptInvoiceDTO
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal AllocatedAmount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? PaymentMethod { get; set; }
}

/// <summary>
/// Data for creating a new receipt
/// </summary>
public class CreateReceiptDTO
{
    public int CustomerId { get; set; }

    public List<int> InvoiceIds { get; set; } = new();
}