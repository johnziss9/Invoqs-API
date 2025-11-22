using Invoqs.API.DTOs;

namespace Invoqs.API.Interfaces;

public interface IEmailService
{
    Task<EmailResponseDto> SendInvoiceEmailAsync(InvoiceDTO invoice, byte[] pdfData);
    Task<EmailResponseDto> SendReceiptEmailAsync(ReceiptDTO receipt, byte[] pdfData);
    Task<EmailResponseDto> SendInvoiceCancelledEmailAsync(InvoiceDTO invoice);
    bool ValidateConfigurationAsync();
}