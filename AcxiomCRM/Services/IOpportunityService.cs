using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services;

public interface IOpportunityService
{
    Task<OpportunityIndexViewModel> GetOpportunitiesAsync(string currentUserId, IList<string> roles, string? searchTerm, string? stageFilter, string? statusFilter, int? customerFilter, int page, int pageSize);
    Task<Opportunity?> GetOpportunityByIdAsync(int id, string currentUserId, IList<string> roles);
    Task<OpportunityDetailsViewModel?> GetOpportunityDetailsAsync(int id, string currentUserId, IList<string> roles);
    Task<(bool Success, string? Error, Opportunity? Opportunity)> CreateOpportunityAsync(OpportunityCreateEditViewModel model, string currentUserId, IList<string> roles, string? ipAddress);
    Task<(bool Success, string? Error)> UpdateOpportunityAsync(OpportunityCreateEditViewModel model, string currentUserId, IList<string> roles, string? ipAddress);
    Task<(bool Success, string? Error)> DeleteOpportunityAsync(int id, string currentUserId, IList<string> roles, string? ipAddress);
    Task<(bool Success, string? Error)> UpdateStageAsync(int id, string newStage, string currentUserId, IList<string> roles, string? ipAddress);

    // API methods
    Task<List<OpportunityDto>> GetAllOpportunityDtosAsync(string currentUserId, IList<string> roles, string? search);
    Task<(bool Success, string? Error, OpportunityDto? Opportunity)> CreateFromDtoAsync(CreateOpportunityDto dto, string currentUserId, IList<string> roles, string? ipAddress);
}
