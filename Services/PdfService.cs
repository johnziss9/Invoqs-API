using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Invoqs.API.Interfaces;
using Invoqs.API.DTOs;

namespace Invoqs.API.Services;

public class PdfService : IPdfService
{
    private readonly IInvoiceService _invoiceService;
    private readonly IReceiptService _receiptService;
    private readonly ILogger<PdfService> _logger;

    public PdfService(IInvoiceService invoiceService, IReceiptService receiptService, ILogger<PdfService> logger)
    {
        _invoiceService = invoiceService;
        _receiptService = receiptService;
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

    // Generate Receipt PDF with multiple invoices
    public async Task<byte[]> GenerateReceiptPdfAsync(int receiptId)
    {
        try
        {
            var receipt = await _receiptService.GetReceiptByIdAsync(receiptId);

            if (receipt == null)
            {
                throw new InvalidOperationException($"Receipt with ID {receiptId} not found");
            }

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Element(ComposeReceiptHeader);
                    page.Content().Element(container => ComposeReceiptContent(container, receipt));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Issued By: User Name • ");
                        x.Span(DateTime.UtcNow.ToString("dd/MM/yy HH:mm"));
                    });
                });
            }).GeneratePdf();

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating receipt PDF for receipt ID: {ReceiptId}", receiptId);
            throw;
        }
    }

    private void ComposeReceiptHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Your Company Name").FontSize(16).SemiBold().FontColor(Colors.Blue.Medium);
                column.Item().Text("123 Business Street").FontSize(8);
                column.Item().Text("City, Postcode").FontSize(8);
                column.Item().Text("Phone: 01234 567890").FontSize(8);
                column.Item().Text("Email: accounts@yourcompany.com").FontSize(8);
            });

            row.RelativeItem().AlignRight().Column(column =>
            {
                column.Item().AlignRight().Text("VAT Registration No.: 123456789").FontSize(8);
                column.Item().AlignRight().Text("Quarry Permit No.: ABC123").FontSize(8);
            });
        });
    }

    private void ComposeReceiptContent(IContainer container, ReceiptDTO receipt)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Spacing(15);

            // Receipt Title
            column.Item().AlignCenter().Text("Receipt").FontSize(28).SemiBold();

            // Receipt Details
            column.Item().Row(row =>
            {
                // Left - Customer info
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(receipt.CustomerName).FontSize(11).SemiBold();
                    col.Item().Text(receipt.CustomerPhone ?? "").FontSize(9);
                    col.Item().Text(receipt.CustomerEmail ?? "").FontSize(9);
                });

                // Right - Receipt info
                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.AutoItem().Width(120).Text("Receipt No.:").SemiBold();
                        r.AutoItem().Text(receipt.ReceiptNumber);
                    });
                    col.Item().Row(r =>
                    {
                        r.AutoItem().Width(120).Text("Receipt Date:").SemiBold();
                        r.AutoItem().Text(receipt.CreatedDate.ToString("dd/MM/yy"));
                    });
                    col.Item().Row(r =>
                    {
                        r.AutoItem().Width(120).Text("Account Reference:").SemiBold();
                        r.AutoItem().Text(receipt.CustomerId.ToString());
                    });
                    col.Item().Row(r =>
                    {
                        r.AutoItem().Width(120).Text("VAT Number:").SemiBold();
                        r.AutoItem().Text("123456789");
                    });
                    col.Item().Row(r =>
                    {
                        r.AutoItem().Width(120).Text("Journal Reference:").SemiBold();
                        r.AutoItem().Text($"J{receipt.Id:D6}");
                    });
                });
            });

            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);

            // Transfer Analysis
            column.Item().PaddingTop(15).Text("Transfer Analysis").FontSize(12).SemiBold();

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(80);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("#").SemiBold();
                    header.Cell().Element(CellStyle).Text("Transfer Date").SemiBold();
                    header.Cell().Element(CellStyle).Text("Payment Details").SemiBold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Amount").SemiBold();

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                    }
                });

                table.Cell().Element(CellStyle).Text("1");
                table.Cell().Element(CellStyle).Text(receipt.PaymentDate.ToString("dd/MM/yy"));
                table.Cell().Element(CellStyle).Text($"- {receipt.PaymentMethod}");
                table.Cell().Element(CellStyle).AlignRight().Text($"{receipt.TotalAmount:N2}");

                static IContainer CellStyle(IContainer container)
                {
                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(5);
                }
            });

            // Payment Summary
            column.Item().PaddingTop(10).AlignRight().Column(summaryColumn =>
            {
                summaryColumn.Item().Text("Payment Analysis").FontSize(11).SemiBold();
                summaryColumn.Item().Row(row =>
                {
                    row.AutoItem().Width(100).Text("Cash");
                    row.AutoItem().Text("0.00");
                });
                summaryColumn.Item().Row(row =>
                {
                    row.AutoItem().Width(100).Text("Other").SemiBold();
                    row.AutoItem().Text($"{receipt.TotalAmount:N2}").SemiBold();
                });
                summaryColumn.Item().Row(row =>
                {
                    row.AutoItem().Width(100).Text("Payment Total").FontSize(11).SemiBold();
                    row.AutoItem().Text($"EUR {receipt.TotalAmount:N2}").FontSize(11).SemiBold();
                });
            });

            column.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Medium);

            // Invoices Paid
            column.Item().PaddingTop(15).Text("Invoices Paid").FontSize(12).SemiBold();

            try
            {
                _logger.LogInformation("About to render invoice table with {Count} invoices", receipt.Invoices?.Count ?? 0);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(80);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("#").SemiBold();
                        header.Cell().Element(CellStyle).Text("Reference Date").SemiBold();
                        header.Cell().Element(CellStyle).Text("Invoice Number").SemiBold();
                        header.Cell().Element(CellStyle).Text("Details").SemiBold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Allocated Amount").SemiBold();

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                        }
                    });

                    if (receipt.Invoices == null || !receipt.Invoices.Any())
                    {
                        _logger.LogWarning("No invoices to render in table!");
                        table.Cell().ColumnSpan(5).Text("No invoices found").Italic();
                    }
                    else
                    {
                        int rowNum = 1;
                        foreach (var invoice in receipt.Invoices)
                        {
                            _logger.LogInformation("Rendering row {RowNum}: Invoice {InvoiceNumber}, Date: {Date}, Amount: {Amount}",
                                rowNum, invoice.InvoiceNumber ?? "NULL", invoice.InvoiceDate, invoice.AllocatedAmount);

                            table.Cell().Element(CellStyle).Text(rowNum.ToString());
                            table.Cell().Element(CellStyle).Text(invoice.InvoiceDate.ToString("dd/MM/yy"));
                            table.Cell().Element(CellStyle).Text(invoice.InvoiceNumber ?? "N/A");
                            table.Cell().Element(CellStyle).Text("AM Sales Invoice");
                            table.Cell().Element(CellStyle).AlignRight().Text($"{invoice.AllocatedAmount:N2}");
                            rowNum++;
                        }
                    }

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(5);
                    }
                });

                _logger.LogInformation("Invoice table rendered successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering invoice table in PDF");
                column.Item().Text($"Error rendering invoices: {ex.Message}").FontColor(Colors.Red.Medium);
            }

            // Allocated Total
            column.Item().PaddingTop(10).AlignRight().Row(row =>
            {
                row.AutoItem().Width(120).Text("Allocated Total").FontSize(11).SemiBold();
                row.AutoItem().Text($"EUR {receipt.TotalAmount:N2}").FontSize(11).SemiBold();
            });
        });
    }
}