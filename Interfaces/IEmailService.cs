using Invoqs.API.DTOs;

namespace Invoqs.API.Interfaces;

public interface IEmailService
{
    Task<EmailResponseDto> SendInvoiceEmailAsync(InvoiceDTO invoice, byte[] pdfData, List<string>? recipientEmails = null);
    Task<EmailResponseDto> SendReceiptEmailAsync(ReceiptDTO receipt, byte[] pdfData, List<string>? recipientEmails = null);
    Task<EmailResponseDto> SendStatementEmailAsync(StatementDTO statement, byte[] pdfData, List<string> recipientEmails);
    Task<EmailResponseDto> SendInvoiceCancelledEmailAsync(InvoiceDTO invoice);
    Task<EmailResponseDto> SendCustomEmailAsync(string toEmail, string toName, string subject, string body, string language = "el");
    bool ValidateConfigurationAsync();
}