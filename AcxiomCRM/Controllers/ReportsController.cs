using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    private string GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private IList<string> GetUserRoles() =>
        User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

    private static bool IsAdminOrManager(IList<string> roles) =>
        roles.Contains("Admin") || roles.Contains("Manager");

    [HttpGet]
    public async Task<IActionResult> Index(string tab = "Pipeline")
    {
        var currentUserId = GetCurrentUserId();
        var roles = GetUserRoles();
        bool isAdminOrMgr = IsAdminOrManager(roles);

        var oppQuery = _context.Opportunities.Include(o => o.Customer).AsNoTracking().AsQueryable();
        var leadQuery = _context.Leads.AsNoTracking().AsQueryable();
        var custQuery = _context.Customers.AsNoTracking().AsQueryable();
        var followQuery = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).AsNoTracking().AsQueryable();

        if (!isAdminOrMgr)
        {
            oppQuery = oppQuery.Where(o => o.AssignedTo == currentUserId);
            leadQuery = leadQuery.Where(l => l.AssignedTo == currentUserId);
            custQuery = custQuery.Where(c => c.CreatedBy == currentUserId);
            followQuery = followQuery.Where(f => f.AssignedTo == currentUserId);
        }

        var opportunities = await oppQuery.ToListAsync();
        var leads = await leadQuery.ToListAsync();
        var customers = await custQuery.ToListAsync();
        var followUps = await followQuery.ToListAsync();

        var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
        var stageSummaries = stages.Select(s =>
        {
            var stageOpps = opportunities.Where(o => string.Equals(o.Stage, s, StringComparison.OrdinalIgnoreCase)).ToList();
            return new StagePipelineSummary
            {
                Stage = s,
                Count = stageOpps.Count,
                TotalAmount = stageOpps.Sum(o => o.Amount),
                WeightedAmount = stageOpps.Sum(o => o.WeightedValue)
            };
        }).ToList();

        var openOpps = opportunities.Where(o => o.Status == "Active").ToList();

        var model = new ReportsDashboardViewModel
        {
            ActiveTab = tab,
            StagePipelineSummaries = stageSummaries,
            TotalPipelineValue = openOpps.Sum(o => o.Amount),
            TotalWeightedPipelineValue = openOpps.Sum(o => o.WeightedValue),
            TotalLeadsCount = leads.Count,
            ConvertedLeadsCount = leads.Count(l => l.Status == "Converted"),
            WonDealsCount = opportunities.Count(o => o.Stage == "Won" || o.Status == "Won"),
            LostDealsCount = opportunities.Count(o => o.Stage == "Lost" || o.Status == "Lost"),
            WonAmount = opportunities.Where(o => o.Stage == "Won" || o.Status == "Won").Sum(o => o.Amount),
            Customers = customers,
            Leads = leads,
            Opportunities = opportunities,
            FollowUps = followUps
        };

        return View(model);
    }
}
