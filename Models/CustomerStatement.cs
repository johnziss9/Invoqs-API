using System.ComponentModel.DataAnnotations.Schema;

namespace Invoqs.API.Models;

public class CustomerStatement
{
    public int Id { get; set; }

    public string StatementNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalInvoiced { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalVatAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalPaid { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalPartiallyPaid { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalCancelled { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal OutstandingBalance { get; set; }

    public int InvoiceCount { get; set; }

    public int CancelledInvoiceCount { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public bool IsDeleted { get; set; } = false;

    public bool IsSent { get; set; } = false;

    public DateTime? SentDate { get; set; }

    public bool IsDelivered { get; set; } = false;

    public DateTime? DeliveredDate { get; set; }

    // Navigation property
    public virtual Customer Customer { get; set; } = null!;
}
