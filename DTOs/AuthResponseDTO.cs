namespace Invoqs.API.DTOs;

public class AuthResponseDTO
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDTO User { get; set; } = new();
}