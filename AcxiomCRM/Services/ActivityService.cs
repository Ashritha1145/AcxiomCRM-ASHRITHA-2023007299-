using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public class ActivityService : IActivityService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public ActivityService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    private static bool IsAdminOrManager(IList<string> roles) =>
        roles.Contains("Admin") || roles.Contains("Manager");

    public async Task<ActivityIndexViewModel> GetActivitiesAsync(string currentUserId, IList<string> roles, string? searchTerm, string? typeFilter, string? statusFilter, int page, int pageSize)
    {
        var query = _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .AsNoTracking()
            .AsQueryable();

        if (!IsAdminOrManager(roles))
        {
            query = query.Where(a => a.AssignedTo == currentUserId);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(a =>
                a.Subject.ToLower().Contains(term) ||
                (a.Description != null && a.Description.ToLower().Contains(term)) ||
                (a.Customer != null && a.Customer.CustomerName.ToLower().Contains(term)) ||
                (a.Lead != null && a.Lead.LeadName.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(typeFilter))
        {
            query = query.Where(a => a.ActivityType == typeFilter);
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(a => a.Status == statusFilter);
        }

        int totalCount = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page < 1) page = 1;

        var items = await query.OrderByDescending(a => a.ActivityDate)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

        return new ActivityIndexViewModel
        {
            SearchTerm = searchTerm,
            TypeFilter = typeFilter,
            StatusFilter = statusFilter,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalCount = totalCount,
            Activities = items
        };
    }

    public async Task<Activity?> GetActivityByIdAsync(int id, string currentUserId, IList<string> roles)
    {
        var item = await _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .FirstOrDefaultAsync(a => a.ActivityId == id);

        if (item == null) return null;

        if (!IsAdminOrManager(roles) && item.AssignedTo != currentUserId)
            return null;

        return item;
    }

    public async Task<(bool Success, string? Error, Activity? Activity)> CreateActivityAsync(ActivityCreateEditViewModel model, string currentUserId, IList<string> roles, string? ipAddress)
    {
        string assignedTo = model.AssignedTo;
        if (!IsAdminOrManager(roles) || string.IsNullOrWhiteSpace(assignedTo))
        {
            assignedTo = currentUserId;
        }

        var activity = new Activity
        {
            ActivityType = model.ActivityType,
            Subject = model.Subject.Trim(),
            Description = model.Description?.Trim(),
            ActivityDate = model.ActivityDate,
            CustomerId = model.CustomerId > 0 ? model.CustomerId : null,
            LeadId = model.LeadId > 0 ? model.LeadId : null,
            AssignedTo = assignedTo,
            Status = model.Status
        };

        _context.Activities.Add(activity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            "Create",
            "Activity",
            activity.ActivityId.ToString(),
            null,
            new { activity.ActivityType, activity.Subject, activity.Status, activity.AssignedTo },
            ipAddress,
            "Success");

        return (true, null, activity);
    }

    public async Task<(bool Success, string? Error)> UpdateActivityAsync(ActivityCreateEditViewModel model, string currentUserId, IList<string> roles, string? ipAddress)
    {
        var activity = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == model.ActivityId);
        if (activity == null)
            return (false, "Activity not found.");

        if (!IsAdminOrManager(roles) && activity.AssignedTo != currentUserId)
            return (false, "You do not have permission to modify this activity.");

        var oldState = new { activity.ActivityType, activity.Subject, activity.Status, activity.ActivityDate };

        activity.ActivityType = model.ActivityType;
        activity.Subject = model.Subject.Trim();
        activity.Description = model.Description?.Trim();
        activity.ActivityDate = model.ActivityDate;
        activity.CustomerId = model.CustomerId > 0 ? model.CustomerId : null;
        activity.LeadId = model.LeadId > 0 ? model.LeadId : null;
        activity.Status = model.Status;

        if (IsAdminOrManager(roles) && !string.IsNullOrWhiteSpace(model.AssignedTo))
        {
            activity.AssignedTo = model.AssignedTo;
        }

        await _context.SaveChangesAsync();

        var newState = new { activity.ActivityType, activity.Subject, activity.Status, activity.ActivityDate };

        await _auditService.LogAsync(
            currentUserId,
            "Update",
            "Activity",
            activity.ActivityId.ToString(),
            oldState,
            newState,
            ipAddress,
            "Success");

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteActivityAsync(int id, string currentUserId, IList<string> roles, string? ipAddress)
    {
        var activity = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == id);
        if (activity == null) return (false, "Activity not found.");

        if (!IsAdminOrManager(roles) && activity.AssignedTo != currentUserId)
            return (false, "You do not have permission to delete this activity.");

        _context.Activities.Remove(activity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            "Delete",
            "Activity",
            id.ToString(),
            new { activity.ActivityId, activity.Subject },
            null,
            ipAddress,
            "Success");

        return (true, null);
    }
}
