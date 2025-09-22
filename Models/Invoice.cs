using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invoqs.API.Models;

public class Invoice
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
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

    [MaxLength(100)]
    public string? PaymentMethod { get; set; }

    [MaxLength(200)]
    public string? PaymentReference { get; set; }

    public DateTime? PaymentDate { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public DateTime? SentDate { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
}