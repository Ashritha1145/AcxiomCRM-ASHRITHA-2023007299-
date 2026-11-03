using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public class FollowUpService : IFollowUpService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public FollowUpService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    private static bool IsAdminOrManager(IList<string> roles) =>
        roles.Contains("Admin") || roles.Contains("Manager");

    public async Task<FollowUpIndexViewModel> GetFollowUpsAsync(string currentUserId, IList<string> roles, string? statusFilter, string? typeFilter, string? dateFilter, int page, int pageSize)
    {
        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .AsNoTracking()
            .AsQueryable();

        if (!IsAdminOrManager(roles))
        {
            query = query.Where(f => f.AssignedTo == currentUserId);
        }

        var allScoped = await query.ToListAsync();
        var today = DateTime.UtcNow.Date;

        int plannedCount = allScoped.Count(f => f.Status == "Planned");
        int completedCount = allScoped.Count(f => f.Status == "Completed");
        int overdueCount = allScoped.Count(f => f.Status == "Planned" && f.FollowUpDate.Date < today);

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(f => f.Status == statusFilter);
        }

        if (!string.IsNullOrWhiteSpace(typeFilter))
        {
            query = query.Where(f => f.FollowUpType == typeFilter);
        }

        if (!string.IsNullOrWhiteSpace(dateFilter))
        {
            if (dateFilter == "Today")
                query = query.Where(f => f.FollowUpDate.Date == today);
            else if (dateFilter == "Upcoming")
                query = query.Where(f => f.FollowUpDate.Date >= today && f.Status == "Planned");
            else if (dateFilter == "Overdue")
                query = query.Where(f => f.FollowUpDate.Date < today && f.Status == "Planned");
        }

        int totalCount = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page < 1) page = 1;

        var items = await query.OrderBy(f => f.FollowUpDate)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

        return new FollowUpIndexViewModel
        {
            StatusFilter = statusFilter,
            TypeFilter = typeFilter,
            DateFilter = dateFilter,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalCount = totalCount,
            FollowUps = items,
            PlannedCount = plannedCount,
            CompletedCount = completedCount,
            OverdueCount = overdueCount
        };
    }

    public async Task<FollowUp?> GetFollowUpByIdAsync(int id, string currentUserId, IList<string> roles)
    {
        var item = await _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .FirstOrDefaultAsync(f => f.FollowUpId == id);

        if (item == null) return null;

        if (!IsAdminOrManager(roles) && item.AssignedTo != currentUserId)
            return null;

        return item;
    }

    public async Task<(bool Success, string? Error, FollowUp? FollowUp)> CreateFollowUpAsync(FollowUpCreateEditViewModel model, string currentUserId, IList<string> roles, string? ipAddress)
    {
        // Business Rule: Follow-up date cannot be earlier than today for a new/planned follow-up
        if (model.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            return (false, "Follow-up date cannot be earlier than today.", null);
        }

        string assignedTo = model.AssignedTo;
        if (!IsAdminOrManager(roles) || string.IsNullOrWhiteSpace(assignedTo))
        {
            assignedTo = currentUserId;
        }

        var followUp = new FollowUp
        {
            CustomerId = model.CustomerId > 0 ? model.CustomerId : null,
            LeadId = model.LeadId > 0 ? model.LeadId : null,
            FollowUpDate = model.FollowUpDate,
            FollowUpType = model.FollowUpType,
            Remarks = model.Remarks?.Trim() ?? string.Empty,
            Status = model.Status,
            AssignedTo = assignedTo
        };

        _context.FollowUps.Add(followUp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            "Create",
            "FollowUp",
            followUp.FollowUpId.ToString(),
            null,
            new { followUp.FollowUpDate, followUp.FollowUpType, followUp.Status, followUp.AssignedTo },
            ipAddress,
            "Success");

        return (true, null, followUp);
    }

    public async Task<(bool Success, string? Error)> UpdateFollowUpAsync(FollowUpCreateEditViewModel model, string currentUserId, IList<string> roles, string? ipAddress)
    {
        var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == model.FollowUpId);
        if (followUp == null)
            return (false, "Follow-up not found.");

        if (!IsAdminOrManager(roles) && followUp.AssignedTo != currentUserId)
            return (false, "You do not have permission to modify this follow-up.");

        // If status is planned and date changed to earlier than today
        if (model.Status == "Planned" && model.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            return (false, "Follow-up date cannot be earlier than today.");
        }

        var oldState = new { followUp.FollowUpDate, followUp.FollowUpType, followUp.Status, followUp.Remarks };

        followUp.CustomerId = model.CustomerId > 0 ? model.CustomerId : null;
        followUp.LeadId = model.LeadId > 0 ? model.LeadId : null;
        followUp.FollowUpDate = model.FollowUpDate;
        followUp.FollowUpType = model.FollowUpType;
        followUp.Remarks = model.Remarks?.Trim() ?? string.Empty;
        followUp.Status = model.Status;

        if (IsAdminOrManager(roles) && !string.IsNullOrWhiteSpace(model.AssignedTo))
        {
            followUp.AssignedTo = model.AssignedTo;
        }

        await _context.SaveChangesAsync();

        var newState = new { followUp.FollowUpDate, followUp.FollowUpType, followUp.Status, followUp.Remarks };

        await _auditService.LogAsync(
            currentUserId,
            "Update",
            "FollowUp",
            followUp.FollowUpId.ToString(),
            oldState,
            newState,
            ipAddress,
            "Success");

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> CompleteFollowUpAsync(int id, string currentUserId, IList<string> roles, string? ipAddress)
    {
        var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == id);
        if (followUp == null) return (false, "Follow-up not found.");

        if (!IsAdminOrManager(roles) && followUp.AssignedTo != currentUserId)
            return (false, "You do not have permission to modify this follow-up.");

        var oldStatus = followUp.Status;
        followUp.Status = "Completed";
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            "Complete FollowUp",
            "FollowUp",
            followUp.FollowUpId.ToString(),
            new { Status = oldStatus },
            new { Status = "Completed" },
            ipAddress,
            "Success");

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteFollowUpAsync(int id, string currentUserId, IList<string> roles, string? ipAddress)
    {
        var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == id);
        if (followUp == null) return (false, "Follow-up not found.");

        if (!IsAdminOrManager(roles) && followUp.AssignedTo != currentUserId)
            return (false, "You do not have permission to delete this follow-up.");

        _context.FollowUps.Remove(followUp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            "Delete",
            "FollowUp",
            id.ToString(),
            new { followUp.FollowUpId, followUp.FollowUpType, followUp.Status },
            null,
            ipAddress,
            "Success");

        return (true, null);
    }

    public async Task<List<FollowUpDto>> GetAllFollowUpDtosAsync(string currentUserId, IList<string> roles)
    {
        var query = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).AsNoTracking().AsQueryable();
        if (!IsAdminOrManager(roles))
        {
            query = query.Where(f => f.AssignedTo == currentUserId);
        }

        return await query.Select(f => new FollowUpDto
        {
            FollowUpId = f.FollowUpId,
            CustomerId = f.CustomerId,
            CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
            LeadId = f.LeadId,
            LeadName = f.Lead != null ? f.Lead.LeadName : null,
            FollowUpDate = f.FollowUpDate,
            FollowUpType = f.FollowUpType,
            Remarks = f.Remarks,
            Status = f.Status,
            AssignedTo = f.AssignedTo
        }).ToListAsync();
    }

    public async Task<(bool Success, string? Error, FollowUpDto? FollowUp)> CreateFromDtoAsync(CreateFollowUpDto dto, string currentUserId, IList<string> roles, string? ipAddress)
    {
        var vm = new FollowUpCreateEditViewModel
        {
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            FollowUpDate = dto.FollowUpDate,
            FollowUpType = dto.FollowUpType,
            Remarks = dto.Remarks,
            Status = dto.Status,
            AssignedTo = dto.AssignedTo ?? currentUserId
        };

        var (success, error, followUp) = await CreateFollowUpAsync(vm, currentUserId, roles, ipAddress);
        if (!success || followUp == null)
            return (false, error, null);

        return (true, null, new FollowUpDto
        {
            FollowUpId = followUp.FollowUpId,
            CustomerId = followUp.CustomerId,
            LeadId = followUp.LeadId,
            FollowUpDate = followUp.FollowUpDate,
            FollowUpType = followUp.FollowUpType,
            Remarks = followUp.Remarks,
            Status = followUp.Status,
            AssignedTo = followUp.AssignedTo
        });
    }
}
