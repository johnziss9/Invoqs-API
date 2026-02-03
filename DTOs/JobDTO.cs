using Invoqs.API.Models;

namespace Invoqs.API.DTOs;

/// <summary>
/// Full job data for API responses
/// </summary>
public class JobDTO
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public JobType Type { get; set; }
    public decimal Price { get; set; }
    public DateTime JobDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool IsInvoiced { get; set; }
    public int? InvoiceId { get; set; }
    public DateTime? InvoicedDate { get; set; }

    // ===== JOB TYPE SPECIFIC FIELDS =====
    
    // Skip Rental specific fields
    public string? SkipType { get; set; }
    public string? SkipNumber { get; set; }
    
    // Sand Delivery specific fields
    public string? SandMaterialType { get; set; }
    public string? SandDeliveryMethod { get; set; }
    
    // Forklift Service specific fields
    public string? ForkliftSize { get; set; }

    // Customer information (included in responses)
    public string CustomerName { get; set; } = string.Empty;
    public bool CustomerIsDeleted { get; set; }
    public string? CustomerPhone { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime CustomerCreatedDate { get; set; }
    public DateTime? CustomerUpdatedDate { get; set; }

    // Computed properties from Blazor model
    public string TypeDisplayName => Type switch
    {
        JobType.SkipRental => "Skip Rental",
        JobType.SandDelivery => "Sand Delivery",
        JobType.ForkLiftService => "Fork Lift Service",
        JobType.Transfer => "Transfer",
        _ => Type.ToString()
    };

    public string TypeIcon => Type switch
    {
        JobType.SkipRental => "/images/icons/skip.png",
        JobType.SandDelivery => "/images/icons/sand.png",
        JobType.ForkLiftService => "/images/icons/forklift.png",
        JobType.Transfer => "/images/icons/transfer.png",
        _ => "bi-briefcase"
    };

    public string ShortAddress
    {
        get
        {
            if (string.IsNullOrEmpty(Address)) return string.Empty;
            var parts = Address.Split(',');
            return parts.Length > 0 ? parts[0].Trim() : Address;
        }
    }

    public bool CanBeInvoiced => !IsInvoiced;

    public decimal GetVatRate() => Type switch
    {
        JobType.SkipRental => 0.05m, // 5%
        JobType.SandDelivery => 0.19m, // 19%
        JobType.ForkLiftService => 0.19m, // 19%
        JobType.Transfer => 0.19m, // 19%
        _ => 0.19m
    };

    public string GetInvoiceDescription() => $"{TypeDisplayName} - {Title} at {ShortAddress}";
}

/// <summary>
/// Basic job info for lists and references
/// </summary>
public class JobSummaryDTO
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public JobType Type { get; set; }
    public decimal Price { get; set; }
    public DateTime JobDate { get; set; }
    public bool IsInvoiced { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    public string TypeDisplayName => Type switch
    {
        JobType.SkipRental => "Skip Rental",
        JobType.SandDelivery => "Sand Delivery",
        JobType.ForkLiftService => "Fork Lift Service",
        JobType.Transfer => "Transfer",
        _ => Type.ToString()
    };
}

/// <summary>
/// Data for creating new jobs
/// </summary>
public class CreateJobDTO
{
    public int CustomerId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Address { get; set; } = string.Empty;

    public JobType Type { get; set; }

    public decimal Price { get; set; }

    public DateTime JobDate { get; set; } = DateTime.Today;

    // ===== JOB TYPE SPECIFIC FIELDS =====
    
    // Skip Rental specific fields
    public string? SkipType { get; set; }
    
    public string? SkipNumber { get; set; }
    
    // Sand Delivery specific fields
    public string? SandMaterialType { get; set; }
    
    public string? SandDeliveryMethod { get; set; }
    
    // Forklift Service specific fields
    public string? ForkliftSize { get; set; }
}

/// <summary>
/// Data for updating existing jobs
/// </summary>
public class UpdateJobDTO
{
    public int CustomerId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Address { get; set; } = string.Empty;

    public JobType Type { get; set; }

    public decimal Price { get; set; }

    public DateTime JobDate { get; set; }

    // ===== JOB TYPE SPECIFIC FIELDS =====
    
    // Skip Rental specific fields
    public string? SkipType { get; set; }
    
    public string? SkipNumber { get; set; }
    
    // Sand Delivery specific fields
    public string? SandMaterialType { get; set; }
    
    public string? SandDeliveryMethod { get; set; }
    
    // Forklift Service specific fields
    public string? ForkliftSize { get; set; }
}

/// <summary>
/// Data for marking jobs as invoiced
/// </summary>
public class MarkJobsAsInvoicedDTO
{
    public List<int> JobIds { get; set; } = new();

    public int InvoiceId { get; set; }
}

/// <summary>
/// Data for removing jobs from invoice
/// </summary>
public class RemoveJobsFromInvoiceDTO
{
    public List<int> JobIds { get; set; } = new();
}