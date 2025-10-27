namespace Invoqs.API.DTOs;

public class EmailResponseDto
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }
}