namespace Invoqs.API.Models;

public class EmailSettings
{
    public string Provider { get; set; } = string.Empty;
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
}