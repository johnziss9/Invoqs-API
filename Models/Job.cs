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

    // ===== JOB TYPE SPECIFIC FIELDS =====
    
    // Skip Rental specific fields
    [MaxLength(50)]
    public string? SkipType { get; set; }  // SmallSkip, LargeSkip, Hook
    
    [MaxLength(50)]
    public string? SkipNumber { get; set; }  // Only for Small/Large skips
    
    // Sand Delivery specific fields
    [MaxLength(100)]
    public string? SandMaterialType { get; set; }  // Sand, CrushedStone, SandMixedWithCrushedStone, Soil
    
    [MaxLength(50)]
    public string? SandDeliveryMethod { get; set; }  // InBags, ByTruck
    
    // Forklift Service specific fields
    [MaxLength(10)]
    public string? ForkliftSize { get; set; }  // 17m, 25m

    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual Invoice? Invoice { get; set; }
}