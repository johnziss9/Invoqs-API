namespace Invoqs.API.Models;

public class BulkEmailRecipient
{
    public int Id { get; set; }
    public int BulkEmailLogId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }

    // Navigation properties
    public virtual BulkEmailLog BulkEmailLog { get; set; } = null!;
    public virtual Customer Customer { get; set; } = null!;
}
