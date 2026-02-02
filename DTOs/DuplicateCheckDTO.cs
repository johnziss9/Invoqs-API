namespace Invoqs.API.DTOs;

public class DuplicateCheckRequestDTO
{
    public List<string> Emails { get; set; } = new();
    public int? ExcludeCustomerId { get; set; }
}

public class DuplicateCheckResponseDTO
{
    public bool HasDuplicates { get; set; }
    public List<DuplicateCustomerDTO> DuplicateCustomers { get; set; } = new();
}

public class DuplicateCustomerDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int TotalJobs { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<string> MatchingEmails { get; set; } = new();
}