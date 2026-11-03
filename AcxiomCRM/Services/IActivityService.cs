using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services;

public interface IActivityService
{
    Task<ActivityIndexViewModel> GetActivitiesAsync(string currentUserId, IList<string> roles, string? searchTerm, string? typeFilter, string? statusFilter, int page, int pageSize);
    Task<Activity?> GetActivityByIdAsync(int id, string currentUserId, IList<string> roles);
    Task<(bool Success, string? Error, Activity? Activity)> CreateActivityAsync(ActivityCreateEditViewModel model, string currentUserId, IList<string> roles, string? ipAddress);
    Task<(bool Success, string? Error)> UpdateActivityAsync(ActivityCreateEditViewModel model, string currentUserId, IList<string> roles, string? ipAddress);
    Task<(bool Success, string? Error)> DeleteActivityAsync(int id, string currentUserId, IList<string> roles, string? ipAddress);
}
