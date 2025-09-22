using Invoqs.API.DTOs;

namespace Invoqs.API.Interfaces;

public interface ICustomerService
{
    Task<IEnumerable<CustomerDTO>> GetAllCustomersAsync();
    Task<CustomerDTO?> GetCustomerByIdAsync(int id);
    Task<CustomerDTO> CreateCustomerAsync(CreateCustomerDTO createDTO);
    Task<CustomerDTO?> UpdateCustomerAsync(int id, UpdateCustomerDTO updateDTO);
    Task<bool> DeleteCustomerAsync(int id);
    Task<bool> CustomerExistsAsync(int id);
    Task<IEnumerable<CustomerDTO>> SearchCustomersAsync(string searchTerm);
}