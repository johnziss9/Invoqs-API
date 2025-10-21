using System.ComponentModel.DataAnnotations.Schema;

namespace Invoqs.API.Models;

public class ReceiptInvoice
{
    public int Id { get; set; }

    public int ReceiptId { get; set; }

    public int InvoiceId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal AllocatedAmount { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    // Navigation properties
    public virtual Receipt Receipt { get; set; } = null!;
    public virtual Invoice Invoice { get; set; } = null!;
}