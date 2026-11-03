using System.Text.Json;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string? userId, string action, string entityName, string? recordId, object? oldValue, object? newValue, string? ipAddress, string result = "Success")
    {
        try
        {
            string? oldJson = oldValue != null ? (oldValue is string s1 ? s1 : JsonSerializer.Serialize(oldValue, _jsonOptions)) : null;
            string? newJson = newValue != null ? (newValue is string s2 ? s2 : JsonSerializer.Serialize(newValue, _jsonOptions)) : null;

            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                RecordId = recordId,
                OldValue = oldJson,
                NewValue = newJson,
                CreatedDate = DateTime.UtcNow,
                IpAddress = ipAddress,
                Result = result
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Logging failure should never block business transactions
        }
    }

    public async Task<(List<AuditLog> Items, int TotalCount)> GetAuditLogsAsync(string? userId, string? entityName, string? action, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 15)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(a => a.UserId == userId);

        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(a => a.EntityName.ToLower().Contains(entityName.ToLower()));

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action.ToLower().Contains(action.ToLower()));

        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedDate >= fromDate.Value.Date);

        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(a => a.CreatedDate <= endOfDay);
        }

        int totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(a => a.CreatedDate)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

        return (items, totalCount);
    }
}
