namespace Invoqs.API.Helpers;

public static class ReceiptNumberGenerator
{
    public static string Generate(int sequenceNumber, DateTime? date = null)
    {
        var year = (date ?? DateTime.UtcNow).Year;
        return $"REC-{year}-{sequenceNumber:D4}";
    }

    public static int GetNextSequenceNumber(string? lastReceiptNumber)
    {
        if (string.IsNullOrWhiteSpace(lastReceiptNumber))
            return 1;

        var parts = lastReceiptNumber.Split('-');
        if (parts.Length == 3 && int.TryParse(parts[2], out int sequence))
        {
            return sequence + 1;
        }

        return 1;
    }

    public static int GetSequenceFromReceiptNumber(string? receiptNumber)
    {
        if (string.IsNullOrWhiteSpace(receiptNumber))
            return 0;

        var parts = receiptNumber.Split('-');
        if (parts.Length == 3 && int.TryParse(parts[2], out int sequence))
        {
            return sequence;
        }

        return 0;
    }
    
    // Extract year from receipt number
    public static int? GetYearFromReceiptNumber(string? receiptNumber)
    {
        if (string.IsNullOrWhiteSpace(receiptNumber))
            return null;

        var parts = receiptNumber.Split('-');
        if (parts.Length == 3 && int.TryParse(parts[1], out int year))
        {
            return year;
        }

        return null;
    }
}