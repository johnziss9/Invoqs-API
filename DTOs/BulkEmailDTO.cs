namespace Invoqs.API.DTOs;

public class BulkEmailRequestDTO
{
    public List<int> CustomerIds { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Language { get; set; } = "el";
}

public class BulkEmailResultDTO
{
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public List<BulkEmailFailureDTO> Failures { get; set; } = new();
}

public class BulkEmailFailureDTO
{
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
