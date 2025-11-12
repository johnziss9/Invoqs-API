using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Invoqs.API.Interfaces;
using Invoqs.API.DTOs;
using Invoqs.API.Data;
using Microsoft.EntityFrameworkCore;
using Invoqs.API.Models;

namespace Invoqs.API.Services;

public class PdfService : IPdfService
{
    private readonly InvoqsDbContext _context;
    private readonly ILogger<PdfService> _logger;

    public PdfService(InvoqsDbContext context, ILogger<PdfService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(int invoiceId)
    {
        _logger.LogInformation("Generating PDF for invoice ID: {InvoiceId}", invoiceId);

        try
        {
            // Fetch invoice data directly from database
            var invoiceEntity = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.LineItems)
                    .ThenInclude(li => li.Job)
                .FirstOrDefaultAsync(i => i.Id == invoiceId && !i.IsDeleted);

            if (invoiceEntity == null)
            {
                throw new InvalidOperationException($"Invoice with ID {invoiceId} not found");
            }

            // Map to DTO for PDF generation
            var invoice = new InvoiceDTO
            {
                Id = invoiceEntity.Id,
                InvoiceNumber = invoiceEntity.InvoiceNumber,
                CustomerId = invoiceEntity.CustomerId,
                CustomerName = invoiceEntity.Customer.Name,
                CustomerEmail = invoiceEntity.Customer.Email ?? "",
                CustomerPhone = invoiceEntity.Customer.Phone ?? "",
                Subtotal = invoiceEntity.Subtotal,
                VatRate = invoiceEntity.VatRate,
                VatAmount = invoiceEntity.VatAmount,
                Total = invoiceEntity.Total,
                Status = invoiceEntity.Status,
                PaymentTermsDays = invoiceEntity.PaymentTermsDays,
                Notes = invoiceEntity.Notes,
                CreatedDate = invoiceEntity.CreatedDate,
                DueDate = invoiceEntity.DueDate,
                LineItems = invoiceEntity.LineItems.Select(li => new InvoiceLineItemDTO
                {
                    Id = li.Id,
                    InvoiceId = li.InvoiceId,
                    JobId = li.JobId,
                    Description = li.Description,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                    LineTotal = li.LineTotal,
                    JobTitle = li.Job?.Title ?? "",
                    JobType = li.Job?.Type ?? JobType.SkipRental,
                    JobAddress = li.Job?.Address ?? ""
                }).ToList()
            };

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
                        x.Span("Σελίδα ");
                        x.CurrentPageNumber();
                        x.Span(" από ");
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
                column.Item().Text("A. SAVVA SERVICES COMPANY LTD").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                column.Item().Text("Ανδρέα Παναγίδη, 5").FontSize(9);
                column.Item().Text("Αραδίππου, 7103, Λάρνακα, Κύπρος").FontSize(9);
                column.Item().Text("Phone: 01234 567890").FontSize(9);
                column.Item().Text("Email: antreasforklift@gmail.com").FontSize(9);
            });

            row.RelativeItem().AlignRight().Column(column =>
            {
                column.Item().AlignRight().Text("ΤΙΜΟΛΟΓΙΟ").FontSize(24).SemiBold();
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
                    col.Item().Text("Πληρωτέος:").SemiBold();
                    col.Item().Text(invoice.CustomerName).FontSize(10);
                    col.Item().Text(invoice.CustomerEmail ?? "Μ/Δ").FontSize(9);
                    col.Item().Text(invoice.CustomerPhone ?? "Μ/Δ").FontSize(9);
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.AutoItem().Width(100).Text("Αριθμός Τιμολογίου:").SemiBold();
                        r.AutoItem().Text(invoice.InvoiceNumber);
                    });
                    col.Item().Row(r =>
                    {
                        r.AutoItem().Width(100).Text("Ημερομηνία Τιμολογίου:").SemiBold();
                        r.AutoItem().Text(invoice.CreatedDate.ToString("dd MMM yyyy"));
                    });
                    col.Item().Row(r =>
                    {
                        r.AutoItem().Width(100).Text("Ημερομηνία Λήξης:").SemiBold();
                        r.AutoItem().Text(invoice.DueDate?.ToString("dd MMM yyyy") ?? "Μ/Δ");
                    });
                    col.Item().Row(r =>
                    {
                        r.AutoItem().Width(100).Text("Κατάσταση:").SemiBold();
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
                    header.Cell().Element(CellStyle).Text("Περιγραφή").SemiBold();
                    header.Cell().Element(CellStyle).AlignCenter().Text("Ποσότητα").SemiBold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Τιμή Μονάδας").SemiBold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Σύνολο").SemiBold();

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5)
                            .BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                    }
                });

                // Line Items
                foreach (var item in invoice.LineItems)
                {
                    table.Cell().Element(CellStyle).Text(item.Description ?? "Μ/Δ");
                    table.Cell().Element(CellStyle).AlignCenter().Text(item.Quantity.ToString());
                    table.Cell().Element(CellStyle).AlignRight().Text($"€{item.UnitPrice:N2}");
                    table.Cell().Element(CellStyle).AlignRight().Text($"€{item.LineTotal:N2}");

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
                    row.AutoItem().Width(100).Text("Υποσύνολο:").SemiBold();
                    row.AutoItem().Text($"€{invoice.Subtotal:N2}");
                });
                totalsColumn.Item().Row(row =>
                {
                    row.AutoItem().Width(100).Text($"ΦΠΑ ({invoice.VatRate}%):").SemiBold();
                    row.AutoItem().Text($"€{invoice.VatAmount:N2}");
                });
                totalsColumn.Item().Row(row =>
                {
                    row.AutoItem().Width(100).Text("Σύνολο:").FontSize(14).SemiBold();
                    row.AutoItem().Text($"€{invoice.Total:N2}").FontSize(14).SemiBold();
                });
            });

            // Notes Section
            if (!string.IsNullOrWhiteSpace(invoice.Notes))
            {
                column.Item().PaddingTop(20).Column(notesColumn =>
                {
                    notesColumn.Item().Text("Σημειώσεις:").SemiBold();
                    notesColumn.Item().Text(invoice.Notes).FontSize(9);
                });
            }

            // Payment Terms
            column.Item().PaddingTop(20).Column(termsColumn =>
            {
                termsColumn.Item().Text("Όροι Πληρωμής:").SemiBold();
                termsColumn.Item().Text($"Πληρωμή εντός {invoice.PaymentTermsDays} ημερών").FontSize(9);
            });
        });
    }

    // Generate Receipt PDF with multiple invoices
    public async Task<byte[]> GenerateReceiptPdfAsync(int receiptId, string userFirstName, string userLastName)
    {
        try
        {
            // Fetch receipt data directly from database
            var receiptEntity = await _context.Receipts
                .Include(r => r.Customer)
                .Include(r => r.ReceiptInvoices)
                    .ThenInclude(ri => ri.Invoice)
                .FirstOrDefaultAsync(r => r.Id == receiptId && !r.IsDeleted);

            if (receiptEntity == null)
            {
                throw new InvalidOperationException($"Receipt with ID {receiptId} not found");
            }

            // Map to DTO for PDF generation
            var receipt = new ReceiptDTO
            {
                Id = receiptEntity.Id,
                ReceiptNumber = receiptEntity.ReceiptNumber,
                CustomerId = receiptEntity.CustomerId,
                CustomerName = receiptEntity.Customer.Name,
                CustomerEmail = receiptEntity.Customer.Email ?? "",
                CustomerPhone = receiptEntity.Customer.Phone ?? "",
                CustomerVatNumber = receiptEntity.Customer.VatNumber,
                TotalAmount = receiptEntity.TotalAmount,
                CreatedDate = receiptEntity.CreatedDate,
                IsSent = receiptEntity.IsSent,
                SentDate = receiptEntity.SentDate,
                Invoices = receiptEntity.ReceiptInvoices.Select(ri => new ReceiptInvoiceDTO
                {
                    InvoiceId = ri.InvoiceId,
                    InvoiceNumber = ri.Invoice.InvoiceNumber,
                    InvoiceDate = ri.Invoice.CreatedDate,
                    AllocatedAmount = ri.AllocatedAmount,
                    PaymentDate = ri.Invoice.PaymentDate,
                    PaymentMethod = ri.Invoice.PaymentMethod
                }).ToList()
            };

            var userFullName = $"{userFirstName} {userLastName}".Trim();
            if (string.IsNullOrWhiteSpace(userFullName))
                userFullName = "Unknown User";

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
                        x.Span($"Εκδόθηκε από: {userFullName} • ");
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
                column.Item().Text("A. SAVVA SERVICES COMPANY LTD").FontSize(16).SemiBold().FontColor(Colors.Blue.Medium);
                column.Item().Text("Ανδρέα Παναγίδη, 5").FontSize(8);
                column.Item().Text("Αραδίππου, 7103, Λάρνακα, Κύπρος").FontSize(8);
                column.Item().Text("Phone: 01234 567890").FontSize(8);
                column.Item().Text("Email: antreasforklift@gmail.com").FontSize(8);
            });
        });
    }

    private void ComposeReceiptContent(IContainer container, ReceiptDTO receipt)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Spacing(15);

            // Receipt Title
            column.Item().AlignCenter().Text("Απόδειξη").FontSize(28).SemiBold();

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
                        r.AutoItem().Width(120).Text("Αριθμός Απόδειξης.:").SemiBold();
                        r.AutoItem().Text(receipt.ReceiptNumber);
                    });
                    col.Item().Row(r =>
                    {
                        r.AutoItem().Width(120).Text("Ημερομηνία Απόδειξης:").SemiBold();
                        r.AutoItem().Text(receipt.CreatedDate.ToString("dd/MM/yy"));
                    });
                    if (!string.IsNullOrWhiteSpace(receipt.CustomerVatNumber))
                    {
                        col.Item().Row(r =>
                        {
                            r.AutoItem().Width(120).Text("Αριθμός ΦΠΑ:").SemiBold();
                            r.AutoItem().Text(receipt.CustomerVatNumber);
                        });
                    }
                });
            });

            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);

            // Transfer Analysis
            column.Item().PaddingTop(15).Text("Ανάλυση Μεταφοράς").FontSize(12).SemiBold();

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
                    header.Cell().Element(CellStyle).Text("Ημερομηνία Μεταφοράς").SemiBold();
                    header.Cell().Element(CellStyle).Text("Στοιχεία Πληρωμής").SemiBold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Ποσό").SemiBold();

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                    }
                });

                table.Cell().Element(CellStyle).Text("1");
                table.Cell().Element(CellStyle).Text(receipt.CreatedDate.ToString("dd/MM/yy"));
                table.Cell().Element(CellStyle).Text("Πληρωμή Ληφθείσα");
                table.Cell().Element(CellStyle).AlignRight().Text($"€ {receipt.TotalAmount:N2}");

                static IContainer CellStyle(IContainer container)
                {
                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(5);
                }
            });

            // Payment Summary
            column.Item().PaddingTop(10).AlignRight().Column(summaryColumn =>
            {
                summaryColumn.Item().Text("Ανάλυση Πληρωμής").FontSize(11).SemiBold();

                // Group invoices by payment method and sum allocated amounts
                var paymentsByMethod = receipt.Invoices
                    .GroupBy(i => i.PaymentMethod ?? "Μ/Δ")
                    .Select(g => new { Method = g.Key, Total = g.Sum(i => i.AllocatedAmount) })
                    .OrderByDescending(x => x.Total)
                    .ToList();

                // Display each payment method with its total
                foreach (var payment in paymentsByMethod)
                {
                    summaryColumn.Item().Row(row =>
                    {
                        row.AutoItem().Width(100).Text(TranslatePaymentMethod(payment.Method));
                        row.AutoItem().Text($"€ {payment.Total:N2}");
                    });
                }

                // Total payment row
                summaryColumn.Item().Row(row =>
                {
                    row.AutoItem().Width(100).Text("Σύνολο Πληρωμής").FontSize(11).SemiBold();
                    row.AutoItem().Text($"€ {receipt.TotalAmount:N2}").FontSize(11).SemiBold();
                });
            });

            column.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Medium);

            // Invoices Paid
            column.Item().PaddingTop(15).Text("Τιμολόγια που Πληρώθηκαν").FontSize(12).SemiBold();

            try
            {
                _logger.LogInformation("About to render invoice table with {Count} invoices", receipt.Invoices?.Count ?? 0);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);   // #
                        columns.RelativeColumn(1);    // Invoice Date
                        columns.RelativeColumn(2);    // Invoice Number
                        columns.RelativeColumn(1);    // Payment Date
                        columns.RelativeColumn(1.5f); // Payment Method
                        columns.RelativeColumn(1);    // Amount
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("#").SemiBold();
                        header.Cell().Element(CellStyle).Text("Ημερομηνία Τιμολογίου").SemiBold();
                        header.Cell().Element(CellStyle).Text("Αριθμός Τιμολογίου").SemiBold();
                        header.Cell().Element(CellStyle).Text("Ημερομηνία Πληρωμής").SemiBold();
                        header.Cell().Element(CellStyle).Text("Μέθοδος Πληρωμής").SemiBold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Ποσό").SemiBold();

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                        }
                    });

                    if (receipt.Invoices == null || !receipt.Invoices.Any())
                    {
                        _logger.LogWarning("No invoices to render in table!");
                        table.Cell().ColumnSpan(6).Text("Δεν βρέθηκαν τιμολόγια").Italic();
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
                            table.Cell().Element(CellStyle).Text(invoice.InvoiceNumber ?? "Μ/Δ");
                            table.Cell().Element(CellStyle).Text(invoice.PaymentDate?.ToString("dd/MM/yy") ?? "-");
                            table.Cell().Element(CellStyle).Text(TranslatePaymentMethod(invoice.PaymentMethod));
                            table.Cell().Element(CellStyle).AlignRight().Text($"€ {invoice.AllocatedAmount:N2}");
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
                row.AutoItem().Width(120).Text("Κατανεμημένο Σύνολο").FontSize(11).SemiBold();
                row.AutoItem().Text($"€ {receipt.TotalAmount:N2}").FontSize(11).SemiBold();
            });
        });
    }

    private string TranslatePaymentMethod(string? method)
    {
        if (string.IsNullOrEmpty(method)) return "Μ/Δ";

        return method switch
        {
            "Bank Transfer" => "Τραπεζική Μεταφορά",
            "Cash" => "Μετρητά",
            "Cheque" => "Επιταγή",
            "Credit Card" => "Πιστωτική Κάρτα",
            "Other" => "Άλλο",
            _ => method // Return as-is if not found (in case it's already in Greek)
        };
    }
}