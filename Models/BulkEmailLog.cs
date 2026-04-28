namespace Invoqs.API.Models;

public class BulkEmailLog
{
    public int Id { get; set; }
    public DateTime SentDate { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Language { get; set; } = "el";
    public int SentByUserId { get; set; }
    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }

    // Navigation properties
    public virtual User SentByUser { get; set; } = null!;
    public virtual ICollection<BulkEmailRecipient> Recipients { get; set; } = new List<BulkEmailRecipient>();
}
