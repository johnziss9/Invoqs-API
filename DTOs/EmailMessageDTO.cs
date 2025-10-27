namespace Invoqs.API.DTOs;

public class EmailMessageDto
{
    public string ToEmail { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public byte[]? AttachmentData { get; set; }
    public string? AttachmentFileName { get; set; }
}