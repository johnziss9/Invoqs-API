using System.ComponentModel.DataAnnotations.Schema;

namespace Invoqs.API.Models;

public class Invoice
{
    public int Id { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal VatRate { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal VatAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    public InvoiceStatus Status { get; set; }
    public int PaymentTermsDays { get; set; } = 30;

    public string? PaymentMethod { get; set; }

    public string? PaymentReference { get; set; }

    public DateTime? PaymentDate { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public DateTime? SentDate { get; set; }
    public bool IsSent { get; set; } = false;
    public DateTime? DeliveredDate { get; set; }
    public bool IsDelivered { get; set; } = false;
    public DateTime? DueDate { get; set; }
    public DateTime? CancelledDate { get; set; }
    public string? CancellationReason { get; set; }
    public string? CancellationNotes { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
    public ICollection<ReceiptInvoice> ReceiptInvoices { get; set; } = new List<ReceiptInvoice>();
}