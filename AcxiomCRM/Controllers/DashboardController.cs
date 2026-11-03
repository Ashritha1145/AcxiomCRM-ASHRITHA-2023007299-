using System.Security.Claims;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    private string GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private IList<string> GetUserRoles() =>
        User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

    [HttpGet]
    public async Task<IActionResult> Index(string dateFilter = "All")
    {
        var userId = GetCurrentUserId();
        var roles = GetUserRoles();

        var model = await _dashboardService.GetDashboardDataAsync(userId, roles, dateFilter);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetChartData(string dateFilter = "All")
    {
        var userId = GetCurrentUserId();
        var roles = GetUserRoles();

        var model = await _dashboardService.GetDashboardDataAsync(userId, roles, dateFilter);
        return Json(new
        {
            leadStatusLabels = model.LeadStatusLabels,
            leadStatusCounts = model.LeadStatusCounts,
            pipelineStageLabels = model.PipelineStageLabels,
            pipelineStageValues = model.PipelineStageValues,
            monthlySalesLabels = model.MonthlySalesLabels,
            monthlySalesValues = model.MonthlySalesValues
        });
    }
}
