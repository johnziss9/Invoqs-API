using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Invoqs.API.Interfaces;
using Invoqs.API.DTOs;

namespace Invoqs.API.Services;

public class PdfService : IPdfService
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<PdfService> _logger;

    public PdfService(IInvoiceService invoiceService, ILogger<PdfService> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(int invoiceId)
    {
        _logger.LogInformation("Generating PDF for invoice ID: {InvoiceId}", invoiceId);

        try
        {
            // Fetch invoice data using existing service
            var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId);

            if (invoice == null)
            {
                throw new InvalidOperationException($"Invoice with ID {invoiceId} not found");
            }

            // Generate PDF using QuestPDF
            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(container => ComposeContent(container, invoice));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf();

            _logger.LogInformation("Successfully generated PDF for invoice: {InvoiceNumber}", invoice.InvoiceNumber);
            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for invoice ID: {InvoiceId}", invoiceId);
            throw;
        }
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Your Company Name").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                column.Item().Text("123 Business Street").FontSize(9);
                column.Item().Text("City, Postcode").FontSize(9);
                column.Item().Text("Phone: 01234 567890").FontSize(9);
                column.Item().Text("Email: info@yourcompany.com").FontSize(9);
            });

            row.RelativeItem().AlignRight().Column(column =>
            {
                column.Item().AlignRight().Text("INVOICE").FontSize(24).SemiBold();
            });
        });
    }

    private void ComposeContent(IContainer container, InvoiceDTO invoice)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Spacing(15);

            // Invoice Details Section
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Bill To:").SemiBold();
                    col.Item().Text(invoice.CustomerName).FontSize(10);
                    col.Item().Text(invoice.CustomerEmail ?? "N/A").FontSize(9);
                    col.Item().Text(invoice.CustomerPhone ?? "N/A").FontSize(9);
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.AutoItem().Width(100).Text("Invoice Number:").SemiBold();
                        r.AutoItem().Text(invoice.InvoiceNumber);
                    });
                    col.Item().Row(r =>
                    {
                        r.AutoItem().Width(100).Text("Invoice Date:").SemiBold();
                        r.AutoItem().Text(invoice.CreatedDate.ToString("dd MMM yyyy"));
                    });
                    col.Item().Row(r =>
                    {
                        r.AutoItem().Width(100).Text("Due Date:").SemiBold();
                        r.AutoItem().Text(invoice.DueDate?.ToString("dd MMM yyyy") ?? "N/A");
                    });
                    col.Item().Row(r =>
                    {
                        r.AutoItem().Width(100).Text("Status:").SemiBold();
                        r.AutoItem().Text(invoice.Status.ToString());
                    });
                });
            });

            // Line Items Table
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3); // Description
                    columns.RelativeColumn(1); // Quantity
                    columns.RelativeColumn(1); // Unit Price
                    columns.RelativeColumn(1); // Total
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Description").SemiBold();
                    header.Cell().Element(CellStyle).AlignCenter().Text("Qty").SemiBold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Unit Price").SemiBold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Total").SemiBold();

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5)
                            .BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                    }
                });

                // Line Items
                foreach (var item in invoice.LineItems)
                {
                    table.Cell().Element(CellStyle).Text(item.Description ?? "N/A");
                    table.Cell().Element(CellStyle).AlignCenter().Text(item.Quantity.ToString());
                    table.Cell().Element(CellStyle).AlignRight().Text($"£{item.UnitPrice:N2}");
                    table.Cell().Element(CellStyle).AlignRight().Text($"£{item.LineTotal:N2}");

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                            .PaddingVertical(5);
                    }
                }
            });

            // Totals Section
            column.Item().AlignRight().Column(totalsColumn =>
            {
                totalsColumn.Item().Row(row =>
                {
                    row.AutoItem().Width(100).Text("Subtotal:").SemiBold();
                    row.AutoItem().Text($"£{invoice.Subtotal:N2}");
                });
                totalsColumn.Item().Row(row =>
                {
                    row.AutoItem().Width(100).Text($"VAT ({invoice.VatRate}%):").SemiBold();
                    row.AutoItem().Text($"£{invoice.VatAmount:N2}");
                });
                totalsColumn.Item().Row(row =>
                {
                    row.AutoItem().Width(100).Text("Total:").FontSize(14).SemiBold();
                    row.AutoItem().Text($"£{invoice.Total:N2}").FontSize(14).SemiBold();
                });
            });

            // Notes Section
            if (!string.IsNullOrWhiteSpace(invoice.Notes))
            {
                column.Item().PaddingTop(20).Column(notesColumn =>
                {
                    notesColumn.Item().Text("Notes:").SemiBold();
                    notesColumn.Item().Text(invoice.Notes).FontSize(9);
                });
            }

            // Payment Terms
            column.Item().PaddingTop(20).Column(termsColumn =>
            {
                termsColumn.Item().Text("Payment Terms:").SemiBold();
                termsColumn.Item().Text($"Payment due within {invoice.PaymentTermsDays} days").FontSize(9);
            });
        });
    }
}