namespace Invoqs.API.Interfaces;

public interface IPdfService
{
    Task<byte[]> GenerateInvoicePdfAsync(int invoiceId);
    Task<byte[]> GenerateReceiptPdfAsync(int receiptId, string userFirstName, string userLastName);
}