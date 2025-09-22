using Invoqs.API.DTOs;

namespace Invoqs.API.Interfaces;

public interface IInvoiceService
{
    Task<IEnumerable<InvoiceDTO>> GetAllInvoicesAsync();
    Task<InvoiceDTO?> GetInvoiceByIdAsync(int id);
    Task<IEnumerable<InvoiceDTO>> GetInvoicesByCustomerIdAsync(int customerId);
    Task<InvoiceDTO> CreateInvoiceAsync(CreateInvoiceDTO createDTO);
    Task<InvoiceDTO?> UpdateInvoiceAsync(int id, UpdateInvoiceDTO updateDTO);
    Task<bool> DeleteInvoiceAsync(int id);
    Task<InvoiceDTO?> MarkInvoiceAsSentAsync(int id);
    Task<InvoiceDTO?> MarkInvoiceAsPaidAsync(int id, MarkInvoiceAsPaidDTO paymentDTO);
    Task<InvoiceDTO?> CancelInvoiceAsync(int id);
    Task<decimal> GetTotalOutstandingAsync();
    Task<InvoiceStatisticsDTO> GetInvoiceStatisticsAsync();
}