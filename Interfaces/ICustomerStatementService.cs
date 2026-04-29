using Invoqs.API.DTOs;

namespace Invoqs.API.Interfaces;

public interface ICustomerStatementService
{
    Task<IEnumerable<CustomerStatementDTO>> GetAllCustomerStatementsAsync();
    Task<IEnumerable<CustomerStatementDTO>> GetCustomerStatementsAsync(int customerId);
    Task<CustomerStatementDTO?> GetCustomerStatementByIdAsync(int id);
    Task<CustomerStatementDTO> CreateCustomerStatementAsync(CreateCustomerStatementDTO createDTO);
    Task<bool> DeleteCustomerStatementAsync(int id);
    Task<bool> SendCustomerStatementAsync(int statementId, List<string> recipientEmails);
    Task<bool> MarkCustomerStatementAsDeliveredAsync(int statementId);
}
