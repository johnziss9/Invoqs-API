namespace Invoqs.API.Helpers;

public static class StatementNumberGenerator
{
    public static string Generate(int sequenceNumber, DateTime? date = null)
    {
        var year = (date ?? DateTime.UtcNow).Year;
        return $"STMT-{year}-{sequenceNumber:D4}";
    }

    public static int GetNextSequenceNumber(string? lastStatementNumber)
    {
        if (string.IsNullOrWhiteSpace(lastStatementNumber))
            return 1;

        var parts = lastStatementNumber.Split('-');
        if (parts.Length == 3 && int.TryParse(parts[2], out int sequence))
        {
            return sequence + 1;
        }

        return 1;
    }

    public static int GetSequenceFromStatementNumber(string? statementNumber)
    {
        if (string.IsNullOrWhiteSpace(statementNumber))
            return 0;

        var parts = statementNumber.Split('-');
        if (parts.Length == 3 && int.TryParse(parts[2], out int sequence))
        {
            return sequence;
        }

        return 0;
    }

    public static int? GetYearFromStatementNumber(string? statementNumber)
    {
        if (string.IsNullOrWhiteSpace(statementNumber))
            return null;

        var parts = statementNumber.Split('-');
        if (parts.Length == 3 && int.TryParse(parts[1], out int year))
        {
            return year;
        }

        return null;
    }
}
