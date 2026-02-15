namespace Invoqs.API.Models;

public enum InvoiceStatus
{
    Draft = 0,
    Sent = 1,
    Delivered = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Overdue = 5,
    Cancelled = 6
}