using System.ComponentModel.DataAnnotations.Schema;

namespace Invoqs.API.Models;

public class Statement
{
    public int Id { get; set; }

    public string StatementNumber { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalVatAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal CancelledAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal CancelledVatAmount { get; set; }

    public int InvoiceCount { get; set; }

    public int CancelledInvoiceCount { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public bool IsDeleted { get; set; } = false;

    public bool IsSent { get; set; } = false;

    public DateTime? SentDate { get; set; }

    public bool IsDelivered { get; set; } = false;

    public DateTime? DeliveredDate { get; set; }
}
