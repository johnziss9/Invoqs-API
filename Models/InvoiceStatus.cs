namespace Invoqs.API.Models;

public enum InvoiceStatus
{
    Draft = 0,
    Sent = 1,
    Delivered = 2,
    Paid = 3,
    Overdue = 4,
    Cancelled = 5
}