using System.ComponentModel.DataAnnotations.Schema;

namespace Invoqs.API.Models;

public class InvoicePayment
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public string? PaymentReference { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public bool IsDeleted { get; set; } = false;

    // Navigation property
    public virtual Invoice Invoice { get; set; } = null!;
}
