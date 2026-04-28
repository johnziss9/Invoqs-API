using Invoqs.API.DTOs;
using Invoqs.API.Models;

namespace Invoqs.API.Interfaces;

public interface IBulkEmailLogService
{
    Task<BulkEmailLog> SaveLogAsync(BulkEmailLog log);
    Task<IEnumerable<BulkEmailLogDTO>> GetAllLogsAsync();
    Task<BulkEmailLogDTO?> GetLogByIdAsync(int id);
}
