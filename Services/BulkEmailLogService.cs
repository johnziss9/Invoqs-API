using Invoqs.API.Data;
using Invoqs.API.DTOs;
using Invoqs.API.Interfaces;
using Invoqs.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Invoqs.API.Services;

public class BulkEmailLogService : IBulkEmailLogService
{
    private readonly InvoqsDbContext _context;
    private readonly ILogger<BulkEmailLogService> _logger;

    public BulkEmailLogService(InvoqsDbContext context, ILogger<BulkEmailLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BulkEmailLog> SaveLogAsync(BulkEmailLog log)
    {
        _context.BulkEmailLogs.Add(log);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Saved bulk email log ID {LogId} for user {UserId}", log.Id, log.SentByUserId);
        return log;
    }

    public async Task<IEnumerable<BulkEmailLogDTO>> GetAllLogsAsync()
    {
        return await _context.BulkEmailLogs
            .Include(l => l.SentByUser)
            .OrderByDescending(l => l.SentDate)
            .Select(l => new BulkEmailLogDTO
            {
                Id = l.Id,
                SentDate = l.SentDate,
                Subject = l.Subject,
                Body = l.Body,
                Language = l.Language,
                SentByUserName = l.SentByUser.FirstName + " " + l.SentByUser.LastName,
                TotalRecipients = l.TotalRecipients,
                SentCount = l.SentCount,
                FailedCount = l.FailedCount
            })
            .ToListAsync();
    }

    public async Task<BulkEmailLogDTO?> GetLogByIdAsync(int id)
    {
        var log = await _context.BulkEmailLogs
            .Include(l => l.SentByUser)
            .Include(l => l.Recipients)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (log == null) return null;

        return new BulkEmailLogDTO
        {
            Id = log.Id,
            SentDate = log.SentDate,
            Subject = log.Subject,
            Body = log.Body,
            Language = log.Language,
            SentByUserName = log.SentByUser.FirstName + " " + log.SentByUser.LastName,
            TotalRecipients = log.TotalRecipients,
            SentCount = log.SentCount,
            FailedCount = log.FailedCount,
            Recipients = log.Recipients.Select(r => new BulkEmailRecipientDTO
            {
                CustomerName = r.CustomerName,
                Email = r.Email,
                Success = r.Success,
                Error = r.Error
            }).ToList()
        };
    }
}
