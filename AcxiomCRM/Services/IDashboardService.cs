using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardDataAsync(string currentUserId, IList<string> roles, string dateFilter);
}
