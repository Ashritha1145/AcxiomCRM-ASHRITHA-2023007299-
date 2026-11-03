using AcxiomCRM.Models;

namespace AcxiomCRM.Services;

public interface IAuditService
{
    Task LogAsync(string? userId, string action, string entityName, string? recordId, object? oldValue, object? newValue, string? ipAddress, string result = "Success");
    Task<(List<AuditLog> Items, int TotalCount)> GetAuditLogsAsync(string? userId, string? entityName, string? action, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 15);
}
