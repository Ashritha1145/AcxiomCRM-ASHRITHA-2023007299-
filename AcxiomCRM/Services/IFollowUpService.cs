using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services;

public interface IFollowUpService
{
    Task<FollowUpIndexViewModel> GetFollowUpsAsync(string currentUserId, IList<string> roles, string? statusFilter, string? typeFilter, string? dateFilter, int page, int pageSize);
    Task<FollowUp?> GetFollowUpByIdAsync(int id, string currentUserId, IList<string> roles);
    Task<(bool Success, string? Error, FollowUp? FollowUp)> CreateFollowUpAsync(FollowUpCreateEditViewModel model, string currentUserId, IList<string> roles, string? ipAddress);
    Task<(bool Success, string? Error)> UpdateFollowUpAsync(FollowUpCreateEditViewModel model, string currentUserId, IList<string> roles, string? ipAddress);
    Task<(bool Success, string? Error)> CompleteFollowUpAsync(int id, string currentUserId, IList<string> roles, string? ipAddress);
    Task<(bool Success, string? Error)> DeleteFollowUpAsync(int id, string currentUserId, IList<string> roles, string? ipAddress);

    // API
    Task<List<FollowUpDto>> GetAllFollowUpDtosAsync(string currentUserId, IList<string> roles);
    Task<(bool Success, string? Error, FollowUpDto? FollowUp)> CreateFromDtoAsync(CreateFollowUpDto dto, string currentUserId, IList<string> roles, string? ipAddress);
}
