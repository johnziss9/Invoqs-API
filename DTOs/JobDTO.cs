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
    public JobStatus Status { get; set; }
    public decimal Price { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
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
    public string CustomerEmail { get; set; } = string.Empty;

    // Computed properties from Blazor model
    public string TypeDisplayName => Type switch
    {
        JobType.SkipRental => "Skip Rental",
        JobType.SandDelivery => "Sand Delivery",
        JobType.ForkLiftService => "Fork Lift Service",
        _ => Type.ToString()
    };

    public string TypeIcon => Type switch
    {
        JobType.SkipRental => "/images/icons/skip.png",
        JobType.SandDelivery => "/images/icons/sand.png",
        JobType.ForkLiftService => "/images/icons/forklift.png",
        _ => "bi-briefcase"
    };

    public string StatusColor => Status switch
    {
        JobStatus.New => "primary",
        JobStatus.Active => "warning",
        JobStatus.Completed => "success",
        JobStatus.Cancelled => "secondary",
        _ => "secondary"
    };

    public string StatusIcon => Status switch
    {
        JobStatus.New => "bi-plus-circle",
        JobStatus.Active => "bi-clock",
        JobStatus.Completed => "bi-check-circle",
        JobStatus.Cancelled => "bi-x-circle",
        _ => "bi-circle"
    };

    public int DurationDays
    {
        get
        {
            var endDate = EndDate ?? DateTime.Now;
            return Math.Max(1, (int)(endDate - StartDate).TotalDays + 1);
        }
    }

    public string ShortAddress
    {
        get
        {
            if (string.IsNullOrEmpty(Address)) return string.Empty;
            var parts = Address.Split(',');
            return parts.Length > 0 ? parts[0].Trim() : Address;
        }
    }

    public bool CanBeInvoiced => Status == JobStatus.Completed && !IsInvoiced;

    public decimal GetVatRate() => Type switch
    {
        JobType.SkipRental => 0.05m, // 5%
        JobType.SandDelivery => 0.19m, // 19%
        JobType.ForkLiftService => 0.19m, // 19%
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
    public JobStatus Status { get; set; }
    public decimal Price { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsInvoiced { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    public string TypeDisplayName => Type switch
    {
        JobType.SkipRental => "Skip Rental",
        JobType.SandDelivery => "Sand Delivery",
        JobType.ForkLiftService => "Fork Lift Service",
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

    public JobStatus Status { get; set; } = JobStatus.New;

    public decimal Price { get; set; }

    public DateTime StartDate { get; set; } = DateTime.Today.AddDays(1);

    public DateTime? EndDate { get; set; }

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

    public JobStatus Status { get; set; }

    public decimal Price { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

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
/// Data for updating only job status
/// </summary>
public class UpdateJobStatusDTO
{
    public JobStatus Status { get; set; }

    public DateTime? EndDate { get; set; }
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