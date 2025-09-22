using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invoqs.API.Models;

public class InvoiceLineItem
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public int JobId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal LineTotal { get; set; }

    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public virtual Invoice Invoice { get; set; } = null!;
    public virtual Job Job { get; set; } = null!;
}