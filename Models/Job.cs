using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invoqs.API.Models;

public class Job
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    public JobType Type { get; set; }
    public JobStatus Status { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Invoice tracking - database fields
    public int? InvoiceId { get; set; }
    public DateTime? InvoicedDate { get; set; }

    // Computed property - NOT stored in database
    [NotMapped]
    public bool IsInvoiced => InvoiceId.HasValue && Invoice != null && !Invoice.IsDeleted;

    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual Invoice? Invoice { get; set; }
}