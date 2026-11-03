using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services;

public interface ILeadService
{
    Task<LeadIndexViewModel> GetLeadsAsync(string currentUserId, IList<string> roles, string? searchTerm, string? statusFilter, string? assignedToFilter, int page, int pageSize);
    Task<Lead?> GetLeadByIdAsync(int id, string currentUserId, IList<string> roles);
    Task<LeadDetailsViewModel?> GetLeadDetailsAsync(int id, string currentUserId, IList<string> roles);
    Task<(bool Success, string? Error, Lead? Lead)> CreateLeadAsync(LeadCreateEditViewModel model, string currentUserId, IList<string> roles, string? ipAddress);
    Task<(bool Success, string? Error)> UpdateLeadAsync(LeadCreateEditViewModel model, string currentUserId, IList<string> roles, string? ipAddress);
    Task<(bool Success, string? Error)> DeleteLeadAsync(int id, string currentUserId, IList<string> roles, string? ipAddress);
    Task<(bool Success, string? Error, int? CustomerId)> ConvertLeadAsync(ConvertLeadViewModel model, string currentUserId, IList<string> roles, string? ipAddress);

    // API methods
    Task<List<LeadDto>> GetAllLeadDtosAsync(string currentUserId, IList<string> roles, string? search);
    Task<(bool Success, string? Error, LeadDto? Lead)> CreateFromDtoAsync(CreateLeadDto dto, string currentUserId, IList<string> roles, string? ipAddress);
}
