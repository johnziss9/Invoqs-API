namespace Invoqs.API.DTOs;

/// <summary>
/// Customer email data for API responses
/// </summary>
public class CustomerEmailDTO
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

/// <summary>
/// Data for creating a new customer email
/// </summary>
public class CreateCustomerEmailDTO
{
    public string Email { get; set; } = string.Empty;
}