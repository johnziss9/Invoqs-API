using Invoqs.API.Models;
using System.ComponentModel.DataAnnotations;

namespace Invoqs.API.DTOs;

/// <summary>
/// Full statement data for API responses
/// </summary>
public class StatementDTO
{
    public int Id { get; set; }
    public string StatementNumber { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalVatAmount { get; set; }
    public decimal CancelledAmount { get; set; }
    public decimal CancelledVatAmount { get; set; }
    public int InvoiceCount { get; set; }
    public int CancelledInvoiceCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsSent { get; set; }
    public DateTime? SentDate { get; set; }
    public bool IsDelivered { get; set; }
    public DateTime? DeliveredDate { get; set; }

    // Invoices included in this statement
    public List<StatementInvoiceDTO> Invoices { get; set; } = new();
    public List<StatementInvoiceDTO> CancelledInvoices { get; set; } = new();
}

/// <summary>
/// Statement summary for lists
/// </summary>
public class StatementSummaryDTO
{
    public int Id { get; set; }
    public string StatementNumber { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int InvoiceCount { get; set; }
    public int CancelledInvoiceCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsSent { get; set; }
}

/// <summary>
/// Invoice info within a statement
/// </summary>
public class StatementInvoiceDTO
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal VatAmount { get; set; }
    public InvoiceStatus Status { get; set; }
}

/// <summary>
/// Data for creating a new statement
/// </summary>
public class CreateStatementDTO
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}

/// <summary>
/// Data for sending a statement with selected emails
/// </summary>
public class SendStatementRequestDTO
{
    [Required]
    public List<string> RecipientEmails { get; set; } = new();
}
