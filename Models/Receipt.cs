using System.ComponentModel.DataAnnotations.Schema;

namespace Invoqs.API.Models;

public class Receipt
{
    public int Id { get; set; }

    public string ReceiptNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public bool IsDeleted { get; set; } = false;

    public bool IsSent { get; set; } = false;
    
    public DateTime? SentDate { get; set; }

    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual ICollection<ReceiptInvoice> ReceiptInvoices { get; set; } = new List<ReceiptInvoice>();
}