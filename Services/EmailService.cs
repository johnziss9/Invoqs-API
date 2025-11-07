using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;
using Invoqs.API.Models;
using Microsoft.Extensions.Options;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;

namespace Invoqs.API.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailSettings> emailSettings,
        ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public bool ValidateConfigurationAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_emailSettings.SmtpServer) ||
                string.IsNullOrWhiteSpace(_emailSettings.ApiKey) ||
                string.IsNullOrWhiteSpace(_emailSettings.SenderEmail) ||
                _emailSettings.SmtpPort <= 0)
            {
                _logger.LogWarning("Email configuration is incomplete");
                return false;
            }

            // Validate email format
            if (!IsValidEmail(_emailSettings.SenderEmail))
            {
                _logger.LogWarning("Invalid sender email format: {Email}", _emailSettings.SenderEmail);
                return false;
            }

            _logger.LogInformation("Email configuration validated successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email configuration validation failed");
            return false;
        }
    }

    public async Task<EmailResponseDto> SendInvoiceEmailAsync(InvoiceDTO invoice, byte[] pdfData)
    {
        try
        {
            _logger.LogInformation("Starting to send invoice email for Invoice ID: {InvoiceId}", invoice.Id);

            // Validate invoice
            if (invoice == null)
            {
                _logger.LogWarning("Invoice is null");
                return new EmailResponseDto
                {
                    Success = false,
                    ErrorMessage = "Invoice not found"
                };
            }

            // Validate customer has email
            if (string.IsNullOrWhiteSpace(invoice.CustomerEmail))
            {
                _logger.LogWarning("Customer has no email address for Invoice ID: {InvoiceId}", invoice.Id);
                return new EmailResponseDto
                {
                    Success = false,
                    ErrorMessage = "Customer has no email address"
                };
            }

            // Build email message
            var emailMessage = new EmailMessageDto
            {
                ToEmail = invoice.CustomerEmail,
                ToName = invoice.CustomerName,
                Subject = $"Τιμολόγιο #{invoice.InvoiceNumber}",
                HtmlBody = GenerateInvoiceEmailHtml(invoice),
                AttachmentData = pdfData,
                AttachmentFileName = $"Invoice_{invoice.InvoiceNumber}.pdf"
            };

            // Send email with retry logic
            return await SendEmailWithRetryAsync(emailMessage, 3);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invoice email for Invoice ID: {InvoiceId}", invoice?.Id);
            return new EmailResponseDto
            {
                Success = false,
                ErrorMessage = $"Failed to send email: {ex.Message}"
            };
        }
    }

    public async Task<EmailResponseDto> SendReceiptEmailAsync(ReceiptDTO receipt, byte[] pdfData)
    {
        try
        {
            _logger.LogInformation("Starting to send receipt email for Receipt ID: {ReceiptId}", receipt.Id);

            // Validate receipt
            if (receipt == null)
            {
                _logger.LogWarning("Receipt is null");
                return new EmailResponseDto
                {
                    Success = false,
                    ErrorMessage = "Receipt not found"
                };
            }

            // Validate customer has email
            if (string.IsNullOrWhiteSpace(receipt.CustomerEmail))
            {
                _logger.LogWarning("Customer has no email address for Receipt ID: {ReceiptId}", receipt.Id);
                return new EmailResponseDto
                {
                    Success = false,
                    ErrorMessage = "Customer has no email address"
                };
            }

            // Build email message
            var emailMessage = new EmailMessageDto
            {
                ToEmail = receipt.CustomerEmail,
                ToName = receipt.CustomerName,
                Subject = $"Απόδειξη Πληρωμής #{receipt.ReceiptNumber}",
                HtmlBody = GenerateReceiptEmailHtml(receipt),
                AttachmentData = pdfData,
                AttachmentFileName = $"Receipt_{receipt.ReceiptNumber}.pdf"
            };

            // Send email with retry logic
            return await SendEmailWithRetryAsync(emailMessage, 3);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send receipt email for Receipt ID: {ReceiptId}", receipt?.Id);
            return new EmailResponseDto
            {
                Success = false,
                ErrorMessage = $"Failed to send email: {ex.Message}"
            };
        }
    }

    private async Task<EmailResponseDto> SendEmailWithRetryAsync(EmailMessageDto emailMessage, int maxRetries)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt < maxRetries)
        {
            attempt++;
            try
            {
                _logger.LogInformation("Sending email attempt {Attempt} of {MaxRetries} to {Email}",
                    attempt, maxRetries, emailMessage.ToEmail);

                var messageId = SendEmailAsync(emailMessage);

                _logger.LogInformation("Email sent successfully to {Email}, MessageId: {MessageId}",
                    emailMessage.ToEmail, messageId);

                return new EmailResponseDto
                {
                    Success = true,
                    MessageId = messageId
                };
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Email send attempt {Attempt} failed. Will retry...", attempt);

                if (attempt < maxRetries)
                {
                    // Exponential backoff: 2s, 4s, 8s
                    var delaySeconds = Math.Pow(2, attempt);
                    await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }
            }
        }

        _logger.LogError(lastException, "Failed to send email after {MaxRetries} attempts", maxRetries);
        return new EmailResponseDto
        {
            Success = false,
            ErrorMessage = $"Failed after {maxRetries} attempts: {lastException?.Message}"
        };
    }

    private string SendEmailAsync(EmailMessageDto emailMessage)
    {
        try
        {
            // Configure Brevo API
            Configuration.Default.ApiKey.Clear();
            Configuration.Default.ApiKey.Add("api-key", _emailSettings.ApiKey);

            var apiInstance = new TransactionalEmailsApi();

            // Create sender
            var sender = new SendSmtpEmailSender(
                _emailSettings.SenderName,
                _emailSettings.SenderEmail
            );

            // Create recipient
            var to = new List<SendSmtpEmailTo>
        {
            new SendSmtpEmailTo(emailMessage.ToEmail, emailMessage.ToName)
        };

            // Prepare email
            var sendSmtpEmail = new SendSmtpEmail(
                sender: sender,
                to: to,
                subject: emailMessage.Subject,
                htmlContent: emailMessage.HtmlBody
            );

            // Add attachment if provided
            if (emailMessage.AttachmentData != null && !string.IsNullOrWhiteSpace(emailMessage.AttachmentFileName))
            {
                var attachment = new SendSmtpEmailAttachment
                {
                    Content = emailMessage.AttachmentData,
                    Name = emailMessage.AttachmentFileName
                };
                sendSmtpEmail.Attachment = new List<SendSmtpEmailAttachment> { attachment };
            }

            // Send email
            _logger.LogInformation("Sending email via Brevo API to {Email}", emailMessage.ToEmail);
            var result = apiInstance.SendTransacEmail(sendSmtpEmail);

            _logger.LogInformation("Email sent successfully via Brevo API. MessageId: {MessageId}", result.MessageId);
            return result.MessageId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Brevo API send failed");
            throw;
        }
    }

    private string GenerateInvoiceEmailHtml(InvoiceDTO invoice)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #2563eb; color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
                    .content {{ background-color: #f9fafb; padding: 30px; border: 1px solid #e5e7eb; }}
                    .invoice-details {{ background-color: white; padding: 20px; border-radius: 8px; margin: 20px 0; }}
                    .detail-row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #e5e7eb; }}
                    .detail-label {{ font-weight: bold; color: #6b7280; }}
                    .detail-value {{ color: #111827; }}
                    .amount {{ font-size: 24px; font-weight: bold; color: #2563eb; }}
                    .footer {{ background-color: #f3f4f6; padding: 20px; text-align: center; font-size: 12px; color: #6b7280; border-radius: 0 0 8px 8px; }}
                    .button {{ display: inline-block; background-color: #2563eb; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
                </style>
            </head>
            <body>
                <div class='header'>
                    <h1>Τιμολόγιο από Invoqs</h1>
                </div>
                <div class='content'>
                    <p>Αγαπητέ/ή {invoice.CustomerName},</p>
                    <p>Σας ευχαριστούμε για την συνεργασία σας. Παρακαλώ βρείτε το τιμολόγιό σας συνημμένο σε αυτό το email.</p>
                    
                    <div class='invoice-details'>
                        <div class='detail-row'>
                            <span class='detail-label'>Αριθμός Τιμολογίου:</span>
                            <span class='detail-value'>{invoice.InvoiceNumber}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Ημερομηνία Τιμολογίου:</span>
                            <span class='detail-value'>{invoice.CreatedDate:MMMM dd, yyyy}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Ημερομηνία Λήξης:</span>
                            <span class='detail-value'>{(invoice.DueDate.HasValue ? invoice.DueDate.Value.ToString("MMMM dd, yyyy") : "N/A")}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Συνολικό Ποσό:</span>
                            <span class='detail-value amount'>€{invoice.Total:N2}</span>
                        </div>
                    </div>
                    
                    <p>Εάν έχετε οποιεσδήποτε ερωτήσεις σχετικά με αυτό το τιμολόγιο, μη διστάσετε να επικοινωνήσετε μαζί μας.</p>
                    <p>Με εκτίμηση,<br>Η Ομάδα Invoqs</p>
                </div>
                <div class='footer'>
                    <p>Αυτό είναι ένα αυτοματοποιημένο email. Παρακαλώ μην απαντήσετε απευθείας σε αυτό το μήνυμα.</p>
                    <p>&copy; {DateTime.Now.Year} Invoqs. Με επιφύλαξη παντός δικαιώματος.</p>
                </div>
            </body>
            </html>";
    }

    private string GenerateReceiptEmailHtml(ReceiptDTO receipt)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #10b981; color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
                    .content {{ background-color: #f9fafb; padding: 30px; border: 1px solid #e5e7eb; }}
                    .receipt-details {{ background-color: white; padding: 20px; border-radius: 8px; margin: 20px 0; }}
                    .detail-row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #e5e7eb; }}
                    .detail-label {{ font-weight: bold; color: #6b7280; }}
                    .detail-value {{ color: #111827; }}
                    .amount {{ font-size: 24px; font-weight: bold; color: #10b981; }}
                    .footer {{ background-color: #f3f4f6; padding: 20px; text-align: center; font-size: 12px; color: #6b7280; border-radius: 0 0 8px 8px; }}
                    .checkmark {{ font-size: 48px; color: #10b981; }}
                </style>
            </head>
            <body>
                <div class='header'>
                    <div class='checkmark'>✓</div>
                    <h1>Πληρωμή Ληφθείσα</h1>
                </div>
                <div class='content'>
                    <p>Αγαπητέ/ή {receipt.CustomerName},</p>
                    <p>Σας ευχαριστούμε για την πληρωμή σας. Αυτό το email επιβεβαιώνει ότι έχουμε λάβει την πληρωμή σας.</p>
                    
                    <div class='receipt-details'>
                        <div class='detail-row'>
                            <span class='detail-label'>Αριθμός Απόδειξης:</span>
                            <span class='detail-value'>{receipt.ReceiptNumber}</span>
                        </div>
                        <div class='detail-row'>
                            <span class='detail-label'>Ποσό που Πληρώθηκε:</span>
                            <span class='detail-value amount'>€{receipt.TotalAmount:N2}</span>
                        </div>
                    </div>
                    
                    <p>Η απόδειξη πληρωμής σας είναι συνημμένη σε αυτό το email για τα αρχεία σας.</p>
                    <p>Σας ευχαριστούμε για την συνεργασία σας!</p>
                    <p>Με εκτίμηση,<br>Η Ομάδα Invoqs</p>
                </div>
                <div class='footer'>
                    <p>This is an automated email. Please do not reply directly to this message.</p>
                    <p>&copy; {DateTime.Now.Year} Invoqs. All rights reserved.</p>
                </div>
            </body>
            </html>";
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}