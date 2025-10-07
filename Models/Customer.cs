using System.ComponentModel.DataAnnotations;

namespace Invoqs.API.Models;

public class Customer
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? CompanyRegistrationNumber { get; set; }

    [MaxLength(50)]
    public string? VatNumber { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}