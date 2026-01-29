namespace Invoqs.API.Models;

public class CustomerEmail
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public string Email { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    // Navigation property
    public virtual Customer Customer { get; set; } = null!;
}