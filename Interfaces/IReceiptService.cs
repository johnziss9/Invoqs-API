using Invoqs.API.DTOs;

namespace Invoqs.API.Interfaces;

public interface IReceiptService
{
    Task<IEnumerable<ReceiptDTO>> GetAllReceiptsAsync();
    Task<ReceiptDTO?> GetReceiptByIdAsync(int id);
    Task<IEnumerable<ReceiptDTO>> GetReceiptsByCustomerIdAsync(int customerId);
    Task<ReceiptDTO> CreateReceiptAsync(CreateReceiptDTO createDTO);
    Task<bool> DeleteReceiptAsync(int id);
    Task<bool> SendReceiptAsync(int receiptId);
}