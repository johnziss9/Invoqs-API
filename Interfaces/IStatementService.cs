using Invoqs.API.DTOs;

namespace Invoqs.API.Interfaces;

public interface IStatementService
{
    Task<IEnumerable<StatementDTO>> GetAllStatementsAsync();
    Task<StatementDTO?> GetStatementByIdAsync(int id);
    Task<StatementDTO> CreateStatementAsync(CreateStatementDTO createDTO);
    Task<bool> DeleteStatementAsync(int id);
    Task<bool> SendStatementAsync(int statementId, List<string> recipientEmails);
    Task<bool> MarkStatementAsDeliveredAsync(int statementId);
}
